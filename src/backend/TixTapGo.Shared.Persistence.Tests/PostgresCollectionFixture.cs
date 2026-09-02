using Testcontainers.PostgreSql;

using Xunit;

namespace TixTapGo.Shared.Persistence.Tests;

public sealed class PostgresCollectionFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
                                      ?? throw new InvalidOperationException("Container has not been started yet.");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("tixtapgo_tests")
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
public sealed class PostgresCollection : ICollectionFixture<PostgresCollectionFixture>
{
    public const string Name = "Postgres collection";
}
