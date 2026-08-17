using Shared.Entities;

namespace EventService.Entities;

public class Event: EntityBase
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset Start { get; set; }
    public required string Location { get; set; }
    public required IEnumerable<AttendeeGroup> AttendeeGroups { get; init; } = new List<AttendeeGroup>();
}