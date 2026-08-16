using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using EventService.Entities;
using EventService.Enums;

namespace EventService.DTO;

public record AttendeeGroupDto
{
    [SetsRequiredMembers]
    public AttendeeGroupDto(AttendeeGroup groupEntity)
    {
        Id = groupEntity.Id;
        Title = groupEntity.Title;
        Type = groupEntity.Type;
        Capacity = groupEntity.Capacity;
    }
    
    public Guid Id { get; init; }
    public required string Title { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
    public required SeatAssignmentType Type { get; init; }
    public required int Capacity { get; init; }
}