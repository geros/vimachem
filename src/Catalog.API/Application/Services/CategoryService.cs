using Catalog.API.Application.DTOs;
using Catalog.API.Application.Interfaces;
using Catalog.API.Domain.Entities;
using Catalog.API.Domain.Exceptions;
using Catalog.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace Catalog.API.Application.Services;

public sealed class CategoryService : ICategoryService {
	private readonly CatalogDbContext _context;
	private readonly IEventPublisher _publisher;

	public CategoryService(CatalogDbContext context, IEventPublisher publisher) {
		_context = context;
		_publisher = publisher;
	}

	public async Task<IEnumerable<CategoryResponse>> GetAllAsync(CancellationToken ct) {
		var categories = await _context.Categories
			.Include(c => c.Books)
			.ToListAsync(ct);

		return categories.Select(MapToResponse);
	}

	public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct) {
		var category = await _context.Categories
			.Include(c => c.Books)
			.FirstOrDefaultAsync(c => c.Id == id, ct)
			?? throw new NotFoundException("Category", id);

		return MapToResponse(category);
	}

	public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct) {
		var category = new Category(request.Name);

		_context.Categories.Add(category);
		await _context.SaveChangesAsync(ct);

		// Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "CategoryCreated",
			EntityType = "Category",
			EntityId = category.Id.ToString(),
			Action = "Created",
			Payload = new { category.Id, category.Name }
		}, ct);

		return MapToResponse(category);
	}

	public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct) {
		var category = await _context.Categories
			.Include(c => c.Books)
			.FirstOrDefaultAsync(c => c.Id == id, ct)
			?? throw new NotFoundException("Category", id);

		category.Update(request.Name);
		await _context.SaveChangesAsync(ct);

		// Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "CategoryUpdated",
			EntityType = "Category",
			EntityId = category.Id.ToString(),
			Action = "Updated",
			Payload = new { category.Id, category.Name }
		}, ct);

		return MapToResponse(category);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct) {
		var category = await _context.Categories
			.FirstOrDefaultAsync(c => c.Id == id, ct)
			?? throw new NotFoundException("Category", id);

		_context.Categories.Remove(category);
		await _context.SaveChangesAsync(ct);

		// Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "CategoryDeleted",
			EntityType = "Category",
			EntityId = category.Id.ToString(),
			Action = "Deleted",
			Payload = new { category.Id, category.Name }
		}, ct);
	}

	private static CategoryResponse MapToResponse(Category category) => new() {
		Id = category.Id,
		Name = category.Name,
		BookCount = category.Books?.Count ?? 0,
		CreatedAt = category.CreatedAt
	};
}
