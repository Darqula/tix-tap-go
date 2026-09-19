namespace TixTapGo.AppHost;

internal static class AuthServiceResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddAuthService(
        this IDistributedApplicationBuilder builder,
        TixTapGoInfrastructure infra,
        ClientAuthParameters authParams)
    {
        var authService = builder.AddProject<Projects.TixTapGo_AuthService>("auth-service")
            .WithReference(infra.AuthDb)
            .WithEnvironment("Authentication__ClientCredentialsEncryptionKey", authParams.EncryptionKey)
            .WithEnvironment("Authentication__ClientServices__event-service", authParams.EventServiceSecret)
            .WithEnvironment("Authentication__ClientServices__order-service", authParams.OrderServiceSecret)
            .WithEnvironment("Authentication__ClientServices__gateway", authParams.GatewaySecret);

        var authDbMigration = authService
            .AddEFMigrations("authdb-migration")
            .RunDatabaseUpdateOnStart()
            .WaitFor(infra.AuthDb);

        authService.WaitForCompletion(authDbMigration);

        return authService;
    }
}
