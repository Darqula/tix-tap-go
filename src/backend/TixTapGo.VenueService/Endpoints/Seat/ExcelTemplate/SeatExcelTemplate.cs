using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using ClosedXML.Excel;

namespace TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal sealed class SeatExcelTemplate
{
    private const int _maxRowCount = 30_000;
    private const int _templateVersion = 1;

    sealed class SeatColumnsV1
    {
        public const int RowNumber = 2;
        public const int SeatNumber = 3;
        public const int Category = 4;
        public const int MapPositionX = 5;
        public const int MapPositionY = 6;
    }
    
    public Stream GetTemplateStream()
    {
        var template = BuildTemplate();
        var templateMemoryStream = new MemoryStream();
        template.SaveAs(templateMemoryStream);
        templateMemoryStream.Position = 0;

        return templateMemoryStream;
    }

    public async Task<ParseSeatsResult> ParseSeatsFromRequestAsync(Stream bodyStream, CancellationToken cancellationToken)
    {
        // ASP.NET Core streams disallow sync read required by ClosedXML
        // So we need to copy the stream to a memory stream asynchronously
        using var memoryStream = new MemoryStream();
        await bodyStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return ParseSeats(memoryStream);
    }

    public ParseSeatsResult ParseSeats(Stream content)
    {
        try
        {
            return ParseSeatsInternal(content);
        }
        catch (Exception e)
        {
            return ParseSeatsResult.Fail(
                -1,
                [new ParseSeatsResult.ParsingError(0, e.Message)]
            );
        }
    }

    private ParseSeatsResult ParseSeatsInternal(Stream content)
    {
        var wb = new XLWorkbook(content);
        if (!wb.TryGetWorksheet("Seats", out var ws))
        {
            return ParseSeatsResult.Fail(
                -1,
                [new ParseSeatsResult.ParsingError(0, "Sheet 'Seats' not found")]
            );
        }

        if (!int.TryParse(ws.Cell(1, 2).Value.ToString(CultureInfo.InvariantCulture), out int version))
        {
            return ParseSeatsResult.Fail(
                version,
                [
                    new ParseSeatsResult.ParsingError(1,
                        "Cannot determine template version. Make sure the document matches the provided template.")
                ]
            );
        }

        if (version <= 0 || version > _templateVersion)
        {
            return ParseSeatsResult.Fail(
                version,
                [new ParseSeatsResult.ParsingError(1, "Invalid template version")]
            );
        }

        var seats = new List<ParsedSeatDto>();
        var errors = new List<ParseSeatsResult.ParsingError>();

        var activeRows = ws.RowsUsed(row => row.RowNumber() > 2 &&
                           !row.Cell(SeatColumnsV1.RowNumber).IsEmpty() &&
                           !row.Cell(SeatColumnsV1.SeatNumber).IsEmpty()
        );
        int remainingRows = _maxRowCount;

        foreach (var row in activeRows)
        {
            try
            {
                if (remainingRows-- == 0)
                {
                    errors.Add(new ParseSeatsResult.ParsingError(row.RowNumber(), $"Too many rows. Max {_maxRowCount} rows allowed."));
                    break;
                }

                if (!TryParseRow(row, out ParsedSeatDto? dto, out ParseSeatsResult.ParsingError? error))
                {
                    errors.Add(error);
                    continue;
                }

                seats.Add(dto);
            }
            catch (Exception e)
            {
                errors.Add(new ParseSeatsResult.ParsingError(row.RowNumber(), e.Message));
            }
        }

        return errors.Count == 0
            ? ParseSeatsResult.Success(version, seats)
            : ParseSeatsResult.Fail(version, errors, seats);
    }

    private bool TryParseRow(IXLRow row,
        [NotNullWhen(true)] out ParsedSeatDto? dto,
        [NotNullWhen(false)] out ParseSeatsResult.ParsingError? error)
    {
        int sheetRowNumber = row.RowNumber();
        var (mapXFailed, mapPositionX) = ParseCoordinate(row, SeatColumnsV1.MapPositionX);
        var (mapYFailed, mapPositionY) = ParseCoordinate(row, SeatColumnsV1.MapPositionY);

        if (mapXFailed || mapYFailed)
        {
            string problemCoordinates = (mapXFailed, mapYFailed) switch
            {
                (true, true) => "X and Y",
                (true, false) => "X",
                (false, true) => "Y",
                (false, false) => ""
            };
            error = new ParseSeatsResult.ParsingError(
                sheetRowNumber,
                $"Invalid seat position ({problemCoordinates})");
            dto = null;
            return false;
        }

        dto = new ParsedSeatDto()
        {
            SheetRowNumber = sheetRowNumber,
            // To uniformly handle both Numeric and Text cells we avoid GetText (it throws cast exceptions)
            // and use simple ToString with invariant culture (to avoid surprises with delimiters)
            RowNumber = row.Cell(SeatColumnsV1.RowNumber).Value.ToString(CultureInfo.InvariantCulture),
            SeatNumber = row.Cell(SeatColumnsV1.SeatNumber).Value.ToString(CultureInfo.InvariantCulture),
            Category = row.Cell(SeatColumnsV1.Category).Value.ToString(CultureInfo.InvariantCulture),
            MapPositionX = mapPositionX,
            MapPositionY = mapPositionY
        };

        error = null;
        return true;
    }

    private (bool success, decimal? coordinate) ParseCoordinate(IXLRow row, int cellIdx)
    {
        string coordinateRaw = row.Cell(cellIdx).Value.ToString(CultureInfo.InvariantCulture);
        bool isValueSet = !string.IsNullOrWhiteSpace(coordinateRaw);
        bool isParseFailed = !decimal.TryParse(coordinateRaw, CultureInfo.InvariantCulture,  out decimal coordinate);
        decimal roundedCoordinate = decimal.Round(coordinate, 2);

        return (isValueSet && isParseFailed, isParseFailed ? null : roundedCoordinate);
    }

    private static XLWorkbook BuildTemplate()
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Seats");
        ws.Cell(1, 1).Value = "Version:";
        ws.Cell(1, 2).Value = _templateVersion;
        ws.Cell(1, 3).Value = "(do not modify this value)";


        ws.Cell(2, 1).Value = "Seat properties:";
        ws.Cell(2, SeatColumnsV1.RowNumber).Value = "Row";
        ws.Cell(2, SeatColumnsV1.SeatNumber).Value = "Seat";
        ws.Cell(2, SeatColumnsV1.Category).Value = "Category";
        ws.Cell(2, SeatColumnsV1.MapPositionX).Value = "MapPositionX";
        ws.Cell(2, SeatColumnsV1.MapPositionY).Value = "MapPositionY";

        // Example
        var exampleSeat = ParsedSeatDto.Example;
        int exampleRowNumber = exampleSeat.SheetRowNumber;
        ws.Cell(exampleRowNumber, 1).Value = "Example:";
        ws.Cell(exampleRowNumber, SeatColumnsV1.RowNumber).Value = exampleSeat.RowNumber;
        ws.Cell(exampleRowNumber, SeatColumnsV1.SeatNumber).Value = exampleSeat.SeatNumber;
        ws.Cell(exampleRowNumber, SeatColumnsV1.Category).Value = exampleSeat.Category;
        ws.Cell(exampleRowNumber, SeatColumnsV1.MapPositionX).Value = exampleSeat.MapPositionX;
        ws.Cell(exampleRowNumber, SeatColumnsV1.MapPositionY).Value = exampleSeat.MapPositionY;

        return wb;
    }
}
