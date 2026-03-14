using Shared.Events;

namespace Lending.API.Infrastructure.Events;

public interface IEventPublisher {
	Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct);
}
