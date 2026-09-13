using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Endpoints.Event.DTO;

public record GetEventResponse
{
    public required Guid Id { get; init; }
    public required Guid VenueId { get; init; }
    public bool IsResolutionRequired { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
    public required EventStatus Status { get; init; }
    public EventCancellationReason? CancellationReason { get; init; }
}
