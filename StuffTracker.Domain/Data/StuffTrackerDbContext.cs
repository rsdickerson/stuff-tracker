using Microsoft.EntityFrameworkCore;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Domain.Data;

public class StuffTrackerDbContext : DbContext
{
    public StuffTrackerDbContext(DbContextOptions<StuffTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<LocationEntity> Locations { get; set; } = null!;
    public DbSet<RoomEntity> Rooms { get; set; } = null!;
    public DbSet<ItemEntity> Items { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure LocationEntity
        modelBuilder.Entity<LocationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Navigation: One Location to Many Rooms
            entity.HasMany(e => e.Rooms)
                .WithOne(e => e.Location)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure RoomEntity
        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.LocationId)
                .IsRequired();
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Foreign key relationship to Location
            entity.HasOne(e => e.Location)
                .WithMany(e => e.Rooms)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Navigation: One Room to Many Items
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Room)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index on LocationId for query performance
            entity.HasIndex(e => e.LocationId);
        });

        // Configure ItemEntity
        modelBuilder.Entity<ItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Quantity)
                .IsRequired();
            entity.Property(e => e.RoomId)
                .IsRequired();
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Foreign key relationship to Room
            entity.HasOne(e => e.Room)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Indexes for query performance
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Quantity);
            entity.HasIndex(e => e.RoomId);
        });
    }
}

