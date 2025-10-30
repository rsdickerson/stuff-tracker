# StuffTracker – Implementation Plan 

**Memory Strategy:** To be defined by Manager Agent during Memory Root Creation
**Last Modification:** Initial enhancement by Setup Agent
**Project Overview:** Backend-only web application using .NET 8, Hot Chocolate (latest stable), and EF Core with MySQL. Demonstrates core GraphQL features (queries, mutations, filtering, pagination, sorting) while strictly separating EF entities from GraphQL DTOs. Includes comprehensive seed data and endpoint tests. Limitations discovered during projection/separation will be documented.

## Phase 1: Project Scaffolding and Infrastructure

### Task 1.1 – Initialize solution and projects │ Agent_API_Backend
- **Objective:** Create the repository structure and projects with correct references for API, Domain, and Tests.
- **Output:** Solution with projects: `StuffTracker.Domain`, `StuffTracker.Api`, `StuffTracker.Tests`; project references; base folders created.
- **Guidance:** Use .NET 8 templates; keep Domain independent (no Hot Chocolate refs) and referenced by API.
- Sub-tasks:
  - Create solution and three projects.
  - Reference Domain from Api; add base folders (`Domain/Entities`, `Api/GraphQL`, `Api/Data`, `Tests/Integration`).
  - Commit baseline.

### Task 1.2 – Add dependencies and configure build/test tooling │ Agent_API_Backend
- **Objective:** Add Hot Chocolate, EF Core MySQL provider, and testing packages with compiler safety settings.
- **Output:** Package references and configuration in `Api` and `Tests` projects; analyzers/nullable enabled.
- **Guidance:** Include Projections, Filtering, Sorting, Paging; use `Pomelo.EntityFrameworkCore.MySql`; xUnit + `WebApplicationFactory`.
- Sub-tasks:
  - Add Hot Chocolate server and data middleware packages.
  - Add EF Core MySQL provider and tools.
  - Add xUnit, `Microsoft.AspNetCore.Mvc.Testing`, assertion helpers; enable nullable and warnings.

## Phase 2: Domain, EF Core, MySQL, Migrations, and Seeding

### Task 2.1 – Define EF entities and DbContext │ Agent_Domain_Data
- **Objective:** Model locations, rooms, and items with relationships and indexes.
- **Output:** `LocationEntity`, `RoomEntity`, `ItemEntity`; `StuffTrackerDbContext` with DbSets and Fluent configs.
- **Guidance:** Use Fluent API for keys/FKs/indexes; navigation properties for collections; design-time factory for tooling.
- Sub-tasks:
  - Implement entities and navigations; add Fluent configurations.
  - Add `StuffTrackerDbContext` and design-time factory.

### Task 2.2 – Configure MySQL and migrations │ Agent_Domain_Data
- **Objective:** Wire up MySQL connection and create initial schema.
- **Output:** Connection config, initial EF migration, and applied database.
- **Guidance:** Depends on: Task 2.1 Output. Configure DI with server version; provide dev settings and migration tooling.
- Steps:
  1. Configure connection and DbContext in DI.
  2. Create initial migration and update the database.

### Task 2.3 – Implement comprehensive seed data │ Agent_Domain_Data
- **Objective:** Seed deterministic data to fully exercise filtering, sorting, and pagination.
- **Output:** Seeder executed at startup or via explicit seeding routine; database populated.
- **Guidance:** Depends on: Task 2.2 Output. Ensure stable expectations for tests; guard against reseeding if data exists.
- Steps:
  1. Define dataset: 3 locations (Cary, Lake House, Storage); 4 rooms per location; ≥200 items across rooms.
  2. Item naming scheme for filter variety (prefixes/substrings/categories) with mixed casing; numeric quantities 0–50.
  3. Implement deterministic seeding routine and integrate into startup path.

## Phase 3: GraphQL Schema, Types, and Resolvers

### Task 3.1 – Define GraphQL DTO types separate from EF entities │ Agent_API_Backend
- **Objective:** Create DTOs for GraphQL that mirror schema without exposing EF entities.
- **Output:** DTO classes `Location`, `Room`, `Item` and optional type extensions for nested fields.
- **Guidance:** Depends on: Task 2.1 Output. Align names/fields for projections; avoid EF-only fields.
- Sub-tasks:
  - Implement DTO classes.
  - Add type extensions if needed for nested resolvers.

### Task 3.2 – Configure Hot Chocolate server and schema │ Agent_API_Backend
- **Objective:** Register schema and middleware for projections, filtering, sorting, and paging.
- **Output:** Program configuration registering Query/Mutation types and DbContext factory.
- **Guidance:** Depends on: Task 1.2 Output. Enable Nitro/Playground in dev only.
- Sub-tasks:
  - Register GraphQL server with data middlewares.
  - Register Query/Mutation types and DbContext factory.

### Task 3.3 – Implement queries and mutations using projections │ Agent_API_Backend
- **Objective:** Provide core operations over EF-backed data with DTO projections.
- **Output:** Query and Mutation resolvers returning DTOs via projections.
- **Guidance:** Depends on: Task 3.1 Output. Use `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, `[UsePaging]` as appropriate; validate existence on mutations.
- Steps:
  1. Implement queries: `locations`, `location(id)`, `items(search)` over IQueryable sources.
  2. Implement mutations: addLocation, addRoom, addItem, moveItem, deleteItem; return projected DTOs.

## Phase 4: Filtering, Sorting, Pagination Integration

### Task 4.1 – Apply filtering, sorting, and pagination │ Agent_API_Backend
- **Objective:** Decorate resolvers/fields to enable rich querying capabilities.
- **Output:** Filtering on text/numeric fields; sorting by name/quantity; paging on collections.
- **Guidance:** Depends on: Task 3.3 Output. Ensure stable default ordering for pagination tests.
- Sub-tasks:
  - Apply filtering/sorting attributes on relevant resolvers.
  - Configure cursor pagination for items and rooms.

### Task 4.2 – Verify DTO-only return paths (no EF leakage) │ Agent_API_Backend
- **Objective:** Ensure resolvers never return EF entities directly.
- **Output:** Verified return types and projection configuration across resolvers.
- **Guidance:** Depends on: Task 4.1 Output. Spot-check via type assertions or compile-time checks.
- Sub-tasks:
  - Audit resolver return types and projections.
  - Add guard tests if practical.

## Phase 5: Endpoint Testing

### Task 5.1 – Build endpoint test harness │ Agent_Testing
- **Objective:** Execute GraphQL over HTTP using a test server and assert results.
- **Output:** `WebApplicationFactory`-based harness with helpers for sending GraphQL and asserting responses.
- **Guidance:** Depends on: Task 3.2 Output. Isolate schema/data per test run; prefer dedicated test database or schema.
- Steps:
  1. Configure `WebApplicationFactory` with overridden connection for tests (separate MySQL schema).
  2. Implement helper to post GraphQL documents and parse JSON responses reliably.

### Task 5.2 – Filtering/sorting/pagination tests over seeded data │ Agent_Testing
- **Objective:** Validate query capabilities across text and numeric fields and page boundaries.
- **Output:** Integration tests covering contains/startsWith/case, numeric ranges, asc/desc sorting, and cursor pagination.
- **Guidance:** Depends on: Task 5.1 Output; Depends on: Task 2.3 Output.
- Steps:
  1. Filtering tests over `Item.name`, `Item.quantity`, `Room.name` with deterministic expected sets.
  2. Sorting tests for asc/desc on name and quantity; verify tie-breaking for determinism.
  3. Pagination tests validating `edges`/`pageInfo`, next/prev pages, and stable ordering.

### Task 5.3 – Mutation tests (create/move/delete) │ Agent_Testing
- **Objective:** Ensure mutations persist and return projected DTOs.
- **Output:** Integration tests for add/move/delete flows with follow-up queries.
- **Guidance:** Depends on: Task 5.1 Output.
- Sub-tasks:
  - Add item, move item, delete item; assert subsequent query reflects changes and DTO shape.

## Phase 6: Limitations Documentation

### Task 6.1 – Document EF/GQL separation limitations │ Agent_Docs
- **Objective:** Record constraints/trade-offs when enforcing strict separation with Hot Chocolate.
- **Output:** `docs/HotChocolate-Limitations.md` with citations.
- **Guidance:** Depends on: Task 4.2 Output. Include references to official docs and observed behaviors.
- Sub-tasks:
  - Summarize projection caveats and any unsupported attribute combinations.
  - Cite official docs and decisions.

---
References:
- Hot Chocolate Documentation (v13 index; use latest stable concepts): https://chillicream.com/docs/hotchocolate/v13

