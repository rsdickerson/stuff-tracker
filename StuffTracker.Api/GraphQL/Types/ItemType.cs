using HotChocolate.Types;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Types;

/// <summary>
/// GraphQL type configuration for Item.
/// Maps from ItemEntity (EF) to Item schema type.
/// This enables UseProjection to work at the DB level while exposing a clean GraphQL schema.
/// </summary>
public class ItemType : ObjectType<ItemEntity>
{
    protected override void Configure(IObjectTypeDescriptor<ItemEntity> descriptor)
    {
        descriptor.Name("Item");
        
        // Let Hot Chocolate infer types from the entity properties
        descriptor.Field(i => i.Id);
        descriptor.Field(i => i.Name);
        descriptor.Field(i => i.Quantity);
        descriptor.Field(i => i.RoomId);
        descriptor.Field(i => i.CreatedAt);
        
        // Optional: expose nested Room via navigation property
        descriptor
            .Field(i => i.Room)
            .Type<RoomType>();
    }
}
