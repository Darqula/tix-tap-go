using System.ComponentModel.DataAnnotations.Schema;

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

    public EventStatus Status { get; private set; }
    public EventCancellationReason? CancellationReason { get; private set; }

    public List<SeatCategoryPrice> SeatCategoryPrices { get; private set; } = new List<SeatCategoryPrice>();

    // There's no in-place "populate" for a plain jsonb converted property,
    // so the field reference itself must be replaceable
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
#pragma warning disable IDE0044
    private List<EventIssue> _activeIssues = new();
#pragma warning restore IDE0044

    [NotMapped]
    public IReadOnlyList<EventIssue> ActiveIssues => _activeIssues;

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
        string venueId = VenueId.ToString();
        if (TryAddIssue(new EventIssue(EventIssueType.VenueDeleted, venueId)))
        {
            AddDomainEvent(new EventDecisionRequired(
                "Event's venue deleted",
                Id,
                new Dictionary<string, string> { [EventDecisionRequired.Keys.VenueId] = venueId }
            ));
        }
    }

    public void OnCategoryPriceExceededVenueCapacity(Guid venueCategoryId, Guid venueCategoryPricingId)
    {
        string venueId = VenueId.ToString();
        string categoryId = venueCategoryId.ToString();
        string categoryPricingId = venueCategoryPricingId.ToString();
        if (TryAddIssue(new EventIssue(EventIssueType.VenueCategoryExceeded,
                $"{venueId}:{categoryId}:{categoryPricingId}")))
        {
            AddDomainEvent(new EventDecisionRequired(
                "Event's category pricing exceeded venue capacity",
                Id,
                new Dictionary<string, string>()
                {
                    [EventDecisionRequired.Keys.VenueId] = venueId,
                    [EventDecisionRequired.Keys.VenueCategoryId] = categoryId,
                    [EventDecisionRequired.Keys.VenueCategoryPricingId] = categoryPricingId
                }
            ));
        }
    }

    public void OnVenueCategoryDeleted(Guid venueCategoryId, Guid venueCategoryPricingId)
    {
        string venueId = VenueId.ToString();
        string categoryId = venueCategoryId.ToString();
        string categoryPricingId = venueCategoryPricingId.ToString();
        if (TryAddIssue(new EventIssue(EventIssueType.VenueCategoryDeleted,
                $"{venueId}:{categoryId}:{categoryPricingId}")))
        {
            AddDomainEvent(new EventDecisionRequired(
                "Event's venue category deleted",
                Id,
                new Dictionary<string, string>()
                {
                    [EventDecisionRequired.Keys.VenueId] = venueId,
                    [EventDecisionRequired.Keys.VenueCategoryId] = categoryId,
                    [EventDecisionRequired.Keys.VenueCategoryPricingId] = categoryPricingId
                }
            ));
        }
    }

    private bool TryAddIssue(EventIssue issue)
    {
        if (_activeIssues.Contains(issue))
        {
            return false;
        }

        _activeIssues.Add(issue);
        return true;
    }
}
