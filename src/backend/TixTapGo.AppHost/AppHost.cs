var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("local-postgres")
    .WithDataVolume();

var postgresDb = postgres.AddDatabase("eventsdb");

var eventService = builder
    .AddProject<Projects.TixTapGo_EventService>("eventservice")
    .WithReference(postgresDb);

builder.AddProject<Projects.TixTapGo_Gateway>("gateway")
    .WithReference(eventService)
    .WaitFor(eventService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
