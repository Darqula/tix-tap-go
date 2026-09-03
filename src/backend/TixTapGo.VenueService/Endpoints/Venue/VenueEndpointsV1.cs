using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.Venue.DTO;

namespace TixTapGo.VenueService.Endpoints.Venue;

internal static class VenueEndpointsV1
{
    public static RouteGroupBuilder MapVenueEndpoints(this IEndpointRouteBuilder groupBuilder)
    {
        var venueGroup = groupBuilder.MapGroup("venues");
        venueGroup.MapGet("/", GetVenues).WithName("GetVenues");
        venueGroup.MapGet("/{id:guid}", GetVenue).WithName("GetVenueById");
        venueGroup.MapPost("/", CreateVenue).WithName("CreateVenue");
        venueGroup.MapPatch("/{id:guid}", PatchVenue).WithName("UpdateVenue");
        venueGroup.MapDelete("/{id:guid}", DeleteVenue).WithName("DeleteVenue");

        return venueGroup;
    }

    public static async Task<Ok<List<GetVenueResponse>>> GetVenues(VenueDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var venues = await dbContext.Venues
            .OrderBy(venue => venue.Id)
            .AsNoTracking()
            .ProjectToGetVenueResponses()
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
        UpdateVenueRequest updateVenueDto, VenueDbContext dbContext)
    {
        var venue = await dbContext.Venues.FindAsync(id);
        if (venue == null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(updateVenueDto.Title))
        {
            venue.Title = updateVenueDto.Title;
        }

        if (!string.IsNullOrWhiteSpace(updateVenueDto.Address))
        {
            venue.Address = updateVenueDto.Address;
        }

        if (updateVenueDto.Description != null)
        {
            venue.Description = updateVenueDto.Description;
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
