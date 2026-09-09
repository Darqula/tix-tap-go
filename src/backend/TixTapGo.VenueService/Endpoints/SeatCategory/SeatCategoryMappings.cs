using Riok.Mapperly.Abstractions;

using TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

namespace TixTapGo.VenueService.Endpoints.SeatCategory;

[Mapper]
internal static partial class SeatCategoryMappings
{
    [MapperIgnoreSource(nameof(Entities.SeatCategory.IsDeleted))]
    [MapperIgnoreSource(nameof(Entities.SeatCategory.CreatedAt))]
    [MapperIgnoreSource(nameof(Entities.SeatCategory.UpdatedAt))]
    [MapperIgnoreSource(nameof(Entities.SeatCategory.DomainEvents))]
    [MapperIgnoreSource(nameof(Entities.SeatCategory.Venue))]
    public static partial GetSeatCategoryResponse ToGetSeatCategoryResponse(this Entities.SeatCategory entity);

    public static partial IQueryable<GetSeatCategoryResponse> ProjectToGetSeatCategoryResponse(
        this IQueryable<Entities.SeatCategory> query);

    [MapperIgnoreTarget(nameof(Entities.SeatCategory.Id))]
    public static partial Entities.SeatCategory ToEntity(this CreateSeatCategoryRequest dto, Guid venueId);
}
