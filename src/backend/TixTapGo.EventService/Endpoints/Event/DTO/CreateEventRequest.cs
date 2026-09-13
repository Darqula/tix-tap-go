using System.ComponentModel.DataAnnotations;

using TixTapGo.Shared.Validation;

namespace TixTapGo.EventService.Endpoints.Event.DTO;

public sealed record CreateEventRequest
{
    [Required]
    public Guid VenueId { get; set; }
    
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(1024)]
    public string Description { get; set; } = null!;

    [Required]
    [FutureDate]
    public DateTimeOffset? Start { get; set; }
    
    [Required]
    [FutureDate]
    public DateTimeOffset? End { get; set; }
}
