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
    /// Sorting: Name, CreatedAt, Id (default: Id ASC for deterministic pagination)
    /// Pagination: Cursor-based pagination with Connection type
    /// UseProjection, UseFiltering, and UseSorting work at the EF level and translate to SQL.
    /// Middleware order: UsePaging → UseProjection → UseFiltering → UseSorting (enforced by Hot Chocolate)
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<LocationEntity> GetLocations(StuffTrackerDbContext context)
        => context.Locations; // No OrderBy - let UseSorting handle all ordering

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
    /// Sorting: Name, Quantity, CreatedAt, Id (default: Id ASC for deterministic pagination)
    /// Pagination: Cursor-based pagination with Connection type
    /// UseProjection, UseFiltering, and UseSorting work at the EF level and translate to SQL.
    /// Middleware order: UsePaging → UseProjection → UseFiltering → UseSorting (enforced by Hot Chocolate)
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
        
        // No OrderBy - let UseSorting handle all ordering
        // Hot Chocolate will add default ORDER BY for pagination when needed
        // Client can specify sorting via order parameter: order: [{ name: ASC }]
        return query;
    }
}
