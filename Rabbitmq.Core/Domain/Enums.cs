namespace Rabbitmq.Core.Domain
{
	public enum MessageStoreResult
	{
		Success,
		Duplicate,
		StorageFailed,
		NoSubscribers,
	}
	public enum ProcessingResult
	{
		Success,
		RetryLater,
		PermanentFailure,
		Duplicate
	}
}
