using Microsoft.IdentityModel.Tokens;

using TixTapGo.EventService;
using TixTapGo.EventService.DAL;
using TixTapGo.Shared.Converters;
using TixTapGo.Shared.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<EventDbContext>("eventsdb");

builder.Services
    .AddOpenApi(o => o.AddOperationTransformer<CaseInsensitiveEnumParameterTransformer>())
    .AddProblemDetails()
    .AddValidation();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new CaseInsensitiveEnumConverterFactory()));

builder.Services.AddOpenIddict()
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

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseDeveloperExceptionPage();
    app.UseHttpLogging();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("events")
    .MapEventEndpoints();

app.Run();
