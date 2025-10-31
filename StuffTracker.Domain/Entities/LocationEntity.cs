namespace StuffTracker.Domain.Entities;

public class LocationEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public ICollection<RoomEntity> Rooms { get; set; } = new List<RoomEntity>();
}

