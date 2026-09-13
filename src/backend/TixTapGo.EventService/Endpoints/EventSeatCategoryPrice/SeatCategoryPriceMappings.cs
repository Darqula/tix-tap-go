using Riok.Mapperly.Abstractions;

using TixTapGo.EventService.Endpoints.EventSeatCategoryPrice.DTO;
using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.Endpoints.EventSeatCategoryPrice;

[Mapper]
internal static partial class SeatCategoryPriceMappings
{
    [MapperIgnoreSource(nameof(VenueSeatCategory.CreatedAt))]
    [MapperIgnoreSource(nameof(VenueSeatCategory.UpdatedAt))]
    [MapperIgnoreSource(nameof(VenueSeatCategory.IsDeleted))]
    [MapperIgnoreSource(nameof(VenueSeatCategory.DomainEvents))]
    [MapperIgnoreSource(nameof(VenueSeatCategory.Prices))]
    public static partial GetVenueSeatCategoryResponse ToGetVenueSeatCategoryResponse(this VenueSeatCategory entity);
    
    [MapperIgnoreSource(nameof(SeatCategoryPrice.CreatedAt))]
    [MapperIgnoreSource(nameof(SeatCategoryPrice.UpdatedAt))]
    [MapperIgnoreSource(nameof(SeatCategoryPrice.IsDeleted))]
    [MapperIgnoreSource(nameof(SeatCategoryPrice.DomainEvents))]
    [MapperIgnoreSource(nameof(SeatCategoryPrice.Event))]
    [MapperIgnoreSource(nameof(SeatCategoryPrice.VenueSeatCategoryId))]
    public static partial GetSeatCategoryPriceResponse ToGetSeatCategoryPriceResponse(this SeatCategoryPrice entity);

    public static partial IQueryable<GetSeatCategoryPriceResponse> ProjectToSeatCategoryResponses(
        this IQueryable<SeatCategoryPrice> query);
}
