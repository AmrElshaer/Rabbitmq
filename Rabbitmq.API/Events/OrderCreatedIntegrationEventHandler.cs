using Rabbitmq.Core.Events;

namespace Rabbitmq.API.Events;

public class OrderCreatedIntegrationEventHandler
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;
    public List<OrderCreatedIntegrationEvent> ReceivedEvents { get; } = new();

    public OrderCreatedIntegrationEventHandler(ILogger<OrderCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreatedIntegrationEvent @event)
    {
        _logger.LogInformation("Handling OrderCreatedIntegrationEvent for OrderId: {OrderId}", @event.OrderId);

        try
        {
            ReceivedEvents.Add(@event);
            _logger.LogDebug("Successfully processed order creation event. Total events received: {EventCount}",
                ReceivedEvents.Count);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderCreatedIntegrationEvent for OrderId: {OrderId}", @event.OrderId);
            throw;
        }
    }
}