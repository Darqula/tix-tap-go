using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.DTO;
using TixTapGo.VenueService.DTO.Mapping;

namespace TixTapGo.VenueService;

internal static class VenueEndpointsV1
{
    public static void MapVenueEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/", GetVenues).WithName("GetVenues");
        groupBuilder.MapGet("/{id:guid}", GetVenue).WithName("GetVenueById");
        groupBuilder.MapPost("/", CreateVenue).WithName("CreateVenue");
        groupBuilder.MapPatch("/{id:guid}", PatchVenue).WithName("UpdateVenue");
        groupBuilder.MapDelete("/{id:guid}", DeleteVenue).WithName("DeleteVenue");
    }

    public static async Task<Ok<List<GetVenueResponse>>> GetVenues(VenueDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var venues = await dbContext.Venues
            .OrderBy(venue => venue.Id)
            .AsNoTracking()
            .Select(venue => venue.ToGetVenueResponse())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(venues);
    }

    public static async Task<Results<Ok<GetVenueResponse>, NotFound>> GetVenue(Guid id, VenueDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var venue = await dbContext.Venues.FindAsync([id], cancellationToken);
        return venue != null
            ? TypedResults.Ok(venue.ToGetVenueResponse())
            : TypedResults.NotFound();
    }

    public static async Task<CreatedAtRoute<GetVenueResponse>> CreateVenue(CreateVenueRequest createVenueDto,
        VenueDbContext dbContext)
    {
        var createdVenue = dbContext.Venues.Add(createVenueDto.ToEntity());
        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(createdVenue.Entity.ToGetVenueResponse(), "GetVenueById",
            new { id = createdVenue.Entity.Id });
    }

    public static async Task<Results<Ok<GetVenueResponse>, NotFound>> PatchVenue(Guid id,
        UpdateVenueRequest updateRequest, VenueDbContext dbContext)
    {
        var venue = await dbContext.Venues.FindAsync(id);
        if (venue == null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Title))
        {
            venue.Title = updateRequest.Title;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Address))
        {
            venue.Address = updateRequest.Address;
        }

        if (updateRequest.Description != null)
        {
            venue.Description = updateRequest.Description;
        }

        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(venue.ToGetVenueResponse());
    }

    public static async Task<Results<NoContent, NotFound>> DeleteVenue(Guid id, VenueDbContext dbContext)
    {
        var venue = await dbContext.Venues.FindAsync(id);
        if (venue == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Venues.Remove(venue);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
