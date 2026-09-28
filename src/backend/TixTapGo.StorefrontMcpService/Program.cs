using ModelContextProtocol.AspNetCore;

using TixTapGo.StorefrontMcpService.Integrations.Catalog;
using TixTapGo.StorefrontMcpService.Schedule;
using TixTapGo.StorefrontMcpService.Security;
using TixTapGo.StorefrontMcpService.Tools;

const string McpPath = "/mcp";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient<ICatalogClient, CatalogHttpClient>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddTransient<OriginValidationMiddleware>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<ScheduleTools>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseWhen(context => context.Request.Path.StartsWithSegments(McpPath),
    mcp => mcp.UseMiddleware<OriginValidationMiddleware>());
app.MapMcp(McpPath);

app.Run();
