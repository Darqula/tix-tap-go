using System.ComponentModel.DataAnnotations;

namespace EventService.DTO;

public record CreateEventRequest
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(1024)]
    public string Description { get; set; } = null!;

    [Required]
    public DateTimeOffset? Start { get; set; }

    [Required]
    [MaxLength(256)]
    public string Location { get; set; } = null!;

    public IEnumerable<CreateAttendeeGroupRequest>? AttendeeGroups { get; set; }
}
