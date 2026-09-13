using System.ComponentModel.DataAnnotations;

using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Endpoints.Event.DTO;

public sealed record UpdateEventRequest
{
    [MaxLength(128)]
    public string? Title { get; set; }

    [MaxLength(1024)]
    public string? Description { get; set; }
    
    public DateTimeOffset? Start { get; set; }
    
    public DateTimeOffset? End { get; set; }
    
    public EventStatus? Status { get; set; }
}
