using EventService;
using EventService.DAL;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenApi()
    .AddDbContext<EventDbContext>(options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection"))
    )
    .AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapGroup("events").MapEventEndpoints();

app.Run();
