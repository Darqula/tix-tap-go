using TixTapGo.AuthService.Cli.Misc;

namespace TixTapGo.AuthService.Cli.Operations;

internal interface IManageAppOperation
{
    Task<Result<string>> ExecuteAsync(string appClientId, CancellationToken cancellationToken);
}
