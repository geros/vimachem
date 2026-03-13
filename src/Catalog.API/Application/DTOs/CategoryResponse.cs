namespace Catalog.API.Application.DTOs;

public record CategoryResponse {
	public Guid Id { get; init; }
	public string Name { get; init; } = string.Empty;
	public int BookCount { get; init; }
	public DateTime CreatedAt { get; init; }
}
