using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rabbitmq.API;
using Rabbitmq.API.Events;
using Rabbitmq.API.MyDbContext;
using Rabbitmq.Core;
using Rabbitmq.Core.Extensions;
using Rabbitmq.Core.Infrastructure.EventBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
builder.Services.AddOptions<EventBusOptions>()
    .Bind(builder.Configuration.GetSection("EventBus"))
    .ValidateDataAnnotations() 
    .ValidateOnStart();
builder.AddRabbitMqEventBus("RabbitMQ")
    .AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>()
    .AddEventDbContext<ApplicationDbContext>(
    builder.Configuration.GetConnectionString("Default"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();
app.MapPost("/add-order",async (CreateOrderCommand command,ApplicationDbContext dbContext,IEventBus eventBus) =>
{
    var order = Order.Create(command.CustomerName);
    var @event =new OrderCreatedIntegrationEvent(order.Id,order.CustomerName);
     await dbContext.Orders.AddAsync(order);
     await dbContext.SaveChangesAsync();
    return order.Id;
});
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}