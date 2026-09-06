namespace TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;

internal sealed record ParseSeatsResult
{
    public static ParseSeatsResult Success(int version, List<ParsedSeatDto> seatDtos)
    {
        return new ParseSeatsResult()
        {
            IsSuccess = true,
            Version = version,
            Seats = seatDtos
        };
    }

    public static ParseSeatsResult Fail(int version, List<ParsingError> errors, List<ParsedSeatDto>? partialSeatDtos = null)
    {
        return new ParseSeatsResult()
        {
            IsSuccess = false,
            Version = version,
            Errors = errors,
            Seats = partialSeatDtos
        };
    }
    
    public bool IsSuccess { get; init; }
    public int Version { get; init; }
    public List<ParsedSeatDto>? Seats { get; init; }
    public List<ParsingError>? Errors { get; init; }

    public sealed record ParsingError(int Row, string Message);
}
