using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TixTapGo.Shared.Persistence.Tests;

/// <summary>
/// Builds an isolated <see cref="TestDbContext"/> per test
/// </summary>
internal sealed class TestDbContextFactory(string connectionString) : IAsyncDisposable
{
    private readonly string _schema = $"test_{Guid.NewGuid():N}";

    public async Task<TestDbContext> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>()
            .Options;

        var context = new TestDbContext(options, _schema);

        await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_schema}\"");
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new TestDbContext(options, _schema);
        await context.Database.ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE");
    }
}
