using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.DTO;

public sealed record CreateAttendeeGroupRequest
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
    public SeatAssignmentType? Type { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}
