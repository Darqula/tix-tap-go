using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.DAL;

public abstract class SharedDbContext : DbContext
{
    private readonly HierarchicalSoftDeleteCommand _hierarchicalSoftDeleteCommand;

    protected SharedDbContext(DbContextOptions options) : base(options)
    {
        _hierarchicalSoftDeleteCommand = new HierarchicalSoftDeleteCommand(this);
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
