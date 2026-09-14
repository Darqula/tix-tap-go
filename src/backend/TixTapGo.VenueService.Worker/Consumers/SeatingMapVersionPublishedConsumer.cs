using MassTransit;

using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.Contracts.Messages;
using TixTapGo.VenueService.Contracts.Messages.Local;
using TixTapGo.VenueService.DAL;

namespace TixTapGo.VenueService.Worker.Consumers;

internal sealed class SeatingMapVersionPublishedConsumer(
    VenueDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<SeatingMapVersionPublishedConsumer> logger) : IConsumer<SeatingMapVersionPublished>
{
    public async Task Consume(ConsumeContext<SeatingMapVersionPublished> context)
    {
        var venueId = context.Message.VenueId;
        var seatingMapVersionId = context.Message.SeatingMapVersionId;

        var categoriesWithSeatCount = await dbContext.VenueSeats
            .Where(seat => seat.SeatingMapVersionId == seatingMapVersionId)
            .GroupBy(seat => seat.Category, seat => seat, (category, seats) => new
            {
                category.Id,
                category.Title,
                category.VenueId,
                Capacity = seats.Count()
            })
            .AsNoTracking()
            .ToListAsync(context.CancellationToken);

        if (categoriesWithSeatCount.Any(c => c.VenueId != venueId))
        {
            logger.LogError(
                "Venue seating map {SeatingMapVersionId} doesn't belong to venue {VenueId}",
                seatingMapVersionId, venueId);
            return;
        }

        var categories = categoriesWithSeatCount
            .Select(c => new VenueNewSeatingMapPublished.Category(c.Id, c.Title, c.Capacity))
            .ToList();

        var message = new VenueNewSeatingMapPublished
        {
            VenueId = venueId,
            SeatingMapVersionId = seatingMapVersionId,
            Categories = categories
        };

        await publishEndpoint.Publish(message, context.CancellationToken);
        // Save changes to run Outbox
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
