using Microsoft.IdentityModel.Tokens;

using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

using TixTapGo.VenueService;
using TixTapGo.VenueService.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<VenueDbContext>("venuesdb");

builder.Services
    .AddOpenApi()
    .AddProblemDetails()
    .AddValidation();

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InternalOnly", policy => policy
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .RequireClaim(OpenIddictConstants.Claims.Subject, ["gateway"])
    );

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app
    .MapGroup("venues")
    .RequireAuthorization("InternalOnly")
    .MapVenueEndpoints();

app.Run();
