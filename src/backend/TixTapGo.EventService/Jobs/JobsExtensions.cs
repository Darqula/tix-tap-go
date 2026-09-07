using Hangfire;
using Hangfire.PostgreSql;

namespace TixTapGo.EventService.Jobs;

internal static class JobsExtensions
{
    public static void AddHangfireConfigured(this IServiceCollection services, string? dbConnectionString)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage((option) =>
                option.UseNpgsqlConnection(dbConnectionString)
            )
        );
    }

    public static void RegisterJobs(this WebApplication app)
    {
        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<ExpireStaleEventsJob>(nameof(ExpireStaleEventsJob),
            job => job.ExecuteAsync(null!), ExpireStaleEventsJob.PeriodCron);
    }
}
