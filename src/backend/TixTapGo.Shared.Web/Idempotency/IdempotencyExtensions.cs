using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace TixTapGo.Shared.Web.Idempotency;

public static class IdempotencyExtensions
{
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        return services
            .AddScoped<IdempotencyStateManager>()
            .AddScoped<IdempotencyCachingMiddleware>();
    }
    
    public static RouteHandlerBuilder WithIdempotencyCheck(this RouteHandlerBuilder routeBuilder)
    {
        return routeBuilder
            .AddEndpointFilter<IdempotentEndpointFilter>()
            .AddOpenApiOperationTransformer((o, _, _) =>
            {
                o.Parameters ??= [];
                o.Parameters.Add(new OpenApiParameter
                {
                    Name = IdempotencyConstants.IdempotencyKeyHeader,
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid"
                    },
                    Description = "Client-generated UUID for one logical attempt at this operation. Reuse the " +
                                  "same key only to retry after receiving no response (e.g. a timeout or dropped " +
                                  "connection) - the server returns the cached result of the original attempt " +
                                  "rather than executing again. After any definitive response, success or error, " +
                                  "use a new key for a new attempt; reusing it will keep returning the original " +
                                  "outcome unchanged, not retry the operation."
                });

                var responses = o.Responses?.Values;

                if (responses != null)
                {
                    foreach (var response in responses)
                    {
                        if (response is not OpenApiResponse concreteResponse) continue;

                        concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
                        concreteResponse.Headers[IdempotencyConstants.IdempotencyReplayedHeader] = new OpenApiHeader
                        {
                            Description = "\"True\" if this response was served from the idempotency cache " +
                                          "instead of re-executing the request; \"False\" on first execution.",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                        };
                    }
                }

                return Task.CompletedTask;
            });
    }
}
