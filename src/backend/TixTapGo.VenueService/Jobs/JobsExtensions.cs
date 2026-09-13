using Hangfire;
using Hangfire.PostgreSql;

namespace TixTapGo.VenueService.Jobs;

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
}
