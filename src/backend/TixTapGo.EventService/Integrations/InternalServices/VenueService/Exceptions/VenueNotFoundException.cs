using TixTapGo.Shared.Exceptions;

namespace TixTapGo.EventService.Integrations.InternalServices.VenueService.Exceptions;

public class VenueNotFoundException(Guid venueId) : DomainValidationException($"Venue with id {venueId} not found");
