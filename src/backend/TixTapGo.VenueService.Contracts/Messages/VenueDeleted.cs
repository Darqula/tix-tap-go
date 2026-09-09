using TixTapGo.Shared.Abstractions;

namespace TixTapGo.VenueService.Contracts.Messages;

public record VenueDeleted(Guid VenueId, Guid? SchemaMapId) : IDomainEvent;
