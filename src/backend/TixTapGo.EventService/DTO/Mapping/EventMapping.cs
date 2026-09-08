using Riok.Mapperly.Abstractions;

using TixTapGo.EventService.Entities;
using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.DTO.Mapping;

[Mapper]
[UseStaticMapper(typeof(AttendeeGroupMapping))]
internal static partial class EventMapping
{
    public static partial IQueryable<GetEventResponse> ProjectToGetEventResponse(this IQueryable<Event> query);
    
    [MapperIgnoreSource(nameof(Event.IsDeleted))]
    [MapperIgnoreSource(nameof(Event.CreatedAt))]
    [MapperIgnoreSource(nameof(Event.UpdatedAt))]
    public static partial GetEventResponse ToGetEventResponse(this Event entity);

    [MapperIgnoreTarget(nameof(Event.Id))]
    [MapperIgnoreTarget(nameof(Event.VenuePendingResolution))]
    [MapValue(nameof(Event.Status), EventStatus.Upcoming)]
    [MapProperty(source: nameof(CreateEventRequest.Start), target: nameof(Event.Start), Use = nameof(ToEventStartDate))]
    public static partial Event ToEntity(this CreateEventRequest dto);

    [UserMapping(Default = false)]
    private static DateTimeOffset ToEventStartDate(DateTimeOffset? startDateDto) => startDateDto!.Value.ToUniversalTime();
}
