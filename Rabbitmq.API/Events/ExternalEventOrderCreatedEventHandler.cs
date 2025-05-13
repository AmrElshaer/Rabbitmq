using Rabbitmq.Core.Events;

namespace Rabbitmq.API.Events;

public class ExternalEventOrderCreatedEventHandler(ILogger<ExternalEventOrderCreatedEventHandler> logger)
    : IIntegrationEventHandler<ExternalEventOrderCreatedEvent>
{
    public List<ExternalEventOrderCreatedEvent> ReceivedEvents { get; } = new();

    public Task Handle(ExternalEventOrderCreatedEvent @event)
    {
        logger.LogInformation("Handling OrderCreatedIntegrationEvent for OrderId: {OrderId}", @event.OrderId);

        try
        {
            ReceivedEvents.Add(@event);
            logger.LogDebug("Successfully processed order creation event. Total events received: {EventCount}",
                ReceivedEvents.Count);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle OrderCreatedIntegrationEvent for OrderId: {OrderId}", @event.OrderId);
            throw;
        }
    }
}