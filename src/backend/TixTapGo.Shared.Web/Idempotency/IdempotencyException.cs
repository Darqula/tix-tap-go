namespace TixTapGo.Shared.Web.Idempotency;

internal sealed class IdempotencyException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
