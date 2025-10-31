namespace StuffTracker.Domain.Entities;

public class RoomEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public LocationEntity Location { get; set; } = null!;
    public ICollection<ItemEntity> Items { get; set; } = new List<ItemEntity>();
}

