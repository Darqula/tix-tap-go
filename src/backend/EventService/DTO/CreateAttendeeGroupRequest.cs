using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using EventService.Enums;

namespace EventService.DTO;

public record CreateAttendeeGroupRequest
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<SeatAssignmentType>))]
    public SeatAssignmentType? Type { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}
