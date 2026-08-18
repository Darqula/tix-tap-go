using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace Shared.DAL;

public abstract class SharedDbContext(DbContextOptions options) : DbContext(options)
{
    public override int SaveChanges()
    {
        ProcessTrackedEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        ProcessTrackedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new EntityBaseConvention());
        base.ConfigureConventions(configurationBuilder);
    }

    private void ProcessTrackedEntities()
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
                    changedEntry.Entity.IsDeleted = true;
                    changedEntry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    changedEntry.State = EntityState.Modified;
                    break;
            }
        }
    }
}