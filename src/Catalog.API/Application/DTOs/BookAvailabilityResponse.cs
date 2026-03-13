namespace Catalog.API.Application.DTOs;

public record BookAvailabilityResponse {
	public Guid BookId { get; init; }
	public string Title { get; init; } = string.Empty;
	public int TotalCopies { get; init; }
	public int AvailableCopies { get; init; }
	public bool IsAvailable { get; init; }
}
