namespace StuffTracker.Domain.Entities;

public class ItemEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RoomId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public RoomEntity Room { get; set; } = null!;
}

