using Riok.Mapperly.Abstractions;

using TixTapGo.EventService.Endpoints.Event.DTO;
using TixTapGo.EventService.Entities;
using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Endpoints.Event;

[Mapper]
internal static partial class EventMapping
{
    public static partial IQueryable<GetEventResponse> ProjectToGetEventResponse(this IQueryable<Entities.Event> query);

    [MapperIgnoreSource(nameof(Entities.Event.IsDeleted))]
    [MapperIgnoreSource(nameof(Entities.Event.CreatedAt))]
    [MapperIgnoreSource(nameof(Entities.Event.UpdatedAt))]
    [MapperIgnoreSource(nameof(Entities.Event.DomainEvents))]
    [MapperIgnoreSource(nameof(Entities.Event.SeatCategoryPrices))]
    [MapProperty(source: nameof(Entities.Event.ActiveIssues), target: nameof(GetEventResponse.IsResolutionRequired),
        Use = nameof(IsResolutionRequired))]
    public static partial GetEventResponse ToGetEventResponse(this Entities.Event entity);

    [IncludeMappingConfiguration(nameof(ToGetEventResponse))]
    public static partial GetEventDetailedResponse ToGetEventDetailedResponse(this Entities.Event entity);

    public static partial GetEventIssueResponse ToGetEventIssueResponse(this EventIssue issue);


    [MapperIgnoreTarget(nameof(Entities.Event.Id))]
    [MapperIgnoreTarget(nameof(Entities.Event.DomainEvents))]
    [MapValue(nameof(Entities.Event.Status), EventStatus.Upcoming)]
    [MapProperty(source: nameof(CreateEventRequest.Start), target: nameof(Entities.Event.Start),
        Use = nameof(ToEventStartDate))]
    public static partial Entities.Event ToEntity(this CreateEventRequest dto);

    [UserMapping(Default = false)]
    private static DateTimeOffset ToEventStartDate(DateTimeOffset? startDateDto) =>
        startDateDto!.Value.ToUniversalTime();

    [UserMapping(Default = false)]
    private static bool IsResolutionRequired(List<EventIssue> issues) => issues.Count != 0;
}
