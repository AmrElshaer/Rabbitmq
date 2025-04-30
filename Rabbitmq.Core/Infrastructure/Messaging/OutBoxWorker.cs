using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rabbitmq.Core.Domain;
using Rabbitmq.Core.Events;
using Rabbitmq.Core.Infrastructure.EventBus;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Rabbitmq.Core.Infrastructure.Messaging;




public class OutboxWorker<TDbContext> : BackgroundService where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxWorker<TDbContext>> _logger;
    private readonly string _connectionString;
    private readonly int BatchSize = 20;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int MaxErrorLength = 500;

    public OutboxWorker(IServiceProvider serviceProvider, ILogger<OutboxWorker<TDbContext>> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("Default"); // From appsettings.json
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task<List<OutboxMessage>> QueryPendingMessagesAsync(CancellationToken ct)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var result = (await connection.QueryAsync<OutboxMessage>(
            @"
            SELECT 
                id AS Id, 
                event_type AS EventType, 
                CAST(payload AS VARBINARY(MAX)) AS Payload,
                created_at AS CreatedAt, 
                retry_count AS RetryCount 
            FROM outbox_messages
            WHERE processed_at IS NULL
            ORDER BY created_at
            OFFSET 0 ROWS FETCH NEXT @BatchSize ROWS ONLY
            ", 
            new { BatchSize })).ToList();

        return result;
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var subscriptionInfo = scope.ServiceProvider.GetRequiredService<IOptions<EventBusSubscriptionInfo>>();

        var messages = await QueryPendingMessagesAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                if (!subscriptionInfo.Value.EventTypes.TryGetValue(message.EventType, out var eventType))
                {
                    await MarkMessageAsFailedAsync(dbContext, message,
                        $"Event type '{message.EventType}' not found.",
                        stoppingToken);
                    continue;
                }

                await MarkMessageAsProcessingAsync(dbContext, message, stoppingToken);
                var @event = JsonSerializer.Deserialize(message.Payload, eventType, subscriptionInfo.Value.JsonSerializerOptions) as IntegrationEvent;

                if (@event == null || @event.Id != message.Id)
                {
                    await MarkMessageAsFailedAsync(dbContext, message,
                        $"ID mismatch or null event (Stored: {message.Id}, Deserialized: {@event?.Id})",
                        stoppingToken);
                    continue;
                }
               
                await eventBus.PublishDirect((dynamic)@event, ct: stoppingToken);

                await MarkMessageAsProcessedAsync(dbContext, message, stoppingToken);
            }
            catch (JsonException jsonEx)
            {
                await HandleProcessingFailureAsync(dbContext, message,
                    new InvalidOperationException($"JSON deserialization failed for {message.EventType}", jsonEx),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                await HandleProcessingFailureAsync(dbContext, message, ex, stoppingToken);
            }
        }
    }

  

	private async Task MarkMessageAsProcessingAsync(
		TDbContext dbContext,
		OutboxMessage message,
		CancellationToken ct)
	{
       await  dbContext.Set<OutboxMessage>().Where(m=>m.Id==message.Id)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, MessageStatus.Processing)
                .SetProperty(x => x.ProcessedAt, DateTime.UtcNow), ct);
	}

	private async Task MarkMessageAsProcessedAsync(
		TDbContext dbContext,
		OutboxMessage message,
		CancellationToken ct)
	{
        await dbContext.Set<OutboxMessage>().Where(m => m.Id == message.Id)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, MessageStatus.Processed)
                .SetProperty(x => x.CompletedAt, DateTime.UtcNow)
                .SetProperty(x => x.Error, ""), ct);
	}

	private async Task MarkMessageAsFailedAsync(
		TDbContext dbContext,
		OutboxMessage message,
		string error,
		CancellationToken ct)
	{
        var errorMessge = error.Length > MaxErrorLength ? error.Substring(0, MaxErrorLength) : error;
        await dbContext.Set<OutboxMessage>().Where(m => m.Id == message.Id)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, MessageStatus.Failed)
                .SetProperty(x => x.Error, errorMessge)
                .SetProperty(x => x.RetryCount, x => x.RetryCount + 1), ct);
	}

	private async Task HandleProcessingFailureAsync(
		TDbContext dbContext,
		OutboxMessage message,
		Exception ex,
		CancellationToken ct)
	{
		_logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
		await MarkMessageAsFailedAsync(dbContext, message, ex.Message, ct);
	}

	
}
