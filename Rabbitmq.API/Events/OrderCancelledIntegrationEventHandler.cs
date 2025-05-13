using Rabbitmq.Core.Events;

namespace Rabbitmq.API.Events;

public class OrderCancelledIntegrationEventHandler
    : IIntegrationEventHandler<CustomerMasstransitOrderCancelledIntegrationEvent>
{
    private readonly ILogger<OrderCancelledIntegrationEventHandler> _logger;

    public OrderCancelledIntegrationEventHandler(ILogger<OrderCancelledIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(CustomerMasstransitOrderCancelledIntegrationEvent @event)
    {
        _logger.LogInformation("Handling OrderCancelledIntegrationEvent for OrderId: {OrderId}", @event.OrderId);

        try
        {
          
            _logger.LogDebug("Successfully processed order cancellation event. Total events received: {EventCount}",@event);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderCancelledIntegrationEvent for OrderId: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
