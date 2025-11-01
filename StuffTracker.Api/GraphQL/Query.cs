using HotChocolate.Data;
using StuffTracker.Domain.Data;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL;

public class Query
{
    /// <summary>
    /// Query all locations with filtering, sorting, and pagination support
    /// Returns LocationEntity which is mapped to Location GraphQL type via LocationType.
    /// Filtering: Name (string)
    /// Sorting: Name, CreatedAt, Id
    /// Pagination: Cursor-based pagination with Connection type
    /// UseProjection, UseFiltering, and UseSorting work at the EF level and translate to SQL.
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<LocationEntity> GetLocations(StuffTrackerDbContext context)
        => context.Locations
            .OrderBy(l => l.Id); // Stable default ordering for deterministic pagination

    /// <summary>
    /// Query a single location by ID
    /// Returns LocationEntity which is mapped to Location GraphQL type via LocationType.
    /// </summary>
    [UseProjection]
    public LocationEntity? GetLocation(int id, StuffTrackerDbContext context)
        => context.Locations
            .Where(l => l.Id == id)
            .FirstOrDefault();

    /// <summary>
    /// Query items filtered by name search (case-insensitive)
    /// Returns ItemEntity which is mapped to Item GraphQL type via ItemType.
    /// Filtering: Name (string), Quantity (int), RoomId (int)
    /// Sorting: Name, Quantity, CreatedAt, Id
    /// Pagination: Cursor-based pagination with Connection type
    /// UseProjection, UseFiltering, and UseSorting work at the EF level and translate to SQL.
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ItemEntity> GetItems(string? search, StuffTrackerDbContext context)
    {
        var query = context.Items.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        
        // Apply stable default ordering before pagination
        // This ensures deterministic cursor behavior and satisfies EF Core's requirement
        // Client sorting via UseSorting can override this default ordering
        return query.OrderBy(i => i.Id); // Stable default ordering for deterministic pagination
    }
}
