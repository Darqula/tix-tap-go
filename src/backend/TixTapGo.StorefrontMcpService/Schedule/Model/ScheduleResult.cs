namespace TixTapGo.StorefrontMcpService.Schedule.Model;

public record ScheduleResult(
    string TimeZone,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<ScheduleEvent> Events,
    string? Hint = null);
