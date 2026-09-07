using TixTapGo.EventService.Enums;
using TixTapGo.Shared.Persistence.Entities;
using TixTapGo.Shared.Validation;

namespace TixTapGo.EventService.Entities;

internal sealed class Event : EntityBase
{
    public required string Title { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }

    public required string Location { get; set; }

    public EventStatus Status { get; set; }

    public List<AttendeeGroup> AttendeeGroups { get; private set; } = new List<AttendeeGroup>();

    public bool Validate(out ValidationErrorsDictionary errors)
    {
        errors = new();

        if (Start > End)
            errors.AddError(nameof(End), "End date must be after start date");
        if (End < DateTimeOffset.UtcNow && Status == EventStatus.Upcoming)
            errors.AddError(nameof(Status),
                "Upcoming event cannot be in the past. Either update the end date or change the status to 'Completed' or 'Cancelled'");
        if (Start > DateTimeOffset.UtcNow && Status == EventStatus.Completed)
            errors.AddError(nameof(Status),
                "Completed event cannot be in the future. Either update the start date or change the status to 'Upcoming'");

        return errors.IsValid;
    }
}
