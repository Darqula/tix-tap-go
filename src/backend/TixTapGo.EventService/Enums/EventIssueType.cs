using System.Text.Json.Serialization;

namespace TixTapGo.EventService.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EventIssueType>))]
public enum EventIssueType
{
    VenueDeleted = 0
}
