namespace Lending.API.Application.DTOs;

public record BorrowerInfo {
	public Guid CustomerId { get; init; }
	public string CustomerName { get; init; } = string.Empty;
	public DateTime BorrowedAt { get; init; }
}
