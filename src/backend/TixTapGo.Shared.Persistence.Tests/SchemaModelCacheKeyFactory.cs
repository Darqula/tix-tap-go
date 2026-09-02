using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TixTapGo.Shared.Persistence.Tests;

internal sealed class SchemaModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is TestDbContext testDbContext
            ? (context.GetType(), testDbContext.Schema, designTime)
            : (context.GetType(), designTime);
}
