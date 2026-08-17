using System.ComponentModel.DataAnnotations;
using EventService.Entities;

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
    public DateTimeOffset Start { get; set; }
    [Required]
    [MaxLength(256)]
    public string Location { get; set; } = null!;
    public IEnumerable<CreateAttendeeGroupRequest>? AttendeeGroups { get; set; }

    public Event ToModel()
    {
        var attendeeGroups = new List<AttendeeGroup>();
        var @event = new Event()
        {
            Id = Guid.Empty,
            Title =  Title,
            Description = Description,
            Location = Location,
            Start =  Start,
            AttendeeGroups = attendeeGroups
        };

        if (AttendeeGroups != null && AttendeeGroups.Any())
        {
            attendeeGroups.AddRange(AttendeeGroups.Select(groupDto => groupDto.ToModel(@event)));
        }

        return @event;
    }
}