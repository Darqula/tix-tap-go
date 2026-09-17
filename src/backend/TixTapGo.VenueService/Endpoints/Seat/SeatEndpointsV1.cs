using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Npgsql;

using TixTapGo.Shared.Persistence.DAL.Idempotency;
using TixTapGo.Shared.Validation;
using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.Seat.DTO;
using TixTapGo.VenueService.Endpoints.Seat.ExcelTemplate;

namespace TixTapGo.VenueService.Endpoints.Seat;

internal static class SeatEndpointsV1
{
    public static IEndpointRouteBuilder MapSeatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var seatEndpointGroup = endpoints.MapGroup("{mapVersionId:guid}/seats");
        seatEndpointGroup.MapGet("/", GetSeats).WithName("GetSeats");
        seatEndpointGroup.MapGet("/template", DownloadSeatsTemplate).WithName("DownloadSeatsTemplate");
        seatEndpointGroup.MapPost("/template", UploadSeatsTemplate)
            .WithName("UploadSeatsTemplate")
            .WithIdempotencyCheck();

        return seatEndpointGroup;
    }

    public static async Task<Ok<List<GetSeatResponse>>> GetSeats(Guid venueId, Guid mapVersionId,
        VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await dbContext.VenueSeats
                .Where(seat => seat.SeatingMapVersionId == mapVersionId && seat.SeatingMapVersion.VenueId == venueId)
                .AsNoTracking()
                .ProjectToGetSeatResponse()
                .ToListAsync(cancellationToken)
        );
    }

    public static FileStreamHttpResult DownloadSeatsTemplate(Guid venueId, Guid mapVersionId,
        SeatExcelTemplate template, CancellationToken cancellationToken)
    {
        var templateMemoryStream = template.GetTemplateStream();

        return TypedResults.File(
            templateMemoryStream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "seats-template.xlsx"
        );
    }

    public static async Task<Results<Ok<UploadTemplateResponse>, ProblemHttpResult, ValidationProblem>>
        UploadSeatsTemplate(Guid venueId, Guid mapVersionId, HttpRequest request, VenueDbContext dbContext,
            SeatExcelTemplate template, CancellationToken cancellationToken)
    {
        const int uploadMaxMb = 50;
        if (request.ContentLength > uploadMaxMb * 1024 * 1024)
        {
            return TypedResults.Problem(
                detail: $"File size exceeds {uploadMaxMb}MB limit",
                statusCode: StatusCodes.Status413PayloadTooLarge
            );
        }

        var mapVersion = await dbContext.VenueSeatingMapVersions
            .Include(version => version.Venue)
            .ThenInclude(venue => venue.SeatCategories)
            .SingleOrDefaultAsync(version => version.Id == mapVersionId, cancellationToken);

        if (mapVersion == null)
        {
            return TypedResults.Problem(
                detail: $"Seating map version {mapVersionId} not found",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (mapVersion.Venue.Id != venueId)
        {
            return TypedResults.Problem(
                detail: $"Seating map version {mapVersionId} doesn't belong to venue {venueId}",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (!mapVersion.Venue.SeatCategories.Any())
        {
            return TypedResults.Problem(
                detail: $"Venue {venueId} must have at least one seat category before creating seats",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        if (!mapVersion.IsDraft)
        {
            return TypedResults.Problem(
                detail: $"Seating map version {mapVersionId} was already published and cannot be modified anymore",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var parsingResults = await template.ParseSeatsFromRequestAsync(request.Body, cancellationToken);
        if (!parsingResults.IsSuccess)
        {
            return TypedResults.ValidationProblem(
                parsingResults.Errors?.ToValidationProblemPayload() ?? new(),
                "Parse failed due to errors");
        }

        if (parsingResults.Seats == null || parsingResults.Seats.Count == 0)
        {
            return TypedResults.Ok(new UploadTemplateResponse(0));
        }

        var categoriesByNames = mapVersion.Venue.SeatCategories.ToDictionary(category => category.Title);
        var existingSeats = await dbContext.VenueSeats
            .Where(seat => seat.SeatingMapVersionId == mapVersionId)
            .AsNoTracking()
            .ToListAsync(CancellationToken.None);

        var validator = new ParsedSeatValidator([..categoriesByNames.Keys], existingSeats);
        if (!validator.Validate(parsingResults.Seats, out ValidationErrorsDictionary errors))
        {
            return TypedResults.ValidationProblem(errors.ToValidationProblemPayload());
        }

        var seatsToInsert = parsingResults.Seats
            .Select(seatDto => seatDto.ToEntity(
                    mapVersionId,
                    categoriesByNames[seatDto.Category].Id
                )
            )
            .ToList();

        try
        {
            dbContext.VenueSeats.AddRange(seatsToInsert);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Problem(
                detail: "One or more seats were added by a concurrent upload for this map version. Please retry.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return TypedResults.Ok(new UploadTemplateResponse(seatsToInsert.Count));
    }
}
