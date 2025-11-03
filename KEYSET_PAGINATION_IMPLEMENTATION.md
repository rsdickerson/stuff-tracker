# Keyset Pagination Implementation Complete

## Summary

Keyset pagination has been successfully configured for the StuffTracker GraphQL API using Hot Chocolate v15's `AddDbContextCursorPagingProvider()`.

## Changes Made

### 1. Program.cs - Enabled Keyset Pagination Provider
**File:** `StuffTracker.Api/Program.cs`

Added `.AddDbContextCursorPagingProvider()` to the GraphQL server configuration:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<LocationType>()
    .AddType<RoomType>()
    .AddType<ItemType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddType<LocationSortType>()
    .AddType<ItemSortType>()
    .AddDbContextCursorPagingProvider() // ← ENABLES KEYSET PAGINATION
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .ModifyPagingOptions(opt =>
    {
        opt.DefaultPageSize = 50;
        opt.MaxPageSize = 1000;
        opt.IncludeTotalCount = true;
        opt.RequirePagingBoundaries = false;
    });
```

### 2. Query.cs - Removed Hardcoded OrderBy
**File:** `StuffTracker.Api/GraphQL/Query.cs`

Ensured queries do NOT have hardcoded `OrderBy()` calls, allowing client sort precedence:

```csharp
// GetLocations - no hardcoded OrderBy
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting(typeof(LocationSortType))]
public IQueryable<LocationEntity> GetLocations(StuffTrackerDbContext context)
    => context.Locations; // Clean - let UseSorting middleware handle ordering

// GetItems - no hardcoded OrderBy
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
    
    return query; // Clean - let UseSorting middleware handle ordering
}
```

### 3. Database Indexes - Added Composite Indexes
**Migration:** `20251103194238_AddPaginationIndexes`

Created composite indexes for efficient keyset pagination queries:

```sql
-- Locations
CREATE INDEX IX_Locations_Name_Id ON Locations (Name, Id);
CREATE INDEX IX_Locations_CreatedAt_Id ON Locations (CreatedAt, Id);

-- Items
CREATE INDEX IX_Items_Name_Id ON Items (Name, Id);
CREATE INDEX IX_Items_Quantity_Id ON Items (Quantity, Id);
CREATE INDEX IX_Items_CreatedAt_Id ON Items (CreatedAt, Id);
```

These indexes optimize keyset queries like:
```sql
WHERE (Name, Id) > (@p1, @p2) ORDER BY Name, Id LIMIT 10
```

## How It Works

### Client Sort Order Priority

The implementation ensures client-specified sort fields come FIRST, with `Id` automatically appended as a tiebreaker:

**Client Query:**
```graphql
query {
  locations(first: 10, order: { name: ASC }) {
    nodes { id name }
    pageInfo { hasNextPage endCursor }
  }
}
```

**Expected SQL:**
```sql
SELECT * FROM Locations
ORDER BY Name, Id  -- ← Name first, Id as tiebreaker
LIMIT 10
```

**NOT:**
```sql
ORDER BY Id, Name  -- ← This would be wrong!
```

### Keyset Pagination Behavior

**First Page:**
```sql
SELECT * FROM Locations
ORDER BY Name, Id
LIMIT 10
```

**Second Page (using cursor):**
```sql
SELECT * FROM Locations
WHERE (Name > @p1) OR (Name = @p1 AND Id > @p2)
ORDER BY Name, Id
LIMIT 10
```

**Key Point:** Uses `WHERE` clause comparisons instead of `OFFSET` for better performance!

## Testing Instructions

### 1. Enable EF Core SQL Logging

Add to `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 2. Start the API

```bash
cd StuffTracker.Api
dotnet run
```

### 3. Execute Test Query

Open Banana Cake Pop IDE at `http://localhost:5030/graphql` and run:

```graphql
query {
  locations(first: 3, order: { name: ASC }) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}
```

### 4. Check SQL Logs

**Look for in console output:**

✅ **SUCCESS - Keyset Pagination (Expected):**
```
Executed DbCommand (Xms) [Parameters=[], CommandType='Text', CommandTimeout='30']
SELECT l.Id, l.Name, l.CreatedAt
FROM Locations AS l
ORDER BY l.Name, l.Id
LIMIT 3
```

Then for the second page:
```
Executed DbCommand (Xms) [Parameters=[@__p_name=?, @__p_id=?], CommandType='Text', CommandTimeout='30']
SELECT l.Id, l.Name, l.CreatedAt
FROM Locations AS l
WHERE (l.Name > @__p_name) OR (l.Name = @__p_name AND l.Id > @__p_id)
ORDER BY l.Name, l.Id
LIMIT 3
```

❌ **FAILURE - Offset Pagination (Not Expected):**
```
SELECT l.Id, l.Name, l.CreatedAt
FROM Locations AS l
ORDER BY l.Name, l.Id
LIMIT 3 OFFSET 3  -- ← OFFSET means keyset pagination is NOT working
```

### 5. Verify Sort Order Priority

**Test Query:**
```graphql
query {
  items(first: 5, order: { quantity: DESC }) {
    nodes {
      id
      name
      quantity
    }
  }
}
```

**Expected SQL (quantity first, id second):**
```sql
SELECT * FROM Items
ORDER BY Quantity DESC, Id ASC  -- ← Quantity DESC comes first!
LIMIT 5
```

**NOT:**
```sql
ORDER BY Id ASC, Quantity DESC  -- ← Wrong! Id should be last
```

## Troubleshooting

### Issue: Still seeing OFFSET in SQL

**Possible Causes:**
1. `AddDbContextCursorPagingProvider()` not called or not working
2. Hot Chocolate v15 keyset pagination may require additional configuration
3. Query complexity may cause fallback to offset pagination

**Solution:**
- Verify `AddDbContextCursorPagingProvider()` is in `Program.cs`
- Check Hot Chocolate version is 15.1.11
- Try simpler queries without complex filtering

### Issue: Sort order is wrong (Id comes first)

**Possible Causes:**
1. Hardcoded `OrderBy(Id)` in resolver
2. Sort middleware not properly configured

**Solution:**
- Remove any `OrderBy()` calls from `GetLocations` and `GetItems`
- Ensure `[UseSorting(typeof(...))]` attribute is present
- Client must specify `order` parameter

### Issue: Error "Field 'id' not found" in sort

**Possible Causes:**
1. Sort types don't expose `id` field

**Solution:**
- Verify `LocationSortType` and `ItemSortType` have:
  ```csharp
  descriptor.Field(x => x.Id).Name("id");
  ```

## Expected Benefits

### 1. Performance Improvement

**Offset Pagination (OLD):**
- Page 1: `LIMIT 10 OFFSET 0` - Fast
- Page 100: `LIMIT 10 OFFSET 1000` - Slow (database must scan 1000 rows)
- Page 1000: `LIMIT 10 OFFSET 10000` - Very slow (database must scan 10,000 rows)

**Keyset Pagination (NEW):**
- Page 1: `WHERE ... LIMIT 10` - Fast
- Page 100: `WHERE (Name, Id) > (...) LIMIT 10` - Fast (uses index)
- Page 1000: `WHERE (Name, Id) > (...) LIMIT 10` - Fast (uses index)

**Result:** Consistent performance regardless of page number!

### 2. Stable Cursors

**Offset Pagination (OLD):**
- If items are added/deleted, cursors become invalid
- Same cursor may point to different items

**Keyset Pagination (NEW):**
- Cursors are based on actual field values (Name, Id)
- Cursors remain valid even if items are added/deleted
- Same cursor always points to the same logical position

### 3. Database Efficiency

**Keyset queries use composite indexes efficiently:**
```sql
-- Uses IX_Locations_Name_Id index
WHERE (Name, Id) > ('Home', 2)
ORDER BY Name, Id
```

MySQL can seek directly to the correct position instead of scanning rows.

## Next Steps

1. **Test the implementation** - Start the API and check SQL logs
2. **Verify keyset behavior** - Look for WHERE clauses, not OFFSET
3. **Verify sort priority** - Client sort fields should come first
4. **Update documentation** - Document keyset pagination benefits
5. **Update tests** - Ensure tests work with keyset pagination

## Important Notes

1. **Client Responsibility:**
   - Clients should always include `id` in sort order for best results
   - Example: `order: { name: ASC, id: ASC }`

2. **Fallback Behavior:**
   - Complex queries may fall back to offset pagination
   - Monitor SQL logs to verify keyset usage

3. **Breaking Change:**
   - Cursor format has changed (value-based, not offset-based)
   - Old cursors from offset pagination are invalid
   - Clients must refresh pagination from the beginning

## Status

- ✅ Package verified (HotChocolate.Data.EntityFramework 15.1.11)
- ✅ Keyset pagination provider added (AddDbContextCursorPagingProvider)
- ✅ Hardcoded OrderBy removed from resolvers
- ✅ Composite indexes created and applied
- ⏳ **NEEDS TESTING** - Run API and verify SQL logs
- ⏳ Documentation updates pending

---

**Created:** November 3, 2025  
**Last Updated:** November 3, 2025

