using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.Shared.Abstractions;

namespace TixTapGo.EventService.Contracts.Messages.EventSeatCategory;

public record EventSeatCategoryPriceEnabled : IDomainEvent
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid VenueId { get; init; }
    public Guid VenueSeatCategoryId { get; init; }
    public int Capacity { get; init; }
    public SeatAssignmentType SeatAssignmentType { get; init; }
    public decimal BasePrice { get; init; }
    public bool IsEnabled { get; init; }
}
