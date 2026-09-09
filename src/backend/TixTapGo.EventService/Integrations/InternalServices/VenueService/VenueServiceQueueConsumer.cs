using MassTransit;

using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Enums;
using TixTapGo.VenueService.Contracts.Messages;

namespace TixTapGo.EventService.Integrations.InternalServices.VenueService;

internal class VenueServiceQueueConsumer : IConsumer<VenueDeleted>
{
    private readonly EventDbContext _dbContext;

    public VenueServiceQueueConsumer(EventDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<VenueDeleted> context)
    {
        var venueEvents = await _dbContext.Events
            .Where(e => e.VenueId == context.Message.VenueId)
            .Where(e => e.Status == EventStatus.Upcoming)
            .ToListAsync();

        if (venueEvents.Count == 0)
        {
            return;
        }
        
        foreach (var eventEntity in venueEvents)
        {
            eventEntity.OnVenueDeleted();
        }
        
        await _dbContext.SaveChangesAsync();
    }
}
