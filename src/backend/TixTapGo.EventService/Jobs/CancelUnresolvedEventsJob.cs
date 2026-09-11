using Hangfire;

using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Enums;

using Event = TixTapGo.EventService.Entities.Event;

namespace TixTapGo.EventService.Jobs;

[DisableConcurrentExecution(10)]
internal class CancelUnresolvedEventsJob
{
    public static readonly string PeriodCron = Cron.HourInterval(6);

    private readonly EventDbContext _eventDbContext;

    public CancelUnresolvedEventsJob(EventDbContext eventDbContext)
    {
        _eventDbContext = eventDbContext;
    }

    public async Task ExecuteAsync(IJobCancellationToken cancellationToken)
    {
        DateTimeOffset oneDayWindowEnd = DateTime.UtcNow.AddHours(24);
        DateTimeOffset oneDayWindowStart = DateTime.UtcNow.AddMinutes(10);
        var problemEvents = await _eventDbContext.Events
            .Where(e => e.Status == EventStatus.Upcoming)
            .Where(e => e.Start < oneDayWindowEnd)
            .Where(e => e.Start > oneDayWindowStart)
            .Where(e => e.ActiveIssues.Count != 0)
            .ToListAsync(cancellationToken.ShutdownToken);

        if (problemEvents.Count == 0)
        {
            return;
        }

        foreach (Event problemEvent in problemEvents)
        {
            problemEvent.Cancel(EventCancellationReason.UnresolvedIssues);
            
            try
            {
                await _eventDbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (DbUpdateConcurrencyException e)
            {
                foreach (var entry in e.Entries)
                {
                    entry.State = EntityState.Detached;
                }
            }
        }
    }
}
