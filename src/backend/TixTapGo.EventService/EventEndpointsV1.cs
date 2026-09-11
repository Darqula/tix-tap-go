using System.Diagnostics;

using TixTapGo.EventService.DAL;
using TixTapGo.EventService.DTO;
using TixTapGo.EventService.DTO.Mapping;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.Contracts.Enums;
using TixTapGo.EventService.Enums;
using TixTapGo.EventService.Integrations.InternalServices.VenueService;
using TixTapGo.Shared.Converters;
using TixTapGo.Shared.Persistence.DAL.Idempotency;
using TixTapGo.Shared.Persistence.Queries;

namespace TixTapGo.EventService;

internal static class EventEndpointsV1
{
    internal static void MapEventEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/", GetEvents).WithName("GetEvents");
        groupBuilder.MapGet("/{id:guid}", GetEvent).WithName("GetEventById");
        groupBuilder.MapPost("/", CreateEvent)
            .WithName("CreateEvent")
            .WithIdempotencyCheck();
        groupBuilder.MapPatch("/{id:guid}", PatchEvent).WithName("UpdateEvent");
        groupBuilder.MapDelete("/{id:guid}", DeleteEvent).WithName("DeleteEvent");
    }

    internal static async Task<Ok<List<GetEventResponse>>> GetEvents(CaseInsensitiveEnum<EventStatus>[] status,
        Guid[] venue, EventDbContext dbContext, CancellationToken cancellationToken)
    {
        EventStatus[] statuses = status.Select(s => s.Value).ToArray();
        var events = await dbContext.Events
            .WhereIf(statuses.Length > 0, @event => statuses.Contains(@event.Status))
            .WhereIf(venue.Length > 0, @event => venue.Contains(@event.VenueId))
            .OrderBy(@event => @event.Id)
            .AsNoTracking()
            .ProjectToGetEventResponse()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(events);
    }

    internal static async Task<Results<Ok<GetEventDetailedResponse>, NotFound>> GetEvent(Guid id, EventDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events
            .Include(@event => @event.AttendeeGroups)
            .AsNoTracking()
            .FirstOrDefaultAsync(@event => @event.Id == id, cancellationToken);

        return @event != null
            ? TypedResults.Ok(@event.ToGetEventDetailedResponse())
            : TypedResults.NotFound();
    }

    internal static async Task<Results<CreatedAtRoute<GetEventResponse>, ValidationProblem, ProblemHttpResult>>
        CreateEvent(CreateEventRequest createEventDto, EventDbContext dbContext,
            VenueServiceHttpClient venueServiceClient)
    {
        var creatingEvent = createEventDto.ToEntity();
        if (!creatingEvent.Validate(out var errorsDictionary))
        {
            return TypedResults.ValidationProblem(errorsDictionary.ToValidationProblemPayload());
        }

        var venueValidationProblem = await ValidateVenueRemote(createEventDto.VenueId, venueServiceClient);
        if (venueValidationProblem != null)
        {
            return venueValidationProblem.Result switch
            {
                ValidationProblem vp => vp,
                ProblemHttpResult pr => pr,
                _ => throw new UnreachableException()
            };
        }

        var createdEventEntry = dbContext.Events.Add(creatingEvent);

        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(createdEventEntry.Entity.ToGetEventResponse(), "GetEventById",
            new { id = createdEventEntry.Entity.Id });
    }

    internal static async Task<Results<Ok<GetEventResponse>, NotFound, ProblemHttpResult, ValidationProblem>>
        PatchEvent(Guid id, UpdateEventRequest updateRequest, EventDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events.FindAsync([id], CancellationToken.None);
        if (@event == null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Title))
        {
            @event.Title = updateRequest.Title;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Description))
        {
            @event.Description = updateRequest.Description;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Location))
        {
            @event.Location = updateRequest.Location;
        }

        if (updateRequest.Start is { } newStart)
        {
            @event.Start = newStart;
        }

        if (updateRequest.End is { } newEnd)
        {
            @event.End = newEnd;
        }

        if (updateRequest.Status is { } newStatus)
        {
            switch (newStatus)
            {
                case EventStatus.Upcoming: @event.SetUpcoming(); break;
                case EventStatus.Completed: @event.Complete(); break;
                case EventStatus.Cancelled: @event.Cancel(EventCancellationReason.Manual); break;
                default: throw new UnreachableException($"Unknown status {newStatus}");
            }
        }

        if (!@event.Validate(out var errorsDictionary))
        {
            return TypedResults.ValidationProblem(errorsDictionary.ToValidationProblemPayload());
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        await dbContext
            .Entry(@event)
            .Collection(e => e.AttendeeGroups)
            .LoadAsync(cancellationToken);

        return TypedResults.Ok(@event.ToGetEventResponse());
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteEvent(Guid id, EventDbContext dbContext)
    {
        var @event = await dbContext.Events
            .Include(e => e.AttendeeGroups)
            .FirstOrDefaultAsync(@event => @event.Id == id);

        if (@event == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Events.Remove(@event);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    
    private static async Task<Results<ValidationProblem, ProblemHttpResult>?> ValidateVenueRemote(Guid venueId,
        VenueServiceHttpClient venueHttpClient, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await venueHttpClient.GetVenueAsync(venueId, cancellationToken);
            return null;
        }
        catch (HttpRequestException hre) when ((int?)hre.StatusCode is >= 400 and < 500)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(CreateEventRequest.VenueId)] = [hre.Message]
            });
        }
        catch (HttpRequestException hre)
        {
            return TypedResults.Problem(
                detail: "Venue service error",
                statusCode: StatusCodes.Status502BadGateway,
                extensions: new Dictionary<string, object?>
                {
                    [hre.StatusCode?.ToString() ?? "0"] = new[] { hre.Message }
                });
        }
    }
}
