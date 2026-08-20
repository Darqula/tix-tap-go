using EventService.Entities;

namespace EventService.DTO.Mapping;

internal static class EventMapping
{
    public static GetEventResponse ToGetEventResponse(this Event entity)
    {
        return new GetEventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Start = entity.Start,
            Location = entity.Location,
            AttendeeGroups = entity.AttendeeGroups.Select(groupModel => groupModel.ToGetAttendeeGroupResponse()),
        };
    }

    public static Event ToEntity(this CreateEventRequest dto)
    {
        var @event = new Event()
        {
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            Start = dto.Start!.Value
        };

        if (dto.AttendeeGroups != null && dto.AttendeeGroups.Any())
        {
            @event.AttendeeGroups.AddRange(dto.AttendeeGroups.Select(groupDto => groupDto.ToEntity(@event)));
        }

        return @event;
    }
}
