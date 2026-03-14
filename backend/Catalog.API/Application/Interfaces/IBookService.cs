using Catalog.API.Application.DTOs;

namespace Catalog.API.Application.Interfaces;

public interface IBookService {
	Task<IEnumerable<BookResponse>> GetAllAsync(CancellationToken ct);
	Task<BookResponse> GetByIdAsync(Guid id, CancellationToken ct);
	Task<IEnumerable<BookResponse>> SearchByTitleAsync(string title, CancellationToken ct);
	Task<BookAvailabilityResponse> GetAvailabilityAsync(Guid id, CancellationToken ct);
	Task<BookResponse> CreateAsync(CreateBookRequest request, CancellationToken ct);
	Task<BookResponse> UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken ct);
	Task DeleteAsync(Guid id, CancellationToken ct);
	Task<BookAvailabilityResponse> ReserveAsync(Guid id, CancellationToken ct);
	Task<BookAvailabilityResponse> ReleaseAsync(Guid id, CancellationToken ct);
}
