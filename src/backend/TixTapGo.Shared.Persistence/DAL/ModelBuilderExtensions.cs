using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.Shared.Persistence.Entities;

namespace TixTapGo.Shared.Persistence.DAL;

public static class ModelBuilderExtensions
{
    public static IndexBuilder<TEntity> IsSoftDeleteUnique<TEntity>(this IndexBuilder<TEntity> builder)
        where TEntity : EntityBase
    {
        return builder.HasFilter($"\"{nameof(EntityBase.IsDeleted)}\" = false").IsUnique();
    }
}
