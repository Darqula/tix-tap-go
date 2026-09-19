namespace TixTapGo.AppHost;

internal static class VenueServiceResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddVenueService(
        this IDistributedApplicationBuilder builder,
        TixTapGoInfrastructure infra,
        ClientAuthParameters authParams,
        IResourceBuilder<ProjectResource> authService)
    {
        var venueService = builder.AddProject<Projects.TixTapGo_VenueService>("venue-service")
            .WithReference(infra.VenuesDb)
            .WithReference(authService)
            .WithReference(infra.RabbitMq)
            .WithReference(infra.Redis)
            .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", authParams.EncryptionKey);

        var venueServiceWorker = builder
            .AddProject<Projects.TixTapGo_VenueService_Worker>("venue-service-worker")
            .WithParentRelationship(venueService)
            .WithReference(infra.VenuesDb)
            .WithReference(infra.RabbitMq)
            .WithReference(infra.Redis);

        var venuesDbMigration = venueService
            .AddEFMigrations("venuesdb-migration")
            .RunDatabaseUpdateOnStart()
            .WaitFor(infra.VenuesDb);

        venueService.WaitForCompletion(venuesDbMigration);
        venueServiceWorker.WaitForCompletion(venuesDbMigration);

        return venueService;
    }
}
