using Hangfire;

using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

string? dbConnectionString = builder.Configuration.GetConnectionString("venuesdb");
builder.AddDbContextWithMassTransit<VenueDbContext>(dbConnectionString, "rabbitmq");
builder.AddRedisDistributedCache("redis");
builder.AddRedisClient("redis");
builder.Services.AddHybridCache();
builder.Services.AddHangfireConfigured(dbConnectionString);
builder.Services.AddHangfireServer();

var host = builder.Build();
host.Run();
