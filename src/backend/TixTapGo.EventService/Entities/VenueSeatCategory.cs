using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.EventService.Entities;

internal sealed class VenueSeatCategory : EntityBase
{
    public Guid VenueId { get; init; }
    public required string Title { get; set; }
    public int TotalCapacity { get; set; }

    private readonly List<SeatCategoryPrice>? _prices = null!;

    public IReadOnlyCollection<SeatCategoryPrice> Prices
    {
        get => _prices ?? throw new InvalidOperationException("Prices are not loaded");
    }

    public bool PendingRemove { get; set; }

    public Dictionary<Guid, List<SeatCategoryPrice>> GetUpcomingEventsPrices()
    {
        return Prices
            .Where(SeatCategoryPrice.IsCountableFunc)
            .GroupBy(p => p.EventId)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    public bool TryAssignCapacityForEvent(Guid eventId, int newCapacity, out int availableCapacity)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Prices.Any(p => p.Event is null))
            throw new InvalidOperationException(
                "Event navigations of all prices must be loaded for capacity accounting");

        int assignedCapacity = Prices
            .Where(SeatCategoryPrice.GetCountableForEvent(eventId).Compile())
            .Sum(p => p.Capacity);

        availableCapacity = TotalCapacity - assignedCapacity;

        return availableCapacity >= newCapacity;
    }
}
