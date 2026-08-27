using OpenIddict.Abstractions;

using TixTapGo.AuthService.Cli.Misc;

namespace TixTapGo.AuthService.Cli.Operations;

internal sealed class RegisterAppOperation : IManageAppOperation
{
    private readonly IOpenIddictApplicationManager _openIddictManager;
    private readonly SecretGenerator _secretGenerator;

    public RegisterAppOperation(IOpenIddictApplicationManager openIddictManager, SecretGenerator secretGenerator)
    {
        _openIddictManager = openIddictManager;
        _secretGenerator = secretGenerator;
    }

    public async Task<Result<string>> ExecuteAsync(string appClientId, CancellationToken cancellationToken)
    {
        var app = await _openIddictManager.FindByClientIdAsync(appClientId, cancellationToken: cancellationToken);
        if (app != null)
        {
            return Result<string>.Failure($"App with client ID {appClientId} already exists");
        }

        string secret = _secretGenerator.Generate();

        await _openIddictManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = appClientId,
            ClientSecret = secret,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            }
        }, cancellationToken: cancellationToken);

        return Result<string>.Success(secret);
    }
}
