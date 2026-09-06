using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion.DTO;

public record CreateSeatingMapVersionDraftRequest
{
    [MaxLength(256)]
    public string? Description { get; init; }
    [MaxLength(256)]
    [Url]
    public string? MapUrl { get; init; }
}
