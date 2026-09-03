namespace TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

public record GetSeatCategoryResponse
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public required string Title { get; init; }
    public required string Color { get; init; }
}
