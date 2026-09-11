namespace TixTapGo.Shared.Persistence.Exceptions;

internal sealed class IdempotencyException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
