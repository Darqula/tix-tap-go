using System.ComponentModel.DataAnnotations;

using TixTapGo.Shared.Entities;

namespace TixTapGo.EventService.Entities;

internal sealed class Event : EntityBase
{
    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(1024)]
    public required string Description { get; set; }

    public required DateTimeOffset Start { get; set; }

    [MaxLength(256)]
    public required string Location { get; set; }

    public List<AttendeeGroup> AttendeeGroups { get; private set; } = new List<AttendeeGroup>();
}
