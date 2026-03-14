using Shared.Events;

namespace Catalog.API.Application.Interfaces;

public interface IEventPublisher {
	Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
