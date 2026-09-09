using MassTransit;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.DAL;

public class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventsInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var entitiesWithDomainEvents = eventData.Context!.ChangeTracker.Entries<EntityBase>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        if (entitiesWithDomainEvents.Count > 0)
        {
            // IPublishEndpoint resolves DbContext internally, thus we must
            // use ServiceLocator to avoid circular dependencies
            var publishEndpoint = _serviceProvider.GetRequiredService<IPublishEndpoint>();

            foreach (var entityWithEvents in entitiesWithDomainEvents)
            {
                foreach (var domainEvent in entityWithEvents.DomainEvents)
                {
                    await publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
                }

                entityWithEvents.ClearDomainEvents();
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
