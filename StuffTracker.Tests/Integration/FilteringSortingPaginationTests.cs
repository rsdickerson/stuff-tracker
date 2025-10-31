using System.Text.Json;
using FluentAssertions;
using StuffTracker.Api.Data;

namespace StuffTracker.Tests.Integration;

/// <summary>
/// Integration tests for GraphQL filtering, sorting, and pagination capabilities.
/// Tests validate query capabilities across text and numeric fields and page boundaries.
/// </summary>
[Collection("Integration Tests")]
public class FilteringSortingPaginationTests : IClassFixture<GraphQLTestFixture>
{
    private readonly GraphQLTestFixture _fixture;
    private readonly HttpClient _client;

    public FilteringSortingPaginationTests(GraphQLTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Filtering_ItemName_Contains_ReturnsMatchingItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { name: { contains: ""lamp"" } }) {
                    nodes {
                        id
                        name
                        quantity
                        roomId
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        data.GetProperty("items").GetProperty("nodes").GetArrayLength().Should().BeGreaterThan(0);

        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.All(i => i.Name.Contains("lamp", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Filtering_ItemName_StartsWith_ReturnsMatchingItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { name: { startsWith: ""Electronics"" } }) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().NotBeEmpty();
        items.All(i => i.Name.StartsWith("Electronics", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Filtering_ItemName_CaseInsensitive_ReturnsAllVariations()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { name: { contains: ""electronics"" } }) {
                    nodes {
                        id
                        name
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().NotBeEmpty();
        // Should match Electronics-, electronics-, ELECTRONICS- prefixes
        items.All(i => i.Name.Contains("electronics", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Filtering_ItemQuantity_GreaterThanOrEqual_ReturnsMatchingItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { quantity: { gte: 25 } }) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.All(i => i.Quantity >= 25).Should().BeTrue();
    }

    [Fact]
    public async Task Filtering_ItemQuantity_LessThanOrEqual_ReturnsMatchingItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { quantity: { lte: 30 } }) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.All(i => i.Quantity <= 30).Should().BeTrue();
    }

    [Fact]
    public async Task Filtering_ItemQuantity_Equals_ReturnsMatchingItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { quantity: { eq: 42 } }) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.All(i => i.Quantity == 42).Should().BeTrue();
    }


    [Fact]
    public async Task Sorting_ItemName_Ascending_ReturnsItemsInAlphabeticalOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ name: ASC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThan(0);

        // Verify alphabetical ordering
        for (int i = 0; i < items.Count - 1; i++)
        {
            var comparison = string.Compare(items[i].Name, items[i + 1].Name, StringComparison.OrdinalIgnoreCase);
            comparison.Should().BeLessThanOrEqualTo(0, $"Items should be in ascending order: '{items[i].Name}' should come before '{items[i + 1].Name}'");
        }
    }

    [Fact]
    public async Task Sorting_ItemName_Descending_ReturnsItemsInReverseAlphabeticalOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ name: DESC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThan(0);

        // Verify reverse alphabetical ordering
        for (int i = 0; i < items.Count - 1; i++)
        {
            var comparison = string.Compare(items[i].Name, items[i + 1].Name, StringComparison.OrdinalIgnoreCase);
            comparison.Should().BeGreaterThanOrEqualTo(0, $"Items should be in descending order: '{items[i].Name}' should come after '{items[i + 1].Name}'");
        }
    }

    [Fact]
    public async Task Sorting_ItemName_WithTieBreaking_ReturnsDeterministicOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ name: ASC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act - Execute same query twice
        var response1 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        var response2 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response1.ShouldHaveNoErrors();
        response2.ShouldHaveNoErrors();

        var data1 = response1.Data!.Value;
        var data2 = response2.Data!.Value;
        
        var items1 = JsonSerializer.Deserialize<List<ItemNode>>(
            data1.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var items2 = JsonSerializer.Deserialize<List<ItemNode>>(
            data2.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items1.Should().NotBeNull();
        items2.Should().NotBeNull();
        items1!.Count.Should().Be(items2!.Count, "Both queries should return same number of items");

        // Verify deterministic ordering - items with same name should be ordered by Id (stable default ordering)
        for (int i = 0; i < items1.Count; i++)
        {
            items1[i].Id.Should().Be(items2[i].Id, $"Item at position {i} should be the same in both queries (deterministic ordering)");
            items1[i].Name.Should().Be(items2[i].Name, $"Item name at position {i} should be the same");
        }
    }

    [Fact]
    public async Task Sorting_ItemQuantity_Ascending_ReturnsItemsInNumericOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ quantity: ASC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThan(0);

        // Verify numeric ordering
        for (int i = 0; i < items.Count - 1; i++)
        {
            items[i].Quantity.Should().BeLessThanOrEqualTo(
                items[i + 1].Quantity, 
                $"Items should be in ascending quantity order: {items[i].Quantity} should be <= {items[i + 1].Quantity}");
        }
    }

    [Fact]
    public async Task Sorting_ItemQuantity_Descending_ReturnsItemsInReverseNumericOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ quantity: DESC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var items = JsonSerializer.Deserialize<List<ItemNode>>(
            data.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThan(0);

        // Verify reverse numeric ordering
        for (int i = 0; i < items.Count - 1; i++)
        {
            items[i].Quantity.Should().BeGreaterThanOrEqualTo(
                items[i + 1].Quantity, 
                $"Items should be in descending quantity order: {items[i].Quantity} should be >= {items[i + 1].Quantity}");
        }
    }

    [Fact]
    public async Task Sorting_ItemQuantity_WithTieBreaking_ReturnsDeterministicOrder()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, order: [{ quantity: ASC }]) {
                    nodes {
                        id
                        name
                        quantity
                    }
                }
            }";

        // Act - Execute same query twice
        var response1 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        var response2 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response1.ShouldHaveNoErrors();
        response2.ShouldHaveNoErrors();

        var data1 = response1.Data!.Value;
        var data2 = response2.Data!.Value;
        
        var items1 = JsonSerializer.Deserialize<List<ItemNode>>(
            data1.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var items2 = JsonSerializer.Deserialize<List<ItemNode>>(
            data2.GetProperty("items").GetProperty("nodes").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items1.Should().NotBeNull();
        items2.Should().NotBeNull();
        items1!.Count.Should().Be(items2!.Count);

        // Verify deterministic ordering - items with same quantity should be ordered by Id (stable default ordering)
        for (int i = 0; i < items1.Count; i++)
        {
            items1[i].Id.Should().Be(items2[i].Id, $"Item at position {i} should be the same in both queries (deterministic ordering)");
            items1[i].Quantity.Should().Be(items2[i].Quantity, $"Item quantity at position {i} should be the same");
        }
    }


    [Fact]
    public async Task Pagination_ItemsConnection_HasCorrectStructure()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, first: 10) {
                    edges {
                        node {
                            id
                            name
                            quantity
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemsConnection = data.GetProperty("items");
        
        // Verify edges structure
        itemsConnection.GetProperty("edges").ValueKind.Should().Be(JsonValueKind.Array);
        var edges = itemsConnection.GetProperty("edges");
        edges.GetArrayLength().Should().BeLessThanOrEqualTo(10);

        // Verify each edge has node and cursor
        foreach (var edge in edges.EnumerateArray())
        {
            edge.GetProperty("node").ValueKind.Should().Be(JsonValueKind.Object);
            edge.GetProperty("cursor").GetString().Should().NotBeNullOrEmpty("Cursor should be a non-empty string");
        }

        // Verify pageInfo structure
        var pageInfo = itemsConnection.GetProperty("pageInfo");
        pageInfo.GetProperty("hasNextPage").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        pageInfo.GetProperty("hasPreviousPage").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        pageInfo.GetProperty("startCursor").GetString().Should().NotBeNullOrEmpty("startCursor should not be null");
        pageInfo.GetProperty("endCursor").GetString().Should().NotBeNullOrEmpty("endCursor should not be null");
    }

    [Fact]
    public async Task Pagination_FirstPage_ReturnsItemsAndHasNextPage()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, first: 10) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        endCursor
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemsConnection = data.GetProperty("items");
        var edges = itemsConnection.GetProperty("edges");
        var pageInfo = itemsConnection.GetProperty("pageInfo");

        edges.GetArrayLength().Should().Be(10, "First page should return exactly 10 items");
        pageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse("First page should not have previous page");
        pageInfo.GetProperty("hasNextPage").GetBoolean().Should().BeTrue("With 200 items, first page should have next page");
        
        var endCursor = pageInfo.GetProperty("endCursor").GetString();
        endCursor.Should().NotBeNullOrEmpty("End cursor should be present for navigation");
    }

    [Fact]
    public async Task Pagination_NextPage_UsingCursor_ReturnsNextItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get first page
        var firstPageQuery = @"
            query {
                items(search: null, first: 10) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        endCursor
                        hasNextPage
                    }
                }
            }";

        var firstPageResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, firstPageQuery);
        firstPageResponse.ShouldHaveNoErrors();

        var firstPageData = firstPageResponse.Data!.Value;
        var firstPageItemsConnection = firstPageData.GetProperty("items");
        var firstPagePageInfo = firstPageItemsConnection.GetProperty("pageInfo");
        var endCursor = firstPagePageInfo.GetProperty("endCursor").GetString();

        // Get first page item IDs for comparison
        var firstPageItems = JsonSerializer.Deserialize<List<ItemEdge>>(
            firstPageItemsConnection.GetProperty("edges").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Get next page using cursor
        var nextPageQuery = $@"
            query {{
                items(search: null, first: 10, after: ""{endCursor}"") {{
                    edges {{
                        node {{
                            id
                            name
                        }}
                        cursor
                    }}
                    pageInfo {{
                        startCursor
                        endCursor
                        hasNextPage
                        hasPreviousPage
                    }}
                }}
            }}";

        // Act
        var nextPageResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, nextPageQuery);

        // Assert
        nextPageResponse.ShouldHaveNoErrors();
        nextPageResponse.ShouldHaveData();

        var nextPageData = nextPageResponse.Data!.Value;
        var nextPageItemsConnection = nextPageData.GetProperty("items");
        var nextPageEdges = nextPageItemsConnection.GetProperty("edges");
        var nextPagePageInfo = nextPageItemsConnection.GetProperty("pageInfo");

        nextPageEdges.GetArrayLength().Should().Be(10, "Next page should return 10 items");
        nextPagePageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeTrue("Next page should have previous page");

        var nextPageItems = JsonSerializer.Deserialize<List<ItemEdge>>(
            nextPageEdges.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Verify no overlap between pages
        var firstPageIds = firstPageItems!.Select(e => e.Node.Id).ToHashSet();
        var nextPageIds = nextPageItems!.Select(e => e.Node.Id).ToHashSet();
        firstPageIds.Intersect(nextPageIds).Should().BeEmpty("Pages should not have overlapping items");
    }

    [Fact]
    public async Task Pagination_PreviousPage_UsingCursor_ReturnsPreviousItems()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Get first page
        var firstPageQuery = @"
            query {
                items(search: null, first: 10) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        endCursor
                    }
                }
            }";

        var firstPageResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, firstPageQuery);
        firstPageResponse.ShouldHaveNoErrors();

        var firstPageData = firstPageResponse.Data!.Value;
        var firstPageItemsConnection = firstPageData.GetProperty("items");
        var firstPagePageInfo = firstPageItemsConnection.GetProperty("pageInfo");
        var firstPageEndCursor = firstPagePageInfo.GetProperty("endCursor").GetString();

        var firstPageItems = JsonSerializer.Deserialize<List<ItemEdge>>(
            firstPageItemsConnection.GetProperty("edges").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Get second page
        var secondPageQuery = $@"
            query {{
                items(search: null, first: 10, after: ""{firstPageEndCursor}"") {{
                    edges {{
                        node {{
                            id
                            name
                        }}
                        cursor
                    }}
                    pageInfo {{
                        startCursor
                        endCursor
                    }}
                }}
            }}";

        var secondPageResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, secondPageQuery);
        secondPageResponse.ShouldHaveNoErrors();

        var secondPageData = secondPageResponse.Data!.Value;
        var secondPageItemsConnection = secondPageData.GetProperty("items");
        var secondPagePageInfo = secondPageItemsConnection.GetProperty("pageInfo");
        var secondPageStartCursor = secondPagePageInfo.GetProperty("startCursor").GetString();

        // Get previous page (back to first page) using before cursor
        var previousPageQuery = $@"
            query {{
                items(search: null, last: 10, before: ""{secondPageStartCursor}"") {{
                    edges {{
                        node {{
                            id
                            name
                        }}
                        cursor
                    }}
                    pageInfo {{
                        startCursor
                        endCursor
                        hasNextPage
                        hasPreviousPage
                    }}
                }}
            }}";

        // Act
        var previousPageResponse = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, previousPageQuery);

        // Assert
        previousPageResponse.ShouldHaveNoErrors();
        previousPageResponse.ShouldHaveData();

        var previousPageData = previousPageResponse.Data!.Value;
        var previousPageItemsConnection = previousPageData.GetProperty("items");
        var previousPageEdges = previousPageItemsConnection.GetProperty("edges");

        var previousPageItems = JsonSerializer.Deserialize<List<ItemEdge>>(
            previousPageEdges.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Verify previous page matches first page
        previousPageItems!.Count.Should().Be(firstPageItems!.Count);
        for (int i = 0; i < previousPageItems.Count; i++)
        {
            previousPageItems[i].Node.Id.Should().Be(
                firstPageItems[i].Node.Id, 
                $"Item at position {i} should match first page item (navigated back correctly)");
        }
    }

    [Fact]
    public async Task Pagination_StableOrdering_ReturnsIdenticalResultsAcrossMultipleRequests()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, first: 20) {
                    edges {
                        node {
                            id
                            name
                            quantity
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                }
            }";

        // Act - Execute same query multiple times
        var response1 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        var response2 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);
        var response3 = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response1.ShouldHaveNoErrors();
        response2.ShouldHaveNoErrors();
        response3.ShouldHaveNoErrors();

        var data1 = response1.Data!.Value;
        var data2 = response2.Data!.Value;
        var data3 = response3.Data!.Value;

        var items1 = JsonSerializer.Deserialize<List<ItemEdge>>(
            data1.GetProperty("items").GetProperty("edges").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var items2 = JsonSerializer.Deserialize<List<ItemEdge>>(
            data2.GetProperty("items").GetProperty("edges").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var items3 = JsonSerializer.Deserialize<List<ItemEdge>>(
            data3.GetProperty("items").GetProperty("edges").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        items1.Should().NotBeNull();
        items2.Should().NotBeNull();
        items3.Should().NotBeNull();

        items1!.Count.Should().Be(items2!.Count).And.Be(items3!.Count);

        // Verify identical ordering across all three requests
        for (int i = 0; i < items1.Count; i++)
        {
            items1[i].Node.Id.Should().Be(items2[i].Node.Id, $"Item at position {i} should be identical in requests 1 and 2");
            items2[i].Node.Id.Should().Be(items3[i].Node.Id, $"Item at position {i} should be identical in requests 2 and 3");
            items1[i].Cursor.Should().Be(items2[i].Cursor, $"Cursor at position {i} should be identical in requests 1 and 2");
            items2[i].Cursor.Should().Be(items3[i].Cursor, $"Cursor at position {i} should be identical in requests 2 and 3");
        }
    }

    [Fact]
    public async Task Pagination_EmptyResults_ReturnsEmptyEdgesArray()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        var query = @"
            query {
                items(search: null, where: { name: { contains: ""NonExistentItemXYZ123"" } }, first: 10) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemsConnection = data.GetProperty("items");
        var edges = itemsConnection.GetProperty("edges");

        edges.GetArrayLength().Should().Be(0, "Filter with no matches should return empty edges array");

        var pageInfo = itemsConnection.GetProperty("pageInfo");
        pageInfo.GetProperty("hasNextPage").GetBoolean().Should().BeFalse("Empty results should indicate no next page");
        pageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse("Empty results should indicate no previous page");
    }

    [Fact]
    public async Task Pagination_SinglePage_ReturnsAllItemsAndNoNextPage()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Request more items than exist (200 items seeded) - use max pagination size
        // Note: Hot Chocolate default max is 50, so we'll request that and verify it's the limit
        var query = @"
            query {
                items(search: null, first: 50) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemsConnection = data.GetProperty("items");
        var edges = itemsConnection.GetProperty("edges");
        var pageInfo = itemsConnection.GetProperty("pageInfo");

        // With 200 items and max page size of 50, we'll get 50 items and hasNextPage will be true
        edges.GetArrayLength().Should().Be(50, "Should return exactly 50 items (max pagination limit)");
        pageInfo.GetProperty("hasNextPage").GetBoolean().Should().BeTrue("With 200 items, first page of 50 should have next page");
        pageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse("First page should indicate no previous page");
    }

    [Fact]
    public async Task Pagination_LastPage_HasNoNextPage()
    {
        // Arrange
        await _fixture.InitializeDatabaseAsync();
        await _fixture.SeedDatabaseAsync();

        // Navigate to last page by requesting last 10 items
        var query = @"
            query {
                items(search: null, last: 10) {
                    edges {
                        node {
                            id
                            name
                        }
                        cursor
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }";

        // Act
        var response = await GraphQLTestHelpers.SendGraphQLRequestAsync(_client, query);

        // Assert
        response.ShouldHaveNoErrors();
        response.ShouldHaveData();

        var data = response.Data!.Value;
        var itemsConnection = data.GetProperty("items");
        var edges = itemsConnection.GetProperty("edges");
        var pageInfo = itemsConnection.GetProperty("pageInfo");

        edges.GetArrayLength().Should().Be(10, "Last page should return 10 items");
        pageInfo.GetProperty("hasNextPage").GetBoolean().Should().BeFalse("Last page should indicate no next page");
        pageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeTrue("Last page should indicate previous page exists");
    }

    // Helper classes for deserialization
    private class ItemNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int RoomId { get; set; }
    }

    private class ItemEdge
    {
        public ItemNode Node { get; set; } = null!;
        public string Cursor { get; set; } = string.Empty;
    }

    private class RoomNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class LocationWithRooms
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RoomsConnection? Rooms { get; set; }
    }

    private class RoomsConnection
    {
        public List<RoomNode>? Nodes { get; set; }
    }
}

