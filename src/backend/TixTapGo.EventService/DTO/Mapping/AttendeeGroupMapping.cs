using Riok.Mapperly.Abstractions;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DTO.Mapping;

[Mapper]
internal static partial class AttendeeGroupMapping
{
    [MapperIgnoreSource(nameof(AttendeeGroup.IsDeleted))]
    [MapperIgnoreSource(nameof(AttendeeGroup.CreatedAt))]
    [MapperIgnoreSource(nameof(AttendeeGroup.UpdatedAt))]
    [MapperIgnoreSource(nameof(AttendeeGroup.DomainEvents))]
    [MapperIgnoreSource(nameof(AttendeeGroup.EventId))]
    [MapperIgnoreSource(nameof(AttendeeGroup.Event))]
    public static partial GetAttendeeGroupResponse ToGetAttendeeGroupResponse(this AttendeeGroup entity);

    [MapperIgnoreTarget(nameof(AttendeeGroup.Id))]
    [MapperIgnoreTarget(nameof(AttendeeGroup.Event))]
    [MapValue(nameof(AttendeeGroup.EventId), Use=nameof(GetEventId))]
    public static partial AttendeeGroup ToEntity(this CreateAttendeeGroupRequest dto);
    
    private static Guid GetEventId() => Guid.Empty;
}
