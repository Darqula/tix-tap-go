namespace TixTapGo.AppHost;

internal static class EventServiceResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddEventService(
        this IDistributedApplicationBuilder builder,
        TixTapGoInfrastructure infra,
        ClientAuthParameters authParams,
        IResourceBuilder<ProjectResource> authService,
        IResourceBuilder<ProjectResource> venueService)
    {
        var eventService = builder.AddProject<Projects.TixTapGo_EventService>("event-service")
            .WithReference(infra.EventsDb)
            .WithReference(authService)
            .WithReference(venueService)
            .WithReference(infra.RabbitMq)
            .WithReference(infra.Redis)
            .WaitFor(authService)
            .WithClientCredentialsAuth(authParams.EventServiceSecret, authParams.EncryptionKey);

        var eventServiceWorker = builder
            .AddProject<Projects.TixTapGo_EventService_Worker>("event-service-worker")
            .WithParentRelationship(eventService)
            .WithReference(infra.EventsDb)
            .WithReference(infra.RabbitMq)
            .WithReference(infra.Redis);

        var eventsDbMigration = eventService
            .AddEFMigrations("eventsdb-migration")
            .RunDatabaseUpdateOnStart()
            .WaitFor(infra.EventsDb);

        eventService.WaitForCompletion(eventsDbMigration);
        eventServiceWorker.WaitForCompletion(eventsDbMigration);

        return eventService;
    }
}
