using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TixTapGo.AuthService.Cli;
using TixTapGo.AuthService.Cli.Operations;
using TixTapGo.AuthService.Data;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("authdb"));
        options.UseOpenIddict();
    });

builder.Services
    .AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    });

builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddTransient<SecretGenerator>();
builder.Services.AddTransient<IListAppsOperation, ListAppsOperation>();
builder.Services.AddKeyedTransient<IManageAppOperation, RegisterAppOperation>(AppOperations.RegisterApp);
builder.Services.AddKeyedTransient<IManageAppOperation, DeleteAppOperation>(AppOperations.DeleteApp);
builder.Services.AddKeyedTransient<IManageAppOperation, RotateAppSecretOperation>(AppOperations.RotateAppSecret);

var app = builder.Build();

var manager = new AppManagementFlow(app);
var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
await manager.StartAsync(appLifetime.ApplicationStopping);
