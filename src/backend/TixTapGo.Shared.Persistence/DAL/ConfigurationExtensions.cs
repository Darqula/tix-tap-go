using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        builder.AddMassTransitRabbitMq(rmqResourceName, massTransitConfiguration: configurator =>
        {
            massTransitConfiguration?.Invoke(configurator);

            configurator.AddEntityFrameworkOutbox<TDbContext>(efconfig =>
            {
                efconfig.UsePostgres();
                efconfig.UseBusOutbox();
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
