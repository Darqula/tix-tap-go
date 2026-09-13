using Hangfire;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.Contracts.Messages;
using TixTapGo.VenueService.DAL;

namespace TixTapGo.VenueService.Jobs;

[DisableConcurrentExecution(10)]
internal class FanoutNewSeatingMapJob
{
    private readonly VenueDbContext _dbContext;
    private readonly IPublishEndpoint _rmqPublishEndpoint;
    private readonly ILogger<FanoutNewSeatingMapJob> _logger;

    public FanoutNewSeatingMapJob(VenueDbContext dbContext, IPublishEndpoint rmqPublishEndpoint,
        ILogger<FanoutNewSeatingMapJob> logger)
    {
        _dbContext = dbContext;
        _rmqPublishEndpoint = rmqPublishEndpoint;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid venueId, Guid seatingMapVersionId, IJobCancellationToken cancellationToken)
    {
        var categoriesWithSeatCount = await _dbContext.VenueSeats
            .Where(seat => seat.SeatingMapVersionId == seatingMapVersionId)
            .GroupBy(seat => seat.Category, seat => seat, (category, seats) => new
            {
                category.Id,
                category.Title,
                category.VenueId,
                Capacity = seats.Count()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken.ShutdownToken);

        if (categoriesWithSeatCount.Any(c => c.VenueId != venueId))
        {
            _logger.LogError(
                "Venue seating map {SeatingMapVersionId} doesn't belong to venue {VenueId}",
                seatingMapVersionId, venueId);
            return;
        }

        var categories = categoriesWithSeatCount
            .Select(c => new VenueNewSeatingMapPublished.Category(c.Id, c.Title, c.Capacity)).ToList();

        var message = new VenueNewSeatingMapPublished
        {
            VenueId = venueId,
            SeatingMapVersionId = seatingMapVersionId,
            Categories = categories
        };

        await _rmqPublishEndpoint.Publish(message);
        // Save changes to run Outbox
        await _dbContext.SaveChangesAsync();
    }
}
