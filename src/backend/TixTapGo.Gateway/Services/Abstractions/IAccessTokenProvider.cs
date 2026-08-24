namespace TixTapGo.Gateway.Services.Abstractions;

internal interface IAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
