using OpenIddict.Abstractions;

using TixTapGo.AuthService.Cli.Misc;

namespace TixTapGo.AuthService.Cli.Operations;

internal sealed class DeleteAppOperation : IManageAppOperation
{
    private readonly IOpenIddictApplicationManager _openIddictManager;

    public DeleteAppOperation(IOpenIddictApplicationManager openIddictManager)
    {
        _openIddictManager = openIddictManager;
    }

    public async Task<Result<string>> ExecuteAsync(string appClientId, CancellationToken cancellationToken)
    {
        var app = await _openIddictManager.FindByClientIdAsync(appClientId, cancellationToken: cancellationToken);
        if (app == null)
        {
            return Result<string>.Failure($"App with client ID {appClientId} was not found.");
        }

        await _openIddictManager.DeleteAsync(app, cancellationToken: cancellationToken);

        return Result<string>.Success(appClientId);
    }
}
