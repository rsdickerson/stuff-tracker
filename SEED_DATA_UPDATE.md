# Seed Data Update Summary

## Changes Made

The seed data has been updated to reflect real-world property management scenarios.

### Previous Locations
- Cary
- Lake House
- Storage

### New Locations
1. **Home**
2. **Rental 101 Howards Ave**
3. **Flip 3231 Gooseneck Rd**

---

## Updated Room Structure

### Home (Location ID: 1)
- Garage
- Basement
- Attic
- Office

### Rental 101 Howards Ave (Location ID: 2)
- Living Room
- Kitchen
- Bedroom
- Bathroom

### Flip 3231 Gooseneck Rd (Location ID: 3)
- Workshop
- Storage
- Main Floor
- Garage

---

## Items

The seeder still generates **~200 test items** distributed across all 12 rooms. Items include various naming patterns for testing filtering and sorting:

- Electronics (TV, Radio, Speaker, etc.)
- Furniture (Chair, Table, Desk, etc.)
- Kitchen items (Microwave, Oven, etc.)
- Tools (Hammer, Screwdriver, Wrench, etc.)
- Office Supplies (Pen, Notebook, Stapler, etc.)

Each item has:
- Unique name (with variations and patterns)
- Quantity (0-50, randomly assigned)
- Room assignment
- Creation timestamp

---

## Files Updated

### 1. StuffTracker.Api/Data/DataSeeder.cs
**Changes:**
- Updated `locations` list to new property names
- Updated `roomNamesByLocation` dictionary with new location names and appropriate room names
- Item generation logic remains unchanged (~200 items with diverse patterns)

### 2. README_Nitro.md
**Changes:**
- Updated query response examples to show new location names
- Updated mutation examples:
  - `addLocation` now creates "Storage Unit 5B" (ID: 4) instead of "Garage"
  - `addRoom` now creates "Office" in Home (ID: 1)
  - `addItem` now creates "Winter Clothes" in Basement (room ID: 2)
- All expected responses reflect the new 3-location structure

### 3. README_Postman.md
**Changes:**
- Updated mutation examples:
  - `addRoom` now creates "Office" in Home with note about location IDs (1 = Home, 2 = Rental, 3 = Flip)
  - `addItem` now creates "Winter Clothes" in room ID 2
- Filter examples now reference "Home" instead of generic location names

---

## How to Apply Changes

### 1. Reset the Database (Recommended)

To see the new seed data, reset your database:

```bash
docker-compose down -v
docker-compose up -d
cd StuffTracker.Api
dotnet run
```

The application will automatically:
1. Apply migrations
2. Seed the database with new locations, rooms, and items

### 2. Verify Seed Data

Query all locations to verify:

```graphql
query {
  locations(first: 10, order: { name: ASC }) {
    nodes {
      id
      name
      createdAt
    }
  }
}
```

Expected result:
- ID 1: "Flip 3231 Gooseneck Rd"
- ID 2: "Home"
- ID 3: "Rental 101 Howards Ave"

(Ordered alphabetically by name)

### 3. Test Queries

Try filtering by location name:

```graphql
query {
  locations(
    first: 10
    where: { name: { contains: "Rental" } }
  ) {
    nodes {
      id
      name
    }
  }
}
```

Expected result: "Rental 101 Howards Ave"

---

## Benefits of New Structure

### 1. Real-World Relevance
The new locations represent actual property management scenarios:
- **Home** - Personal residence with typical home storage areas
- **Rental Property** - Investment property with tenant furniture/appliances
- **Flip Property** - Renovation project with tools and materials

### 2. Better Testing Scenarios
- Test queries across different property types
- Demonstrate multi-property management
- Realistic room names for each location type

### 3. Clear Documentation Examples
- Examples now use recognizable property types
- Easier to understand query/mutation context
- Better demonstrations for real-world use cases

---

## API Behavior

### Seeding Logic
- **Development Environment:** Automatically seeds on startup if database is empty
- **Test Environment:** Seeding is disabled; tests manage their own data
- **Check Logic:** Currently commented out to allow re-seeding after table truncation

### Item Distribution
- **Total Items:** ~200 items
- **Distribution:** Evenly distributed across 12 rooms (~16-17 items per room)
- **Quantities:** Random values between 0-50
- **Timestamps:** Slight variations for realistic data

---

## Testing Considerations

The seed data provides excellent test coverage for:

✅ **Filtering**
- Name contains (case-insensitive)
- Quantity ranges (gt, gte, lt, lte, eq, neq)
- Room/Location ID matching
- Multiple filter combinations

✅ **Sorting**
- Alphabetical sorting (location/room/item names)
- Numeric sorting (quantities, IDs)
- Multiple sort fields
- ASC/DESC directions

✅ **Pagination**
- Forward pagination (`first`, `after`)
- Backward pagination (`last`, `before`)
- Cursor stability across pages
- HasNextPage/HasPreviousPage logic

✅ **Relationships**
- Locations → Rooms (1-to-many)
- Rooms → Items (1-to-many)
- Location → Room → Item navigation

---

## Next Steps

1. ✅ Database seed data updated
2. ✅ Documentation updated (Nitro and Postman guides)
3. ⏭️ Consider creating additional mutation examples for:
   - Moving items between properties
   - Bulk operations
   - Property-specific reports

---

**Last Updated:** November 3, 2025

