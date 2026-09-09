using MassTransit;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.VenueService.Contracts.Messages;
using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.Venue.DTO;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.Endpoints.Venue;

internal static class VenueEndpointsV1
{
    public static RouteGroupBuilder MapVenueEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var venueGroup = routeBuilder.MapGroup("venues");
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

    public static async Task<Results<Ok<GetVenueDetailedResponse>, NotFound>> GetVenue(Guid id,
        VenueDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var result = await dbContext.Venues
            .Where(venue => venue.Id == id)
            .Select(venue => new
            {
                Venue = venue,
                CurrentMapVersion = venue.SeatingMapVersions!.AsQueryable()
                    .Where(version => version.VenueId == id)
                    .Where(VenueSeatingMapVersion.IsCurrentActive)
                    .SingleOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result?.Venue != null
            ? TypedResults.Ok(result.Venue.ToGetVenueDetailedResponse(result.CurrentMapVersion?.Id))
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

    public static async Task<Results<NoContent, NotFound>> DeleteVenue(Guid id, VenueDbContext dbContext,
        IPublishEndpoint rmqPublishEndpoint)
    {
        var venueWithActiveMap = await dbContext.Venues
            .Where(venue => venue.Id == id)
            .Select(venue =>
                new
                {
                    Venue = venue,
                    SeatingMapId = venue.SeatingMapVersions!
                        .AsQueryable()
                        .Where(version => version.VenueId == id)
                        .Where(VenueSeatingMapVersion.IsCurrentActive)
                        .Select<VenueSeatingMapVersion, Guid?>(version => version.Id)
                        .FirstOrDefault()
                })
            .FirstOrDefaultAsync();

        if (venueWithActiveMap?.Venue == null)
        {
            return TypedResults.NotFound();
        }

        var venue = venueWithActiveMap.Venue;
        var seatingMap = venueWithActiveMap.SeatingMapId;

        dbContext.Venues.Remove(venue);
        await rmqPublishEndpoint.Publish(new VenueDeleted(venue.Id, seatingMap));
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
