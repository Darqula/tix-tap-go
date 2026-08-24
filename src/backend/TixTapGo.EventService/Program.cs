using Microsoft.IdentityModel.Tokens;

using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

using TixTapGo.EventService;
using TixTapGo.EventService.DAL;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<EventDbContext>("eventsdb");

builder.Services
    .AddOpenApi()
    .AddProblemDetails()
    .AddValidation();

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://authservice");
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("dGl4dGFwZ28tbG9uZy1lbmNyeXB0aW9uLXBhc3N3b3I=")));

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InternalOnly", policy => policy
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .RequireClaim(OpenIddictConstants.Claims.Subject, ["gateway"])
    );

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    
    logging.RequestBodyLogLimit = 4096; 
    logging.ResponseBodyLogLimit = 4096;
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
    app.UseHttpLogging();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("events")
    .RequireAuthorization("InternalOnly")
    .MapEventEndpoints();

app.Run();
