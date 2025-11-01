using HotChocolate;
using HotChocolate.Data;
using StuffTracker.Domain.Data;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL;

public class Mutation
{
    /// <summary>
    /// Create a new location
    /// Returns LocationEntity which is mapped to Location GraphQL type via LocationType.
    /// </summary>
    [UseProjection]
    public async Task<LocationEntity> AddLocation(
        string name,
        StuffTrackerDbContext context,
        CancellationToken cancellationToken)
    {
        var location = new LocationEntity
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);

        return location;
    }

    /// <summary>
    /// Create a new room in a location
    /// Returns RoomEntity which is mapped to Room GraphQL type via RoomType.
    /// </summary>
    [UseProjection]
    public async Task<RoomEntity> AddRoom(
        string name,
        int locationId,
        StuffTrackerDbContext context,
        CancellationToken cancellationToken)
    {
        var location = await context.Locations.FindAsync(new object[] { locationId }, cancellationToken);
        if (location == null)
        {
            throw new GraphQLException($"Location with ID {locationId} not found.");
        }

        var room = new RoomEntity
        {
            Name = name,
            LocationId = locationId,
            CreatedAt = DateTime.UtcNow
        };

        context.Rooms.Add(room);
        await context.SaveChangesAsync(cancellationToken);

        return room;
    }

    /// <summary>
    /// Create a new item in a room
    /// Returns ItemEntity which is mapped to Item GraphQL type via ItemType.
    /// </summary>
    [UseProjection]
    public async Task<ItemEntity> AddItem(
        string name,
        int quantity,
        int roomId,
        StuffTrackerDbContext context,
        CancellationToken cancellationToken)
    {
        var room = await context.Rooms.FindAsync(new object[] { roomId }, cancellationToken);
        if (room == null)
        {
            throw new GraphQLException($"Room with ID {roomId} not found.");
        }

        var item = new ItemEntity
        {
            Name = name,
            Quantity = quantity,
            RoomId = roomId,
            CreatedAt = DateTime.UtcNow
        };

        context.Items.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return item;
    }

    /// <summary>
    /// Move an item to a different room
    /// Returns ItemEntity which is mapped to Item GraphQL type via ItemType.
    /// </summary>
    [UseProjection]
    public async Task<ItemEntity> MoveItem(
        int itemId,
        int newRoomId,
        StuffTrackerDbContext context,
        CancellationToken cancellationToken)
    {
        var item = await context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new GraphQLException($"Item with ID {itemId} not found.");
        }

        var room = await context.Rooms.FindAsync(new object[] { newRoomId }, cancellationToken);
        if (room == null)
        {
            throw new GraphQLException($"Room with ID {newRoomId} not found.");
        }

        item.RoomId = newRoomId;
        await context.SaveChangesAsync(cancellationToken);

        return item;
    }

    /// <summary>
    /// Delete an item
    /// </summary>
    public async Task<bool> DeleteItem(
        int itemId,
        StuffTrackerDbContext context,
        CancellationToken cancellationToken)
    {
        var item = await context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new GraphQLException($"Item with ID {itemId} not found.");
        }

        context.Items.Remove(item);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}