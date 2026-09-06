using Riok.Mapperly.Abstractions;

using TixTapGo.VenueService.Endpoints.Seat.DTO;
using TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.Endpoints.Seat;

[Mapper]
internal static partial class SeatMappings
{
    public static partial IQueryable<GetSeatResponse> ProjectToGetSeatResponse(this IQueryable<VenueSeat> query);

    [MapperIgnoreSource(nameof(VenueSeat.IsDeleted))]
    [MapperIgnoreSource(nameof(VenueSeat.CreatedAt))]
    [MapperIgnoreSource(nameof(VenueSeat.UpdatedAt))]
    [MapperIgnoreSource(nameof(VenueSeat.Category))]
    [MapperIgnoreSource(nameof(VenueSeat.SeatingMapVersion))]
    public static partial GetSeatResponse ToGetSeatResponse(this VenueSeat seat);

    [MapperIgnoreTarget(nameof(VenueSeat.Id))]
    [MapperIgnoreTarget(nameof(VenueSeat.Category))]
    [MapperIgnoreTarget(nameof(VenueSeat.SeatingMapVersion))]
    [MapperIgnoreSource(nameof(ParsedSeatDto.Category))]
    [MapperIgnoreSource(nameof(ParsedSeatDto.SheetRowNumber))]
    public static partial VenueSeat ToEntity(this ParsedSeatDto seatDto, Guid seatingMapVersionId, Guid categoryId);
}
