using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.EventService.Contracts.Messages;
using TixTapGo.EventService.Enums;
using TixTapGo.Shared.Persistence.Entities;
using TixTapGo.Shared.Validation;

namespace TixTapGo.EventService.Entities;

internal sealed class Event : EntityBase
{
    public required Guid VenueId { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }

    public required string Location { get; set; }

    public EventStatus Status { get; private set; }
    public EventCancellationReason? CancellationReason { get; private set; }

    public List<EventIssue> ActiveIssues { get; private set; } = new List<EventIssue>();

    public List<AttendeeGroup> AttendeeGroups { get; private set; } = new List<AttendeeGroup>();

    #region Status

    public void SetUpcoming()
    {
        Status = EventStatus.Upcoming;
        CancellationReason = null;
    }

    public void Complete()
    {
        Status = EventStatus.Completed;
        CancellationReason = null;
    }

    public void Cancel(EventCancellationReason reason)
    {
        if (Status == EventStatus.Cancelled)
            return;
        
        Status = EventStatus.Cancelled;
        CancellationReason = reason;
        AddDomainEvent(new EventCancelled(Id, reason));
    }

    #endregion Status

    public bool Validate(out ValidationErrorsDictionary errors)
    {
        errors = new();

        if (VenueId == Guid.Empty)
            errors.AddError(nameof(VenueId), "VenueId is required");
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

    public void OnVenueDeleted()
    {
        if (ActiveIssues.All(issue => issue.Type != EventIssueType.VenueDeleted))
        {
            ActiveIssues.Add(new EventIssue(EventIssueType.VenueDeleted, DateTimeOffset.UtcNow));
            AddDomainEvent(new EventDecisionRequired(
                "Event's venue deleted",
                Id,
                new Dictionary<string, string>() { [nameof(VenueId)] = VenueId.ToString() }
            ));
        }
    }
}
