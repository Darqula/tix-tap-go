using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

public record CreateSeatCategoryRequest
{
    [MaxLength(48)]
    public required string Title { get; set; }
    [MaxLength(9)]
    [RegularExpression("^#([0-9a-fA-F]{3,4}){1,2}$", ErrorMessage = "Color must be a valid hex code (e.g., #FFFF or #ffffff).")]
    public required string Color { get; set; }
}
