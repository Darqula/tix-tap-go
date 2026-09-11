using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using TixTapGo.Shared.Validation;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;

internal sealed class ParsedSeatValidator
{
    private const int _inDatabaseFakeSheetRowNumber = -42;

    private readonly HashSet<string> _categories;
    private readonly Dictionary<(decimal x, decimal y), int> _sheetRowsByCoordinates;
    private readonly Dictionary<(string seatRow, string seatNumber), int> _sheetRowsBySeatIdentity;

    public ParsedSeatValidator(HashSet<string> categories, List<VenueSeat>? existingSeats = null)
    {
        _categories = categories;
        if (existingSeats == null)
        {
            _sheetRowsByCoordinates = new();
            _sheetRowsBySeatIdentity = new();
        }
        else
        {
            _sheetRowsByCoordinates = existingSeats
                .Where(seat => seat.MapPositionX.HasValue && seat.MapPositionY.HasValue)
                .ToDictionary(seat => (seat.MapPositionX!.Value, seat.MapPositionY!.Value),
                    _ => _inDatabaseFakeSheetRowNumber);

            _sheetRowsBySeatIdentity = existingSeats
                .ToDictionary(seat => (seat.RowNumber, seat.SeatNumber), _ => _inDatabaseFakeSheetRowNumber);
        }
    }

    public bool Validate(IList<ParsedSeatDto> dtos, out ValidationErrorsDictionary errors)
    {
        var errorsList = new List<ParseSeatsResult.ParsingError>();
        bool isSuccessful = true;
        foreach (var dto in dtos)
        {
            try
            {
                if (!ValidateSeatDto(dto, out ParseSeatsResult.ParsingError? error))
                {
                    errorsList.Add(error);
                    isSuccessful = false;
                }
            }
            catch (Exception e)
            {
                isSuccessful = false;
                errorsList.Add(new ParseSeatsResult.ParsingError(dto.SheetRowNumber, e.Message));
            }
        }

        errors = new ValidationErrorsDictionary();
        foreach (var error in errorsList)
        {
            errors.AddError($"Line: {error.Row}", error.Message);
        }
        return isSuccessful;
    }

    private bool ValidateSeatDto(ParsedSeatDto dto, [NotNullWhen(false)] out ParseSeatsResult.ParsingError? error)
    {
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        bool isCategoryValid = _categories.Contains(dto.Category);
        if (!isCategoryValid)
        {
            validationResults.Add(new ValidationResult(
                $"Invalid category {dto.Category}. Supported values: {string.Join(", ", _categories)}",
                ["Category"])
            );
        }

        bool isTemplateExampleDto = dto == ParsedSeatDto.Example;
        if (isTemplateExampleDto)
        {
            validationResults.Add(new ValidationResult("Data of this seat leaked from the template example"));
        }

        bool areCoordinatesValid = ValidateCoordinates(dto, validationResults);
        bool isIdentityValid = ValidateIdentity(dto, validationResults);

        bool isValidationSuccessful =
            Validator.TryValidateObject(dto, validationContext, validationResults, true) &&
            areCoordinatesValid &&
            isCategoryValid &&
            isIdentityValid &&
            !isTemplateExampleDto;

        if (!isValidationSuccessful)
        {
            error = new ParseSeatsResult.ParsingError(
                dto.SheetRowNumber,
                $"Validation errors:\n{string.Join(Environment.NewLine, validationResults)}");
            return false;
        }

        error = null;
        return true;
    }

    private bool ValidateCoordinates(ParsedSeatDto dto, List<ValidationResult> validationResults)
    {
        bool areBothCoordinatesAligned = dto.MapPositionX.HasValue == dto.MapPositionY.HasValue;
        if (!areBothCoordinatesAligned)
        {
            validationResults.Add(new ValidationResult("Coordinates X and Y must be both set or both unset",
                ["MapPositionX", "MapPositionY"]));
            return false;
        }

        if (!dto.MapPositionX.HasValue && !dto.MapPositionY.HasValue)
        {
            // Nothing to validate further
            return true;
        }

        decimal x = dto.MapPositionX!.Value;
        decimal y = dto.MapPositionY!.Value;

        bool areBothCoordinatesParsed =
            ValidateCoordinate(x, "X", validationResults) &&
            ValidateCoordinate(y, "Y", validationResults);

        if (!areBothCoordinatesParsed)
        {
            return false;
        }

        if (_sheetRowsByCoordinates.TryGetValue((x, y), out int duplicatedRow))
        {
            validationResults.Add(new ValidationResult(
                duplicatedRow == _inDatabaseFakeSheetRowNumber
                    ? "These coordinates are already defined in the database"
                    : $"These coordinates are already used in row {duplicatedRow}"
            ));
            return false;
        }

        _sheetRowsByCoordinates.Add((x, y), dto.SheetRowNumber);
        return true;
    }

    private static bool ValidateCoordinate(decimal coordinate, string coordinateKey, List<ValidationResult> results)
    {
        if (Math.Abs(coordinate / 10000) >= 1)
        {
            results.Add(new ValidationResult("Coordinates cannot exceed 9999.99", [coordinateKey]));
            return false;
        }

        return true;
    }

    private bool ValidateIdentity(ParsedSeatDto dto, List<ValidationResult> validationResults)
    {
        string row = dto.RowNumber;
        string seat = dto.SeatNumber;

        if (string.IsNullOrWhiteSpace(row) || string.IsNullOrWhiteSpace(seat))
        {
            validationResults.Add(new ValidationResult("Row number and seat number must be set"));
            return false;
        }

        var key = (row, seat);

        if (_sheetRowsBySeatIdentity.TryGetValue(key, out int duplicatedRow))
        {
            validationResults.Add(new ValidationResult(
                duplicatedRow == _inDatabaseFakeSheetRowNumber
                    ? $"Seat with the same row/seat numbers ({row}/{seat}) already exists in the database"
                    : $"Seat with the same row/seat numbers ({row}/{seat}) already exists at row {duplicatedRow}"));
            return false;
        }

        _sheetRowsBySeatIdentity.Add(key, dto.SheetRowNumber);

        return true;
    }
}
