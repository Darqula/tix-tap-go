using System.ComponentModel.DataAnnotations;

using TixTapGo.Shared.Validation;

namespace TixTapGo.EventService.DTO;

public sealed record UpdateEventRequest
{
    [MaxLength(128)]
    public string? Title { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }

    [FutureDate]
    public DateTimeOffset? Start { get; set; }

    [MaxLength(256)]
    public string? Location { get; set; }
}
