using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages.EventSeatCategory;

/// <summary>
/// Represents changes made to a seat category price.
/// If some value (say, BasePrice) wasn't changed, 
/// the corresponding Old* and New* properties will be null
/// </summary>
public record EventSeatCategoryPriceChanged(
    Guid EventId,
    Guid VenueId,
    Guid VenueSeatCategoryId,
    Guid EventSeatCategoryPricingId) : IDomainEvent
{
    public void WithPriceChanged(decimal newBasePrice, decimal oldBasePrice)
    {
        OldBasePrice = oldBasePrice;
        NewBasePrice = newBasePrice;
    }

    public void WithAssignmentChanged(SeatAssignmentType newAssignmentType, SeatAssignmentType oldAssignmentType)
    {
        OldAssignmentType = oldAssignmentType;
        NewAssignmentType = newAssignmentType;
    }

    public void WithCapacityChanged(int newCapacity, int oldCapacity)
    {
        NewCapacity = newCapacity;
        OldCapacity = oldCapacity;
    }

    public void WithEnabledChanged(bool newIsEnabled)
    {
        IsEnabled = newIsEnabled;
    }

    public decimal? NewBasePrice { get; private set; }
    public decimal? OldBasePrice { get; private set; }
    public SeatAssignmentType? NewAssignmentType { get; private set; }
    public SeatAssignmentType? OldAssignmentType { get; private set; }
    public int? NewCapacity { get; private set; }
    public int? OldCapacity { get; private set; }

    /// <summary>
    /// Can be either false or null, because IsEnabled: true is processed 
    /// by EventSeatCategoryPriceEnabled event 
    /// </summary>
    public bool? IsEnabled { get; private set; }
}
