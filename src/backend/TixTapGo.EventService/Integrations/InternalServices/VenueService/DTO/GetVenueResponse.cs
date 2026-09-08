namespace TixTapGo.EventService.Integrations.InternalServices.VenueService.DTO;

public record GetVenueResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public Guid? SeatingMapVersionId { get; init; }
}
