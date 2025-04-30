using Microsoft.EntityFrameworkCore.Storage;
using Rabbitmq.Core.Domain;
using Rabbitmq.Core.Events;

namespace Rabbitmq.Core.Infrastructure.Messaging
{
	public interface ITransactionalOutbox : IAsyncDisposable
	{
		Task<IDbContextTransaction> BeginTransactionAsync();
		Task CommitAsync();
		Task RollbackAsync();
		Task<MessageStoreResult> IsDuplicateAsync(Guid messageId);
		Task<MessageStoreResult> StoreOutgoingMessageAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IntegrationEvent;
		Task<MessageStoreResult> StoreOutgoingMessageAsync<TEvent>(TEvent @event, IDbContextTransaction ts) where TEvent : IntegrationEvent;
		Task<MessageStoreResult> StoreIncomingMessageAsync(
			Guid messageId,
			string eventType,
			byte[] payload,
			string serviceName);
		Task MarkMessageAsProcessedAsync(Guid messageId);
		Task UpdateHandlerStatuses(List<(string handlerType, ProcessingResult result, Guid messageID)> resultStatuses);
	}
}