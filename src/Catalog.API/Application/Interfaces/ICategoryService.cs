using Catalog.API.Application.DTOs;

namespace Catalog.API.Application.Interfaces;

public interface ICategoryService {
	Task<IEnumerable<CategoryResponse>> GetAllAsync(CancellationToken ct);
	Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct);
	Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
	Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct);
	Task DeleteAsync(Guid id, CancellationToken ct);
}
