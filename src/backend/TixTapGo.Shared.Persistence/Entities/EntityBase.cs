using System.ComponentModel.DataAnnotations;

namespace TixTapGo.Shared.Persistence.Entities;

public abstract class EntityBase
{
    public Guid Id { get; init; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public bool IsDeleted { get; internal set; }
}
