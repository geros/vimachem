using Catalog.API.Application.DTOs;
using Catalog.API.Application.Interfaces;
using Catalog.API.Domain.Entities;
using Catalog.API.Domain.Exceptions;
using Catalog.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace Catalog.API.Application.Services;

public sealed class BookService : IBookService {
	private readonly CatalogDbContext _context;
	private readonly IEventPublisher _publisher;
	private readonly IPartyServiceClient _partyClient;

	public BookService(
		CatalogDbContext context,
		IEventPublisher publisher,
		IPartyServiceClient partyClient) {
		_context = context;
		_publisher = publisher;
		_partyClient = partyClient;
	}

	public async Task<IEnumerable<BookResponse>> GetAllAsync(CancellationToken ct) {
		var books = await _context.Books
			.Include(b => b.Category)
			.ToListAsync(ct);

		return books.Select(b => MapToResponse(b, b.Category?.Name ?? ""));
	}

	public async Task<BookResponse> GetByIdAsync(Guid id, CancellationToken ct) {
		var book = await _context.Books
			.Include(b => b.Category)
			.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		return MapToResponse(book, book.Category?.Name ?? "");
	}

	public async Task<IEnumerable<BookResponse>> SearchByTitleAsync(string title, CancellationToken ct) {
		var searchTerm = title.ToLowerInvariant();
		var books = await _context.Books
			.Include(b => b.Category)
			.Where(b => b.Title.ToLower().Contains(searchTerm))
			.ToListAsync(ct);

		return books.Select(b => MapToResponse(b, b.Category?.Name ?? ""));
	}

	public async Task<BookAvailabilityResponse> GetAvailabilityAsync(Guid id, CancellationToken ct) {
		var book = await _context.Books
			.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		return new BookAvailabilityResponse {
			BookId = book.Id,
			Title = book.Title,
			TotalCopies = book.TotalCopies,
			AvailableCopies = book.AvailableCopies,
			IsAvailable = book.AvailableCopies > 0
		};
	}

	public async Task<BookResponse> CreateAsync(CreateBookRequest request, CancellationToken ct) {
		// 1. Validate author via Party.API
		var author = await _partyClient.GetByIdAsync(request.AuthorId, ct)
			?? throw new DomainException($"Author with ID {request.AuthorId} not found");

		if (!author.Roles.Contains("Author"))
			throw new DomainException("Party does not have Author role");

		// 2. Validate category exists locally
		var category = await _context.Categories
			.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
			?? throw new NotFoundException("Category", request.CategoryId);

		// 3. Create book with denormalized author name
		var authorName = $"{author.FirstName} {author.LastName}";
		var book = new Book(request.Title, request.ISBN, request.AuthorId,
			authorName, request.CategoryId, request.TotalCopies);

		_context.Books.Add(book);
		await _context.SaveChangesAsync(ct);

		// 4. Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "BookCreated",
			EntityType = "Book",
			EntityId = book.Id.ToString(),
			Action = "Created",
			RelatedEntityIds = new Dictionary<string, string> {
				["AuthorId"] = book.AuthorId.ToString(),
				["CategoryId"] = book.CategoryId.ToString()
			},
			Payload = new {
				book.Id, book.Title, book.ISBN,
				book.AuthorId, book.AuthorName,
				book.CategoryId, CategoryName = category.Name,
				book.TotalCopies, book.AvailableCopies
			}
		}, ct);

		return MapToResponse(book, category.Name);
	}

	public async Task<BookResponse> UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken ct) {
		var book = await _context.Books
			.Include(b => b.Category)
			.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		var category = await _context.Categories
			.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
			?? throw new NotFoundException("Category", request.CategoryId);

		book.Update(request.Title, request.CategoryId, request.TotalCopies);
		await _context.SaveChangesAsync(ct);

		// Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "BookUpdated",
			EntityType = "Book",
			EntityId = book.Id.ToString(),
			Action = "Updated",
			RelatedEntityIds = new Dictionary<string, string> {
				["AuthorId"] = book.AuthorId.ToString(),
				["CategoryId"] = book.CategoryId.ToString()
			},
			Payload = new {
				book.Id, book.Title, book.ISBN,
				book.AuthorId, book.AuthorName,
				book.CategoryId, CategoryName = category.Name,
				book.TotalCopies, book.AvailableCopies
			}
		}, ct);

		return MapToResponse(book, category.Name);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct) {
		var book = await _context.Books
			.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		_context.Books.Remove(book);
		await _context.SaveChangesAsync(ct);

		// Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "BookDeleted",
			EntityType = "Book",
			EntityId = book.Id.ToString(),
			Action = "Deleted",
			RelatedEntityIds = new Dictionary<string, string> {
				["AuthorId"] = book.AuthorId.ToString(),
				["CategoryId"] = book.CategoryId.ToString()
			},
			Payload = new { book.Id, book.Title }
		}, ct);
	}

	public async Task<BookAvailabilityResponse> ReserveAsync(Guid id, CancellationToken ct) {
		var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		book.Reserve(); // domain logic — throws if unavailable
		await _context.SaveChangesAsync(ct);

		return new BookAvailabilityResponse {
			BookId = book.Id,
			Title = book.Title,
			TotalCopies = book.TotalCopies,
			AvailableCopies = book.AvailableCopies,
			IsAvailable = book.AvailableCopies > 0
		};
	}

	public async Task<BookAvailabilityResponse> ReleaseAsync(Guid id, CancellationToken ct) {
		var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, ct)
			?? throw new NotFoundException("Book", id);

		book.Release(); // domain logic — throws if all already available
		await _context.SaveChangesAsync(ct);

		return new BookAvailabilityResponse {
			BookId = book.Id,
			Title = book.Title,
			TotalCopies = book.TotalCopies,
			AvailableCopies = book.AvailableCopies,
			IsAvailable = book.AvailableCopies > 0
		};
	}

	private static BookResponse MapToResponse(Book book, string categoryName) => new() {
		Id = book.Id,
		Title = book.Title,
		ISBN = book.ISBN,
		AuthorId = book.AuthorId,
		AuthorName = book.AuthorName,
		CategoryId = book.CategoryId,
		CategoryName = categoryName,
		TotalCopies = book.TotalCopies,
		AvailableCopies = book.AvailableCopies,
		CreatedAt = book.CreatedAt,
		UpdatedAt = book.UpdatedAt
	};
}
