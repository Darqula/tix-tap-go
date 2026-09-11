using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace TixTapGo.Shared.Persistence.DAL.Idempotency;

internal sealed class IdempotencyPrerequisitesValidator
{
    private readonly HashSet<string> _supportedHttpMethods = ["POST", "PUT", "PATCH"];

    public Result Validate(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(IdempotencyConstants.IdempotencyKeyHeader,
                out StringValues idempotencyKeyValueRaw))
        {
            return new Result(false, Error: "The Idempotency-Key header must be provided",
                StatusCode: StatusCodes.Status400BadRequest);
        }

        if (!Guid.TryParse(idempotencyKeyValueRaw, out Guid idempotencyKeyValue))
        {
            return new Result(false, Error: "Idempotency-Key header value must be a UUID",
                StatusCode: StatusCodes.Status400BadRequest);
        }

        string httpMethod = httpContext.Request.Method;
        if (!_supportedHttpMethods.Contains(httpMethod))
        {
            return new Result(false, Error: $"Idempotency handling is not supported for {httpMethod}",
                StatusCode: StatusCodes.Status500InternalServerError);
        }

        return new Result(true,  idempotencyKeyValue);
    }

    internal sealed record Result(bool Success, Guid? IdempotencyKey = null, string? Error = null, int? StatusCode = null);
}
