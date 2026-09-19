namespace TixTapGo.AppHost;

internal static class GatewayResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddGateway(
        this IDistributedApplicationBuilder builder,
        ClientAuthParameters authParams,
        params IResourceBuilder<ProjectResource>[] upstreams)
    {
        var gateway = builder.AddProject<Projects.TixTapGo_Gateway>("gateway")
            .WithExternalHttpEndpoints()
            .WithClientCredentialsAuth(authParams.GatewaySecret, authParams.EncryptionKey);

        foreach (var upstream in upstreams)
        {
            gateway
                .WithReference(upstream)
                .WaitFor(upstream);
        }

        return gateway;
    }
}
