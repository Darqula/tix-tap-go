using OpenIddict.Abstractions;

namespace TixTapGo.AuthService.Cli.Operations;

internal sealed class ListAppsOperation : IListAppsOperation
{
    private readonly IOpenIddictApplicationManager _openIddictManager;

    public ListAppsOperation(IOpenIddictApplicationManager openIddictManager)
    {
        _openIddictManager = openIddictManager;
    }

    public async Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var appClientIds = new List<string>();
        await foreach (var app in _openIddictManager.ListAsync(cancellationToken: cancellationToken))
        {
            var clientId = await _openIddictManager.GetClientIdAsync(app, cancellationToken: cancellationToken);
            appClientIds.Add(clientId ?? "null");
        }

        return appClientIds;
    }
}
