namespace TixTapGo.AppHost;

internal static class StorefrontMcpServiceResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddStorefrontMcpService(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> gateway)
    {
        return builder.AddProject<Projects.TixTapGo_StorefrontMcpService>("storefront-mcp-service")
            .WithReference(gateway)
            .WaitFor(gateway)
            .WithExternalHttpEndpoints();
    }
}
