using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.GraphQL.Sorting;
using StuffTracker.Domain.Data;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL;

public class Query
{
    /// <summary>
    /// Query all locations with filtering, sorting, and pagination support.
    /// Filtering: Name (string)
    /// Sorting: Name, CreatedAt, Id (LocationSortType exposes Id for tiebreaker)
    /// Pagination: Keyset pagination via AddDbContextCursorPagingProvider
    /// Client sort order is respected: order: { name: ASC } → SQL: ORDER BY Name, Id
    /// No hardcoded OrderBy - let UseSorting middleware handle client-specified ordering
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting(typeof(LocationSortType))]
    public IQueryable<LocationEntity> GetLocations(StuffTrackerDbContext context)
        => context.Locations;

    /// <summary>
    /// Query a single location by ID.
    /// </summary>
    [UseProjection]
    public LocationEntity? GetLocation(int id, StuffTrackerDbContext context)
        => context.Locations.FirstOrDefault(l => l.Id == id);

    /// <summary>
    /// Query items filtered by name (case-insensitive).
    /// Filtering: Name, Quantity, RoomId
    /// Sorting: Name, Quantity, CreatedAt, Id (ItemSortType exposes Id for tiebreaker)
    /// Pagination: Keyset pagination via AddDbContextCursorPagingProvider
    /// Client sort order is respected: order: { quantity: DESC } → SQL: ORDER BY Quantity DESC, Id ASC
    /// No hardcoded OrderBy - let UseSorting middleware handle client-specified ordering
    /// </summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting(typeof(ItemSortType))]
    public IQueryable<ItemEntity> GetItems(string? search, StuffTrackerDbContext context)
    {
        var query = context.Items.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }
}
