using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

public record UpdateSeatCategoryRequest
{
    [MaxLength(48)]
    public string? Title { get; set; }
    [MaxLength(9)]
    [RegularExpression("^#([0-9a-fA-F]{3,4}){1,2}$", ErrorMessage = "Color must be a valid hex code (e.g., #FFFF or #ffffff).")]
    public string? Color { get; set; }
}
