using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(EntityBase)))
            {
                var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
                ApplyQueryFilter(entityType, b => !b.IsDeleted, "SoftDelete");
                entityTypeBuilder.Property<DateTimeOffset>(nameof(EntityBase.CreatedAt)).HasDefaultValueSql("now()");
                entityTypeBuilder.Property<DateTimeOffset>(nameof(EntityBase.UpdatedAt)).HasDefaultValueSql("now()");
                entityTypeBuilder.Property<bool>(nameof(EntityBase.IsDeleted)).HasDefaultValue(false);
            }
        }
    }

    private void ApplyQueryFilter(IMutableEntityType entityType, Expression<Func<EntityBase, bool>> filter,
        string filterKey)
    {
        var filterParam = Expression.Parameter(entityType.ClrType, "e");
        var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters[0], filterParam, filter.Body);
        var filterLambda = Expression.Lambda(filterBody, filterParam);
        entityType.SetQueryFilter(filterKey, filterLambda);
    }
}