using Microsoft.Extensions.Caching.Memory;

using OpenIddict.Client;

using TixTapGo.Shared.Auth.Services.Abstractions;

namespace TixTapGo.Shared.Auth.Services;

public sealed class AccessTokenProvider : IAccessTokenProvider, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly OpenIddictClientService _openIddictClientService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AccessTokenProvider(IMemoryCache memoryCache, OpenIddictClientService openIddictClientService)
    {
        _memoryCache = memoryCache;
        _openIddictClientService = openIddictClientService;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (TryGetAccessToken(out var accessToken))
        {
            return accessToken;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (TryGetAccessToken(out accessToken))
            {
                return accessToken;
            }

            var authenticateRequest = new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
            {
                CancellationToken = cancellationToken
            };
            var tokenResult =
                await _openIddictClientService.AuthenticateWithClientCredentialsAsync(authenticateRequest);

            accessToken = tokenResult.AccessToken;
            if (!tokenResult.AccessTokenExpirationDate.HasValue)
            {
                throw new InvalidDataException("Access token expiration date cannot be null");
            }

            DateTimeOffset expiration = tokenResult.AccessTokenExpirationDate.Value.AddMinutes(-1);
            _memoryCache.Set(Constants.CacheKeys.ClientCredentialsAccessToken, accessToken, expiration);

            return accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool TryGetAccessToken(out string accessToken)
    {
        if (_memoryCache.TryGetValue(Constants.CacheKeys.ClientCredentialsAccessToken, out var rawAccessToken)
            && rawAccessToken is string token)
        {
            accessToken = token;
            return true;
        }

        accessToken = string.Empty;
        return false;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
