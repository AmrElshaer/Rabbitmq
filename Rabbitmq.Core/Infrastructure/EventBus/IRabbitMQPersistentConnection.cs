
using RabbitMQ.Client;

namespace Rabbitmq.Core.Infrastructure.EventBus
{
	public interface IRabbitMQPersistentConnection : IDisposable
	{
		bool IsConnected { get; }
		Task<IModel> CreateModelAsync(CancellationToken cancellationToken = default);
		Task<bool> TryConnectAsync(CancellationToken cancellationToken = default);
	}
}
