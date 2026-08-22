using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query;

using TixTapGo.Shared.Entities;

namespace TixTapGo.Shared.DAL;

public class EntityBaseConvention : IModelFinalizingConvention
{
    private static void ApplyQueryFilter<TEntity>(IConventionEntityType entityType,
        Expression<Func<TEntity, bool>> filter, string filterKey) where TEntity : EntityBase
    {
        var filterParam = Expression.Parameter(entityType.ClrType, "e");
        var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters[0], filterParam, filter.Body);
        var filterLambda = Expression.Lambda(filterBody, filterParam);
        entityType.SetQueryFilter(filterKey, filterLambda);
    }

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(EntityBase)))
            {
                ApplyQueryFilter(entityType, (EntityBase b) => !b.IsDeleted, "SoftDelete");
                entityType.FindProperty(nameof(EntityBase.CreatedAt))?.Builder.HasDefaultValueSql("now()");
                entityType.FindProperty(nameof(EntityBase.UpdatedAt))?.Builder.HasDefaultValueSql("now()");
                entityType.FindProperty(nameof(EntityBase.IsDeleted))?.Builder.HasDefaultValue(false);
            }
        }
    }
}
