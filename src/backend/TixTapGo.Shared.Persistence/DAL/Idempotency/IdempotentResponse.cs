using Microsoft.AspNetCore.Http;

namespace TixTapGo.Shared.Persistence.DAL.Idempotency;

internal sealed record IdempotentResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    Dictionary<string, string?[]> Headers)
{
    private static readonly HashSet<string> ExcludedHeaders = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "Content-Type",
        "Content-Length",
        "Date",
        "Server",
        "Transfer-Encoding",
        "Connection"
    };
    
    public static IdempotentResponse Capture(MemoryStream stream, HttpContext httpContext)
    {
        var capturedHeaders = httpContext.Response.Headers
            .Where(header => !ExcludedHeaders.Contains(header.Key))
            .ToDictionary(
                header => header.Key,
                header => header.Value.ToArray()
            );

        return new IdempotentResponse(
            httpContext.Response.StatusCode,
            httpContext.Response.ContentType,
            stream.ToArray(),
            capturedHeaders);
    }

    public IResult ToResult() => new ReplayResult(this);

    private sealed class ReplayResult(IdempotentResponse response) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = response.StatusCode;
            if (response.ContentType != null)
            {
                httpContext.Response.ContentType = response.ContentType;
            }

            foreach (var (key, values) in response.Headers)
            {
                httpContext.Response.Headers[key] = values;
            }

            httpContext.Response.ContentLength = response.Body.Length;
            await httpContext.Response.Body.WriteAsync(response.Body);
        }
    }
}
