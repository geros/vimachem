using System.Text.Json;
using RabbitMQ.Client;
using Shared.Events;

namespace Lending.API.Infrastructure.Events;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable {
	private readonly IConnection _connection;
	private readonly IModel _channel;
	private const string ExchangeName = "library.events";

	public RabbitMqEventPublisher(IConfiguration config) {
		var factory = new ConnectionFactory {
			HostName = config["RabbitMQ:Host"] ?? "localhost",
			Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
			UserName = config["RabbitMQ:Username"] ?? "guest",
			Password = config["RabbitMQ:Password"] ?? "guest"
		};

		_connection = factory.CreateConnection();
		_channel = _connection.CreateModel();
		_channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
	}

	public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct) {
		var routingKey = $"{integrationEvent.EntityType.ToLowerInvariant()}.{integrationEvent.Action.ToLowerInvariant()}";
		var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent);

		var properties = _channel.CreateBasicProperties();
		properties.ContentType = "application/json";
		properties.DeliveryMode = 2; // persistent

		_channel.BasicPublish(
			exchange: ExchangeName,
			routingKey: routingKey,
			basicProperties: properties,
			body: body);

		return Task.CompletedTask;
	}

	public void Dispose() {
		_channel.Close();
		_connection.Close();
	}
}
