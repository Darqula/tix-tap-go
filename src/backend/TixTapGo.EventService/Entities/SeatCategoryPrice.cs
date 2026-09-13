using System.Linq.Expressions;

using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.EventService.Contracts.Messages.EventSeatCategory;
using TixTapGo.EventService.Enums;
using TixTapGo.EventService.Exceptions;
using TixTapGo.Shared.Exceptions;
using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.EventService.Entities;

internal sealed class SeatCategoryPrice : EntityBase
{
    public static Expression<Func<SeatCategoryPrice, bool>> IsCountable { get; } =
        price => price.IsEnabled && price.Event.Status == EventStatus.Upcoming;

    public static Func<SeatCategoryPrice, bool> IsCountableFunc { get; } = IsCountable.Compile();

    public static Expression<Func<SeatCategoryPrice, bool>> GetCountableForEvent(Guid eventId)
    {
        Expression<Func<SeatCategoryPrice, bool>> eventMatch = price => price.EventId == eventId;
        var param = IsCountable.Parameters[0];
        var body = Expression.AndAlso(
            IsCountable.Body,
            Expression.Invoke(eventMatch, param));
        return Expression.Lambda<Func<SeatCategoryPrice, bool>>(body, param);
    }

    private SeatCategoryPrice() { }

    public SeatCategoryPrice(Guid eventId, VenueSeatCategory venueSeatCategory, bool isEnabled, decimal basePrice,
        int capacity, SeatAssignmentType seatAssignmentType)
    {
        // Generate ID manually to attach to the "created" domain event within outbox 
        Id = Guid.CreateVersion7();
        EventId = eventId;
        VenueSeatCategory = venueSeatCategory;
        VenueSeatCategoryId = venueSeatCategory.Id;
        IsEnabled = isEnabled;
        BasePrice = basePrice;
        Capacity = capacity;
        SeatAssignmentType = seatAssignmentType;

        if (isEnabled)
        {
            OnEnabled();
        }
    }

    public Guid VenueSeatCategoryId { get; init; }

    private readonly VenueSeatCategory? _venueSeatCategory;

    public VenueSeatCategory VenueSeatCategory
    {
        get => _venueSeatCategory ?? throw new InvalidOperationException("VenueSeatCategory is not loaded");
        init => _venueSeatCategory = value;
    }

    public Guid EventId { get; init; }
    public Event Event { get; set; } = null!;

    public decimal BasePrice { get; private set; }
    public int Capacity { get; private set; }
    public SeatAssignmentType SeatAssignmentType { get; private set; }

    /// <summary>
    /// Whether this category is enabled for purchases 
    /// </summary>
    public bool IsEnabled { get; private set; }

    public bool ResolutionRequired { get; private set; }

    /// <summary>
    /// Label this category as enabled or disabled for purchases.
    /// Switching to the enabled state publishes a message with all pricing details.
    /// To avoid uncoordinated changes, this method must be called strictly after other SetXXX
    /// methods (otherwise their Updated domain event throws an exception) 
    /// </summary>
    /// <exception cref="InsufficientCategoryCapacityException"></exception>
    public void SetEnabled(bool newIsEnabled)
    {
        if (newIsEnabled == IsEnabled)
        {
            return;
        }

        if (newIsEnabled)
        {
            if (!VenueSeatCategory.TryAssignCapacityForEvent(EventId, Capacity, out int availableCapacity))
            {
                throw new InsufficientCategoryCapacityException(VenueSeatCategory.Title, Capacity, availableCapacity);
            }
        }
        else
        {
            GetChangedDomainEvent().WithEnabledChanged(false);
        }

        IsEnabled = newIsEnabled;

        if (newIsEnabled)
        {
            OnEnabled();
        }
    }

    public void SetAssignmentType(SeatAssignmentType newAssignmentType)
    {
        if (newAssignmentType == SeatAssignmentType)
        {
            return;
        }

        if (IsEnabled)
        {
            GetChangedDomainEvent().WithAssignmentChanged(newAssignmentType, SeatAssignmentType);
        }

        SeatAssignmentType = newAssignmentType;
    }

    public void SetCapacity(int newCapacity)
    {
        if (newCapacity != Capacity)
        {
            if (newCapacity < 0)
                throw new DomainException("Capacity cannot be negative");

            if (IsEnabled)
            {
                int diffCapacity = newCapacity - Capacity;
                if (diffCapacity > 0)
                {
                    if (!VenueSeatCategory.TryAssignCapacityForEvent(EventId, diffCapacity, out int availableCapacity))
                    {
                        throw new InsufficientCategoryCapacityException(VenueSeatCategory.Title, diffCapacity,
                            availableCapacity);
                    }
                }

                GetChangedDomainEvent().WithCapacityChanged(newCapacity, Capacity);
            }

            Capacity = newCapacity;
        }
    }

    public void SetPrice(decimal newPrice)
    {
        if (newPrice == BasePrice)
        {
            return;
        }

        if (newPrice < 0)
        {
            throw new DomainException("Price cannot be negative");
        }

        if (IsEnabled)
        {
            GetChangedDomainEvent().WithPriceChanged(newPrice, BasePrice);
        }

        BasePrice = newPrice;
    }
    
    public void OnVenueCategoryExceeded()
    {
        Event.OnCategoryPriceExceededVenueCapacity(VenueSeatCategoryId, Id);
        SetEnabled(false);
        ResolutionRequired = true;
    }

    public void OnVenueCategoryRemoved()
    {
        Event.OnVenueCategoryDeleted(VenueSeatCategoryId, Id);
        SetEnabled(false);
        ResolutionRequired = true;
    }

    private EventSeatCategoryPriceChanged GetChangedDomainEvent()
    {
        if (DomainEvents.OfType<EventSeatCategoryPriceEnabled>().Any())
        {
            throw new InvalidOperationException(
                $"Seat category pricing changed events cannot be send alongside the enabled event");
        }

        var existingEvent = DomainEvents.OfType<EventSeatCategoryPriceChanged>().FirstOrDefault();
        if (existingEvent == null)
        {
            existingEvent = new EventSeatCategoryPriceChanged(
                EventId: EventId,
                VenueId: VenueSeatCategory.VenueId,
                VenueSeatCategoryId: VenueSeatCategoryId,
                EventSeatCategoryPricingId: Id
            );
            AddDomainEvent(existingEvent);
        }

        return existingEvent;
    }

    /// <summary>
    /// Disabled category pricing doesn't notify other services about its changes.
    /// When it becomes enabled, it must publish all the necessary information 
    /// </summary>
    private void OnEnabled()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("OnEnabled was called on a disabled seat category pricing");
        }

        // Do not send Enabled events alongside other Changed events
        var changedEvents = DomainEvents.OfType<EventSeatCategoryPriceChanged>().ToList();
        if (changedEvents.Count > 0)
        {
            foreach (var categoryPriceChangedEvent in changedEvents)
            {
                RemoveDomainEvent(categoryPriceChangedEvent);
            }
        }
        
        // Remove existing enabled events if any
        var existingEnabledEvents = DomainEvents.OfType<EventSeatCategoryPriceEnabled>().ToList();
        foreach (var enabledEvent in existingEnabledEvents)
        {
            RemoveDomainEvent(enabledEvent);
        }

        AddDomainEvent(new EventSeatCategoryPriceEnabled
        {
            Id = Id,
            EventId = EventId,
            VenueId = VenueSeatCategory.VenueId,
            VenueSeatCategoryId = VenueSeatCategoryId,
            Capacity = Capacity,
            BasePrice = BasePrice,
            SeatAssignmentType = SeatAssignmentType,
            IsEnabled = IsEnabled
        });
    }
}
