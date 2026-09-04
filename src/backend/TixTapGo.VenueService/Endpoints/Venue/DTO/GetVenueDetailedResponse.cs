namespace TixTapGo.VenueService.Endpoints.Venue.DTO;

public record GetVenueDetailedResponse : GetVenueResponse
{
    public Guid? SeatingMapVersionId { get; init; }
}
