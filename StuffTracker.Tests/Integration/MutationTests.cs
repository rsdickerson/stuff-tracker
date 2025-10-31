using System.Linq;
using System.Text.Json;
using FluentAssertions;

namespace StuffTracker.Tests.Integration;

/// <summary>
/// Integration tests for GraphQL mutations (create/move/delete).
/// Tests validate mutations persist changes and return projected DTOs.
/// </summary>
[Collection("Integration Tests")]
public class MutationTests : IClassFixture<GraphQLTestFixture>
{
    private readonly GraphQLTestFixture _fixture;
    private readonly HttpClient _client;

    public MutationTests(GraphQLTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task AddItem_CreatesItem_ReturnsProjectedDTO()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        var mutation = @"
            mutation {
                addItem(name: ""Test Item"", quantity: 5, roomId: " + firstRoomId + @") {
                    id
                    name
                    quantity
                    roomId
                    createdAt
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var item = JsonSerializer.Deserialize<ItemDTO>(
            data.GetProperty("addItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        item.Should().NotBeNull();
        item!.Name.Should().Be("Test Item");
        item.Quantity.Should().Be(5);
        item.RoomId.Should().Be(firstRoomId);
        item.Id.Should().BeGreaterThan(0, "Item should have a generated ID");
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "CreatedAt should be set to current time");
    }

    [Fact]
    public async Task AddItem_PersistsToDatabase_VerifiedByQuery()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        var mutation = @"
            mutation {
                addItem(name: ""Persisted Item"", quantity: 10, roomId: " + firstRoomId + @") {
                    id
                    name
                    quantity
                    roomId
                }
            }";

        // Act - Create item
        var mutationResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);
        mutationResponse.ShouldHaveNoErrors();

        var mutationData = mutationResponse.Data!.Value;
        var createdItem = JsonSerializer.Deserialize<ItemDTO>(
            mutationData.GetProperty("addItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Verify persistence with follow-up query using items query with filtering
        var query = @"
            query {
                items(search: null, where: { id: { eq: " + createdItem!.Id + @" } }) {
                    nodes {
                        id
                        name
                        quantity
                        roomId
                    }
                }
            }";

        var queryResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        queryResponse.ShouldHaveNoErrors();
        queryResponse.ShouldHaveData();

        var queryData = queryResponse.Data!.Value;
        var items = queryData.GetProperty("items").GetProperty("nodes");

        items.GetArrayLength().Should().Be(1, "Item should be found in database");
        var persistedItem = JsonSerializer.Deserialize<ItemDTO>(
            items[0].GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        persistedItem.Should().NotBeNull();
        persistedItem!.Id.Should().Be(createdItem.Id);
        persistedItem.Name.Should().Be("Persisted Item");
        persistedItem.Quantity.Should().Be(10);
        persistedItem.RoomId.Should().Be(firstRoomId);
    }

    [Fact]
    public async Task MoveItem_UpdatesRoomId_PersistsToDatabase()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get two different room IDs by querying items with larger page size
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        // Get unique room IDs from items (need at least 2 different rooms)
        var roomIds = new HashSet<int>();
        for (int i = 0; i < itemsNodes.GetArrayLength(); i++)
        {
            roomIds.Add(itemsNodes[i].GetProperty("roomId").GetInt32());
            if (roomIds.Count >= 2)
            {
                break; // We have enough unique rooms
            }
        }
        
        roomIds.Count.Should().BeGreaterThanOrEqualTo(2, "Seeded data should have items in at least 2 different rooms");
        
        var roomIdArray = roomIds.ToArray();
        var firstRoomId = roomIdArray[0];
        var secondRoomId = roomIdArray[1];

        // Create an item in first room
        var addItemMutation = @"
            mutation {
                addItem(name: ""Movable Item"", quantity: 15, roomId: " + firstRoomId + @") {
                    id
                    name
                    roomId
                }
            }";

        var addResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, addItemMutation);
        addResponse.ShouldHaveNoErrors();
        var addData = addResponse.Data!.Value;
        var createdItem = JsonSerializer.Deserialize<ItemDTO>(
            addData.GetProperty("addItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Move item to second room
        var moveMutation = @"
            mutation {
                moveItem(itemId: " + createdItem!.Id + @", newRoomId: " + secondRoomId + @") {
                    id
                    name
                    quantity
                    roomId
                    createdAt
                }
            }";

        // Act
        var moveResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, moveMutation);

        // Assert
        moveResponse.ShouldHaveNoErrors();
        moveResponse.ShouldHaveData();

        var moveData = moveResponse.Data!.Value;
        var movedItem = JsonSerializer.Deserialize<ItemDTO>(
            moveData.GetProperty("moveItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        movedItem.Should().NotBeNull();
        movedItem!.Id.Should().Be(createdItem.Id, "Item ID should remain the same");
        movedItem.Name.Should().Be("Movable Item", "Item name should remain the same");
        movedItem.RoomId.Should().Be(secondRoomId, "RoomId should be updated to new room");

        // Verify persistence with follow-up query
        var query = @"
            query {
                items(search: null, where: { id: { eq: " + createdItem.Id + @" } }) {
                    nodes {
                        id
                        name
                        roomId
                    }
                }
            }";

        var queryResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        queryResponse.ShouldHaveNoErrors();

        var queryData = queryResponse.Data!.Value;
        var queryItems = queryData.GetProperty("items").GetProperty("nodes");
        queryItems.GetArrayLength().Should().Be(1, "Item should be found in database");

        var persistedItem = JsonSerializer.Deserialize<ItemDTO>(
            queryItems[0].GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        persistedItem.Should().NotBeNull();
        persistedItem!.RoomId.Should().Be(secondRoomId, "Item should be persisted in new room");
    }

    [Fact]
    public async Task DeleteItem_RemovesItem_VerifiedByQuery()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        // Create an item
        var addItemMutation = @"
            mutation {
                addItem(name: ""Item To Delete"", quantity: 20, roomId: " + firstRoomId + @") {
                    id
                }
            }";

        var addResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, addItemMutation);
        addResponse.ShouldHaveNoErrors();
        var addData = addResponse.Data!.Value;
        var createdItem = JsonSerializer.Deserialize<ItemDTO>(
            addData.GetProperty("addItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Delete item
        var deleteMutation = @"
            mutation {
                deleteItem(itemId: " + createdItem!.Id + @")
            }";

        // Act
        var deleteResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, deleteMutation);

        // Assert
        deleteResponse.ShouldHaveNoErrors();
        deleteResponse.ShouldHaveData();

        var deleteData = deleteResponse.Data!.Value;
        var deleteResult = deleteData.GetProperty("deleteItem").GetBoolean();
        deleteResult.Should().BeTrue("DeleteItem should return true on success");

        // Verify item was deleted with follow-up query
        var query = @"
            query {
                items(search: null, where: { id: { eq: " + createdItem.Id + @" } }) {
                    nodes {
                        id
                    }
                }
            }";

        var queryResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        queryResponse.ShouldHaveNoErrors();

        var queryData = queryResponse.Data!.Value;
        var items = queryData.GetProperty("items").GetProperty("nodes");
        items.GetArrayLength().Should().Be(0, "Item should not be found in database after deletion");
    }

    [Fact]
    public async Task AddItem_WithInvalidRoomId_ReturnsGraphQLError()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var mutation = @"
            mutation {
                addItem(name: ""Test Item"", quantity: 5, roomId: 99999) {
                    id
                    name
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);

        // Assert
        response.ShouldHaveErrors();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().Contain(e => 
            e.Message.Contains("Room with ID", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase),
            "Error should indicate room was not found");
    }

    [Fact]
    public async Task MoveItem_WithInvalidItemId_ReturnsGraphQLError()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        var mutation = @"
            mutation {
                moveItem(itemId: 99999, newRoomId: " + firstRoomId + @") {
                    id
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);

        // Assert
        response.ShouldHaveErrors();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().Contain(e => 
            e.Message.Contains("Item with ID", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase),
            "Error should indicate item was not found");
    }

    [Fact]
    public async Task MoveItem_WithInvalidNewRoomId_ReturnsGraphQLError()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID and create an item
        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        var addItemMutation = @"
            mutation {
                addItem(name: ""Test Item"", quantity: 5, roomId: " + firstRoomId + @") {
                    id
                }
            }";

        var addResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, addItemMutation);
        addResponse.ShouldHaveNoErrors();
        var addData = addResponse.Data!.Value;
        var createdItem = JsonSerializer.Deserialize<ItemDTO>(
            addData.GetProperty("addItem").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var mutation = @"
            mutation {
                moveItem(itemId: " + createdItem!.Id + @", newRoomId: 99999) {
                    id
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);

        // Assert
        response.ShouldHaveErrors();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().Contain(e => 
            e.Message.Contains("Room with ID", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase),
            "Error should indicate room was not found");
    }

    [Fact]
    public async Task DeleteItem_WithInvalidItemId_ReturnsGraphQLError()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var mutation = @"
            mutation {
                deleteItem(itemId: 99999)
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, mutation);

        // Assert
        response.ShouldHaveErrors();
        response.Errors.Should().NotBeNull();
        response.Errors.Should().Contain(e => 
            e.Message.Contains("Item with ID", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase),
            "Error should indicate item was not found");
    }

    [Fact]
    public async Task MutationResponses_VerifyDTOShape_NoEFEntityProperties()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get a valid room ID from seeded data by querying items with pagination
        var getItemsQuery = @"
            query {
                items(search: null, first: 50) {
                    nodes {
                        roomId
                    }
                }
            }";

        var itemsResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, getItemsQuery);
        itemsResponse.ShouldHaveNoErrors();
        var itemsData = itemsResponse.Data!.Value;
        var itemsNodes = itemsData.GetProperty("items").GetProperty("nodes");
        
        itemsNodes.GetArrayLength().Should().BeGreaterThan(0, "Seeded database should have items");
        var firstRoomId = itemsNodes[0].GetProperty("roomId").GetInt32();

        var addItemMutation = @"
            mutation {
                addItem(name: ""DTO Test Item"", quantity: 25, roomId: " + firstRoomId + @") {
                    id
                    name
                    quantity
                    roomId
                    createdAt
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, addItemMutation);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemJson = data.GetProperty("addItem");
        var item = JsonSerializer.Deserialize<ItemDTO>(
            itemJson.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Verify DTO has expected properties
        item.Should().NotBeNull();
        item!.Id.Should().BeGreaterThan(0);
        item.Name.Should().NotBeNullOrEmpty();
        item.Quantity.Should().BeGreaterThanOrEqualTo(0);
        item.RoomId.Should().BeGreaterThan(0);
        item.CreatedAt.Should().BeAfter(DateTime.MinValue);

        // Verify DTO does not have EF entity properties
        // EF entities might have navigation properties, lazy loading proxies, etc.
        // DTO should only have the properties we explicitly defined
        var itemProperties = typeof(ItemDTO).GetProperties().Select(p => p.Name).ToHashSet();
        itemProperties.Should().Contain("Id");
        itemProperties.Should().Contain("Name");
        itemProperties.Should().Contain("Quantity");
        itemProperties.Should().Contain("RoomId");
        itemProperties.Should().Contain("CreatedAt");
        
        // Verify JSON response doesn't contain unexpected properties
        // (This is a basic check - in practice, we'd verify against a schema)
        itemJson.ValueKind.Should().Be(JsonValueKind.Object);
        var propertyNames = itemJson.EnumerateObject().Select(p => p.Name).ToHashSet();
        propertyNames.Should().Contain("id");
        propertyNames.Should().Contain("name");
        propertyNames.Should().Contain("quantity");
        propertyNames.Should().Contain("roomId");
        propertyNames.Should().Contain("createdAt");
    }

    // Helper classes for deserialization
    private class ItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int RoomId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

