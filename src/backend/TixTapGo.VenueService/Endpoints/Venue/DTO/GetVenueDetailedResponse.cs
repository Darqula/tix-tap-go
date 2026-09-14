using TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

namespace TixTapGo.VenueService.Endpoints.Venue.DTO;

public record GetVenueDetailedResponse : GetVenueResponse
{
    public Guid? SeatingMapVersionId { get; init; }
    public int? SeatingMapTotalSeats { get; init; }
    public required List<GetSeatCategoryResponse> Categories { get; init; }
}
