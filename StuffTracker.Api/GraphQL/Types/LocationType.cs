using HotChocolate.Types;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Types;

/// <summary>
/// GraphQL type configuration for Location.
/// Maps from LocationEntity (EF) to Location schema type.
/// This enables UseProjection to work at the DB level while exposing a clean GraphQL schema.
/// </summary>
public class LocationType : ObjectType<LocationEntity>
{
    protected override void Configure(IObjectTypeDescriptor<LocationEntity> descriptor)
    {
        descriptor.Name("Location");
        
        // Let Hot Chocolate infer types from the entity properties
        descriptor.Field(l => l.Id);
        descriptor.Field(l => l.Name);
        descriptor.Field(l => l.CreatedAt);
        
        // Ignore navigation properties that we don't want to expose
        descriptor.Ignore(l => l.Rooms);
    }
}

