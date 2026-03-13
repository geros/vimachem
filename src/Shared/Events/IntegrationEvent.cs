namespace Shared.Events;

public class IntegrationEvent {
	public string EventType { get; set; } = string.Empty;
	public string EntityType { get; set; } = string.Empty;
	public string EntityId { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public Dictionary<string, string> RelatedEntityIds { get; set; } = new();
	public object? Payload { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
