namespace TixTapGo.AppHost;

internal static class OrderServiceResourceExtensions
{
    public static IResourceBuilder<ProjectResource> AddOrderService(
        this IDistributedApplicationBuilder builder,
        TixTapGoInfrastructure infra,
        ClientAuthParameters authParams,
        IResourceBuilder<ProjectResource> authService)
    {
        var orderService = builder.AddProject<Projects.TixTapGo_OrderService>("order-service")
            .WithReference(infra.OrdersDb)
            .WithReference(authService)
            .WithReference(infra.RabbitMq)
            .WithReference(infra.Redis)
            .WaitFor(authService)
            .WithClientCredentialsAuth(authParams.OrderServiceSecret, authParams.EncryptionKey);

        var ordersDbMigration = orderService
            .AddEFMigrations("ordersdb-migration")
            .RunDatabaseUpdateOnStart()
            .WaitFor(infra.OrdersDb);

        orderService.WaitForCompletion(ordersDbMigration);

        return orderService;
    }
}
