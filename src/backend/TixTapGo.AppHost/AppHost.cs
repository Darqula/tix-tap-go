var builder = DistributedApplication.CreateBuilder(args);

var pgPassword = builder.AddParameter("local-postgres-password", secret: true);
var postgres = builder
    .AddPostgres("local-postgres", port: 5432, password: pgPassword)
    .WithDataVolume();

var eventsDb = postgres.AddDatabase("eventsdb");
var authDb = postgres.AddDatabase("authdb");

var authService = builder.AddProject<Projects.TixTapGo_AuthService>("authservice")
    .WithReference(authDb)
    .WaitFor(authDb);

var eventService = builder
    .AddProject<Projects.TixTapGo_EventService>("eventservice")
    .WithReference(eventsDb)
    .WithReference(authService)
    .WaitFor(eventsDb);

var gatewaySecret = builder.AddParameter("gateway-secret", secret: true);
builder.AddProject<Projects.TixTapGo_Gateway>("gateway")
    .WithReference(eventService)
    .WithReference(authService)
    .WaitFor(eventService)
    .WaitFor(authService)
    .WithEnvironment("ClientCredentialsFlow__ClientSecret", gatewaySecret)
    .WithExternalHttpEndpoints();

builder.Build().Run();
