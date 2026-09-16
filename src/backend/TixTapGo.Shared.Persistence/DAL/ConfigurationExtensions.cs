using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TixTapGo.Shared.Persistence.Exceptions;

namespace TixTapGo.Shared.Persistence.DAL;

public static class ConfigurationExtensions
{
    public static void AddDbContextWithMassTransit<TDbContext>(this IHostApplicationBuilder builder,
        string? dbConnectionString, string rmqResourceName,
        Action<IBusRegistrationConfigurator>? massTransitConfiguration = null) where TDbContext : DbContext
    {
        // Replaced AddNpgsqlDbContext with this to avoid context pooling
        // The pooling break the MassTransit's transactional outbox operations
        builder.Services.AddDbContext<TDbContext>(options =>
        {
            options.UseNpgsql(dbConnectionString);
        });
        builder.EnrichNpgsqlDbContext<TDbContext>();
        builder.Services.AddExceptionHandler<DbConcurrencyExceptionHandler>();

        builder.AddMassTransitRabbitMq(rmqResourceName, massTransitConfiguration: configurator =>
        {
            massTransitConfiguration?.Invoke(configurator);

            configurator.AddEntityFrameworkOutbox<TDbContext>(efconfig =>
            {
                efconfig.UsePostgres();
                // ReadCommitted keeps conflicts resolvable via the endpoint retry below.
                // By default, MassTransit uses Serializable
                efconfig.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
                efconfig.UseBusOutbox();
            });

            // Configuration for every consumer, setup consumer outboxing
            // (inbox + outbox + one transaction per consumed message). Conflict retries
            // at the message level, and the _error DLQ once retries are exhausted
            configurator.AddConfigureEndpointsCallback((context, _, endpointConfigurator) =>
            {
                endpointConfigurator.UseMessageRetry(retry =>
                {
                    retry.Handle<DbUpdateConcurrencyException>();
                    // hardcoded the intervals to support long and short jobs well
                    retry.Intervals(
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(20),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(45));
                });
                endpointConfigurator.UseEntityFrameworkOutbox<TDbContext>(context);
            });
        });
        builder.Services.AddScoped<ISaveChangesInterceptor, DomainEventsInterceptor>();
        // Toolkit's multi-bus path doesn't register IBusControl, which the outbox delivery service requires
        builder.Services.AddSingleton<IBusControl>(sp => (IBusControl)sp.GetRequiredService<IBus>());
    }

    public static void AddOutboxMessageTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessageEntity(e => e.ToTable("OutboxMessage", "outbox"));
        modelBuilder.AddOutboxStateEntity(e => e.ToTable("OutboxState", "outbox"));
        modelBuilder.AddInboxStateEntity(e => e.ToTable("InboxState", "outbox"));
    }
}
