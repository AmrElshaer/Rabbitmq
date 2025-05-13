using Rabbitmq.Core.Events;

namespace ExternalService.API.Events;

public record ExternalEventOrderCreatedEvent: IntegrationEvent 
{
    public Guid OrderId { get; }
    public string CustomerName { get; }

    public ExternalEventOrderCreatedEvent(Guid orderId, string customerName)
    {
        OrderId = orderId;
        CustomerName = customerName;
    }
}