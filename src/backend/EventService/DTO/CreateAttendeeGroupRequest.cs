using System.Text.Json.Serialization;
using EventService.Entities;
using EventService.Enums;

namespace EventService.DTO;

public record CreateAttendeeGroupRequest
{
    public CreateAttendeeGroupRequest()
    {
    }
    
    public required string Title { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
    public required SeatAssignmentType Type { get; init; }
    public required int Capacity { get; init; }

    public AttendeeGroup ToModel(Event @event)
    {
        return new AttendeeGroup
        {
            Id = Guid.Empty,
            EventId = @event.Id,
            Title = Title,
            Type = Type,
            Capacity = Capacity
        };
    }
}