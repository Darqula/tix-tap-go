namespace TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

internal sealed record VenueDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
}
