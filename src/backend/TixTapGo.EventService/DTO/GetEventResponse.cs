using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.DTO;

public sealed record GetEventResponse
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public bool VenuePendingResolution { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
    public required EventStatus Status { get; init; }
    public required string Location { get; init; }
    public required IEnumerable<GetAttendeeGroupResponse> AttendeeGroups { get; init; }
}
