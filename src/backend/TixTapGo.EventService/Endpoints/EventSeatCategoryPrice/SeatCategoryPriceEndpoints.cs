using MassTransit;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using TixTapGo.EventService.Contracts.Messages.EventSeatCategory;
using TixTapGo.EventService.DAL;
using TixTapGo.EventService.Endpoints.EventSeatCategoryPrice.DTO;
using TixTapGo.EventService.Entities;
using TixTapGo.EventService.Enums;

namespace TixTapGo.EventService.Endpoints.EventSeatCategoryPrice;

internal static class SeatCategoryPriceEndpoints
{
    public static IEndpointRouteBuilder MapEventSeatCategoryPriceEndpoints(this RouteGroupBuilder routeBuilder)
    {
        var categoryPriceGroup = routeBuilder
            .MapGroup("/{eventId:guid}/category-prices")
            .AddEndpointFilter<EventExistsFilter>()
            .ProducesProblem(400)
            .ProducesProblem(404);
        categoryPriceGroup.MapGet("/", GetEventCategoryPrices).WithName("GetEventCategoryPrices");
        categoryPriceGroup.MapGet("/{id:guid}", GetEventCategoryPrice).WithName("GetEventCategoryPrice");
        categoryPriceGroup.MapPost("/", CreateEventCategoryPrice).WithName("CreateEventCategoryPrice");
        categoryPriceGroup.MapPut("/{id:guid}", UpdateCategoryPrice).WithName("UpdateEventCategoryPrice");
        categoryPriceGroup.MapPost("/{id:guid}/enabled", SetCategoryPriceEnabled)
            .WithName("SetCategoryPriceEnabled");
        categoryPriceGroup.MapDelete("/{id:guid}", DeleteEventCategoryPrice).WithName("DeleteEventCategoryPrice");

        return categoryPriceGroup;
    }

    public static async Task<Results<CreatedAtRoute<GetSeatCategoryPriceResponse>, NotFound, ProblemHttpResult>>
        CreateEventCategoryPrice(Guid eventId, CreateSeatCategoryPriceRequest request, EventDbContext dbContext)
    {
        var eventStatus = await dbContext.Events
            .Where(e => e.Id == eventId)
            .Select(e => e.Status)
            .FirstAsync();

        if (eventStatus != EventStatus.Upcoming)
        {
            return TypedResults.Problem(
                detail: $"Impossible to set pricing for event in {eventStatus} state",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var categoryWithEventCapacity = await dbContext.VenueSeatCategories
            .Where(category => category.Id == request.VenueSeatCategoryId)
            .Select(category => new
            {
                Category = category,
                AssignedCapacity = category.Prices
                    .AsQueryable()
                    .Where(SeatCategoryPrice.GetCountableForEvent(eventId))
                    .Sum(price => price.Capacity)
            })
            .FirstOrDefaultAsync();

        if (categoryWithEventCapacity?.Category == null)
        {
            return TypedResults.Problem(
                detail: $"Venue seat category with id {request.VenueSeatCategoryId} not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        var venueSeatCategory = categoryWithEventCapacity.Category;
        var eventCategoryAssignedCapacity = categoryWithEventCapacity.AssignedCapacity;

        if (venueSeatCategory.PendingRemove)
        {
            return TypedResults.Problem(
                detail:
                $"Venue seat category with id {request.VenueSeatCategoryId} was removed. It is impossible to create new pricing for it",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (venueSeatCategory.TotalCapacity == 0)
        {
            return TypedResults.Problem(
                detail:
                $"Venue seat category with id {request.VenueSeatCategoryId} is not configured properly yet (capacity 0)",
                statusCode: StatusCodes.Status400BadRequest);
        }

        int remainingCapacity = venueSeatCategory.TotalCapacity - eventCategoryAssignedCapacity;

        if (remainingCapacity < request.Capacity)
        {
            return TypedResults.Problem(
                detail: $"Venue seat category with id {request.VenueSeatCategoryId} doesn't have " +
                        $"enough capacity - only {remainingCapacity} out of {request.Capacity} attendees are available",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var categoryPricing = new SeatCategoryPrice(
            eventId: eventId,
            venueSeatCategory: venueSeatCategory,
            isEnabled: request.IsEnabled,
            basePrice: request.BasePrice,
            capacity: request.Capacity,
            seatAssignmentType: request.SeatAssignmentType
        );

        dbContext.SeatCategoryPrices.Add(categoryPricing);
        await dbContext.SaveChangesAsync();

        return TypedResults.CreatedAtRoute(categoryPricing.ToGetSeatCategoryPriceResponse(), "GetEventCategoryPrice",
            new { id = categoryPricing.Id });
    }

    public static async Task<Results<Ok<List<GetSeatCategoryPriceResponse>>, ProblemHttpResult>> GetEventCategoryPrices(
        Guid eventId, EventDbContext dbContext, CancellationToken cancellationToken)
    {
        var seatCategoryPrices = await dbContext.SeatCategoryPrices
            .Where(price => price.EventId == eventId)
            .OrderBy(price => price.Id)
            .AsNoTracking()
            .ProjectToSeatCategoryResponses()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(seatCategoryPrices);
    }

    public static async Task<Results<Ok<GetSeatCategoryPriceResponse>, NotFound, ProblemHttpResult>>
        GetEventCategoryPrice(Guid eventId, Guid id, EventDbContext dbContext, CancellationToken cancellationToken)
    {
        var seatCategoryPrice = await dbContext.SeatCategoryPrices
            .Include(price => price.VenueSeatCategory)
            .Where(price => price.EventId == eventId)
            .AsNoTracking()
            .FirstOrDefaultAsync(price => price.Id == id, cancellationToken);

        return seatCategoryPrice == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(seatCategoryPrice.ToGetSeatCategoryPriceResponse());
    }

    public static async Task<Results<Ok<GetSeatCategoryPriceResponse>, NotFound>> UpdateCategoryPrice(Guid eventId,
        Guid id, UpdateSeatCategoryPriceRequest updatePriceRequest, EventDbContext dbContext)
    {
        var categoryPrice = await dbContext.SeatCategoryPrices
            .Include(price => price.VenueSeatCategory)
            .ThenInclude(vsc => vsc.Prices)
            .ThenInclude(prices => prices.Event)
            .Include(price => price.Event)
            .Where(price => price.EventId == eventId)
            .FirstOrDefaultAsync(price => price.Id == id);

        if (categoryPrice == null)
        {
            return TypedResults.NotFound();
        }

        categoryPrice.SetAssignmentType(updatePriceRequest.SeatAssignmentType);
        categoryPrice.SetCapacity(updatePriceRequest.Capacity);
        categoryPrice.SetPrice(updatePriceRequest.BasePrice);

        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(categoryPrice.ToGetSeatCategoryPriceResponse());
    }

    public static async Task<Results<Ok<GetSeatCategoryPriceResponse>, NotFound>> SetCategoryPriceEnabled(Guid eventId,
        Guid id, SetSeatCategoryPriceEnabledRequest setEnabledRequest, EventDbContext dbContext)
    {
        var categoryPrice = await dbContext.SeatCategoryPrices
            .Where(price => price.EventId == eventId)
            .Include(price => price.VenueSeatCategory)
            .ThenInclude(category => category.Prices)
            .ThenInclude(prices => prices.Event)
            .Include(price => price.Event)
            .FirstOrDefaultAsync(price => price.Id == id);

        if (categoryPrice == null)
        {
            return TypedResults.NotFound();
        }

        categoryPrice.SetEnabled(setEnabledRequest.SetEnabled);

        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(categoryPrice.ToGetSeatCategoryPriceResponse());
    }

    public static async Task<Results<NoContent, NotFound>> DeleteEventCategoryPrice(Guid eventId, Guid id,
        EventDbContext dbContext, IPublishEndpoint rmqPublishEndpoint)
    {
        var categoryPrice = await dbContext.SeatCategoryPrices
            .Where(price => price.EventId == eventId)
            .FirstOrDefaultAsync(price => price.Id == id);

        if (categoryPrice == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.SeatCategoryPrices.Remove(categoryPrice);
        await rmqPublishEndpoint.Publish(new EventSeatCategoryPriceDeleted(eventId, id));

        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private class EventExistsFilter(EventDbContext dbContext) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            // Guid validation is actually redundant because eventId format is already validated
            // by routing, but we still need to check that the event exists
            string? eventIdRaw = context.HttpContext.GetRouteValue("eventId") as string;
            if (string.IsNullOrWhiteSpace(eventIdRaw))
            {
                return TypedResults.Problem(
                    detail: "EventId is required",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            if (!Guid.TryParse(eventIdRaw, out Guid eventId))
            {
                return TypedResults.Problem(
                    detail: "EventId value must be a valid UUID",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            if (!await dbContext.Events.AnyAsync(e => e.Id == eventId))
            {
                return TypedResults.Problem(
                    detail: $"Event with id {eventId} not found",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            return await next(context);
        }
    }
}
