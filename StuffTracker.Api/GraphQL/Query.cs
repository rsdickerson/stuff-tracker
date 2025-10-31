using HotChocolate.Data;
using StuffTracker.Api.GraphQL.Types;
using StuffTracker.Domain.Data;
using StuffTracker.Domain.Entities;
using Types = StuffTracker.Api.GraphQL.Types;

namespace StuffTracker.Api.GraphQL;

public class Query
{
    /// <summary>
    /// Query all locations with filtering, sorting, and pagination support
    /// Filtering: Name (string)
    /// Sorting: Name, CreatedAt, Id
    /// Pagination: Cursor-based pagination with Connection type
    /// Filtering/sorting translate to SQL: EF Core translates filters/sorts on DTO properties
    /// back to entity properties because property names match and projection is simple direct mapping
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Types.Location> GetLocations(StuffTrackerDbContext context)
        => context.Locations
            .OrderBy(l => l.Id) // Stable default ordering for deterministic pagination
            .Select(l => new Types.Location
            {
                Id = l.Id,
                Name = l.Name,
                CreatedAt = l.CreatedAt
            });

    /// <summary>
    /// Query a single location by ID
    /// </summary>
    [UseProjection]
    public Types.Location? GetLocation(int id, StuffTrackerDbContext context)
        => context.Locations
            .Where(l => l.Id == id)
            .Select(l => new Types.Location
            {
                Id = l.Id,
                Name = l.Name,
                CreatedAt = l.CreatedAt
            })
            .FirstOrDefault();

    /// <summary>
    /// Query items filtered by name search (case-insensitive)
    /// Filtering: Name (string), Quantity (int), RoomId (int)
    /// Sorting: Name, Quantity, CreatedAt, Id
    /// Pagination: Cursor-based pagination with Connection type
    /// Filtering/sorting translate to SQL: EF Core translates filters/sorts on DTO properties
    /// back to entity properties because property names match and projection is simple direct mapping
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Types.Item> GetItems(string? search, StuffTrackerDbContext context)
    {
        var query = context.Items.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        
        // Don't apply default OrderBy here - let UseSorting handle it
        // If no sort is specified, UseSorting will use default (Id)
        // Apply projection after sorting (Hot Chocolate middleware handles this)
        return query.Select(i => new Types.Item
        {
            Id = i.Id,
            Name = i.Name,
            Quantity = i.Quantity,
            RoomId = i.RoomId,
            CreatedAt = i.CreatedAt
        });
    }
}
