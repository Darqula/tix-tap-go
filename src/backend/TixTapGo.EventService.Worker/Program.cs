using Hangfire;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Jobs;
using TixTapGo.Shared.Persistence.DAL;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

string? dbConnectionString = builder.Configuration.GetConnectionString("eventsdb");
builder.AddDbContextWithMassTransit<EventDbContext>(dbConnectionString, "rabbitmq");
builder.Services.AddHangfireConfigured(dbConnectionString);
builder.Services.AddHangfireServer();

var host = builder.Build();
host.Run();
