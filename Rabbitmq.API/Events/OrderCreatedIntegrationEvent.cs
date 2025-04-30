using Rabbitmq.Core.Events;

namespace Rabbitmq.API.Events;

public record OrderCreatedIntegrationEvent : IntegrationEvent // if we need fallback to direct publish : IAllowDirectFallback
{
    public Guid OrderId { get; }
    public string CustomerName { get; }

    public OrderCreatedIntegrationEvent(Guid orderId, string customerName)
    {
        OrderId = orderId;
        CustomerName = customerName;
    }
}