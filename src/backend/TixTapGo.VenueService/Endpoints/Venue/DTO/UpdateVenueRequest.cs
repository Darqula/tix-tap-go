using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.Venue.DTO;

public record UpdateVenueRequest
{
    [MaxLength(128)]
    public string? Title { get; set; }

    [MaxLength(256)]
    public string? Address { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }
}
