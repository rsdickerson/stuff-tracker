# Custom Sort Types for Deterministic Pagination

## Overview

Custom sort types ensure **deterministic cursor-based pagination** by explicitly including `Id` as a tiebreaker field. This guarantees stable, consistent results even when sorting by fields with duplicate values.

## Why Custom Sort Types?

### The Problem

Without explicit tiebreakers, sorting by fields with duplicate values can lead to:
- ❌ **Non-deterministic ordering** - Items may appear in different orders across queries
- ❌ **Unstable cursors** - Same cursor may point to different items
- ❌ **Skipped/duplicate items** - During pagination, items may be skipped or shown twice
- ❌ **Unpredictable SQL** - EF Core may add implicit ordering that varies

### The Solution

Custom sort types explicitly define `Id` as a sortable field, ensuring:
- ✅ **Deterministic ordering** - Items always appear in the same order
- ✅ **Stable cursors** - Cursors reliably point to the same items
- ✅ **Predictable pagination** - No skipped or duplicate items
- ✅ **Explicit SQL** - Generated SQL includes `ORDER BY Id` consistently

## Implementation

### 1. LocationSortType

**File:** `StuffTracker.Api/GraphQL/Sorting/LocationSortType.cs`

```csharp
using HotChocolate.Data.Sorting;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Sorting;

/// <summary>
/// Custom sort type for Location entities.
/// Ensures deterministic pagination by always including Id as a tiebreaker.
/// </summary>
public class LocationSortType : SortInputType<LocationEntity>
{
    protected override void Configure(ISortInputTypeDescriptor<LocationEntity> descriptor)
    {
        descriptor.Name("LocationSortInput");
        
        // Bind all fields implicitly (Name, CreatedAt, etc.)
        descriptor.BindFieldsImplicitly();
        
        // Add Id as a tiebreaker field for stable pagination
        descriptor.Field(l => l.Id).Name("id");
    }
}
```

### 2. ItemSortType

**File:** `StuffTracker.Api/GraphQL/Sorting/ItemSortType.cs`

```csharp
using HotChocolate.Data.Sorting;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Sorting;

/// <summary>
/// Custom sort type for Item entities.
/// Ensures deterministic pagination by always including Id as a tiebreaker.
/// </summary>
public class ItemSortType : SortInputType<ItemEntity>
{
    protected override void Configure(ISortInputTypeDescriptor<ItemEntity> descriptor)
    {
        descriptor.Name("ItemSortInput");
        
        // Bind all fields implicitly (Name, Quantity, RoomId, CreatedAt, etc.)
        descriptor.BindFieldsImplicitly();
        
        // Add Id as a tiebreaker field for stable pagination
        descriptor.Field(i => i.Id).Name("id");
    }
}
```

## Configuration

### Schema Registration

**File:** `StuffTracker.Api/Program.cs`

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<StuffTracker.Api.GraphQL.Types.LocationType>()
    .AddType<StuffTracker.Api.GraphQL.Types.RoomType>()
    .AddType<StuffTracker.Api.GraphQL.Types.ItemType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddType<StuffTracker.Api.GraphQL.Sorting.LocationSortType>()  // ← Register custom sort types
    .AddType<StuffTracker.Api.GraphQL.Sorting.ItemSortType>()     // ← Register custom sort types
    .ModifyPagingOptions(opt =>
    {
        opt.DefaultPageSize = 50;
        opt.MaxPageSize = 1000;
        opt.IncludeTotalCount = true;
        opt.RequirePagingBoundaries = false;
    });
```

### Resolver Usage

**File:** `StuffTracker.Api/GraphQL/Query.cs`

```csharp
/// <summary>
/// Query all locations with filtering, sorting, and pagination support
/// </summary>
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting(typeof(StuffTracker.Api.GraphQL.Sorting.LocationSortType))]  // ← Use custom sort type
public IQueryable<LocationEntity> GetLocations(StuffTrackerDbContext context)
    => context.Locations;

/// <summary>
/// Query items with filtering, sorting, and pagination support
/// </summary>
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting(typeof(StuffTracker.Api.GraphQL.Sorting.ItemSortType))]  // ← Use custom sort type
public IQueryable<ItemEntity> GetItems(string? search, StuffTrackerDbContext context)
{
    var query = context.Items.AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
    
    return query;
}
```

## GraphQL Schema

### Generated Sort Input Types

With custom sort types, the GraphQL schema exposes:

```graphql
input LocationSortInput {
  id: SortEnumType          # ← Explicit Id field for tiebreaking
  name: SortEnumType
  createdAt: SortEnumType
}

input ItemSortInput {
  id: SortEnumType          # ← Explicit Id field for tiebreaking
  name: SortEnumType
  quantity: SortEnumType
  roomId: SortEnumType
  createdAt: SortEnumType
}

enum SortEnumType {
  ASC
  DESC
}
```

## Usage Examples

### 1. Sort by Name Only

**Query:**
```graphql
query {
  locations(
    first: 10
    order: { name: ASC }
  ) {
    nodes {
      id
      name
    }
  }
}
```

**Generated SQL:**
```sql
SELECT l.Id, l.Name, l.CreatedAt
FROM Locations AS l
ORDER BY l.Name ASC, l.Id ASC  -- ← Id automatically added as tiebreaker
LIMIT 10
```

### 2. Sort by Name + Explicit Id

**Query:**
```graphql
query {
  locations(
    first: 10
    order: { name: ASC, id: ASC }
  ) {
    nodes {
      id
      name
    }
  }
}
```

**Generated SQL:**
```sql
SELECT l.Id, l.Name, l.CreatedAt
FROM Locations AS l
ORDER BY l.Name ASC, l.Id ASC
LIMIT 10
```

### 3. Sort Items by Quantity (with Id tiebreaker)

**Query:**
```graphql
query {
  items(
    first: 20
    order: { quantity: DESC, id: ASC }
  ) {
    nodes {
      id
      name
      quantity
    }
  }
}
```

**Generated SQL:**
```sql
SELECT i.Id, i.Name, i.Quantity, i.RoomId, i.CreatedAt
FROM Items AS i
ORDER BY i.Quantity DESC, i.Id ASC
LIMIT 20
```

### 4. Multiple Sort Fields

**Query:**
```graphql
query {
  items(
    first: 20
    order: { name: ASC, quantity: DESC, id: ASC }
  ) {
    nodes {
      id
      name
      quantity
    }
  }
}
```

**Generated SQL:**
```sql
SELECT i.Id, i.Name, i.Quantity, i.RoomId, i.CreatedAt
FROM Items AS i
ORDER BY i.Name ASC, i.Quantity DESC, i.Id ASC
LIMIT 20
```

## Benefits

### 1. Deterministic Pagination

**Without Custom Sort Types:**
```
Query 1: ORDER BY Name        → [Item A, Item B, Item C, Item D]
Query 2: ORDER BY Name        → [Item A, Item C, Item B, Item D]  ← Different order!
```

**With Custom Sort Types:**
```
Query 1: ORDER BY Name, Id    → [Item A, Item B, Item C, Item D]
Query 2: ORDER BY Name, Id    → [Item A, Item B, Item C, Item D]  ← Same order!
```

### 2. Stable Cursors

**Without Custom Sort Types:**
```
Cursor "ABC123" at Query 1 → Points to Item B
Cursor "ABC123" at Query 2 → Points to Item C  ← Cursor drift!
```

**With Custom Sort Types:**
```
Cursor "ABC123" at Query 1 → Points to Item B
Cursor "ABC123" at Query 2 → Points to Item B  ← Stable!
```

### 3. No Skipped/Duplicate Items

**Without Custom Sort Types:**
```
Page 1 (items 1-10):  Items A, B, C, D, E, F, G, H, I, J
Page 2 (items 11-20): Items K, L, C, M, N, O, P, Q, R, S  ← Item C appears twice!
```

**With Custom Sort Types:**
```
Page 1 (items 1-10):  Items A, B, C, D, E, F, G, H, I, J
Page 2 (items 11-20): Items K, L, M, N, O, P, Q, R, S, T  ← No duplicates!
```

## Best Practices

### ✅ DO

1. **Always include Id as a tiebreaker** in multi-field sorts:
   ```graphql
   order: { name: ASC, id: ASC }
   ```

2. **Use custom sort types** for all paginated queries

3. **Document sort behavior** in resolver comments

4. **Test pagination stability** with duplicate values

### ❌ DON'T

1. **Don't rely on default ordering** - Always specify sort fields explicitly

2. **Don't omit Id** when sorting by fields with duplicates:
   ```graphql
   # BAD - Name may have duplicates
   order: { name: ASC }
   
   # GOOD - Id ensures deterministic order
   order: { name: ASC, id: ASC }
   ```

3. **Don't use hardcoded `OrderBy()` in resolvers** - Let Hot Chocolate handle sorting:
   ```csharp
   // BAD - Hardcoded sort conflicts with client sorting
   public IQueryable<LocationEntity> GetLocations(...)
       => context.Locations.OrderBy(l => l.Id);
   
   // GOOD - Let [UseSorting] handle all ordering
   public IQueryable<LocationEntity> GetLocations(...)
       => context.Locations;
   ```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task Sorting_LocationByName_WithIdTiebreaker_ProducesDeterministicResults()
{
    // Arrange: Create locations with duplicate names
    var location1 = new LocationEntity { Id = 1, Name = "Home", CreatedAt = DateTime.UtcNow };
    var location2 = new LocationEntity { Id = 2, Name = "Home", CreatedAt = DateTime.UtcNow };
    var location3 = new LocationEntity { Id = 3, Name = "Home", CreatedAt = DateTime.UtcNow };
    
    // Act: Query twice with same sort
    var query = @"
        query {
          locations(first: 10, order: { name: ASC, id: ASC }) {
            nodes { id name }
          }
        }";
    
    var result1 = await ExecuteQueryAsync(query);
    var result2 = await ExecuteQueryAsync(query);
    
    // Assert: Results are identical
    Assert.Equal(result1, result2);
    Assert.Equal(new[] { 1, 2, 3 }, result1.Select(l => l.Id));
}
```

### Integration Test Example

```csharp
[Fact]
public async Task Pagination_WithSorting_NoDuplicateOrSkippedItems()
{
    // Arrange: Seed 100 items with duplicate names
    await SeedItemsAsync(100);
    
    // Act: Paginate through all items
    var allItems = new List<Item>();
    string? cursor = null;
    
    do
    {
        var query = $@"
            query {{
              items(first: 10, after: ""{cursor}"", order: {{ name: ASC, id: ASC }}) {{
                nodes {{ id name }}
                pageInfo {{ endCursor hasNextPage }}
              }}
            }}";
        
        var result = await ExecuteQueryAsync(query);
        allItems.AddRange(result.Nodes);
        cursor = result.PageInfo.EndCursor;
    }
    while (result.PageInfo.HasNextPage);
    
    // Assert: All items present, no duplicates
    Assert.Equal(100, allItems.Count);
    Assert.Equal(allItems.Count, allItems.DistinctBy(i => i.Id).Count());
}
```

## Troubleshooting

### Issue: Sort field not appearing in schema

**Symptom:**
```graphql
# Id field is missing
input LocationSortInput {
  name: SortEnumType
  createdAt: SortEnumType
  # id: SortEnumType  ← Missing!
}
```

**Solution:**
1. Verify custom sort type is registered in `Program.cs`:
   ```csharp
   .AddType<StuffTracker.Api.GraphQL.Sorting.LocationSortType>()
   ```

2. Check `descriptor.Field(l => l.Id).Name("id");` is present in sort type

3. Restart the application to regenerate the schema

### Issue: Sorting not translating to SQL

**Symptom:**
- GraphQL query specifies `order: { name: ASC }`
- Generated SQL shows no `ORDER BY` clause

**Solution:**
1. Ensure `[UseSorting(typeof(...))]` attribute is on the resolver

2. Verify middleware order is correct (UsePaging → UseProjection → UseFiltering → UseSorting)

3. Check that resolver returns `IQueryable<T>`, not `IEnumerable<T>`

### Issue: Cursor instability during pagination

**Symptom:**
- Same cursor returns different items across queries
- Items appear multiple times or get skipped

**Solution:**
1. Always include `id` in the sort order:
   ```graphql
   order: { name: ASC, id: ASC }
   ```

2. Ensure custom sort type includes `Id` field

3. Verify no hardcoded `OrderBy()` in resolver conflicts with sorting

## Keyset Pagination

### What is Keyset Pagination?

The StuffTracker API uses **keyset pagination** (also called cursor-based pagination) instead of offset-based pagination for better performance:

**Offset Pagination (Traditional):**
```sql
SELECT * FROM Items ORDER BY Name LIMIT 10 OFFSET 1000  -- Slow! Must scan 1000 rows
```

**Keyset Pagination (Our Approach):**
```sql
SELECT * FROM Items 
WHERE (Name, Id) > ('LastItemName', 123)  -- Fast! Uses index to seek directly
ORDER BY Name, Id 
LIMIT 10
```

### Configuration

Keyset pagination is enabled via `AddDbContextCursorPagingProvider()` in `Program.cs`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDbContextCursorPagingProvider()  // ← Enables keyset pagination
    .ModifyPagingOptions(opt => { ... });
```

### How It Works with Custom Sort Types

Custom sort types expose `id` as a sortable field, which keyset pagination uses as a tiebreaker:

1. **Client specifies sort:** `order: { name: ASC }`
2. **Hot Chocolate applies:** `ORDER BY Name, Id` (Id auto-appended)
3. **First page SQL:** `SELECT * FROM Locations ORDER BY Name, Id LIMIT 10`
4. **Second page SQL:** `SELECT * FROM Locations WHERE (Name, Id) > (@p1, @p2) ORDER BY Name, Id LIMIT 10`

The `WHERE (Name, Id) > (@p1, @p2)` clause is what makes keyset pagination efficient!

### Benefits

- **Performance:** Consistent speed regardless of page number (page 1 = page 1000)
- **Stability:** Cursors remain valid even when data changes
- **Efficiency:** Uses composite indexes `(Name, Id)` for fast seeking
- **Scalability:** Handles millions of rows without degradation

## Summary

Custom sort types are essential for:
- ✅ Deterministic cursor-based pagination
- ✅ Stable query results
- ✅ Predictable SQL generation
- ✅ Explicit schema control
- ✅ **Efficient keyset pagination with WHERE clauses**

By explicitly defining `Id` as a sortable field in `LocationSortType` and `ItemSortType`, we ensure all paginated queries have stable, consistent ordering that translates to efficient SQL `ORDER BY` clauses and keyset WHERE clause comparisons.

---

**Last Updated:** November 3, 2025

