using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Audit.API.Domain;

public class DomainEvent {
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = string.Empty;

	public string EventType { get; set; } = string.Empty;
	public string EntityType { get; set; } = string.Empty;
	public string EntityId { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public Dictionary<string, string> RelatedEntityIds { get; set; } = new();
	public BsonDocument Payload { get; set; } = new();
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
