using System.Text.Json.Serialization;

using EventService.Enums;

namespace EventService.DTO;

public sealed record GetAttendeeGroupResponse
{
    public Guid Id { get; init; }
    public required string Title { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
    public required SeatAssignmentType Type { get; init; }

    public required int Capacity { get; init; }
}
