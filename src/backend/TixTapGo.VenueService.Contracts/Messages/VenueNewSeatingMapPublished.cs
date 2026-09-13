using TixTapGo.Shared.Abstractions;

namespace TixTapGo.VenueService.Contracts.Messages;

public record VenueNewSeatingMapPublished : IDomainEvent
{
    public required Guid VenueId { get; init; }
    public required Guid SeatingMapVersionId { get; init; }
    public required List<Category> Categories { get; init; }

    public record Category(Guid Id, string Title, int Capacity);
}
