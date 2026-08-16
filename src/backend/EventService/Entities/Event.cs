using Shared.Entities;

namespace EventService.Entities;

public class Event: EntityBase
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required string Location { get; init; }
    public required IEnumerable<AttendeeGroup> AttendeeGroups { get; init; } = new List<AttendeeGroup>();
}