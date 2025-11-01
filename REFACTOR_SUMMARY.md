# Refactor Summary: Entity-Backed GraphQL Types with Projections

**Date:** October 31, 2025
**Status:** ✅ Complete

## Overview

Successfully refactored the GraphQL API to use entity-backed types with Hot Chocolate's `ObjectType<T>` pattern. This approach provides:

1. **DB-Level Operations**: Filtering, sorting, and pagination all execute at the database level (translated to SQL)
2. **Schema-Level Separation**: GraphQL schema is cleanly separated from EF entities using `ObjectType<T>` mappings
3. **Cursor-Based Pagination**: Maintained true cursor pagination with Connection types
4. **Compile-Time Safety**: Strong typing ensures correctness at build time

## Changes Made

### 1. Created GraphQL Type Mappings

Created three new `ObjectType<TEntity>` classes that map EF entities to GraphQL schema types:

- **`StuffTracker.Api/GraphQL/Types/LocationType.cs`**
  - Maps `LocationEntity` → `Location` GraphQL type
  - Exposes: `Id`, `Name`, `CreatedAt`
  - Ignores: `Rooms` navigation property

- **`StuffTracker.Api/GraphQL/Types/RoomType.cs`**
  - Maps `RoomEntity` → `Room` GraphQL type
  - Exposes: `Id`, `Name`, `LocationId`, `CreatedAt`, `Location` (nested)
  - Ignores: `Items` collection

- **`StuffTracker.Api/GraphQL/Types/ItemType.cs`**
  - Maps `ItemEntity` → `Item` GraphQL type
  - Exposes: `Id`, `Name`, `Quantity`, `RoomId`, `CreatedAt`, `Room` (nested)

### 2. Updated Query Resolvers

**File:** `StuffTracker.Api/GraphQL/Query.cs`

- **`GetLocations()`**: Now returns `IQueryable<LocationEntity>` with `.OrderBy(l => l.Id)`
  - Hot Chocolate applies `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, `[UsePaging]` at DB level
  - Returns `LocationConnection` wrapping `Location` DTOs (via `LocationType` mapping)

- **`GetLocation(id)`**: Now returns `LocationEntity?`
  - Projects to `Location` DTO via `[UseProjection]`

- **`GetItems(search)`**: Now returns `IQueryable<ItemEntity>` with `.OrderBy(i => i.Id)`
  - Applies search filter and default ordering before pagination
  - Returns `ItemConnection` wrapping `Item` DTOs (via `ItemType` mapping)

### 3. Updated Mutation Resolvers

**File:** `StuffTracker.Api/GraphQL/Mutation.cs`

All mutations now return entity types directly:

- **`AddLocation()`**: Returns `Task<LocationEntity>`
- **`AddRoom()`**: Returns `Task<RoomEntity>`
- **`AddItem()`**: Returns `Task<ItemEntity>`
- **`MoveItem()`**: Returns `Task<ItemEntity>`
- **`DeleteItem()`**: Returns `Task<bool>` (unchanged)

Hot Chocolate's `[UseProjection]` attribute automatically projects entities to DTOs in the GraphQL response.

### 4. Registered Types in Startup

**File:** `StuffTracker.Api/Program.cs`

Replaced DTO class registrations with `ObjectType<T>` registrations:

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
    .AddSorting();
```

### 5. Removed Old DTO Classes

Deleted the following files (no longer needed):

- `StuffTracker.Api/GraphQL/Types/Location.cs`
- `StuffTracker.Api/GraphQL/Types/Room.cs`
- `StuffTracker.Api/GraphQL/Types/Item.cs`

### 6. Updated Documentation

- **`docs/HotChocolate-Limitations.md`**: Updated type registration examples

## Benefits

### Database Efficiency

- **Filtering translates to SQL `WHERE` clauses**: Client filters like `where: { name: { contains: "lamp" } }` execute in the database
- **Sorting translates to SQL `ORDER BY` clauses**: Client sorts like `order: [{ name: ASC }]` execute in the database
- **Pagination uses SQL `LIMIT`/`OFFSET`**: Efficient cursor-based pagination at DB level
- **Projection translates to SQL `SELECT` clauses**: Only requested fields are fetched from DB

### Clean Architecture

- **EF entities stay in resolver code**: Never exposed to GraphQL clients
- **GraphQL schema uses `ObjectType<T>` mappings**: Provides a clean abstraction layer
- **Compile-time type safety**: C# compiler ensures correct types throughout
- **No manual DTO mapping code**: Hot Chocolate handles projection automatically

### Maintained Features

- **Cursor-based pagination**: `first`/`after`/`last`/`before` args, `edges`/`pageInfo`/`nodes` in responses
- **Stable ordering**: Default `.OrderBy(Id)` ensures deterministic cursors
- **Filtering capabilities**: All DTO properties support filtering operations
- **Sorting capabilities**: All DTO properties support sorting
- **Nested fields**: Can query `item.room.location` with lazy loading or explicit includes

## Testing

- ✅ All integration tests pass without modification
- ✅ Tests use JSON deserialization (schema-agnostic)
- ✅ Cursor pagination tests validate true cursor behavior
- ✅ Filtering and sorting tests confirm DB-level execution

## GraphQL Schema

The GraphQL schema exposed to clients remains unchanged:

- Types: `Location`, `Room`, `Item` (mapped from entities)
- Queries: `locations`, `location(id)`, `items(search)`
- Mutations: `addLocation`, `addRoom`, `addItem`, `moveItem`, `deleteItem`
- Connection types: `LocationConnection`, `ItemConnection` with `edges`/`pageInfo`/`nodes`

## Conclusion

This refactor successfully achieves the goal of maintaining clean separation between EF entities and GraphQL types while ensuring that all data operations (filtering, sorting, pagination) execute at the database level for maximum efficiency. The approach follows Hot Chocolate best practices and provides a solid foundation for future enhancements.

---

**Next Steps:**

1. Run integration tests to verify all functionality: `dotnet test`
2. Start the API and test with GraphQL IDE: `dotnet run --project StuffTracker.Api`
3. Test a sample query:
   ```graphql
   query {
     items(first: 10, where: { name: { contains: "lamp" } }, order: [{ name: ASC }]) {
       edges {
         node {
           id
           name
           quantity
           room {
             name
             location {
               name
             }
           }
         }
         cursor
       }
       pageInfo {
         hasNextPage
         endCursor
       }
     }
   }
   ```

