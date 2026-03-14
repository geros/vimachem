namespace Catalog.API.Application.DTOs;

public record BookResponse {
	public Guid Id { get; init; }
	public string Title { get; init; } = string.Empty;
	public string ISBN { get; init; } = string.Empty;
	public Guid AuthorId { get; init; }
	public string AuthorName { get; init; } = string.Empty;
	public Guid CategoryId { get; init; }
	public string CategoryName { get; init; } = string.Empty;
	public int TotalCopies { get; init; }
	public int AvailableCopies { get; init; }
	public DateTime CreatedAt { get; init; }
	public DateTime? UpdatedAt { get; init; }
}
