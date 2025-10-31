# Hot Chocolate Limitations and Trade-offs: EF/GQL Separation

This document records constraints, trade-offs, and implementation findings when enforcing strict separation between Entity Framework (EF) entities and GraphQL DTOs in the StuffTracker application using Hot Chocolate GraphQL.

## Table of Contents

1. [Cursor Pagination Implementation](#cursor-pagination-implementation)
2. [Projection Caveats](#projection-caveats)
3. [EF/GQL Separation Constraints](#efgql-separation-constraints)
4. [Best Practices and Patterns](#best-practices-and-patterns)
5. [References](#references)

---

## Cursor Pagination Implementation

### Research Findings (Task 4.1)

Hot Chocolate's `[UsePaging]` attribute on `IQueryable<T>` provides **true cursor-based pagination** that complies with the [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm), not offset/limit pagination.

### Key Characteristics

#### True Cursor-Based Pagination

- **Connection Types**: When `[UsePaging]` is applied to an `IQueryable<T>` method, Hot Chocolate automatically generates Connection types with:
  - `edges` array containing `{ node: T, cursor: string }` objects
  - `pageInfo` object with `hasNextPage`, `hasPreviousPage`, `startCursor`, `endCursor`
  - Cursor-based query arguments: `first`, `after`, `last`, `before` (not `skip`/`take` or `offset`/`limit`)

- **GraphQL API Surface**: The GraphQL API is fully cursor-based per the GraphQL Cursor Connections Specification, even if the internal SQL implementation may use `Take`/`Skip` operations for query execution.

#### UsePaging vs UseOffsetPaging

Hot Chocolate provides two pagination approaches:

- **`[UsePaging]`**: Cursor-based pagination (Connection types) - **Recommended for stable, deterministic pagination**
- **`[UseOffsetPaging]`**: Offset/limit pagination (skip/take parameters) - Useful for simpler scenarios but less stable for changing datasets

**Decision**: Use `[UsePaging]` for all paginated queries to provide true cursor-based pagination.

### Stable Default Ordering Requirement

**Critical Limitation**: Cursor pagination requires stable, deterministic ordering to ensure consistent cursor behavior across pagination requests.

**Implementation Pattern**:
```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Types.Location> GetLocations(StuffTrackerDbContext context)
    => context.Locations
        .OrderBy(l => l.Id) // Stable default ordering for deterministic pagination
        .Select(l => new Types.Location { /* ... */ });
```

**Rationale**: 
- Default ordering by `Id` ensures deterministic cursor positions
- Client sorting via `[UseSorting]` can override default ordering while maintaining stable pagination
- Without stable ordering, cursor positions may shift if data changes between requests

### Configuration

No explicit `.AddPaging()` call is required in `Program.cs`. Hot Chocolate automatically enables pagination middleware when `[UsePaging]` attributes are detected on resolver methods.

**Configuration Pattern**:
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
    // Pagination auto-enabled via [UsePaging] attribute
```

### Official Documentation References

- [Hot Chocolate Pagination Documentation](https://chillicream.com/docs/hotchocolate/v13/fetching-data/pagination)
- [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm)

---

## Projection Caveats

### Manual Projections vs Automatic Projections

To maintain strict EF/GQL separation, **manual `Select()` projections** are used instead of Hot Chocolate's automatic projection features that work directly with EF entities.

#### Manual Projection Pattern

**Query Pattern**:
```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Types.Location> GetLocations(StuffTrackerDbContext context)
    => context.Locations
        .OrderBy(l => l.Id)
        .Select(l => new Types.Location
        {
            Id = l.Id,
            Name = l.Name,
            CreatedAt = l.CreatedAt
        });
```

**Mutation Pattern**:
```csharp
[UseProjection]
public async Task<Types.Location> AddLocation(string name, StuffTrackerDbContext context, CancellationToken cancellationToken)
{
    var location = new LocationEntity { Name = name, CreatedAt = DateTime.UtcNow };
    context.Locations.Add(location);
    await context.SaveChangesAsync(cancellationToken);
    
    return new Types.Location
    {
        Id = location.Id,
        Name = location.Name,
        CreatedAt = location.CreatedAt
    };
}
```

### Why Manual Projections?

1. **Explicit Control**: Ensures only intended DTO properties are exposed
2. **Type Safety**: Compiler enforces correct property mapping
3. **Separation of Concerns**: EF entities never appear in GraphQL schema
4. **No Entity Leakage**: Prevents accidental exposure of entity properties or navigation properties

### Filtering and Sorting Translation

**Important Finding**: When using manual projections with simple property mappings (matching names between entity and DTO), EF Core can translate filtering and sorting operations on DTO properties back to entity properties.

**Example**:
```csharp
// Client GraphQL query filters on DTO property:
// items(where: { name: { contains: "drill" } })

// EF Core translates filter to entity property in SQL:
// WHERE Name LIKE '%drill%'
```

**Limitation**: This translation only works when:
- DTO property names match entity property names exactly
- Projection uses simple direct mapping (not computed properties or complex expressions)
- Filtering/sorting attributes are applied to the `IQueryable<DTO>` after the projection

### Attribute Combinations

#### Supported Combinations

The following attribute combinations work correctly:

- `[UsePaging]` + `[UseProjection]` + `[UseFiltering]` + `[UseSorting]` on `IQueryable<DTO>`
- `[UseProjection]` on single-item queries/mutations returning `DTO` or `Task<DTO>`

#### Considerations

- **`[UseProjection]` on Manual Projections**: The `[UseProjection]` attribute is still required even with manual `Select()` projections. It enables Hot Chocolate's projection optimization middleware, which analyzes GraphQL query selection sets to optimize database queries.

- **Projection Optimization**: Hot Chocolate's projection middleware can optimize queries by analyzing which fields are requested in the GraphQL query, even when using manual projections. However, the effectiveness depends on the complexity of the projection expression.

### Trade-offs

**Benefits**:
- Complete control over exposed data
- Compiler-enforced type safety
- No risk of entity leakage
- Clear separation between database schema and API contract

**Costs**:
- More verbose code (manual mapping required)
- Maintenance overhead when entity/DTO properties change
- Potential for mapping errors if properties are added but not mapped

---

## EF/GQL Separation Constraints

### Strict Separation Enforcement

The StuffTracker application enforces strict separation between EF entities and GraphQL DTOs through multiple mechanisms:

#### 1. Separate Type Definitions

- **EF Entities**: Defined in `StuffTracker.Domain/Entities/` (e.g., `LocationEntity`, `RoomEntity`, `ItemEntity`)
- **GraphQL DTOs**: Defined in `StuffTracker.Api/GraphQL/Types/` (e.g., `Location`, `Room`, `Item`)

#### 2. Manual Projections

All queries and mutations use explicit manual projections:
- **Queries**: `Select()` expressions mapping entities to DTOs
- **Mutations**: Manual DTO construction from entities after persistence

#### 3. Schema Registration

Only DTO types are registered in the GraphQL schema:
```csharp
.AddType<StuffTracker.Api.GraphQL.Types.Location>()
.AddType<StuffTracker.Api.GraphQL.Types.Room>()
.AddType<StuffTracker.Api.GraphQL.Types.Item>()
```

No EF entity types are registered, preventing accidental exposure.

#### 4. Return Type Enforcement

All resolver methods enforce DTO-only returns:
- **Query resolvers**: Return `IQueryable<DTO>` or `Connection<DTO>` (never `IQueryable<Entity>`)
- **Mutation resolvers**: Return `Task<DTO>` (never `Task<Entity>`)

### Limitations and Constraints

#### 1. Manual Mapping Maintenance

**Constraint**: When EF entity properties change, corresponding DTO mappings must be manually updated.

**Impact**: Risk of missing property updates if mapping code is not updated alongside entity changes.

**Mitigation**: Compiler warnings/errors help catch missing properties, but requires discipline to update all mappings.

#### 2. Nested Relationship Projection Complexity

**Constraint**: Projecting nested relationships (e.g., `Item.Room.Location`) requires careful manual projection or separate resolvers.

**Current Approach**: DTOs include optional navigation properties (`Item.Room`, `Room.Location`) that can be resolved separately, but projections in queries do not automatically include nested data.

**Example**:
```csharp
// DTO definition includes optional navigation:
public class Item
{
    public int Id { get; set; }
    // ... other properties
    public Room? Room { get; set; } // Optional nested DTO
}

// Query projection does not include nested data:
.Select(i => new Types.Item
{
    Id = i.Id,
    // ... direct properties only
    // Room not projected here - requires separate resolver or explicit Include()
})
```

#### 3. Filtering/Sorting on Nested Properties

**Constraint**: Filtering and sorting on nested DTO properties (e.g., `items { room { location { name } } }`) requires special handling.

**Current Limitation**: Hot Chocolate's filtering/sorting middleware works best with direct properties. Complex nested filtering may require custom resolvers or explicit query construction.

#### 4. Projection Performance

**Trade-off**: Manual projections provide explicit control but may prevent some Hot Chocolate projection optimizations that work with automatic entity projections.

**Impact**: May result in slightly less efficient queries compared to automatic projections, but still benefits from EF Core query translation and Hot Chocolate's projection analysis.

### Patterns and Best Practices

#### Query Resolver Pattern

```csharp
[UsePaging]           // Cursor-based pagination
[UseProjection]       // Enable projection optimization
[UseFiltering]        // Enable filtering
[UseSorting]          // Enable sorting
public IQueryable<Types.Location> GetLocations(StuffTrackerDbContext context)
    => context.Locations
        .OrderBy(l => l.Id) // Stable default ordering
        .Select(l => new Types.Location
        {
            Id = l.Id,
            Name = l.Name,
            CreatedAt = l.CreatedAt
        });
```

#### Mutation Resolver Pattern

```csharp
[UseProjection]
public async Task<Types.Location> AddLocation(
    string name,
    StuffTrackerDbContext context,
    CancellationToken cancellationToken)
{
    // Create and persist entity
    var location = new LocationEntity { Name = name, CreatedAt = DateTime.UtcNow };
    context.Locations.Add(location);
    await context.SaveChangesAsync(cancellationToken);
    
    // Return DTO (never return entity)
    return new Types.Location
    {
        Id = location.Id,
        Name = location.Name,
        CreatedAt = location.CreatedAt
    };
}
```

#### Connection Type Handling

Connection types (from `[UsePaging]`) automatically wrap DTOs:
- `IQueryable<Types.Location>` → `LocationConnection` Generator automatically creates Connection type
- Connection structure: `{ edges: [{ node: Location, cursor: string }], pageInfo: {...} }`
- No manual Connection type definition required

---

## Best Practices and Patterns

### 1. Always Use Stable Default Ordering

**Practice**: Apply `.OrderBy(x => x.Id)` as default ordering before projection to ensure deterministic cursor pagination.

**Rationale**: Cursor positions depend on stable ordering. Without it, pagination results may be inconsistent.

### 2. Always Project Entities to DTOs

**Practice**: Never return EF entities directly from resolvers. Always use explicit `Select()` projections or manual DTO construction.

**Rationale**: Maintains strict separation and prevents entity leakage.

### 3. Register Only DTO Types

**Practice**: Only register GraphQL DTO types in schema configuration, never EF entity types.

**Rationale**: Prevents accidental entity exposure in GraphQL schema.

### 4. Use [UseProjection] Even with Manual Projections

**Practice**: Always include `[UseProjection]` attribute even when using manual `Select()` projections.

**Rationale**: Enables Hot Chocolate's projection optimization middleware, which analyzes GraphQL query selection sets to optimize queries.

### 5. Keep DTO Properties Simple

**Practice**: Keep DTO properties as simple mappings from entity properties when possible. Complex computed properties may prevent filtering/sorting translation.

**Rationale**: EF Core can translate filters/sorts on simple DTO properties back to entity properties, but complex expressions may not translate.

### 6. Validate Return Types at Build Time

**Practice**: Rely on compiler type checking to enforce DTO-only returns.

**Rationale**: Catches violations at compile time, preventing runtime entity leakage.

---

## References

### Hot Chocolate Official Documentation

- [Projections](https://chillicream.com/docs/hotchocolate/v13/data-fetching/projections)
- [Pagination](https://chillicream.com/docs/hotchocolate/v13/fetching-data/pagination)
- [Filtering](https://chillicream.com/docs/hotchocolate/v13/data-fetching/filtering)
- [Sorting](https://chillicream.com/docs/hotchocolate/v13/data-fetching/sorting)

### GraphQL Specifications

- [GraphQL Cursor Connections Specification](https://relay.dev/graphql/connections.htm) (Relay)

### Implementation Task References

- **Task 4.1**: Research findings on cursor pagination approach - `.apm/Memory/Phase_04_filtering-sorting-pagination-integration/Task_4_1_Research_Cursor_Pagination_Approach_and_Apply_Filtering_Sorting_Pagination.md`
- **Task 4.2**: DTO-only return path verification - `.apm/Memory/Phase_04_filtering-sorting-pagination-integration/Task_4_2_Verify_DTO_only_Return_Paths.md`

### Related Files

- Query Resolvers: `StuffTracker.Api/GraphQL/Query.cs`
- Mutation Resolvers: `StuffTracker.Api/GraphQL/Mutation.cs`
- GraphQL Configuration: `StuffTracker.Api/Program.cs`
- DTO Types: `StuffTracker.Api/GraphQL/Types/*.cs`

---

## Document History

- **Created**: Task 6.1 - Document EF/GQL separation limitations
- **Last Updated**: 2025-01-XX
- **Author**: Agent_Docs

