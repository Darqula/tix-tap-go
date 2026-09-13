using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Endpoints.Event.DTO;

public sealed record GetEventDetailedResponse : GetEventResponse
{
    public required List<GetEventIssueResponse> ActiveIssues { get; set; }
}

public sealed record GetEventIssueResponse(EventIssueType Type, DateTimeOffset DetectedAt, string? Key = null);
