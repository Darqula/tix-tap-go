using System.Linq.Expressions;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.VenueService.Entities;

internal sealed class VenueSeatingMapVersion : EntityBase
{
    public static readonly Expression<Func<VenueSeatingMapVersion, bool>> IsCurrentActive =
        v => !v.IsDraft && v.ValidToExclusive == null;

    public static readonly Func<VenueSeatingMapVersion, bool> IsCurrentActiveCompiled = IsCurrentActive.Compile();

    public required Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public string? Description { get; set; }
    public string? MapUrl { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidToExclusive { get; set; }
    public bool IsDraft { get; set; } = true;

    private readonly List<VenueSeat>? _seats = null!;

    public List<VenueSeat> Seats
    {
        get => _seats ?? throw new InvalidOperationException("Seats are not loaded");
    }
}
