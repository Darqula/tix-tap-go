using TixTapGo.Shared.Entities;

namespace TixTapGo.VenueService.Entities;

internal sealed class VenueSeatingMapVersion : EntityBase
{
    public required Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public string? Description { get; set; }
    public required string? MapUrl { get; set; }
    public required DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidToExclusive { get; set; }
}
