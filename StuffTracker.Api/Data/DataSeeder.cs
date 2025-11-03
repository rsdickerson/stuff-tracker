using Microsoft.EntityFrameworkCore;
using StuffTracker.Domain.Data;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StuffTrackerDbContext>();

        // Note: In test environment, tables may be truncated before seeding
        // So we don't check for existing data - we always seed if called
        // The check below would prevent seeding after truncate
        // if (await dbContext.Locations.AnyAsync())
        // {
        //     return; // Data already seeded
        // }

        var now = DateTime.UtcNow;
        var locations = new List<LocationEntity>
        {
            new LocationEntity { Name = "Home", CreatedAt = now },
            new LocationEntity { Name = "Rental 101 Howards Ave", CreatedAt = now },
            new LocationEntity { Name = "Flip 3231 Gooseneck Rd", CreatedAt = now }
        };

        await dbContext.Locations.AddRangeAsync(locations);
        await dbContext.SaveChangesAsync();

        // Define room names for each location (4 rooms per location)
        var roomNamesByLocation = new Dictionary<string, List<string>>
        {
            { "Home", new List<string> { "Garage", "Basement", "Attic", "Office" } },
            { "Rental 101 Howards Ave", new List<string> { "Living Room", "Kitchen", "Bedroom", "Bathroom" } },
            { "Flip 3231 Gooseneck Rd", new List<string> { "Workshop", "Storage", "Main Floor", "Garage" } }
        };

        var rooms = new List<RoomEntity>();
        foreach (var location in locations)
        {
            var roomNames = roomNamesByLocation[location.Name];
            foreach (var roomName in roomNames)
            {
                rooms.Add(new RoomEntity
                {
                    Name = roomName,
                    LocationId = location.Id,
                    CreatedAt = now
                });
            }
        }

        await dbContext.Rooms.AddRangeAsync(rooms);
        await dbContext.SaveChangesAsync();

        // Generate items with diverse naming patterns for filtering tests
        var items = new List<ItemEntity>();
        
        // Item naming patterns for filter variety
        var prefixes = new[] { "Electronics-", "electronics-", "ELECTRONICS-", "Furniture-", "furniture-", "Kitchen-", "kitchen-", "Tools-", "tools-", "Office Supplies-", "Office supplies-", "office_supplies-" };
        var substrings = new[] { "lamp", "chair", "box", "table", "desk", "shelf", "cabinet", "drawer", "couch", "TV", "radio", "speaker", "microwave", "oven", "refrigerator", "fan", "clock", "mirror", "picture", "book", "tool", "hammer", "screwdriver", "wrench" };
        var categories = new[] { "Electronics-TV", "electronics-Radio", "ELECTRONICS-Speaker", "Furniture-Chair", "furniture-Table", "Kitchen-Microwave", "kitchen-Oven", "Tools-Hammer", "tools-Screwdriver", "Office Supplies-Pen", "Office supplies-Notebook", "office_supplies-Stapler" };

        var itemIndex = 0;
        var random = new Random(42); // Fixed seed for deterministic data

        // Create at least 200 items distributed across all rooms
        var targetItemsPerRoom = 200 / rooms.Count;
        var remainder = 200 % rooms.Count;
        var roomIndex = 0;
        
        foreach (var room in rooms)
        {
            // Distribute remainder items across first few rooms
            var itemsPerRoom = targetItemsPerRoom + (roomIndex < remainder ? 1 : 0);

            for (int i = 0; i < itemsPerRoom; i++)
            {
                string itemName;
                
                // Use different naming patterns for variety
                if (itemIndex % 3 == 0)
                {
                    // Prefix pattern
                    var prefix = prefixes[itemIndex % prefixes.Length];
                    var substring = substrings[itemIndex % substrings.Length];
                    itemName = $"{prefix}{substring}";
                }
                else if (itemIndex % 3 == 1)
                {
                    // Category pattern with mixed casing
                    itemName = categories[itemIndex % categories.Length];
                }
                else
                {
                    // Standalone substring pattern
                    itemName = substrings[itemIndex % substrings.Length];
                }

                // Add variations to increase uniqueness
                if (itemIndex > 50)
                {
                    itemName = $"{itemName} {itemIndex}";
                }

                items.Add(new ItemEntity
                {
                    Name = itemName,
                    Quantity = random.Next(0, 51), // Range 0-50
                    RoomId = room.Id,
                    CreatedAt = now.AddSeconds(itemIndex) // Slight variation in timestamps
                });

                itemIndex++;
            }
            
            roomIndex++;
        }

        await dbContext.Items.AddRangeAsync(items);
        await dbContext.SaveChangesAsync();
    }
}

