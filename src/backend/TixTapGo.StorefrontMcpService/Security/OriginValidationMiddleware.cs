using Microsoft.Net.Http.Headers;

namespace TixTapGo.StorefrontMcpService.Security;

/// <summary>
/// Rejects requests with not supported value in the Origin header
/// </summary>
internal sealed class OriginValidationMiddleware : IMiddleware
{
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<OriginValidationMiddleware> _logger;

    public OriginValidationMiddleware(IConfiguration configuration, ILogger<OriginValidationMiddleware> logger)
    {
        _logger = logger;
        _allowedOrigins = new HashSet<string>(
            configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers.TryGetValue(HeaderNames.Origin, out var origin)
            && !_allowedOrigins.Contains(origin.ToString().TrimEnd('/')))
        {
            _logger.LogWarning("Rejected a request with a disallowed Origin header");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
