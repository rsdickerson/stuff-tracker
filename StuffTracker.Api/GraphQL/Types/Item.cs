namespace StuffTracker.Api.GraphQL.Types;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RoomId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Optional nested DTO for GraphQL resolution
    public Room? Room { get; set; }
}
