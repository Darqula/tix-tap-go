using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.DAL;

public class HierarchicalSoftDeleteCommand(DbContext dbContext)
{
    private readonly Dictionary<Type, MethodInfo> _loadDependentsMethods = new();

    public async Task ExecuteAsync(EntityEntry<EntityBase> entry)
    {
        if (entry.Entity.IsDeleted)
        {
            return;
        }

        entry.Entity.IsDeleted = true;
        entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        entry.State = EntityState.Modified;

        await RemoveInternalAsync(entry);
    }

    private async Task RemoveInternalAsync(EntityEntry entry)
    {
        foreach (var referencingForeignKey in entry.Metadata.GetReferencingForeignKeys())
        {
            var navigationFromPrincipal = referencingForeignKey.GetNavigation(false);

            IEnumerable<object> dependantEntities = navigationFromPrincipal == null
                ? await GetDependantsByReflectionAsync(referencingForeignKey, entry)
                : await GetDependantsByNavigationAsync(navigationFromPrincipal, entry);

            await ApplyDeleteBehaviorAsync(referencingForeignKey, entry, dependantEntities);
        }
    }

    /// <summary>
    /// Resolve unidirectional links to principal entities by reflection
    /// </summary>
    /// <returns>Dependant entities</returns>
    private async Task<List<object>> GetDependantsByReflectionAsync(IForeignKey referencingForeignKey,
        EntityEntry entry)
    {
        var dependantsType = referencingForeignKey.DeclaringEntityType.ClrType;
        if (!_loadDependentsMethods.TryGetValue(dependantsType, out MethodInfo? method))
        {
            method = typeof(HierarchicalSoftDeleteCommand)
                .GetMethod(nameof(LoadDependentsGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(referencingForeignKey.DeclaringEntityType.ClrType);

            _loadDependentsMethods[dependantsType] = method;
        }

        return await (Task<List<object>>)method.Invoke(this, [referencingForeignKey, entry])!;
    }

    private async Task<List<object>> LoadDependentsGeneric<TDependent>(IReadOnlyForeignKey fk,
        EntityEntry principalEntry) where TDependent : class
    {
        var efPropertyMethod = typeof(EF)
            .GetMethod(nameof(EF.Property))!
            .MakeGenericMethod(typeof(object));

        var parameter = Expression.Parameter(typeof(TDependent), "entity");
        Expression expression = Expression.Constant(true);

        for (int i = 0; i < fk.Properties.Count; i++)
        {
            var fkPropertyName = Expression.Constant(fk.Properties[i].Name);
            string principalPropertyName = fk.PrincipalKey.Properties[i].Name;
            var principalPropertyValue =
                Expression.Constant(principalEntry.Property(principalPropertyName).CurrentValue, typeof(object));

            var fkPropertyValue = Expression.Call(efPropertyMethod, parameter, fkPropertyName);
            var propertyEqualNode = Expression.Equal(fkPropertyValue, principalPropertyValue);
            expression = Expression.AndAlso(expression, propertyEqualNode);
        }

        var isMatchedPrincipalPropertyValuesExpression =
            Expression.Lambda<Func<TDependent, bool>>(expression, parameter);

        var result = await dbContext.Set<TDependent>()
            .Where(isMatchedPrincipalPropertyValuesExpression)
            .ToListAsync();

        return result.Cast<object>().ToList();
    }

    private async Task<List<object>> GetDependantsByNavigationAsync(INavigation navigationFromPrincipal,
        EntityEntry entry)
    {
        List<object> dependantEntities = new();

        bool isCollection = navigationFromPrincipal.IsCollection;
        if (!isCollection)
        {
            var referenceEntry = entry.Reference(navigationFromPrincipal);
            if (!referenceEntry.IsLoaded)
            {
                await referenceEntry.LoadAsync();
            }

            if (referenceEntry.CurrentValue != null)
            {
                dependantEntities.Add(referenceEntry.CurrentValue);
            }
        }
        else
        {
            var collectionEntry = entry.Collection(navigationFromPrincipal);
            if (!collectionEntry.IsLoaded)
            {
                await collectionEntry.LoadAsync();
            }

            if (collectionEntry.CurrentValue != null)
            {
                foreach (var collectionEntryEntity in collectionEntry.CurrentValue)
                {
                    if (collectionEntryEntity != null)
                    {
                        dependantEntities.Add(collectionEntryEntity);
                    }
                }
            }
        }

        return dependantEntities;
    }

    private async Task ApplyDeleteBehaviorAsync(IForeignKey referencingForeignKey, EntityEntry principalEntry,
        IEnumerable<object> dependantEntities)
    {
        switch (referencingForeignKey.DeleteBehavior)
        {
            case DeleteBehavior.Cascade:
            case DeleteBehavior.ClientCascade:
                foreach (object dependantEntity in dependantEntities)
                {
                    await CascadeDeleteEntityObjectAsync(dependantEntity);
                }
                break;
            case DeleteBehavior.SetNull:
            case DeleteBehavior.ClientSetNull:
                foreach (var dependantEntity in dependantEntities)
                {
                    SetNull(dependantEntity, referencingForeignKey);
                }
                break;
            case DeleteBehavior.Restrict:
            case DeleteBehavior.ClientNoAction:
            case DeleteBehavior.NoAction:
                if (dependantEntities.Any())
                {
                    IEnumerable<string> principalKeyValues =
                        principalEntry.Metadata
                            .FindPrimaryKey()?.Properties
                            .Select(pk => principalEntry.Property(pk.Name).CurrentValue?.ToString() ?? "unknown")
                        ?? ["unknown"];

                    throw new InvalidOperationException(
                        $"Cannot delete entity {principalEntry.Metadata.ClrType.FullName} " +
                        $"(id: {string.Join("_", principalKeyValues)}) with a foreign key " +
                        $"that has a delete behavior {referencingForeignKey.DeleteBehavior}");
                }
                break;
        }
    }

    private void SetNull(object entity, IForeignKey referencingForeignKey)
    {
        var dependantEntry = dbContext.Entry(entity);
        foreach (var fkProperty in referencingForeignKey.Properties)
        {
            dependantEntry.Property(fkProperty).CurrentValue = null;
        }

        dependantEntry.State = EntityState.Modified;
        if (dependantEntry.Entity is EntityBase dependantEntityBase)
        {
            dependantEntityBase.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async ValueTask CascadeDeleteEntityObjectAsync(object entity)
    {
        var dependantEntry = dbContext.Entry(entity);
        if (dependantEntry.State == EntityState.Deleted)
        {
            return;
        }

        if (entity is EntityBase entityBase)
        {
            if (entityBase.IsDeleted)
            {
                return;
            }

            entityBase.IsDeleted = true;
            entityBase.UpdatedAt = DateTimeOffset.UtcNow;
            dependantEntry.State = EntityState.Modified;
        }
        else
        {
            dbContext.Entry(entity).State = EntityState.Deleted;
        }

        await RemoveInternalAsync(dependantEntry);
    }
}
