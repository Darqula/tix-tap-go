using System.ComponentModel.DataAnnotations;

namespace Shared.Entities;

public abstract class EntityBase
{
    public Guid Id { get; init; }

    [Required]
    public DateTimeOffset CreatedAt { get; internal set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; internal set; }

    [Required]
    public bool IsDeleted { get; internal set; }
}
