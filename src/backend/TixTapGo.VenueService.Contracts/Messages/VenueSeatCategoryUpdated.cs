namespace TixTapGo.VenueService.Contracts.Messages;

public record VenueSeatCategoryUpdated(Guid VenueId, Guid CategoryId, string Title, int Capacity);
