using Hangfire;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<EventDbContext>("eventsdb");
builder.Services.AddHangfireConfigured(builder.Configuration.GetConnectionString("eventsdb"));
builder.Services.AddHangfireServer();

var host = builder.Build();
host.Run();
