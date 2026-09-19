namespace TixTapGo.AppHost;

internal static class ClientAuthExtensions
{
    public static IResourceBuilder<T> WithClientCredentialsAuth<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> clientSecret,
        IResourceBuilder<ParameterResource> encryptionKey)
        where T : IResourceWithEnvironment
        => builder
            .WithEnvironment("Authentication__ClientSecret", clientSecret)
            .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", encryptionKey);
}
