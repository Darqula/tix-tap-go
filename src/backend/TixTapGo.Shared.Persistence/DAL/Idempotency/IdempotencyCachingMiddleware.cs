using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TixTapGo.Shared.Persistence.DAL.Idempotency;

public class IdempotencyCachingMiddleware : IMiddleware
{
    private readonly IdempotencyPrerequisitesValidator _validator = new();
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var prerequisitesValidationResult = _validator.Validate(context);
        if (!prerequisitesValidationResult.Success)
        {
            // Idempotency processing is not required
            await next(context);
            return;
        }
        
        Stream originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        
        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
        
        if (context.Items.TryGetValue(IdempotencyConstants.IdempotencyCacheKey, out object? cacheKeyRaw) &&
            cacheKeyRaw is string cacheKey)
        {
            // Downstream only returns a result and never caches it directly, since exceptions
            // must be propagated to the exception handlers. Thus, cache everything here
            var serviceProvider = context.RequestServices;
            var stateManager = serviceProvider.GetRequiredService<IdempotencyStateManager>();
            var idempotencyResponse = IdempotentResponse.Capture(buffer, context);
            await stateManager.CacheIdempotentResponseAsync(cacheKey, idempotencyResponse, context);
        }
        
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
    }
}
