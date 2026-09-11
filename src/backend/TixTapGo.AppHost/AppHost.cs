using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var pgPassword = builder.AddParameter("local-postgres-password", secret: true);
var postgres = builder
    .AddPostgres("local-postgres", password: pgPassword)
    .WithDataVolume();

var eventsDb = postgres.AddDatabase("eventsdb");
var authDb = postgres.AddDatabase("authdb");
var venuesDb = postgres.AddDatabase("venuesdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var clientCredentialsEncryptionKey = builder
    .AddParameter("client-credentials-encryption-key", secret: true)
    .WithDescription($"Randomly generated key to set: {Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}");

var authService = builder.AddProject<Projects.TixTapGo_AuthService>("auth-service")
    .WithReference(authDb)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey);

var authDbMigration = authService
    .AddEFMigrations("authdb-migration")
    .RunDatabaseUpdateOnStart()
    .WaitFor(authDb);

authService.WaitForCompletion(authDbMigration);

var eventService = builder.AddProject<Projects.TixTapGo_EventService>("event-service");
var venueService = builder.AddProject<Projects.TixTapGo_VenueService>("venue-service");

var eventServiceSecret = builder.AddParameter("event-service-secret", secret: true);
eventService
    .WithReference(eventsDb)
    .WithReference(authService)
    .WithReference(venueService)
    .WithReference(rabbitMq)
    .WithReference(redis)
    .WaitFor(authService)
    .WithEnvironment("Authentication__ClientSecret", eventServiceSecret)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey);

var eventServiceWorker = builder
    .AddProject<Projects.TixTapGo_EventService_Worker>("event-service-worker")
    .WithParentRelationship(eventService)
    .WithReference(eventsDb)
    .WithReference(rabbitMq)
    .WithReference(redis);

var eventsDbMigration = eventService
    .AddEFMigrations("eventsdb-migration")
    .RunDatabaseUpdateOnStart()
    .WaitFor(eventsDb);

eventService.WaitForCompletion(eventsDbMigration);
eventServiceWorker.WaitForCompletion(eventsDbMigration);

venueService
    .WithReference(venuesDb)
    .WithReference(authService)
    .WithReference(eventService)
    .WithReference(rabbitMq)
    .WithReference(redis)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey);

var venuesDbMigration = venueService
    .AddEFMigrations("venuesdb-migration")
    .RunDatabaseUpdateOnStart()
    .WaitFor(venuesDb);

venueService.WaitForCompletion(venuesDbMigration);

var gatewaySecret = builder.AddParameter("gateway-secret", secret: true);
builder.AddProject<Projects.TixTapGo_Gateway>("gateway")
    .WithReference(eventService)
    .WithReference(authService)
    .WithReference(venueService)
    .WaitFor(eventService)
    .WaitFor(authService)
    .WaitFor(venueService)
    .WithEnvironment("Authentication__ClientSecret", gatewaySecret)
    .WithExternalHttpEndpoints();

builder.Build().Run();
