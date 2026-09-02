using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.VenueService.Entities;

internal sealed class VenueSeat : EntityBase
{
    public required Guid VenueSeatMapVersionId { get; set; }
    public VenueSeatingMapVersion VenueSeatingMapVersion { get; set; } = null!;

    public required Guid CategoryId { get; set; }
    public SeatCategory Category { get; set; } = null!;

    public required string RowNumber { get; set; }

    public required string SeatNumber { get; set; }

    public decimal? MapPositionX { get; set; }
    public decimal? MapPositionY { get; set; }
}
