namespace TixTapGo.AppHost;

internal sealed record TixTapGoInfrastructure(
    IResourceBuilder<PostgresServerResource> Postgres,
    IResourceBuilder<PostgresDatabaseResource> EventsDb,
    IResourceBuilder<PostgresDatabaseResource> AuthDb,
    IResourceBuilder<PostgresDatabaseResource> VenuesDb,
    IResourceBuilder<PostgresDatabaseResource> OrdersDb,
    IResourceBuilder<RabbitMQServerResource> RabbitMq,
    IResourceBuilder<RedisResource> Redis);

internal static class InfrastructureExtensions
{
    public static TixTapGoInfrastructure AddInfrastructure(this IDistributedApplicationBuilder builder)
    {
        var pgPassword = builder.AddParameter("local-postgres-password", secret: true);

        var postgres = builder
            .AddPostgres("local-postgres", password: pgPassword)
            .WithDataVolume();

        var rabbitMq = builder.AddRabbitMQ("rabbitmq")
            .WithManagementPlugin();

        var redis = builder.AddRedis("redis")
            .WithRedisInsight();

        return new TixTapGoInfrastructure(
            postgres,
            postgres.AddDatabase("eventsdb"),
            postgres.AddDatabase("authdb"),
            postgres.AddDatabase("venuesdb"),
            postgres.AddDatabase("ordersdb"),
            rabbitMq,
            redis);
    }
}
