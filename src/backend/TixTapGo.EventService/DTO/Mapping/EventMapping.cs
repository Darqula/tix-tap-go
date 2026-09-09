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
    [MapperIgnoreSource(nameof(Event.DomainEvents))]
    [MapProperty(source: nameof(Event.ActiveIssues), target: nameof(GetEventResponse.IsResolutionRequired), Use = nameof(IsResolutionRequired))]
    
    public static partial GetEventResponse ToGetEventResponse(this Event entity);
    
    [IncludeMappingConfiguration(nameof(ToGetEventResponse))]
    public static partial GetEventDetailedResponse ToGetEventDetailedResponse(this Event entity);
    
    public static partial GetEventIssueResponse ToGetEventIssueResponse(this EventIssue issue);
    

    [MapperIgnoreTarget(nameof(Event.Id))]
    [MapperIgnoreTarget(nameof(Event.DomainEvents))]
    [MapValue(nameof(Event.Status), EventStatus.Upcoming)]
    [MapProperty(source: nameof(CreateEventRequest.Start), target: nameof(Event.Start), Use = nameof(ToEventStartDate))]
    public static partial Event ToEntity(this CreateEventRequest dto);

    [UserMapping(Default = false)]
    private static DateTimeOffset ToEventStartDate(DateTimeOffset? startDateDto) => startDateDto!.Value.ToUniversalTime();
    
    [UserMapping(Default = false)]
    private static bool IsResolutionRequired(List<EventIssue> issues) => issues.Count != 0;
}
