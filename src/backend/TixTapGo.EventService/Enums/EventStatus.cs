using System.Text.Json.Serialization;

namespace TixTapGo.EventService.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EventStatus>))]
public enum EventStatus
{
    [JsonStringEnumMemberName("upcoming")]
    Upcoming = 0,
    [JsonStringEnumMemberName("completed")]
    Completed = 1,
    [JsonStringEnumMemberName("cancelled")]
    Cancelled = 2
}
