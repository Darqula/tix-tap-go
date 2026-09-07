using TixTapGo.Shared.Validation;

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

    public static ParseSeatsResult Fail(int version, List<ParsingError> errors,
        List<ParsedSeatDto>? partialSeatDtos = null)
    {
        var errorsDict = new ValidationErrorsDictionary();
        foreach (ParsingError parsingError in errors)
        {
            errorsDict.AddError($"Line: {parsingError.Row}", parsingError.Message);
        }

        return new ParseSeatsResult()
        {
            IsSuccess = false,
            Version = version,
            Errors = errorsDict,
            Seats = partialSeatDtos
        };
    }

    public bool IsSuccess { get; init; }
    public int Version { get; init; }
    public List<ParsedSeatDto>? Seats { get; init; }
    public ValidationErrorsDictionary? Errors { get; init; }

    public sealed record ParsingError(int Row, string Message);
}
