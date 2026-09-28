namespace TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

internal sealed record EventDto
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}
