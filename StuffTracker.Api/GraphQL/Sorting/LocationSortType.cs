using HotChocolate.Data.Sorting;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Sorting;

/// <summary>
/// Custom sort type for Location entities.
/// Ensures deterministic pagination by making Id available as an explicit sort field.
/// Client should include 'id: ASC' in sort order for stable pagination: order: { name: ASC, id: ASC }
/// </summary>
public class LocationSortType : SortInputType<LocationEntity>
{
    protected override void Configure(ISortInputTypeDescriptor<LocationEntity> descriptor)
    {
        descriptor.Name("LocationSortInput");
        
        // Bind all fields implicitly (Name, CreatedAt, etc.)
        descriptor.BindFieldsImplicitly();
        
        // Make Id explicitly available for sorting as a tiebreaker
        descriptor.Field(l => l.Id).Name("id");
    }
}

