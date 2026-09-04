using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.SeatCategory.DTO;

namespace TixTapGo.VenueService.Endpoints.SeatCategory;

internal static class SeatCategoryEndpointsV1
{
    public static RouteGroupBuilder MapSeatCategoryEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var categoryGroup = routeBuilder.MapGroup("/{venueId:guid}/seat-categories");
        categoryGroup.MapGet("/", GetSeatCategories).WithName("GetSeatCategories");
        categoryGroup.MapGet("/{id:guid}", GetSeatCategory).WithName("GetSeatCategoryById");
        categoryGroup.MapPost("/", CreateSeatCategory).WithName("CreateSeatCategory");
        categoryGroup.MapPatch("/{id:guid}", PatchSeatCategory).WithName("UpdateSeatCategory");
        categoryGroup.MapDelete("/{id:guid}", DeleteSeatCategory).WithName("DeleteSeatCategory");

        return categoryGroup;
    }

    public static async Task<Ok<List<GetSeatCategoryResponse>>> GetSeatCategories(Guid venueId,
        VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dbContext.SeatCategories
            .Where(category => category.VenueId == venueId)
            .OrderBy(seatCategory => seatCategory.Id)
            .AsNoTracking()
            .ProjectToGetSeatCategoryResponse()
            .ToListAsync(cancellationToken));
    }

    public static async Task<Results<Ok<GetSeatCategoryResponse>, NotFound>> GetSeatCategory(Guid venueId, Guid id,
        VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        var category = await dbContext.SeatCategories.FindAsync([id], cancellationToken);
        return category != null && category.VenueId == venueId
            ? TypedResults.Ok(category.ToGetSeatCategoryResponse())
            : TypedResults.NotFound();
    }

    public static async Task<Results<CreatedAtRoute<GetSeatCategoryResponse>, ProblemHttpResult>> CreateSeatCategory(
        Guid venueId, CreateSeatCategoryRequest createSeatCategoryDto, VenueDbContext dbContext)
    {
        if (!await dbContext.Venues.AnyAsync(v => v.Id == venueId))
        {
            return TypedResults.Problem(
                detail: $"Venue with id {venueId} not found",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var seatCategoryEntry = dbContext.SeatCategories.Add(createSeatCategoryDto.ToEntity(venueId));
        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(seatCategoryEntry.Entity.ToGetSeatCategoryResponse(), "GetSeatCategoryById",
            new { id = seatCategoryEntry.Entity.Id });
    }

    public static async Task<Results<Ok<GetSeatCategoryResponse>, NotFound>> PatchSeatCategory(Guid venueId,
        Guid id, UpdateSeatCategoryRequest updateSeatCategoryDto, VenueDbContext dbContext)
    {
        var category = await dbContext.SeatCategories
            .Where(category => category.Id == id && category.VenueId == venueId)
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(updateSeatCategoryDto.Title))
        {
            category.Title = updateSeatCategoryDto.Title;
        }

        if (!string.IsNullOrWhiteSpace(updateSeatCategoryDto.Color))
        {
            category.Color = updateSeatCategoryDto.Color;
        }

        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(category.ToGetSeatCategoryResponse());
    }

    public static async Task<Results<NoContent, NotFound>> DeleteSeatCategory(Guid venueId, Guid id,
        VenueDbContext dbContext)
    {
        var seatCategory = await dbContext.SeatCategories
            .Where(category => category.Id == id && category.VenueId == venueId)
            .FirstOrDefaultAsync();
        if (seatCategory == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.SeatCategories.Remove(seatCategory);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
