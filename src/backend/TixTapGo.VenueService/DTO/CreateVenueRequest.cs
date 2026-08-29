using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.DTO;

public record CreateVenueRequest
{
    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(256)]
    public required string Address { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }
}
