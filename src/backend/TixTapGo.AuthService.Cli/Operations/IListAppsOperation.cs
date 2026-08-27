namespace TixTapGo.AuthService.Cli.Operations;

internal interface IListAppsOperation
{
    Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken);
}
