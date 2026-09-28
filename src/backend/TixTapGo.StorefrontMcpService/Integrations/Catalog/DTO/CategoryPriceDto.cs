namespace TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

internal sealed record CategoryPriceDto
{
    public required Guid EventId { get; init; }
    public required bool IsEnabled { get; init; }
    public required int Capacity { get; init; }
    public required string SeatAssignmentType { get; init; }
    public required decimal BasePrice { get; init; }
    public required SeatCategoryDto VenueSeatCategory { get; init; }
    public bool IsOnSale => IsEnabled && Capacity > 0;
}
