namespace TixTapGo.StorefrontMcpService.Schedule.Model;

public record ScheduleCategory(Guid CategoryId, string Title, decimal BasePrice, int Capacity, string SeatAssignment);
