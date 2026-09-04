using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion.DTO;

public record UpdateSeatingMapVersionDraftRequest
{
    [MaxLength(256)]
    public string? Description { get; init; }
    [MaxLength(256)]
    public string? MapUrl { get; init; }
}
