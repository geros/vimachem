namespace Audit.API.Application;

public record EventFilter {
	public string? EntityType { get; init; }
	public string? Action { get; init; }
	public string? EntityId { get; init; }
	public DateTime? From { get; init; }
	public DateTime? To { get; init; }
}
