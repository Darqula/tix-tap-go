namespace TixTapGo.Shared.Web.Idempotency;

internal static class IdempotencyConstants
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotencyReplayedHeader = "Idempotency-Replayed";
    public const string IdempotencyCacheKey = "Idempotency-Cache-Key";
}
