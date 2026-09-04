using TixTapGo.Shared.Exceptions;

namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion.Exceptions;

public class SeatingMapVersioningException(string message) : DomainValidationException(message);
