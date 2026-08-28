using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var pgPassword = builder.AddParameter("local-postgres-password", secret: true);
var postgres = builder
    .AddPostgres("local-postgres", port: 5432, password: pgPassword)
    .WithDataVolume();

var eventsDb = postgres.AddDatabase("eventsdb");
var authDb = postgres.AddDatabase("authdb");
var venuesDb = postgres.AddDatabase("venuesdb");

var clientCredentialsEncryptionKey = builder
    .AddParameter("client-credentials-encryption-key", secret: true)
    .WithDescription($"Randomly generated key to set: {Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}");

var authService = builder.AddProject<Projects.TixTapGo_AuthService>("authservice")
    .WithReference(authDb)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey)
    .WaitFor(authDb);

var eventService = builder
    .AddProject<Projects.TixTapGo_EventService>("eventservice")
    .WithReference(eventsDb)
    .WithReference(authService)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey)
    .WaitFor(eventsDb);

var venueService = builder.AddProject<Projects.TixTapGo_VenueService>("venueservice")
    .WithReference(venuesDb)
    .WithReference(authService)
    .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", clientCredentialsEncryptionKey)
    .WaitFor(venuesDb);

var gatewaySecret = builder.AddParameter("gateway-secret", secret: true);
builder.AddProject<Projects.TixTapGo_Gateway>("gateway")
    .WithReference(eventService)
    .WithReference(authService)
    .WithReference(venueService)
    .WaitFor(eventService)
    .WaitFor(authService)
    .WaitFor(venueService)
    .WithEnvironment("ClientCredentialsFlow__ClientSecret", gatewaySecret)
    .WithExternalHttpEndpoints();

builder.Build().Run();
