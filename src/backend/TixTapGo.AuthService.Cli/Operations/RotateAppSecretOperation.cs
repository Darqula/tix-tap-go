using OpenIddict.Abstractions;

using TixTapGo.AuthService.Cli.Misc;

namespace TixTapGo.AuthService.Cli.Operations;

internal sealed class RotateAppSecretOperation : IManageAppOperation
{
    private readonly IOpenIddictApplicationManager _openIddictManager;
    private readonly SecretGenerator _secretGenerator;

    public RotateAppSecretOperation(IOpenIddictApplicationManager openIddictManager, SecretGenerator secretGenerator)
    {
        _openIddictManager = openIddictManager;
        _secretGenerator = secretGenerator;
    }

    public async Task<Result<string>> ExecuteAsync(string appClientId, CancellationToken cancellationToken)
    {
        var app = await _openIddictManager.FindByClientIdAsync(appClientId, cancellationToken: cancellationToken);
        if (app == null)
        {
            return Result<string>.Failure($"App with client ID {appClientId} was not found.");
        }

        string newSecret = _secretGenerator.Generate();
        await _openIddictManager.UpdateAsync(app, newSecret, cancellationToken: cancellationToken);

        return Result<string>.Success(newSecret);
    }
}
