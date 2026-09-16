using MassTransit;

using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Entities;
using TixTapGo.EventService.Enums;
using TixTapGo.VenueService.Contracts.Messages;

namespace TixTapGo.EventService.Integrations.InternalServices.VenueService;

internal class VenueServiceQueueConsumer : IConsumer<VenueDeleted>, IConsumer<VenueNewSeatingMapPublished>
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
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task Consume(ConsumeContext<VenueNewSeatingMapPublished> context)
    {
        var venueId = context.Message.VenueId;
        var newCategories = context.Message.Categories;

        var currentCategories = await _dbContext.VenueSeatCategories
            .Where(c => c.VenueId == venueId)
            .Include(c => c.Prices)
            .ThenInclude(p => p.Event)
            .ToDictionaryAsync(category => category.Id, context.CancellationToken);

        foreach (var newCategory in newCategories)
        {
            if (!currentCategories.TryGetValue(newCategory.Id, out var currentCategory))
            {
                currentCategory = new VenueSeatCategory
                {
                    Id = newCategory.Id,
                    VenueId = venueId,
                    Title = newCategory.Title,
                    TotalCapacity = newCategory.Capacity
                };
                _dbContext.VenueSeatCategories.Add(currentCategory);
                // Here and below save changes often to narrow transaction conflicts scope
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                if (currentCategory.TotalCapacity > newCategory.Capacity)
                {
                    // We have to check that the new capacity is not less than the sum of category price capacities for upcoming events
                    foreach (var (_, eventPrices) in currentCategory.GetUpcomingEventsPrices())
                    {
                        if (eventPrices.Sum(p => p.Capacity) > newCategory.Capacity)
                        {
                            foreach (var seatCategoryPrice in eventPrices)
                            {
                                seatCategoryPrice.OnVenueCategoryExceeded();
                            }
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }

                if (currentCategory.PendingRemove)
                {
                    currentCategory.PendingRemove = false;
                }

                currentCategory.Title = newCategory.Title;
                currentCategory.TotalCapacity = newCategory.Capacity;
                await _dbContext.SaveChangesAsync();
            }
        }

        HashSet<Guid> newCategoriesIds = new(newCategories.Select(c => c.Id));
        List<VenueSeatCategory> categoriesToDelete = new();

        foreach (VenueSeatCategory currentCategory in currentCategories.Values)
        {
            if (newCategoriesIds.Contains(currentCategory.Id))
                continue;

            if (currentCategory.PendingRemove)
                continue;

            var upcomingEventsPrices = currentCategory.GetUpcomingEventsPrices();
            if (upcomingEventsPrices.Count > 0)
            {
                // Removed category still holds prices for upcoming events.
                // To avoid removing side effects, mark it as pending remove with manual resolution
                foreach (var eventSeatCategoryPrice in upcomingEventsPrices.Values.SelectMany(ps => ps))
                {
                    eventSeatCategoryPrice.OnVenueCategoryRemoved();
                }

                currentCategory.PendingRemove = true;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                categoriesToDelete.Add(currentCategory);
            }
        }

        _dbContext.VenueSeatCategories.RemoveRange(categoriesToDelete);
        await _dbContext.SaveChangesAsync();
    }
}
