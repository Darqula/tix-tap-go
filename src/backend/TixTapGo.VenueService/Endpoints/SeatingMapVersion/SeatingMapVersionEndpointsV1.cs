using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.Web.Idempotency;
using TixTapGo.VenueService.DAL;
using TixTapGo.VenueService.Endpoints.SeatingMapVersion.DTO;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.Endpoints.SeatingMapVersion;

internal static class SeatingMapVersionEndpointsV1
{
    public static IEndpointRouteBuilder MapSeatingMapVersionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var seatingMapVersionGroup = endpoints.MapGroup("/{venueId:guid}/seating-maps");
        seatingMapVersionGroup.MapGet("/", GetSeatingMapVersions).WithName("GetSeatingMapVersions");
        seatingMapVersionGroup.MapGet("/{id:guid}", GetSeatingMapVersion).WithName("GetSeatingMapVersionById");
        seatingMapVersionGroup.MapGet("/current", GetCurrentSeatingMapVersion).WithName("GetCurrentSeatingMapVersion");
        seatingMapVersionGroup.MapPost("/unpublish", UnpublishSeatingMapVersion)
            .WithName("GetSeatingMapVersionDrafts")
            .WithIdempotencyCheck();

        var seatingMapVersionDraftGroup = seatingMapVersionGroup.MapGroup("/drafts");
        seatingMapVersionDraftGroup.MapPost("/", CreateSeatingMapVersionDraft)
            .WithName("CreateSeatingMapVersionDraft")
            .WithIdempotencyCheck();
        seatingMapVersionDraftGroup.MapPatch("/{id:guid}", PatchSeatingMapVersionDraft)
            .WithName("UpdateSeatingMapVersionDraft");
        seatingMapVersionDraftGroup.MapDelete("/{id:guid}", DeleteSeatingMapVersionDraft)
            .WithName("DeleteSeatingMapVersionDraft");
        seatingMapVersionDraftGroup.MapPost("/{id:guid}/publish", PublishSeatingMapVersionDraft)
            .WithName("PublishSeatingMapVersionDraft")
            .WithIdempotencyCheck();

        return seatingMapVersionGroup;
    }

    public static async Task<Ok<List<GetSeatingMapVersionResponse>>> GetSeatingMapVersions(Guid venueId,
        VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await dbContext.VenueSeatingMapVersions
            .Where(version => version.VenueId == venueId)
            .OrderBy(version => version.ValidFrom)
            .ProjectToSeatingMapVersionResponse()
            .AsNoTracking()
            .ToListAsync(cancellationToken));
    }

    public static async Task<Results<Ok<GetSeatingMapVersionResponse>, NotFound>> GetSeatingMapVersion(Guid venueId,
        Guid id, VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        var mapVersion = await dbContext.VenueSeatingMapVersions.FindAsync([id], cancellationToken);
        return mapVersion?.VenueId == venueId
            ? TypedResults.Ok(mapVersion.ToGetSeatingMapVersionResponse())
            : TypedResults.NotFound();
    }

    public static async Task<Ok<GetSeatingMapVersionResponse?>> GetCurrentSeatingMapVersion(
        Guid venueId, VenueDbContext dbContext, CancellationToken cancellationToken)
    {
        var currentMapVersion = await dbContext.VenueSeatingMapVersions
            .Where(VenueSeatingMapVersion.IsCurrentActive)
            .SingleOrDefaultAsync(version => version.VenueId == venueId, cancellationToken);

        return TypedResults.Ok<GetSeatingMapVersionResponse?>(currentMapVersion?.ToGetSeatingMapVersionResponse());
    }

    public static async Task<Results<CreatedAtRoute<GetSeatingMapVersionResponse>, ProblemHttpResult>>
        CreateSeatingMapVersionDraft(Guid venueId, CreateSeatingMapVersionDraftRequest createDraftDto,
            VenueDbContext dbContext)
    {
        if (!await dbContext.Venues.AnyAsync(venue => venue.Id == venueId))
        {
            return TypedResults.Problem(detail: $"Venue with id {venueId} not found");
        }

        var newDraftVersion = dbContext.VenueSeatingMapVersions.Add(createDraftDto.ToEntity(venueId));
        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(
            newDraftVersion.Entity.ToGetSeatingMapVersionResponse(),
            "GetSeatingMapVersionById",
            new
            {
                venueId,
                id = newDraftVersion.Entity.Id
            });
    }

    public static async Task<Results<Ok<GetSeatingMapVersionResponse>, NotFound>> PatchSeatingMapVersionDraft(
        Guid venueId, Guid id, UpdateSeatingMapVersionDraftRequest updateDto, VenueDbContext dbContext)
    {
        var versionDraft = await dbContext.VenueSeatingMapVersions.SingleOrDefaultAsync(version =>
            version.Id == id && version.VenueId == venueId && version.IsDraft);

        if (versionDraft == null)
        {
            return TypedResults.NotFound();
        }

        versionDraft.Description = updateDto.Description;
        versionDraft.MapUrl = updateDto.MapUrl;

        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(versionDraft.ToGetSeatingMapVersionResponse());
    }

    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteSeatingMapVersionDraft(Guid venueId,
        Guid id, VenueDbContext dbContext)
    {
        var versionDraft = await dbContext.VenueSeatingMapVersions.SingleOrDefaultAsync(version =>
            version.Id == id && version.VenueId == venueId);

        if (versionDraft == null)
        {
            return TypedResults.NotFound();
        }

        if (!versionDraft.IsDraft)
        {
            return TypedResults.Problem(detail: $"Venue map version {id} is not draft and cannot be deleted");
        }

        dbContext.VenueSeatingMapVersions.Remove(versionDraft);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<GetSeatingMapVersionResponse>, ProblemHttpResult, NotFound>>
        PublishSeatingMapVersionDraft(Guid venueId, Guid id, VenueDbContext dbContext)
    {
        var versionDraft = await dbContext.VenueSeatingMapVersions
            .Include(version => version.Venue)
            .ThenInclude(venue => venue.SeatingMapVersions)
            .SingleOrDefaultAsync(version =>
                version.Id == id && version.VenueId == venueId && version.IsDraft);

        if (versionDraft == null)
        {
            return TypedResults.NotFound();
        }

        versionDraft.Venue.PublishVersionMap(versionDraft);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(versionDraft.ToGetSeatingMapVersionResponse());
    }

    public static async Task<Results<Ok<GetSeatingMapVersionResponse>, ProblemHttpResult>> UnpublishSeatingMapVersion(
        Guid venueId, VenueDbContext dbContext)
    {
        var venue = await dbContext.Venues
            .Include(venue => venue.SeatingMapVersions)
            .SingleOrDefaultAsync(venue => venue.Id == venueId);

        if (venue == null)
        {
            return TypedResults.Problem(detail: $"Venue with id {venueId} not found");
        }

        var unpublishedVersion = venue.UnpublishActiveMapVersion();
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(unpublishedVersion.ToGetSeatingMapVersionResponse());
    }
}
