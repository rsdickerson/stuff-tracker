namespace StuffTracker.Api.GraphQL.Types;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Optional nested DTO for GraphQL resolution
    public Location? Location { get; set; }
}
