using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rabbitmq.Core.Events;
using Rabbitmq.Core.Infrastructure;
using Rabbitmq.Core.Infrastructure.DbContext;
using Rabbitmq.Core.Infrastructure.EventBus;
using Rabbitmq.Core.Infrastructure.Messaging;

namespace Rabbitmq.Core.Extensions;

public static class EventBusExtensions
	{

	public static IEventBusBuilder AddRabbitMqEventBus(this IHostApplicationBuilder builder, string connectionName)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.Services.AddOptions<EventBusOptions>()
			.Bind(builder.Configuration.GetSection("EventBus"));
		builder.AddRabbitMQClient(connectionName, configureConnectionFactory: factory =>
		{
			(factory).DispatchConsumersAsync = true;
		});
		builder.Services.AddSingleton<IResiliencePipelineProvider, ResiliencePipelineFactory>();
		builder.Services.AddSingleton<IRabbitMQPersistentConnection, RabbitMQPersistentConnection>();
		builder.Services.AddSingleton<IEventBus, EventBus>();
		builder.Services.AddSingleton<IHostedService>(sp =>
			(EventBus)sp.GetRequiredService<IEventBus>());
		builder.Services.AddSingleton<EventBusSubscriptionInfo>();
		return new EventBusBuilder(builder.Services);
	
	}

	private class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
	{
		public IServiceCollection Services => services;
	}
	public static IEventBusBuilder AddEventDbContext<TDbContext>(
	this IEventBusBuilder builder,
	string? connectionString)
	where TDbContext : DbContext , IEventStoreDbContext
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.Services.AddHostedService<OutboxWorker<EventBusDbContext>>();
		builder.Services.AddScoped<IMessageDeduplicationService, MessageDeduplicationService>();
		builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
		builder.Services.AddDbContext<EventBusDbContext>((serviceProvider, options) =>
		{
			var context = serviceProvider.GetRequiredService<TDbContext>();
			options.UseSqlServer(context.Database.GetDbConnection());
		});	

		builder.Services.AddDbContextFactory<EventBusDbContext>((provider, options) =>
		{
			//var configuration = provider.GetRequiredService<IConfiguration>();
			//var connectionString = configuration.GetConnectionString(connectionString);

			options.UseSqlServer(connectionString);
		}, lifetime: ServiceLifetime.Scoped);

		builder.Services.AddScoped<ITransactionalOutbox, TransactionalOutbox<TDbContext>>();

		return new EventBusBuilder(builder.Services);
	}

	// when eventstore dbset are not part of dbcontext
	public static IEventBusBuilder AddSeeder<TDbContext>(
	this IEventBusBuilder builder,
	string connectionName)
	where TDbContext : DbContext, IEventStoreDbContext
	{
		builder.Services.AddHostedService(provider =>
		{
			var logger = provider.GetRequiredService<ILogger<DatabaseSeeder>>();
			return new DatabaseSeeder(provider, logger);
		});

		return new EventBusBuilder(builder.Services);
	}

	public static IEventBusBuilder AddSubscription<TEvent, THandler>(
			this IEventBusBuilder builder)
			where TEvent : IntegrationEvent
			where THandler : class, IIntegrationEventHandler<TEvent>
		{
			builder.Services.AddKeyedTransient<IIntegrationEventHandler, THandler>(typeof(TEvent));
			builder.Services.Configure<EventBusSubscriptionInfo>(o =>
			{
				o.EventTypes[typeof(TEvent).Name] = typeof(TEvent);
			});
			return builder;
		}
		public static IEventBusBuilder AddSubscription<TEvent, THandler>(
			this IEventBusBuilder builder,string queueName)
			where TEvent : IntegrationEvent
			where THandler : class, IIntegrationEventHandler<TEvent>
		{
			builder.Services.AddKeyedTransient<IIntegrationEventHandler, THandler>(typeof(TEvent));
			builder.Services.Configure<EventBusSubscriptionInfo>(o =>
			{
				o.EventTypes[queueName] = typeof(TEvent);
			});
			return builder;
		}
	}

public interface IEventBusBuilder
	{
		IServiceCollection Services { get; }
}

