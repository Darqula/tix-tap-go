using TixTapGo.EventService;
using TixTapGo.EventService.DAL;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<EventDbContext>("eventsdb");

builder.Services
    .AddOpenApi()
    .AddProblemDetails()
    .AddValidation();

var app = builder.Build();

app.MapDefaultEndpoints();
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
