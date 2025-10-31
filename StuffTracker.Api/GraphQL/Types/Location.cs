namespace StuffTracker.Api.GraphQL.Types;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
