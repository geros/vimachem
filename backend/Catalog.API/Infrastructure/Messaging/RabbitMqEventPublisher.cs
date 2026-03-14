using Catalog.API.Application.Interfaces;
using RabbitMQ.Client;
using System.Text.Json;
using Shared.Events;

namespace Catalog.API.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable {
	private readonly IConnection _connection;
	private readonly IChannel _channel;
	private const string ExchangeName = "library.events";

	public RabbitMqEventPublisher(string hostname) {
		var factory = new ConnectionFactory { HostName = hostname };
		_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
		_channel = _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
		_channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
	}

	public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default) {
		var routingKey = $"{integrationEvent.EntityType.ToLowerInvariant()}.{integrationEvent.Action.ToLowerInvariant()}";
		var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

		await _channel.BasicPublishAsync(
			exchange: ExchangeName,
			routingKey: routingKey,
			body: body,
			cancellationToken: cancellationToken);
	}

	public async ValueTask DisposeAsync() {
		if (_channel != null) await _channel.DisposeAsync();
		if (_connection != null) await _connection.DisposeAsync();
	}
}
