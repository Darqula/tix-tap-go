using System.Security.Cryptography;

namespace TixTapGo.AuthService.Cli;

internal sealed class SecretGenerator
{
#pragma warning disable CA1822
    public string Generate() => RandomNumberGenerator.GetHexString(36, true);
#pragma warning restore CA1822
}
