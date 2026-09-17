using Hangfire;

using Microsoft.IdentityModel.Tokens;

using OpenIddict.Client;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Endpoints.Event;
using TixTapGo.EventService.Endpoints.EventSeatCategoryPrice;
using TixTapGo.EventService.Integrations.InternalServices.VenueService;
using TixTapGo.EventService.Jobs;
using TixTapGo.Shared.Auth;
using TixTapGo.Shared.Converters;
using TixTapGo.Shared.Exceptions;
using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.Shared.Persistence.DAL.Idempotency;

using Extensions = Microsoft.Extensions.Hosting.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("eventsdb");
builder.AddDbContextWithMassTransit<EventDbContext>(connectionString, "rabbitmq");
builder.AddRedisDistributedCache("redis");
builder.AddRedisClient("redis");
builder.Services.AddHybridCache();

builder.Services
    .AddOpenApi(o => o.AddOperationTransformer<CaseInsensitiveEnumParameterTransformer>())
    .AddProblemDetails()
    .AddValidation();

builder.Services
    .AddAccessTokenProvider()
    .AddInternalServiceHttpClient<VenueServiceHttpClient>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new CaseInsensitiveEnumConverterFactory()));

builder.Services.AddOpenIddict()
    .AddClient(options =>
    {
        var clientCredentialsFlowConfig = builder.Configuration.GetSection("Authentication");
        string? clientId = clientCredentialsFlowConfig["ClientId"];
        string? clientSecret = clientCredentialsFlowConfig["ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Authentication:ClientId or Authentication:ClientSecret are not configured.");
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
    })
    .AddValidation(options =>
    {
        string? clientCredentialsEncryptionKey =
            builder.Configuration.GetSection("Authentication")["ClientCredentialsEncryptionKey"];
        if (string.IsNullOrWhiteSpace(clientCredentialsEncryptionKey))
        {
            throw new InvalidOperationException("Authentication:ClientCredentialsEncryptionKey is not configured");
        }

        options.SetIssuer("https://auth-service");
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String(clientCredentialsEncryptionKey)));

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddHangfireConfigured(builder.Configuration.GetConnectionString("eventsdb"));

builder.AddInternalOnlyAuthorization(Extensions.GatewayClientId);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddIdempotency();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMiddleware<IdempotencyCachingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseDeveloperExceptionPage();
    app.MapHangfireDashboard("/events/hangfire").RequireAuthorization(Extensions.InternalOnlyPolicy);
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapEventEndpoints()
    .MapEventSeatCategoryPriceEndpoints();

app.RegisterJobs();

app.Run();
