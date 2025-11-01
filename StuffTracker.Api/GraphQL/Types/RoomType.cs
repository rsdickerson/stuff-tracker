using HotChocolate.Types;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Types;

/// <summary>
/// GraphQL type configuration for Room.
/// Maps from RoomEntity (EF) to Room schema type.
/// This enables UseProjection to work at the DB level while exposing a clean GraphQL schema.
/// </summary>
public class RoomType : ObjectType<RoomEntity>
{
    protected override void Configure(IObjectTypeDescriptor<RoomEntity> descriptor)
    {
        descriptor.Name("Room");
        
        // Let Hot Chocolate infer types from the entity properties
        descriptor.Field(r => r.Id);
        descriptor.Field(r => r.Name);
        descriptor.Field(r => r.LocationId);
        descriptor.Field(r => r.CreatedAt);
        
        // Optional: expose nested Location via navigation property
        descriptor
            .Field(r => r.Location)
            .Type<LocationType>();
        
        // Ignore Items collection to avoid over-fetching
        descriptor.Ignore(r => r.Items);
    }
}

