namespace Lending.API.Application.DTOs;

public record BorrowingResponse {
	public Guid Id { get; init; }
	public Guid BookId { get; init; }
	public string BookTitle { get; init; } = string.Empty;
	public Guid CustomerId { get; init; }
	public string CustomerName { get; init; } = string.Empty;
	public DateTime BorrowedAt { get; init; }
	public DateTime? ReturnedAt { get; init; }
	public bool IsActive { get; init; }
}
