using EventService.DAL;
using EventService.DTO;
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
    .MapPost("/",
        async (CreateEventRequest createEventDto, EventDbContext dbContext) =>
        {
            var createdEvent = dbContext.Events.Add(createEventDto.ToModel());
            await dbContext.SaveChangesAsync();
            return TypedResults.Created($"/events/{createdEvent.Entity.Id}", new GetEventResponse(createdEvent.Entity));
        })
    .WithName("Create Event");

app.Run();
