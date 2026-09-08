using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using TixTapGo.Shared.Auth.Handlers;
using TixTapGo.Shared.Auth.Services;
using TixTapGo.Shared.Auth.Services.Abstractions;

namespace TixTapGo.Shared.Auth;

public static class Extensions
{
    public static IServiceCollection AddAccessTokenProvider(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddMemoryCache()
            .AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
    }

    public static IHttpStandardResiliencePipelineBuilder AddInternalServiceHttpClient<TClient>(
        this IServiceCollection serviceCollection)
        where TClient : class
    {
        return serviceCollection
            .AddTransient<AuthHeaderHandler>()
            .AddHttpClient<TClient>()
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddStandardResilienceHandler();
    }

    public static IHttpStandardResiliencePipelineBuilder AddInternalServiceHttpClient(
        this IServiceCollection serviceCollection, string clientKey)
    {
        return serviceCollection
            .AddTransient<AuthHeaderHandler>()
            .AddHttpClient(clientKey)
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddStandardResilienceHandler();
    }
}
