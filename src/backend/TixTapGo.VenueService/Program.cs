using Microsoft.IdentityModel.Tokens;

using TixTapGo.Shared.Converters;
using TixTapGo.Shared.Exceptions;
using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.Seat;
using TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;
using TixTapGo.VenueService.Endpoints.SeatCategory;
using TixTapGo.VenueService.Endpoints.SeatingMapVersion;
using TixTapGo.VenueService.Endpoints.Venue;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string? dbConnectionString = builder.Configuration.GetConnectionString("venuesdb");
builder.AddDbContextWithMassTransit<VenueDbContext>(dbConnectionString, "rabbitmq");

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

builder.AddInternalOnlyAuthorization(Extensions.GatewayClientId, Extensions.EventServiceId);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddTransient<SeatExcelTemplate>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseDeveloperExceptionPage();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var venueEndpoints = app
    .MapVenueEndpoints();

venueEndpoints.MapSeatCategoryEndpoints();
venueEndpoints
    .MapSeatingMapVersionEndpoints()
    .MapSeatEndpoints();

app.Run();
