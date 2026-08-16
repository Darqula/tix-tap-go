using EventService.Entities;

namespace EventService.DTO;

public record GetEventResponse
{
    public GetEventResponse(Event eventEntity)
    {
        Id = eventEntity.Id.ToString();
        Title = eventEntity.Title;
        Description = eventEntity.Description;
        Start = eventEntity.Start;
        Location = eventEntity.Location;
        AttendeeGroups = eventEntity.AttendeeGroups.Select(groupModel => new AttendeeGroupDto(groupModel));
    }

    public string Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTimeOffset Start { get; init; }
    public string Location { get; init; }
    public IEnumerable<AttendeeGroupDto> AttendeeGroups { get; init; }

    public void Deconstruct(out string id, out string title, out string description, out DateTimeOffset start,
        out string location, out IEnumerable<AttendeeGroupDto> attendeeGroups)
    {
        id = Id;
        title = Title;
        description = Description;
        start = Start;
        location = Location;
        attendeeGroups = AttendeeGroups;
    }
}