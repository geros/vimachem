using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Audit.API.Infrastructure;

public sealed class RabbitMqEventConsumer : BackgroundService {
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RabbitMqEventConsumer> _logger;
	private readonly IConfiguration _config;
	private IConnection? _connection;
	private IChannel? _channel;

	private const string ExchangeName = "library.events";
	private const string QueueName = "audit.events";
	private const string DeadLetterExchange = "library.events.dlx";
	private const string DeadLetterQueue = "audit.events.dlq";
	private const int MaxRetries = 3;

	public RabbitMqEventConsumer(
		IServiceProvider serviceProvider,
		ILogger<RabbitMqEventConsumer> logger,
		IConfiguration config) {
		_serviceProvider = serviceProvider;
		_logger = logger;
		_config = config;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		// Wait for RabbitMQ to be ready (retry loop)
		await WaitForRabbitMqAsync(stoppingToken);

		var factory = new ConnectionFactory {
			HostName = _config["RabbitMQ:Host"] ?? "rabbitmq",
			Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
			UserName = _config["RabbitMQ:Username"] ?? "guest",
			Password = _config["RabbitMQ:Password"] ?? "guest"
		};

		_connection = await factory.CreateConnectionAsync(stoppingToken);
		_channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

		// Dead Letter Exchange + Queue
		await _channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
		await _channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
		await _channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "", cancellationToken: stoppingToken);

		// Main queue with DLX
		await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
		await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false,
			arguments: new Dictionary<string, object?> {
				{ "x-dead-letter-exchange", DeadLetterExchange }
			}, cancellationToken: stoppingToken);
		await _channel.QueueBindAsync(QueueName, ExchangeName, "#", cancellationToken: stoppingToken); // subscribe to ALL events

		await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (_, ea) => {
			var retryCount = GetRetryCount(ea.BasicProperties);

			try {
				var json = Encoding.UTF8.GetString(ea.Body.ToArray());
				var @event = JsonSerializer.Deserialize<IntegrationEvent>(json)
					?? throw new InvalidOperationException("Failed to deserialize event");

				using var scope = _serviceProvider.CreateScope();
				var repo = scope.ServiceProvider.GetRequiredService<Application.IEventRepository>();

				await repo.SaveEventAsync(new Domain.DomainEvent {
					EventType = @event.EventType,
					EntityType = @event.EntityType,
					EntityId = @event.EntityId,
					Action = @event.Action,
					RelatedEntityIds = @event.RelatedEntityIds,
					Payload = BsonDocument.Parse(
						JsonSerializer.Serialize(@event.Payload)),
					Timestamp = @event.Timestamp
				});

				await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
				_logger.LogInformation("Processed event {EventType}", @event.EventType);

			} catch (Exception ex) when (retryCount < MaxRetries) {
				_logger.LogWarning(ex,
					"Event processing failed (attempt {Attempt}/{Max}). Requeueing...",
					retryCount + 1, MaxRetries);

				var properties = new BasicProperties {
					Headers = new Dictionary<string, object?> {
						{ "x-retry-count", retryCount + 1 }
					},
					Persistent = true
				};

				await _channel.BasicPublishAsync(ExchangeName, ea.RoutingKey, false, properties, ea.Body);
				await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

			} catch (Exception ex) {
				_logger.LogError(ex,
					"Event processing failed after {Max} retries. Sending to DLQ.", MaxRetries);
				await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
			}
		};

		await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
		await Task.Delay(Timeout.Infinite, stoppingToken);
	}

	private static int GetRetryCount(IReadOnlyBasicProperties properties) {
		if (properties.Headers?.TryGetValue("x-retry-count", out var value) == true && value != null)
			return Convert.ToInt32(value);
		return 0;
	}

	private async Task WaitForRabbitMqAsync(CancellationToken ct) {
		var retries = 0;
		while (retries < 10 && !ct.IsCancellationRequested) {
			try {
				await using var testConnection = await new ConnectionFactory {
					HostName = _config["RabbitMQ:Host"] ?? "rabbitmq"
				}.CreateConnectionAsync(ct);
				_logger.LogInformation("Connected to RabbitMQ");
				return;
			} catch {
				retries++;
				_logger.LogInformation("Waiting for RabbitMQ... (attempt {N})", retries);
				await Task.Delay(3000, ct);
			}
		}
	}

	public override void Dispose() {
		_channel?.Dispose();
		_connection?.Dispose();
		base.Dispose();
	}
}

public class IntegrationEvent {
	public string EventType { get; set; } = string.Empty;
	public string EntityType { get; set; } = string.Empty;
	public string EntityId { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public Dictionary<string, string> RelatedEntityIds { get; set; } = new();
	public object? Payload { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
