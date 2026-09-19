using System.Security.Cryptography;

namespace TixTapGo.AppHost;

internal sealed record ClientAuthParameters(
    IResourceBuilder<ParameterResource> EncryptionKey,
    IResourceBuilder<ParameterResource> EventServiceSecret,
    IResourceBuilder<ParameterResource> OrderServiceSecret,
    IResourceBuilder<ParameterResource> GatewaySecret);

internal static class ParametersExtensions
{
    public static ClientAuthParameters AddClientAuthParameters(this IDistributedApplicationBuilder builder)
    {
        var encryptionKey = builder
            .AddParameter("client-credentials-encryption-key", secret: true)
            .WithDescription($"Randomly generated key to set: {Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}");

        var eventServiceSecret = builder.AddParameter(
            "event-service-secret", ClientSecretDefault(), secret: true, persist: true);
        var orderServiceSecret = builder.AddParameter(
            "order-service-secret", ClientSecretDefault(), secret: true, persist: true);
        var gatewaySecret = builder.AddParameter(
            "gateway-secret", ClientSecretDefault(), secret: true, persist: true);

        return new ClientAuthParameters(encryptionKey, eventServiceSecret, orderServiceSecret, gatewaySecret);
    }

    private static GenerateParameterDefault ClientSecretDefault() => new()
    {
        MinLength = 36,
        Lower = true,
        Numeric = true,
        Upper = false,
        Special = false
    };
}
