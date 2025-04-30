using System.ComponentModel.DataAnnotations.Schema;
using Rabbitmq.API.Events;
using Rabbitmq.Core.Events;

namespace Rabbitmq.API;

public class BaseEntity
{
    private readonly List<IntegrationEvent> _integrationEvents = new ();
    [NotMapped]
    public IReadOnlyCollection<IntegrationEvent> IntegrationEvents => _integrationEvents.AsReadOnly();
    public void AddIntegrationEvent(IntegrationEvent integrationEvent)
    {
        _integrationEvents.Add(integrationEvent);
    }

    public void ClearIntegrationEvents()
    {
        _integrationEvents.Clear();
    }
}
public class Order:BaseEntity
{
    public Guid Id { get; set; }
    public string Number { get; set; }
    public string CustomerName { get; set; }

    public static Order Create(string customerName)
    {
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            Number = $"ORD-{Guid.NewGuid()}",
            CustomerName = customerName
        };
        var @event =new OrderCreatedIntegrationEvent(order.Id,order.CustomerName);
        order.AddIntegrationEvent(@event);
        return order;
    }
}
public record CreateOrderCommand(string CustomerName);