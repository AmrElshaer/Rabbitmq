using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rabbitmq.Core.Infrastructure.EventBus;

namespace Rabbitmq.Core.Infrastructure
{
	internal class RabbitMQHealthCheck : IHealthCheck
	{
		private readonly IRabbitMQPersistentConnection _connection;

		public RabbitMQHealthCheck(IRabbitMQPersistentConnection connection)
		{
			_connection = connection;
		}

		public async Task<HealthCheckResult> CheckHealthAsync(
			HealthCheckContext context,
			CancellationToken cancellationToken = default)
		{
			try
			{
				if (!_connection.IsConnected)
				{
					await _connection.TryConnectAsync(cancellationToken);
				}

				return _connection.IsConnected
					? HealthCheckResult.Healthy()
					: HealthCheckResult.Unhealthy();
			}
			catch (Exception ex)
			{
				return HealthCheckResult.Unhealthy(exception: ex);
			}
		}
	}
}
