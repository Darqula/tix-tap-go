namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion.DTO;

public record GetSeatingMapVersionResponse
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public string? Description { get; init; }
    public string? MapUrl { get; init; }
    public DateTimeOffset? ValidFrom { get; init; }
    public DateTimeOffset? ValidToExclusive { get; init; }
    public bool IsDraft { get; init; }
}
