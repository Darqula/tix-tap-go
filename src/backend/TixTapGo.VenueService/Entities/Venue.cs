using TixTapGo.Shared.Entities;

namespace TixTapGo.VenueService.Entities;

internal sealed class Venue : EntityBase
{
    public required string Title { get; set; }
    public required string Address { get; set; }
    public string? Description { get; set; }

    public Guid? CurrentSeatingMapId { get; set; }
    public VenueSeatingMapVersion? CurrentSeatingMap { get; set; }

    public IEnumerable<VenueSeatingMapVersion> SeatingMapVersions { get; } = new List<VenueSeatingMapVersion>();
}
