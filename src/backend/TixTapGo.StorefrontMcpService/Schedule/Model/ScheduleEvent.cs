namespace TixTapGo.StorefrontMcpService.Schedule.Model;

public record ScheduleEvent(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    ScheduleVenue Venue,
    bool TicketsOnSale,
    IReadOnlyList<ScheduleCategory> Categories);
