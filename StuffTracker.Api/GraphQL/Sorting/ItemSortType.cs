using HotChocolate.Data.Sorting;
using StuffTracker.Domain.Entities;

namespace StuffTracker.Api.GraphQL.Sorting;

/// <summary>
/// Custom sort type for Item entities.
/// Ensures deterministic pagination by making Id available as an explicit sort field.
/// Client should include 'id: ASC' in sort order for stable pagination: order: { quantity: DESC, name: ASC, id: ASC }
/// </summary>
public class ItemSortType : SortInputType<ItemEntity>
{
    protected override void Configure(ISortInputTypeDescriptor<ItemEntity> descriptor)
    {
        descriptor.Name("ItemSortInput");
        
        // Bind all fields implicitly (Name, Quantity, RoomId, CreatedAt, etc.)
        descriptor.BindFieldsImplicitly();
        
        // Make Id explicitly available for sorting as a tiebreaker
        descriptor.Field(i => i.Id).Name("id");
    }
}

