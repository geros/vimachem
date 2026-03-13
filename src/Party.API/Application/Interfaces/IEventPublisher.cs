using Shared.Events;

namespace Party.API.Application.Interfaces;

public interface IEventPublisher {
	Task PublishAsync(IntegrationEvent @event, CancellationToken ct);
}
