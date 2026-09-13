using TixTapGo.EventService.Contracts.Enums;

namespace TixTapGo.EventService.Endpoints.EventSeatCategoryPrice.DTO;

public record GetSeatCategoryPriceResponse
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }

    public required bool IsEnabled { get; init; }
    public required int Capacity { get; init; }
    public required SeatAssignmentType SeatAssignmentType { get; init; }
    public required decimal BasePrice { get; init; }
    public required GetVenueSeatCategoryResponse VenueSeatCategory { get; init; }
    public required bool ResolutionRequired { get; init; }
}

public record GetVenueSeatCategoryResponse
{
    public Guid Id { get; init; }
    public Guid VenueId { get; init; }
    public required string Title { get; init; }
    public int TotalCapacity { get; init; }
    public bool PendingRemove { get; init; }
}
