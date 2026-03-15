using System.Text.Json;
using Party.API.Application.Interfaces;
using RabbitMQ.Client;
using Shared.Events;

namespace Party.API.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable {
	private readonly IConnection _connection;
	private readonly IChannel _channel;
	private readonly ILogger<RabbitMqEventPublisher> _logger;
	private const string ExchangeName = "library.events";
	private bool _disposed;

	public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger) {
		_logger = logger;

		var host = configuration["RabbitMQ:Host"] ?? "localhost";
		var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
		var username = configuration["RabbitMQ:Username"] ?? "guest";
		var password = configuration["RabbitMQ:Password"] ?? "guest";

		var factory = new ConnectionFactory {
			HostName = host,
			Port = port,
			UserName = username,
			Password = password
		};

		_logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", host, port);
		_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
		_channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

		_channel.ExchangeDeclareAsync(
			exchange: ExchangeName,
			type: ExchangeType.Topic,
			durable: true
		).GetAwaiter().GetResult();
	}

	public async Task PublishAsync(IntegrationEvent @event, CancellationToken ct) {
		var routingKey = $"{@event.EntityType.ToLowerInvariant()}.{@event.Action.ToLowerInvariant()}";

		var body = JsonSerializer.SerializeToUtf8Bytes(@event);

		var props = new BasicProperties {
			ContentType = "application/json",
			DeliveryMode = DeliveryModes.Persistent
		};

		await _channel.BasicPublishAsync(
			exchange: ExchangeName,
			routingKey: routingKey,
			mandatory: false,
			basicProperties: props,
			body: body,
			cancellationToken: ct
		);

		_logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", @event.EventType, routingKey);
	}

	public void Dispose() {
		if (_disposed) return;
		_channel?.CloseAsync().GetAwaiter().GetResult();
		_connection?.CloseAsync().GetAwaiter().GetResult();
		_disposed = true;
	}
}
