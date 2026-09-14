using TixTapGo.Shared.Abstractions;

namespace TixTapGo.VenueService.Contracts.Messages.Local;

/// <summary>
/// Local to VenueService/VenueService.Worker. Deliberately not a public contract
/// </summary>
internal record SeatingMapVersionPublished(Guid VenueId, Guid SeatingMapVersionId) : IDomainEvent;
