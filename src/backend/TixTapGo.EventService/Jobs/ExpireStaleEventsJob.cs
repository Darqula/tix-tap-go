using Hangfire;

using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Jobs;

[DisableConcurrentExecution(10)]
internal class ExpireStaleEventsJob(EventDbContext dbContext, ILogger<ExpireStaleEventsJob> logger)
{
    public static readonly string PeriodCron = Cron.Hourly();
    
    public async Task ExecuteAsync(IJobCancellationToken cancellationToken)
    {
        var staleEvents = await dbContext.Events
            .Where(e => e.Status == EventStatus.Upcoming && e.End < DateTime.UtcNow)
            .ToListAsync(cancellationToken.ShutdownToken);

        if (staleEvents.Count == 0)
        {
            logger.LogInformation("No stale events found");
            return;
        }
        
        logger.LogInformation("{StaleEventsCount} stale events found", staleEvents.Count);
        foreach (var staleEvent in staleEvents)
        {
            staleEvent.Status = EventStatus.Completed;
        }
        
        await dbContext.SaveChangesAsync(cancellationToken.ShutdownToken);
        logger.LogInformation("Processed {StaleEventsCount} stale events", staleEvents.Count);
    }
}
