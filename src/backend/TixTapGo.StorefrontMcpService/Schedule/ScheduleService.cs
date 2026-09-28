using ModelContextProtocol;

using TixTapGo.StorefrontMcpService.Integrations.Catalog;
using TixTapGo.StorefrontMcpService.Integrations.Catalog.DTO;
using TixTapGo.StorefrontMcpService.Schedule.Model;

namespace TixTapGo.StorefrontMcpService.Schedule;

internal sealed class ScheduleService(ICatalogClient catalogClient, ILogger<ScheduleService> logger)
{
    private const int MaxLocationLength = 256;

    public async Task<ScheduleResult> GetScheduleAsync(string location, string dateFrom, string dateTo,
        string? timeZone, CancellationToken cancellationToken = default)
    {
        string trimmedLocation = location?.Trim() ?? string.Empty;
        if (trimmedLocation.Length == 0)
        {
            throw new McpException("location must not be empty.");
        }

        if (trimmedLocation.Length > MaxLocationLength)
        {
            throw new McpException($"location must not be longer than {MaxLocationLength} characters.");
        }

        var range = ScheduleRange.Parse(dateFrom, dateTo, timeZone);

        var venues = await CallCatalogAsync(ct => catalogClient.GetVenuesAsync(ct), cancellationToken);
        var matchedVenues = venues
            .Where(venue => LocationMatcher.Matches(venue.Title, venue.Address, trimmedLocation))
            .ToDictionary(venue => venue.Id);

        if (matchedVenues.Count == 0)
        {
            logger.LogInformation("Schedule requested: location length {LocationLength}, no venue matched",
                trimmedLocation.Length);
            return EmptyResult(range, NoVenueHint(trimmedLocation, venues));
        }

        Guid[] venueIds = matchedVenues.Keys.ToArray();
        var upcomingEvents = await CallCatalogAsync(ct => catalogClient.GetUpcomingEventsAsync(venueIds, ct),
            cancellationToken);
        var events = upcomingEvents
            .Where(@event => matchedVenues.ContainsKey(@event.VenueId) && range.Overlaps(@event.Start, @event.End))
            .ToList();

        if (events.Count == 0)
        {
            logger.LogInformation(
                "Schedule requested: location length {LocationLength}, range {RangeLength}, {VenueCount} venues, no events",
                trimmedLocation.Length, range.To - range.From, matchedVenues.Count);
            return EmptyResult(range,
                $"No upcoming events at the matching venues between {range.Format(range.From)} and {range.Format(range.To)}.");
        }

        Guid[] eventIds = events.Select(@event => @event.Id).ToArray();
        var prices = await CallCatalogAsync(ct => catalogClient.GetCategoryPricesAsync(eventIds, ct),
            cancellationToken);
        var onSalePricesByEvent = prices
            .Where(price => price.IsOnSale)
            .ToLookup(price => price.EventId);

        var scheduleEvents = events
            .OrderBy(@event => @event.Start)
            .ThenBy(@event => @event.Id)
            .Select(@event => ToScheduleEvent(@event, matchedVenues[@event.VenueId], onSalePricesByEvent[@event.Id],
                range))
            .ToList();

        logger.LogInformation(
            "Schedule requested: location length {LocationLength}, range {RangeLength}, {VenueCount} venues, {EventCount} events",
            trimmedLocation.Length, range.To - range.From, matchedVenues.Count, scheduleEvents.Count);

        return new ScheduleResult(range.TimeZoneLabel, range.AdjustTimeZone(range.From), range.AdjustTimeZone(range.To),
            scheduleEvents, null);
    }

    private static ScheduleEvent ToScheduleEvent(EventDto @event, VenueDto venue,
        IEnumerable<CategoryPriceDto> onSalePrices, ScheduleRange range)
    {
        var categories = onSalePrices
            .OrderBy(price => price.BasePrice)
            .ThenBy(price => price.VenueSeatCategory.Title, StringComparer.Ordinal)
            .Select(price => new ScheduleCategory(price.VenueSeatCategory.Id, price.VenueSeatCategory.Title,
                price.BasePrice, price.Capacity, price.SeatAssignmentType))
            .ToList();

        return new ScheduleEvent(
            @event.Id,
            @event.Title,
            @event.Description,
            range.AdjustTimeZone(@event.Start),
            range.AdjustTimeZone(@event.End),
            new ScheduleVenue(venue.Id, venue.Title, venue.Address),
            categories.Count > 0,
            categories);
    }

    private static ScheduleResult EmptyResult(ScheduleRange range, string hint)
    {
        return new ScheduleResult(range.TimeZoneLabel, range.AdjustTimeZone(range.From), range.AdjustTimeZone(range.To), [], hint);
    }

    private static string NoVenueHint(string location, IReadOnlyList<VenueDto> venues)
    {
        string known = venues.Count == 0
            ? "none"
            : string.Join("; ", venues.Select(venue => $"{venue.Title} — {venue.Address}"));
        return $"No venue title or address contains '{location}'. Known venues: {known}";
    }

    private async Task<T> CallCatalogAsync<T>(Func<CancellationToken, Task<T>> call,
        CancellationToken cancellationToken)
    {
        try
        {
            return await call(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Catalog request failed");
            throw new McpException("The event data is temporarily unavailable. Please try again later.", ex);
        }
    }
}
