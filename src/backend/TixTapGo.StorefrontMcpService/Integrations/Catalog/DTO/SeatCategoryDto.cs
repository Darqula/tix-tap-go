namespace TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

internal sealed record SeatCategoryDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
}
