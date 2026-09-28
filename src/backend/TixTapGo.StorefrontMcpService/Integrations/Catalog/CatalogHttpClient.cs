using TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;

namespace TixTapGo.StorefrontMcpService.Integrations.Catalog;

internal sealed class CatalogHttpClient : ICatalogClient
{
    internal const int MaxIdsPerRequest = 100;

    private readonly HttpClient _httpClient;

    public CatalogHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        httpClient.BaseAddress = new Uri("https+http://gateway/");
    }

    public async Task<IReadOnlyList<VenueDto>> GetVenuesAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<VenueDto>>("venues", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<EventDto>> GetUpcomingEventsAsync(IReadOnlyCollection<Guid> venueIds,
        CancellationToken cancellationToken = default)
    {
        return await GetChunkedAsync<EventDto>(venueIds,
            chunk => $"events?status=upcoming&{ToQuery("venue", chunk)}", cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryPriceDto>> GetCategoryPricesAsync(IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        return await GetChunkedAsync<CategoryPriceDto>(eventIds,
            chunk => $"events/category-prices?{ToQuery("eventId", chunk)}", cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetChunkedAsync<T>(IReadOnlyCollection<Guid> ids,
        Func<Guid[], string> buildUri, CancellationToken cancellationToken)
    {
        var result = new List<T>();
        foreach (Guid[] chunk in ids.Distinct().Chunk(MaxIdsPerRequest))
        {
            var items = await _httpClient.GetFromJsonAsync<List<T>>(buildUri(chunk), cancellationToken);
            if (items != null)
            {
                result.AddRange(items);
            }
        }

        return result;
    }

    private static string ToQuery(string name, IEnumerable<Guid> ids)
    {
        return string.Join('&', ids.Select(id => $"{name}={id}"));
    }
}
