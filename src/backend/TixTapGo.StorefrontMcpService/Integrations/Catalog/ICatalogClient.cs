using TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

namespace TixTapGo.StorefrontMcpService.Integrations.Catalog;

internal interface ICatalogClient
{
    Task<IReadOnlyList<VenueDto>> GetVenuesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventDto>> GetUpcomingEventsAsync(IReadOnlyCollection<Guid> venueIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryPriceDto>> GetCategoryPricesAsync(IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default);
}
