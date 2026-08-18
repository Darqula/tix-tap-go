using EventService.Entities;

namespace EventService.DTO.Mapping;

internal static class AttendeeGroupMapping
{
    public static GetAttendeeGroupResponse ToGetAttendeeGroupResponse(this AttendeeGroup entity)
    {
        return new GetAttendeeGroupResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Type = entity.Type,
            Capacity = entity.Capacity,
        };
    }

    public static AttendeeGroup ToEntity(this CreateAttendeeGroupRequest dto, Event @event)
    {
        return new AttendeeGroup
        {
            EventId = @event.Id,
            Title = dto.Title,
            Type = dto.Type,
            Capacity = dto.Capacity
        };
    }
}
