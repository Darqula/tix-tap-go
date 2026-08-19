using EventService.DAL;
using EventService.DTO;
using EventService.DTO.Mapping;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventService;

internal static class EventEndpointsV1
{
    internal static void MapEventEndpoints(this RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/", GetEvents).WithName("GetEvents");
        groupBuilder.MapGet("/{id:guid}", GetEvent).WithName("GetEventById");
        groupBuilder.MapPost("/", CreateEvent).WithName("CreateEvent");
        groupBuilder.MapPatch("/{id:guid}", PatchEvent).WithName("UpdateEvent");
        groupBuilder.MapDelete("/{id:guid}", DeleteEvent).WithName("DeleteEvent");
    }

    internal static async Task<Ok<List<GetEventResponse>>> GetEvents(EventDbContext dbContext)
    {
        var events = await dbContext.Events
            .Include(@event => @event.AttendeeGroups)
            .OrderBy(@event => @event.Id)
            .AsNoTracking()
            .Select(e => e.ToGetEventResponse())
            .ToListAsync();

        return TypedResults.Ok(events);
    }

    internal static async Task<Results<Ok<GetEventResponse>, NotFound>> GetEvent(Guid id, EventDbContext dbContext)
    {
        var @event = await dbContext.Events
            .Include(@event => @event.AttendeeGroups)
            .AsNoTracking()
            .FirstOrDefaultAsync(@event => @event.Id == id);

        return @event != null
            ? TypedResults.Ok(@event.ToGetEventResponse())
            : TypedResults.NotFound();
    }

    internal static async Task<CreatedAtRoute<GetEventResponse>> CreateEvent(CreateEventRequest createEventDto,
        EventDbContext dbContext)
    {
        var createdEvent = dbContext.Events.Add(createEventDto.ToEntity());
        await dbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(createdEvent.Entity.ToGetEventResponse(), "GetEventById",
            new { id = createdEvent.Entity.Id });
    }

    internal static async Task<Results<Ok<GetEventResponse>, NotFound>> PatchEvent(Guid id,
        UpdateEventRequest updateRequest, EventDbContext dbContext)
    {
        var @event = await dbContext.Events.FindAsync(id);
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

        await dbContext.SaveChangesAsync();

        await dbContext
            .Entry(@event)
            .Collection(e => e.AttendeeGroups)
            .LoadAsync();

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
}
