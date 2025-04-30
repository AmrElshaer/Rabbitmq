using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Rabbitmq.API.Events;
using Rabbitmq.Core.Domain;
using Rabbitmq.Core.Events;
using Rabbitmq.Core.Extensions;
using Rabbitmq.Core.Infrastructure.DbContext;
using Rabbitmq.Core.Infrastructure.EventBus;

namespace Rabbitmq.API.MyDbContext;

public class ApplicationDbContext:DbContext,IEventStoreDbContext
{
    private readonly IEventBus _eventBus;

    public ApplicationDbContext(IEventBus eventBus,DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _eventBus = eventBus;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      
        modelBuilder.UseEventStore(); 
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
    public DbSet<InboxSubscriber> InboxSubscriber { get; set; }
    public DbSet<Order> Orders { get; set; }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return BaseSaveChangesAsyncChangesAsync(cancellationToken:cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        return BaseSaveChangesAsyncChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    private async Task<int> BaseSaveChangesAsyncChangesAsync(bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = new())
    {
         return  await BaseSaveChanges();
        async Task<int> BaseSaveChanges()
        {
            var integrationEventsEmitters = GetIntegrationEventsEmitters().ToList();
       

            if (integrationEventsEmitters.Count == 0)
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            if (Database.CurrentTransaction is not null) // the transaction commit and rollback is managed outside
            {
             
                var result =  await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                await PublishIntegrationEventsAsync(integrationEventsEmitters, Database.CurrentTransaction, cancellationToken);

                return result;
            }

            await using var transaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                await PublishIntegrationEventsAsync(integrationEventsEmitters, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }
    }
    private IEnumerable<(BaseEntity EventEmitter, IReadOnlyList<IntegrationEvent> EmittedEvents)> GetIntegrationEventsEmitters()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            var emitter = entry.Entity;
            var events = emitter.IntegrationEvents;

            if (events.Count == 0)
            {
                continue;
            }

            yield return (emitter, events.ToList());
        }
    }
    private async Task PublishIntegrationEventsAsync(List<(BaseEntity EventEmitter, IReadOnlyList<IntegrationEvent> EmittedEvents)> emitters, IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        if (emitters.Count == 0)
        {
            return;
        }

       
        var allEmittedEvents = emitters
            .SelectMany(x => x.EmittedEvents)
            .ToList();

        foreach (var emittedEvent in allEmittedEvents)
        {
            await _eventBus.PublishAsync(emittedEvent,transaction);
        }

        emitters.ForEach(x => x.EventEmitter.ClearIntegrationEvents());
    }
    
}