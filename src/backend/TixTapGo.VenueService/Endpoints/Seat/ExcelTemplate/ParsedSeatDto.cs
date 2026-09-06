using System.ComponentModel.DataAnnotations;

namespace TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;

internal sealed record ParsedSeatDto
{
    public static readonly ParsedSeatDto Example = new()
    {
        SheetRowNumber = 3,
        RowNumber = "5",
        SeatNumber = "3A",
        Category = "VIP",
        MapPositionX = 3.25m,
        MapPositionY = 18.33m
    };

    public required int SheetRowNumber { get; init; }

    [MaxLength(6)]
    [MinLength(1)]
    public required string RowNumber { get; init; }

    [MaxLength(6)]
    [MinLength(1)]
    public required string SeatNumber { get; init; }

    [MinLength(1)]
    public required string Category { get; init; }

    public decimal? MapPositionX { get; init; }
    public decimal? MapPositionY { get; init; }

    public bool Equals(ParsedSeatDto? other)
    {
        if (other == null) return false;

        // Skip SheetRowNumber to clearly compare only parsed data
        return RowNumber == other.RowNumber &&
               SeatNumber == other.SeatNumber &&
               Category == other.Category &&
               MapPositionX == other.MapPositionX &&
               MapPositionY == other.MapPositionY;
    }

    public override int GetHashCode()
    {
        // Skip SheetRowNumber to clearly compare only parsed data
        return HashCode.Combine(RowNumber, SeatNumber, Category, MapPositionX, MapPositionY);
    }
}
