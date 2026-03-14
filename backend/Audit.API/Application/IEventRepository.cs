namespace Audit.API.Application;

public interface IEventRepository {
	Task SaveEventAsync(Domain.DomainEvent @event, CancellationToken ct = default);
	Task<PagedResponse<EventResponse>> GetPartyEventsAsync(string partyId, int page, int pageSize, CancellationToken ct);
	Task<PagedResponse<EventResponse>> GetBookEventsAsync(string bookId, int page, int pageSize, CancellationToken ct);
}
