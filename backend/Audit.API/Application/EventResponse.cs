namespace Audit.API.Application;

public record EventResponse {
	public string Id { get; init; } = string.Empty;
	public string EventType { get; init; } = string.Empty;
	public string EntityType { get; init; } = string.Empty;
	public string EntityId { get; init; } = string.Empty;
	public string Action { get; init; } = string.Empty;
	public Dictionary<string, string> RelatedEntityIds { get; init; } = new();
	public object? Payload { get; init; }
	public DateTime Timestamp { get; init; }
}
