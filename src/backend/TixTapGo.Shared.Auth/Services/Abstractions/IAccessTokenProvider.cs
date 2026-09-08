namespace TixTapGo.Shared.Auth.Services.Abstractions;

public interface IAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
