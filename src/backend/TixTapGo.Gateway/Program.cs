using System.Net.Http.Headers;

using OpenIddict.Client;

using Scalar.AspNetCore;

using TixTapGo.Gateway.Services;
using TixTapGo.Gateway.Services.Abstractions;

using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenIddict()
    .AddClient(options =>
    {
        var clientCredentialsFlowConfig = builder.Configuration.GetSection("ClientCredentialsFlow");
        string? clientId = clientCredentialsFlowConfig["ClientId"];
        string? clientSecret = clientCredentialsFlowConfig["ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "ClientCredentialsFlow:ClientId or ClientCredentialsFlow:ClientSecret are not configured.");
        }

        options.AllowClientCredentialsFlow();
        options.DisableTokenStorage();
        options.UseSystemNetHttp();
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://auth-service", UriKind.Absolute),
            ConfigurationEndpoint = new Uri("https://auth-service/.well-known/openid-configuration", UriKind.Absolute),
            ClientId = clientId,
            ClientSecret = clientSecret,
        });
    });

builder.Services
    .AddMemoryCache()
    .AddSingleton<IAccessTokenProvider, AccessTokenProvider>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(builderContext =>
    {
        if (builderContext.Route.Metadata != null
            && builderContext.Route.Metadata.TryGetValue("RequiresInternalToken", out string? requiresTokenString))
        {
            if (!bool.TryParse(requiresTokenString, out var requiresToken))
            {
                throw new ArgumentException(
                    $"Route '{builderContext.Route.RouteId}': RequiresInternalToken must be a boolean (true or false)");
            }

            if (!requiresToken)
            {
                return;
            }

            builderContext.AddRequestTransform(async transformContext =>
            {
                var token = await transformContext.HttpContext.RequestServices
                    .GetRequiredService<IAccessTokenProvider>()
                    .GetAccessTokenAsync(transformContext.CancellationToken);
                transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });
        }
    });

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(o =>
    {
        o.Title = "TixTapGo API";
        o.AddDocuments([
            new ScalarDocument("events", "Events", "/openapi/events.json", true),
            new ScalarDocument("venues", "Venues", "/openapi/venues.json")
        ]);
        o.Servers = [];
    });
}

app.Run();
