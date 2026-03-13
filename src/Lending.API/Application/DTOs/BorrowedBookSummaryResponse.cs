namespace Lending.API.Application.DTOs;

public record BorrowedBookSummaryResponse {
	public Guid BookId { get; init; }
	public string BookTitle { get; init; } = string.Empty;
	public List<BorrowerInfo> Borrowers { get; init; } = new();
}
