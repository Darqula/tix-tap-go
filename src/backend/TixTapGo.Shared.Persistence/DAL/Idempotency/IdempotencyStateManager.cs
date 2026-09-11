using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

using TixTapGo.Shared.Persistence.Exceptions;

namespace TixTapGo.Shared.Persistence.DAL.Idempotency;

internal sealed class IdempotencyStateManager(
    IConnectionMultiplexer redisConnectionMultiplexer,
    ILogger<IdempotencyStateManager> logger)
{
    public async Task<object?> HandleIdempotencyKeyState(HttpContext context, Guid idempotencyKeyValue,
        Func<ValueTask<object?>> next)
    {
        var redisCacheDb = redisConnectionMultiplexer.GetDatabase();
    
        string cacheKey = GetIdempotencyCacheKey(context, idempotencyKeyValue);
    
        // Initialize new cache records with the Processing status to avoid cross-instance stampede
        IdempotentCacheValue currentCachedResult = await redisCacheDb.StringSetAndGetAsync(cacheKey,
            IdempotentCacheValue.Processing(), TimeSpan.FromMinutes(1), when: When.NotExists);
    
        switch (currentCachedResult.Status)
        {
            case IdempotentCacheValueStatus.Processing:
                // The request was already started but not yet completed.
                // We shouldn't start a new one, and we can't return any result yet
                throw new IdempotencyException("The Idempotency-Key header value is already being processed",
                    StatusCodes.Status409Conflict);
    
            case IdempotentCacheValueStatus.Completed:
                // Request was already successfully processed and cached - return cached result  
                context.Response.Headers[IdempotencyConstants.IdempotencyReplayedHeader] = bool.TrueString;
                return currentCachedResult.Value!.ToResult();
    
            case IdempotentCacheValueStatus.Null:
                // Request with this idempotency key wasn't processed yet.
                // Store the cache key in the context to cache the final response in the IdempotencyCachingMiddleware
                context.Items[IdempotencyConstants.IdempotencyCacheKey] = cacheKey;
                return await next();
    
            default:
                throw new IdempotencyException($"Unknown idempotency cache state: {currentCachedResult.Status}");
        }
    }

    public async Task<IdempotentResponse> CacheIdempotentResponseAsync(string cacheKey,
        IdempotentResponse idempotentResponse, HttpContext context)
    {
        var redisCacheDb = redisConnectionMultiplexer.GetDatabase();
        IdempotentCacheValue completedValue = IdempotentCacheValue.Completed(idempotentResponse);
        await redisCacheDb.StringSetAsync(cacheKey, completedValue, TimeSpan.FromHours(24));
        context.Response.Headers[IdempotencyConstants.IdempotencyReplayedHeader] = bool.FalseString;
        context.Items.Remove(IdempotencyConstants.IdempotencyCacheKey);

        return idempotentResponse;
    }

    private string GetIdempotencyCacheKey(HttpContext httpContext, Guid idempotencyKeyValue)
    {
        string requestPath = httpContext.Request.Path;
        if (string.IsNullOrEmpty(requestPath))
        {
            requestPath = httpContext.Request.PathBase;
            logger.LogWarning("Idempotency key cached with PathBase ({RequestPath}) key", requestPath);
        }

        string refinedPath = requestPath.Trim('/').Replace('/', '_');
        string httpMethod = httpContext.Request.Method.ToLowerInvariant();

        return $"idempotency:{refinedPath}:{httpMethod}:{idempotencyKeyValue}";
    }

    private sealed record IdempotentCacheValue(IdempotentCacheValueStatus Status, IdempotentResponse? Value)
    {
        public static IdempotentCacheValue Null() => new(IdempotentCacheValueStatus.Null, null);
        public static IdempotentCacheValue Processing() => new(IdempotentCacheValueStatus.Processing, null);

        public static IdempotentCacheValue Completed(IdempotentResponse? value) =>
            new(IdempotentCacheValueStatus.Completed, value);

        public static implicit operator RedisValue(IdempotentCacheValue value) => new(JsonSerializer.Serialize(value));

        public static implicit operator IdempotentCacheValue(RedisValue redisValue)
        {
            if (redisValue.IsNullOrEmpty)
            {
                return Null();
            }

            return JsonSerializer.Deserialize<IdempotentCacheValue>((string)redisValue!) ?? Null();
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    private enum IdempotentCacheValueStatus
    {
        Null,
        Processing,
        Completed
    }
}
