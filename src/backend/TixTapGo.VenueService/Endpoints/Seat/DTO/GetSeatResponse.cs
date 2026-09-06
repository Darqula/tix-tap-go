namespace TixTapGo.VenueService.Endpoints.Seat.DTO;

public record GetSeatResponse()
{
    public required Guid Id { get; init; }
    public required Guid SeatingMapVersionId { get; init; }
    public required Guid CategoryId { get; init;  }
    public required string RowNumber { get; init; }
    public required string SeatNumber { get; init; }
    public decimal? MapPositionX { get; init; }
    public decimal? MapPositionY { get; init; }
}
