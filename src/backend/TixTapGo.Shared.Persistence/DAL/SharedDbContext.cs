using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.DAL;

public abstract class SharedDbContext : DbContext
{
    private readonly IEnumerable<ISaveChangesInterceptor> _interceptors;
    private readonly HierarchicalSoftDeleteCommand _hierarchicalSoftDeleteCommand;

    protected SharedDbContext(DbContextOptions options, IEnumerable<ISaveChangesInterceptor> interceptors) :
        base(options)
    {
        _interceptors = interceptors;
        _hierarchicalSoftDeleteCommand = new HierarchicalSoftDeleteCommand(this);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder.AddInterceptors(_interceptors));
    }

    public override int SaveChanges()
    {
        ProcessTrackedEntitiesAsync().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        await ProcessTrackedEntitiesAsync();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new EntityBaseConvention());
        base.ConfigureConventions(configurationBuilder);
    }

    private async Task ProcessTrackedEntitiesAsync()
    {
        foreach (var changedEntry in ChangeTracker.Entries<EntityBase>())
        {
            switch (changedEntry.State)
            {
                case EntityState.Added:
                    changedEntry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    changedEntry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    changedEntry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Deleted:
                    await _hierarchicalSoftDeleteCommand.ExecuteAsync(changedEntry);
                    break;
            }
        }
    }
}
