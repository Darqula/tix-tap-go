using EventService.DAL;
using EventService.DTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services
    .AddOpenApi()
    .AddDbContext<EventDbContext>(options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection"))
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var eventsApi = app.MapGroup("events");
eventsApi
    .MapGet("/",
        async (EventDbContext dbContext) =>
        {
            return TypedResults.Ok(await dbContext.Events
                .Include(@event => @event.AttendeeGroups)
                .Select(e => new GetEventResponse(e)).ToListAsync());
        })
    .WithName("Get Events");

eventsApi
    .MapGet("/{id:guid}",
        async Task<Results<Ok<GetEventResponse>, NotFound>> (Guid id, EventDbContext dbContext) =>
        {
            var @event = await dbContext.Events
                .Include(@event => @event.AttendeeGroups)
                .FirstOrDefaultAsync(@event => @event.Id == id);

            return @event != null
                ? TypedResults.Ok(new GetEventResponse(@event))
                : TypedResults.NotFound();
        })
    .WithName("Get Event by Id");

eventsApi
    .MapPost("/",
        async (CreateEventRequest createEventDto, EventDbContext dbContext) =>
        {
            var createdEvent = dbContext.Events.Add(createEventDto.ToModel());
            await dbContext.SaveChangesAsync();
            return TypedResults.Created($"/events/{createdEvent.Entity.Id}", new GetEventResponse(createdEvent.Entity));
        })
    .WithName("Create Event");

eventsApi.MapPatch("/{id:guid}",
        async Task<Results<Ok<GetEventResponse>, NotFound>>
            (Guid id, UpdateEventRequest updateRequest, EventDbContext dbContext) =>
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

            return TypedResults.Ok(new GetEventResponse(@event));
        })
    .WithName("Update Event");

eventsApi.MapDelete("/{id:guid}",
        async Task<Results<NoContent, NotFound>> (Guid id, EventDbContext dbContext) =>
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
        })
    .WithName("Delete Event");

app.Run();