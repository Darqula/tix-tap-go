using System.ComponentModel.DataAnnotations;

namespace EventService.DTO;

public sealed record UpdateEventRequest
{
    [MaxLength(128)]
    public string? Title { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }

    public DateTimeOffset? Start { get; set; }

    [MaxLength(256)]
    public string? Location { get; set; }
}
