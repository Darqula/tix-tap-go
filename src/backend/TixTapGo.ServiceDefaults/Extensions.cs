using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Name of the authorization policy that only callers authenticated as one of the client ids
    /// passed to <see cref="AddInternalOnlyAuthorization{TBuilder}"/> satisfy.
    /// </summary>
    public const string InternalOnlyPolicy = "InternalOnly";

    /// <summary>
    /// OpenIddict client id the Gateway authenticates as when it exchanges its own client
    /// credentials to call into internal services.
    /// </summary>
    public const string GatewayClientId = "gateway";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Registers the <see cref="InternalOnlyPolicy"/> authorization policy restricted to
    /// callers authenticated as one of <paramref name="allowedClientIds"/>. Sets it as the
    /// app's fallback policy
    /// </summary>
    /// <param name="allowedClientIds">
    /// OpenIddict client ids (matched against the <c>sub</c> claim) allowed to call this service
    /// </param>
    public static TBuilder AddInternalOnlyAuthorization<TBuilder>(this TBuilder builder,
        params string[] allowedClientIds)
        where TBuilder : IHostApplicationBuilder
    {
        if (allowedClientIds.Length == 0)
        {
            throw new ArgumentException("At least one allowed client id must be specified.",
                nameof(allowedClientIds));
        }

        var authorizationBuilder = builder.Services.AddAuthorizationBuilder()
            .AddPolicy(InternalOnlyPolicy, policy => ConfigureInternalOnlyPolicy(policy, allowedClientIds));

        var fallbackPolicy = new AuthorizationPolicyBuilder();
        ConfigureInternalOnlyPolicy(fallbackPolicy, allowedClientIds);
        authorizationBuilder.SetFallbackPolicy(fallbackPolicy.Build());

        return builder;
    }

    private static void ConfigureInternalOnlyPolicy(AuthorizationPolicyBuilder policy,
        IReadOnlyCollection<string> allowedClientIds) =>
        policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireClaim(OpenIddictConstants.Claims.Subject, allowedClientIds);

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            // Anonymous: the orchestrator/dashboard probes these without a bearer token.
            app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath,
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }).AllowAnonymous();
        }

        return app;
    }
}
