using TixTapGo.EventService.Integrations.InternalServices.VenueService.DTO;
using TixTapGo.EventService.Integrations.InternalServices.VenueService.Exceptions;

namespace TixTapGo.EventService.Integrations.InternalServices.VenueService;

public class VenueServiceHttpClient
{
    private readonly HttpClient _httpClient;

    public VenueServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        httpClient.BaseAddress = new Uri("https://venue-service/venues/");
    }

    public async Task<GetVenueResponse> GetVenueAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        var venue = await _httpClient.GetFromJsonAsync<GetVenueResponse>(venueId.ToString(), cancellationToken);

        if (venue == null || venue.Id == Guid.Empty)
        {
            throw new VenueNotFoundException(venueId);
        }

        return venue;
    }
}
