using MongoDB.Bson;
using MongoDB.Driver;
using Audit.API.Application;
using Audit.API.Domain;

namespace Audit.API.Infrastructure;

public sealed class EventRepository : IEventRepository {
	private readonly IMongoCollection<DomainEvent> _events;

	public EventRepository(IMongoDatabase database) {
		_events = database.GetCollection<DomainEvent>("events");

		// TTL index: auto-delete after 365 days
		var ttlIndex = Builders<DomainEvent>.IndexKeys.Ascending(e => e.Timestamp);
		_events.Indexes.CreateOne(new CreateIndexModel<DomainEvent>(
			ttlIndex,
			new CreateIndexOptions {
				ExpireAfter = TimeSpan.FromDays(365),
				Name = "event_ttl_1year"
			}
		));

		// Query performance indexes
		_events.Indexes.CreateOne(new CreateIndexModel<DomainEvent>(
			Builders<DomainEvent>.IndexKeys.Ascending(e => e.EntityId)));
		_events.Indexes.CreateOne(new CreateIndexModel<DomainEvent>(
			Builders<DomainEvent>.IndexKeys.Ascending(e => e.EntityType)));
		_events.Indexes.CreateOne(new CreateIndexModel<DomainEvent>(
			Builders<DomainEvent>.IndexKeys.Ascending(e => e.Action)));
		_events.Indexes.CreateOne(new CreateIndexModel<DomainEvent>(
			Builders<DomainEvent>.IndexKeys
				.Ascending(e => e.EntityType)
				.Ascending(e => e.Action)
				.Descending(e => e.Timestamp),
			new CreateIndexOptions { Name = "entity_type_action_ts" }));
	}

	public async Task SaveEventAsync(DomainEvent @event, CancellationToken ct = default) {
		await _events.InsertOneAsync(@event, cancellationToken: ct);
	}

	public async Task<PagedResponse<EventResponse>> GetPartyEventsAsync(
		string partyId, int page, int pageSize, CancellationToken ct) {

		// Match events where:
		// - EntityType is "Party" and EntityId matches, OR
		// - RelatedEntityIds contains "CustomerId" matching the partyId
		var filter = Builders<DomainEvent>.Filter.Or(
			Builders<DomainEvent>.Filter.And(
				Builders<DomainEvent>.Filter.Eq(e => e.EntityType, "Party"),
				Builders<DomainEvent>.Filter.Eq(e => e.EntityId, partyId)
			),
			Builders<DomainEvent>.Filter.Eq("RelatedEntityIds.CustomerId", partyId),
			Builders<DomainEvent>.Filter.Eq("RelatedEntityIds.AuthorId", partyId)
		);

		return await ExecutePagedQueryAsync(filter, page, pageSize, ct);
	}

	public async Task<PagedResponse<EventResponse>> GetBookEventsAsync(
		string bookId, int page, int pageSize, CancellationToken ct) {

		var filter = Builders<DomainEvent>.Filter.Or(
			Builders<DomainEvent>.Filter.And(
				Builders<DomainEvent>.Filter.Eq(e => e.EntityType, "Book"),
				Builders<DomainEvent>.Filter.Eq(e => e.EntityId, bookId)
			),
			Builders<DomainEvent>.Filter.Eq("RelatedEntityIds.BookId", bookId)
		);

		return await ExecutePagedQueryAsync(filter, page, pageSize, ct);
	}

	public async Task<PagedResponse<EventResponse>> GetAllEventsAsync(
		EventFilter filter, int page, int pageSize, CancellationToken ct) {

		var fd = Builders<DomainEvent>.Filter.Empty;

		if (!string.IsNullOrEmpty(filter.EntityType))
			fd &= Builders<DomainEvent>.Filter.Eq(e => e.EntityType, filter.EntityType);
		if (!string.IsNullOrEmpty(filter.Action))
			fd &= Builders<DomainEvent>.Filter.Eq(e => e.Action, filter.Action);
		if (!string.IsNullOrEmpty(filter.EntityId))
			fd &= Builders<DomainEvent>.Filter.Eq(e => e.EntityId, filter.EntityId);
		if (filter.From.HasValue)
			fd &= Builders<DomainEvent>.Filter.Gte(e => e.Timestamp, filter.From.Value);
		if (filter.To.HasValue)
			fd &= Builders<DomainEvent>.Filter.Lte(e => e.Timestamp, filter.To.Value);

		return await ExecutePagedQueryAsync(fd, page, pageSize, ct);
	}

	private async Task<PagedResponse<EventResponse>> ExecutePagedQueryAsync(
		FilterDefinition<DomainEvent> filter, int page, int pageSize,
		CancellationToken ct) {

		var totalCount = await _events.CountDocumentsAsync(filter, cancellationToken: ct);

		var events = await _events.Find(filter)
			.SortByDescending(e => e.Timestamp)
			.Skip((page - 1) * pageSize)
			.Limit(pageSize)
			.ToListAsync(ct);

		return new PagedResponse<EventResponse> {
			Items = events.Select(MapToResponse).ToList(),
			Page = page,
			PageSize = pageSize,
			TotalCount = totalCount,
			TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
		};
	}

	private static EventResponse MapToResponse(DomainEvent e) => new() {
		Id = e.Id,
		EventType = e.EventType,
		EntityType = e.EntityType,
		EntityId = e.EntityId,
		Action = e.Action,
		RelatedEntityIds = e.RelatedEntityIds,
		Payload = BsonTypeMapper.MapToDotNetValue(e.Payload),
		Timestamp = e.Timestamp
	};
}
