using System.Text.Json.Serialization;

namespace TixTapGo.EventService.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
public enum SeatAssignmentType
{
    /// <summary>
    /// Seats are not assigned to attendees
    /// </summary>
    GeneralAdmission = 0,

    /// <summary>
    /// Seats are assigned to attendees
    /// </summary>
    Reserved = 1,
}
