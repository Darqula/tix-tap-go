using System.ComponentModel.DataAnnotations;

using TixTapGo.EventService.Enums;
using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.EventService.Entities;

internal sealed class AttendeeGroup : EntityBase
{
    public required Guid EventId { get; init; }

    public Event Event { get; set; } = null!;

    [MaxLength(128)]
    public required string Title { get; set; }

    public required SeatAssignmentType Type { get; set; }

    public required int Capacity { get; set; }
}
