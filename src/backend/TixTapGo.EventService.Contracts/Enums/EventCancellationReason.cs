using System.Text.Json.Serialization;

namespace TixTapGo.EventService.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EventCancellationReason>))]
public enum EventCancellationReason
{
    Manual = 0,
    UnresolvedIssues = 1
}
