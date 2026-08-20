using System.ComponentModel.DataAnnotations;

using EventService.Enums;

using Shared.Entities;

namespace EventService.Entities;

internal sealed class AttendeeGroup : EntityBase
{
    [Required]
    public required Guid EventId { get; init; }

    public Event Event { get; private set; } = null!;

    [Required]
    [MaxLength(128)]
    public required string Title { get; set; }

    [Required]
    public required SeatAssignmentType Type { get; set; }

    [Required]
    public required int Capacity { get; set; }
}
