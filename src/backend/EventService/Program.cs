using EventService;
using EventService.DAL;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenApi()
    .AddDbContext<EventDbContext>(options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection"))
    )
    .AddProblemDetails()
    .AddValidation();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapGroup("events").MapEventEndpoints();

app.Run();
