using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.VenueService.Entities;

internal sealed class SeatCategory(Guid venueId) : EntityBase
{
    public Guid VenueId { get; private set; } = venueId;
    public Venue Venue { get; private set; } = null!;
    public required string Title { get; set; }
    public required string Color { get; set; }
}
