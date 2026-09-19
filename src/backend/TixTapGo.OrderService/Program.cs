using Microsoft.IdentityModel.Tokens;

using OpenIddict.Client;

using TixTapGo.OrderService.DAL;
using TixTapGo.Shared.Auth;
using TixTapGo.Shared.Converters;
using TixTapGo.Shared.Exceptions;
using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.Shared.Web.ETag;
using TixTapGo.Shared.Web.Idempotency;

using Extensions = Microsoft.Extensions.Hosting.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("ordersdb");
builder.AddDbContextWithMassTransit<OrderDbContext>(connectionString, "rabbitmq");
builder.AddRedisDistributedCache("redis");
builder.AddRedisClient("redis");
builder.Services.AddHybridCache();

builder.Services
    .AddOpenApi(o => o.AddOperationTransformer<CaseInsensitiveEnumParameterTransformer>())
    .AddProblemDetails()
    .AddValidation();

builder.Services.AddAccessTokenProvider();
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

builder.AddInternalOnlyAuthorization(Extensions.GatewayClientId);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddIdempotency();
builder.Services.AddTransient<ETagMiddleware>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMiddleware<ETagMiddleware>();
app.UseMiddleware<IdempotencyCachingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseDeveloperExceptionPage();
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("orders/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
