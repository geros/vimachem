using System.Text.Json;
using RabbitMQ.Client;
using Shared.Events;

namespace Lending.API.Infrastructure.Events;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable {
	private readonly IConnection _connection;
	private readonly IChannel _channel;
	private const string ExchangeName = "library.events";

	public RabbitMqEventPublisher(IConfiguration config) {
		var factory = new ConnectionFactory {
			HostName = config["RabbitMQ:Host"] ?? "localhost",
			Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
			UserName = config["RabbitMQ:Username"] ?? "guest",
			Password = config["RabbitMQ:Password"] ?? "guest"
		};

		_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
		_channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
		_channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
	}

	public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct) {
		var routingKey = $"{integrationEvent.EntityType.ToLowerInvariant()}.{integrationEvent.Action.ToLowerInvariant()}";
		var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

		await _channel.BasicPublishAsync(
			exchange: ExchangeName,
			routingKey: routingKey,
			body: body,
			cancellationToken: ct);
	}

	public async ValueTask DisposeAsync() {
		if (_channel != null) await _channel.DisposeAsync();
		if (_connection != null) await _connection.DisposeAsync();
	}
}
