namespace Rabbitmq.Core.Infrastructure.Messaging
{
	public interface IMessageDeduplicationService
	{
		Task<bool> IsDuplicateAsync(Guid messageId);
		Task<bool> MarkAsProcessedAsync(Guid messageId);
	}
}