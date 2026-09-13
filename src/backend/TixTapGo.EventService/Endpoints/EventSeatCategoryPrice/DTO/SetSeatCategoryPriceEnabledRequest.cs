namespace TixTapGo.EventService.Endpoints.EventSeatCategoryPrice.DTO;

public record SetSeatCategoryPriceEnabledRequest
{
    public bool SetEnabled { get; init; }
}
