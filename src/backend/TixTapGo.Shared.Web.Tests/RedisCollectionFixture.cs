using Testcontainers.Redis;

using Xunit;

namespace TixTapGo.Shared.Web.Tests;

public sealed class RedisCollectionFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
                                      ?? throw new InvalidOperationException("Container has not been started yet.");

    public async Task InitializeAsync()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisCollectionFixture>
{
    public const string Name = "Redis collection";
}
