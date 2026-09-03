namespace TixTapGo.VenueService.Endpoints.Venue.DTO;

public record GetVenueResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
    public string? Description { get; init; }
    public Guid? SeatingMapVersionId { get; init; }
}
