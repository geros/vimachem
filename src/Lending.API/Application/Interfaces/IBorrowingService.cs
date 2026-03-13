using Lending.API.Application.DTOs;

namespace Lending.API.Application.Interfaces;

public interface IBorrowingService {
	Task<BorrowingResponse> BorrowAsync(BorrowBookRequest request, CancellationToken ct);
	Task<BorrowingResponse> ReturnAsync(Guid bookId, Guid customerId, CancellationToken ct);
	Task<BorrowingResponse> GetByIdAsync(Guid id, CancellationToken ct);
	Task<IEnumerable<BorrowingResponse>> GetByCustomerAsync(Guid customerId, CancellationToken ct);
	Task<IEnumerable<BorrowingResponse>> GetByBookAsync(Guid bookId, CancellationToken ct);
	Task<IEnumerable<BorrowedBookSummaryResponse>> GetBorrowedSummaryAsync(CancellationToken ct);
}
