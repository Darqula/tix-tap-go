using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using TixTapGo.Shared.Persistence.DAL.Idempotency;

using Xunit;

namespace TixTapGo.Shared.Persistence.Tests;

/// <summary>
/// Integration tests for <see cref="IdempotentEndpointFilter"/> against a real Redis instance
/// (via Testcontainers). The filter under test is hosted in a minimal in-memory
/// <see cref="WebApplication"/> (TestServer) that registers the container-backed
/// <c>IConnectionMultiplexer</c>, mirroring how production services wire it up. A fresh app is
/// built per test, so endpoint-side state (counters, blocking gates) is always clean; Redis keys
/// are unique per test because every test generates its own idempotency keys.
/// </summary>
[Collection(RedisCollection.Name)]
public sealed class IdempotentEndpointFilterTests(RedisCollectionFixture fixture) : IAsyncLifetime
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string IdempotencyReplayedHeader = "Idempotency-Replayed";

    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private ConnectionMultiplexer _redis = null!;

    public async Task InitializeAsync()
    {
        _redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);

        // Pin the environment: IDE test runners inject ASPNETCORE_ENVIRONMENT=Development, which
        // auto-adds the Developer Exception Page middleware and would swallow handler exceptions
        // as 500 responses, breaking tests that assert exceptions propagate out of the pipeline.
        // Changing the environment later via builder.WebHost throws, so it must be set upfront.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IConnectionMultiplexer>(_redis);
        builder.Services.AddSingleton<InvocationCounter>();
        builder.Services.AddSingleton<BlockingState>();
        builder.Services.AddSingleton<FailOnceHandler>();
        builder.Services.AddIdempotency();

        WebApplication app = builder.Build();

        // Pipeline order mirrors the production contract: the exception handler must sit
        // DOWNSTREAM of IdempotencyCachingMiddleware, so the error response it produces is
        // written into the caching middleware's buffered body and can be captured and cached.
        app.UseMiddleware<IdempotencyCachingMiddleware>();
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            try
            {
                await next();
            }
            catch (InvalidOperationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { detail = ex.Message });
            }
        });
        app.MapGet("/pass-through", () => Results.Ok(new { method = "get" }))
            .WithIdempotencyCheck();

        app.MapPost("/echo", async (HttpRequest request) =>
            {
                using var reader = new StreamReader(request.Body);
                string body = await reader.ReadToEndAsync();
                return Results.Ok(new { echoed = body });
            })
            .WithIdempotencyCheck();

        app.MapPost("/count", (InvocationCounter counter) => Results.Ok(new { count = counter.Increment() }))
            .WithIdempotencyCheck();

        app.MapPost("/not-found", () => Results.NotFound(new { error = "missing" }))
            .WithIdempotencyCheck();

        app.MapPost("/created", () => Results.Created("/created/1", new { id = 1 }))
            .WithIdempotencyCheck();

        app.MapPost("/custom-header", (HttpContext httpContext) =>
            {
                httpContext.Response.Headers["X-Custom"] = "custom-value";
                return Results.Ok(new { ok = true });
            })
            .WithIdempotencyCheck();

        app.MapPost("/items/{id}", (string id) => Results.Ok(new { id, method = "post" }))
            .WithIdempotencyCheck();

        app.MapPut("/items/{id}", (string id) => Results.Ok(new { id, method = "put" }))
            .WithIdempotencyCheck();

        app.MapPatch("/patch", () => Results.Ok(new { method = "patch" }))
            .WithIdempotencyCheck();

        app.MapPost("/fail-once", (FailOnceHandler handler) => handler.Invoke())
            .WithIdempotencyCheck();

        // Throws an exception type the test app's exception middleware does not handle, so the
        // exception propagates out of the pipeline instead of producing an error response.
        app.MapPost("/fail-unhandled", () => { throw new NotSupportedException("unhandled"); })
            .WithIdempotencyCheck();

        app.MapPost("/blocking", async (BlockingState state) =>
            {
                state.SignalStarted();
                await state.WaitUntilReleasedAsync();
                return Results.Ok(new { done = true });
            })
            .WithIdempotencyCheck();

        _app = app;
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        _client.Dispose();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task Invoke_MissingIdempotencyKeyHeader_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("The Idempotency-Key header must be provided", body);
    }

    [Fact]
    public async Task Invoke_NonUuidIdempotencyKey_Returns400()
    {
        using var request = CreateRequest(HttpMethod.Post, "/echo", "not-a-uuid");

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key header value must be a UUID", body);
    }

    /// <summary>
    /// Documents the validation ordering: header presence is checked before the supported-method
    /// check, so even a GET request is rejected with 400 when the key is missing entirely.
    /// </summary>
    [Fact]
    public async Task Invoke_UnsupportedMethodWithoutHeader_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/pass-through");

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("The Idempotency-Key header must be provided", body);
    }

    /// <summary>
    /// Format validation also precedes the supported-method check: a GET with a malformed key is
    /// rejected with 400 before the method check runs.
    /// </summary>
    [Fact]
    public async Task Invoke_UnsupportedMethodWithMalformedKey_Returns400()
    {
        using var request = CreateRequest(HttpMethod.Get, "/pass-through", "not-a-uuid");

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key header value must be a UUID", body);
    }

    /// <summary>
    /// Unsupported methods fail fast with 500 before Redis is ever touched: the method check
    /// returns before the cache key is built or any cache entry is created.
    /// </summary>
    [Fact]
    public async Task Invoke_UnsupportedMethodWithValidKey_Returns500ProblemDetails()
    {
        Guid key = NewKey();

        using var request = CreateRequest(HttpMethod.Get, "/pass-through", key.ToString());
        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency handling is not supported for GET", body);
    }

    [Fact]
    public async Task Invoke_FirstExecution_SetsReplayedHeaderToFalse()
    {
        Guid key = NewKey();

        using var request = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "hello" }));
        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("False", GetRequiredHeader(response, IdempotencyReplayedHeader));
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("hello", body);
    }

    [Fact]
    public async Task Replay_SameKey_ReturnsCachedBodyWithReplayedHeader()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "hello" }));

        HttpResponseMessage first = await _client.SendAsync(firstRequest);
        string firstBody = await first.Content.ReadAsStringAsync();

        using var replayRequest = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "different-body-ignored-on-replay" }));
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(firstBody, await replay.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Replay_DoesNotReexecuteHandler()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/count", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/count", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        using var freshKeyRequest = CreateRequest(HttpMethod.Post, "/count", NewKey().ToString());
        HttpResponseMessage freshKeyResponse = await _client.SendAsync(freshKeyRequest);

        Assert.Equal((await first.Content.ReadAsStringAsync()), (await replay.Content.ReadAsStringAsync()));
        JsonElement replayJson = await replay.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement freshJson = await freshKeyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, replayJson.GetProperty("count").GetInt32());
        Assert.Equal(2, freshJson.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Replay_PreservesNonSuccessStatusCode()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/not-found", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/not-found", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Replay_CreatedResponse_PreservesStatusCodeAndLocation()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/created", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/created", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal("/created/1", replay.Headers.Location?.ToString());
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
    }

    [Fact]
    public async Task Replay_CapturesAndRestoresCustomResponseHeaders()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/custom-header", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/custom-header", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal("custom-value", GetRequiredHeader(first, "X-Custom"));
        Assert.Equal("custom-value", GetRequiredHeader(replay, "X-Custom"));
    }

    [Fact]
    public async Task Invoke_PatchRequestsAreCachedAndReplayed()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Patch, "/patch", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Patch, "/patch", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// A cached entry is scoped to the exact request path: reusing the key against another path
    /// (even one sharing the same route template's method) executes the handler for that path.
    /// </summary>
    [Fact]
    public async Task Replay_SameKeyDifferentPath_ExecutesHandlerAgain()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/items/1", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/items/2", key.ToString());
        HttpResponseMessage second = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("False", GetRequiredHeader(second, IdempotencyReplayedHeader));
        JsonElement firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("1", firstJson.GetProperty("id").GetString());
        Assert.Equal("2", secondJson.GetProperty("id").GetString());
    }

    /// <summary>
    /// A cached entry is scoped to the HTTP method too: the same key on a different method
    /// executes that method's handler instead of replaying the other method's cached response.
    /// </summary>
    [Fact]
    public async Task Replay_SameKeyDifferentMethod_ExecutesHandlerAgain()
    {
        Guid key = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/items/1", key.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var replayRequest = CreateRequest(HttpMethod.Put, "/items/1", key.ToString());
        HttpResponseMessage second = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("False", GetRequiredHeader(second, IdempotencyReplayedHeader));
        JsonElement firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("post", firstJson.GetProperty("method").GetString());
        Assert.Equal("put", secondJson.GetProperty("method").GetString());
    }

    [Fact]
    public async Task Invoke_SamePathDifferentKey_ExecutesHandlerAgain()
    {
        Guid firstKey = NewKey();
        Guid secondKey = NewKey();
        using var firstRequest = CreateRequest(HttpMethod.Post, "/count", firstKey.ToString());

        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        using var secondRequest = CreateRequest(HttpMethod.Post, "/count", secondKey.ToString());
        HttpResponseMessage second = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("False", GetRequiredHeader(second, IdempotencyReplayedHeader));
        JsonElement firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, firstJson.GetProperty("count").GetInt32());
        Assert.Equal(2, secondJson.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Invoke_StoresCompletedCacheEntryUnderRefinedPathKey()
    {
        Guid key = NewKey();
        string cacheKey = $"idempotency:items_42:post:{key}";

        using var request = CreateRequest(HttpMethod.Post, "/items/42", key.ToString());
        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string rawValue = await GetRequiredRedisString(cacheKey);

        using JsonDocument document = JsonDocument.Parse(rawValue);
        Assert.Equal("Completed", document.RootElement.GetProperty("Status").GetString());
    }

    [Fact]
    public async Task Invoke_CompletedEntryHasApproximately24HourTtl()
    {
        Guid key = NewKey();

        using var request = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "hello" }));
        await _client.SendAsync(request);

        TimeSpan? ttl = await _redis.GetDatabase().KeyTimeToLiveAsync($"idempotency:echo:post:{key}");

        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, TimeSpan.FromHours(23), TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task Stampede_WhileFirstRequestInFlight_SecondGets409AndProcessingEntryIsCached()
    {
        Guid key = NewKey();
        string cacheKey = $"idempotency:blocking:post:{key}";
        BlockingState state = _app.Services.GetRequiredService<BlockingState>();

        Task<HttpResponseMessage> firstTask = _client.SendAsync(
            CreateRequest(HttpMethod.Post, "/blocking", key.ToString()));
        await state.Started.WaitAsync(TimeSpan.FromSeconds(10));

        string rawValue = await GetRequiredRedisString(cacheKey);
        TimeSpan? ttl = await _redis.GetDatabase().KeyTimeToLiveAsync(cacheKey);
        using JsonDocument processingDocument = JsonDocument.Parse(rawValue);
        Assert.Equal("Processing", processingDocument.RootElement.GetProperty("Status").GetString());
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));

        using var secondRequest = CreateRequest(HttpMethod.Post, "/blocking", key.ToString());
        HttpResponseMessage second = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        string secondBody = await second.Content.ReadAsStringAsync();
        Assert.Contains("already being processed", secondBody);

        // The 409 must not have finalized the claim into a cached response: the entry is still
        // Processing while the first request is in flight.
        string afterConflict = await GetRequiredRedisString(cacheKey);
        using (JsonDocument conflictDocument = JsonDocument.Parse(afterConflict))
        {
            Assert.Equal("Processing", conflictDocument.RootElement.GetProperty("Status").GetString());
        }

        state.Release();

        HttpResponseMessage first = await firstTask;
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("False", GetRequiredHeader(first, IdempotencyReplayedHeader));

        using var replayRequest = CreateRequest(HttpMethod.Post, "/blocking", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Reworked exception processing: an unhandled endpoint exception no longer deletes the
    /// cache entry. The exception-handling middleware downstream of
    /// <see cref="IdempotencyCachingMiddleware"/> produces the error response, the caching
    /// middleware finalizes the <c>Processing</c> claim into a <c>Completed</c> entry with that
    /// response, and a retry with the same key replays the cached error instead of
    /// re-executing the handler.
    /// </summary>
    [Fact]
    public async Task HandlerFailure_ErrorResponseIsCached_RetryReplaysIt()
    {
        Guid key = NewKey();
        string cacheKey = $"idempotency:fail-once:post:{key}";

        using var firstRequest = CreateRequest(HttpMethod.Post, "/fail-once", key.ToString());
        HttpResponseMessage first = await _client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);
        string firstBody = await first.Content.ReadAsStringAsync();
        Assert.Contains("first attempt fails", firstBody);
        Assert.Equal("False", GetRequiredHeader(first, IdempotencyReplayedHeader));

        string rawValue = await GetRequiredRedisString(cacheKey);
        using JsonDocument document = JsonDocument.Parse(rawValue);
        Assert.Equal("Completed", document.RootElement.GetProperty("Status").GetString());

        using var retryRequest = CreateRequest(HttpMethod.Post, "/fail-once", key.ToString());
        HttpResponseMessage replay = await _client.SendAsync(retryRequest);

        Assert.Equal(HttpStatusCode.InternalServerError, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(firstBody, await replay.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// The caching middleware only finalizes the claim when the error response is produced
    /// downstream of it. If no exception handler catches the exception, it unwinds through the
    /// caching middleware (whose finally restores the original body, so the capture block after
    /// its try/finally never runs): the exception propagates to the client and the
    /// <c>Processing</c> claim is left in place until its TTL expires. This pins the
    /// middleware-ordering contract - an exception handler registered upstream of the caching
    /// middleware would make this test fail with a 500 response instead of the thrown exception.
    /// </summary>
    [Fact]
    public async Task HandlerExceptionWithoutDownstreamHandler_Propagates_AndKeepsProcessingClaim()
    {
        Guid key = NewKey();
        string cacheKey = $"idempotency:fail-unhandled:post:{key}";

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _client.SendAsync(CreateRequest(HttpMethod.Post, "/fail-unhandled", key.ToString())));

        string rawValue = await GetRequiredRedisString(cacheKey);
        using JsonDocument document = JsonDocument.Parse(rawValue);
        Assert.Equal("Processing", document.RootElement.GetProperty("Status").GetString());
    }

    /// <summary>
    /// Covers the crash-mid-request recovery scenario the <c>Processing</c> TTL exists
    /// for: a request that claimed the key but never completed (crashed instance, network loss)
    /// leaves a <c>Processing</c> entry that expires on its own, after which a retry with the
    /// same key must execute the handler again instead of being stuck at 409 forever.
    /// </summary>
    [Fact]
    public async Task ExpiredProcessingClaim_AllowsFreshExecutionWithSameKey()
    {
        Guid key = NewKey();
        string cacheKey = $"idempotency:echo:post:{key}";

        // Simulate an in-flight claim from a request that will never complete, with the same
        // serialized shape the filter writes, but a short TTL standing in for the 1-minute TTL.
        await _redis.GetDatabase().StringSetAsync(cacheKey, "{\"Status\":\"Processing\",\"Value\":null}",
            TimeSpan.FromMilliseconds(300));

        for (int attempt = 0; attempt < 50 && await RedisKeyExists(cacheKey); attempt++)
        {
            await Task.Delay(50);
        }

        Assert.False(await RedisKeyExists(cacheKey)); // the stale claim expired on its own

        using var retryRequest = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "after-expiry" }));
        HttpResponseMessage retry = await _client.SendAsync(retryRequest);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("False", GetRequiredHeader(retry, IdempotencyReplayedHeader));
        string retryBody = await retry.Content.ReadAsStringAsync();
        Assert.Contains("after-expiry", retryBody);

        using var replayRequest = CreateRequest(HttpMethod.Post, "/echo", key.ToString(),
            JsonContentFromString(new { message = "different-body-ignored-on-replay" }));
        HttpResponseMessage replay = await _client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("True", GetRequiredHeader(replay, IdempotencyReplayedHeader));
        Assert.Equal(retryBody, await replay.Content.ReadAsStringAsync());
    }

    private static Guid NewKey() => Guid.NewGuid();

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string key,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add(IdempotencyKeyHeader, key);
        return request;
    }

    private static StringContent JsonContentFromString(object value) =>
        new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string GetRequiredHeader(HttpResponseMessage response, string header) =>
        Assert.Single(response.Headers.GetValues(header));

    private async Task<bool> RedisKeyExists(string cacheKey) =>
        await _redis.GetDatabase().KeyExistsAsync(cacheKey);

    private async Task<string> GetRequiredRedisString(string cacheKey)
    {
        RedisValue value = await _redis.GetDatabase().StringGetAsync(cacheKey);
        Assert.True(!value.IsNullOrEmpty, $"Expected Redis key '{cacheKey}' to exist.");
        return value.ToString();
    }

    private sealed class InvocationCounter
    {
        private int _count;

        public int Increment() => Interlocked.Increment(ref _count);
    }

    /// <summary>
    /// Throws on the first invocation and succeeds on every later one, letting a test observe
    /// whether a retry re-executed the handler or was served from the cache.
    /// </summary>
    private sealed class FailOnceHandler
    {
        private int _invocations;

        public IResult Invoke()
        {
            if (Interlocked.Increment(ref _invocations) == 1)
            {
                throw new InvalidOperationException("first attempt fails");
            }

            return Results.Ok(new { attempt = _invocations });
        }
    }

    private sealed class BlockingState
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void SignalStarted() => _started.TrySetResult();

        public void Release() => _release.TrySetResult();

        public Task WaitUntilReleasedAsync() => _release.Task;
    }
}
