using Lending.API.Application.DTOs;
using Lending.API.Application.Interfaces;
using Lending.API.Domain;
using Lending.API.HttpClients;
using Lending.API.Infrastructure.Data;
using Lending.API.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace Lending.API.Application;

public sealed class BorrowingService : IBorrowingService {
	private readonly LendingDbContext _context;
	private readonly IEventPublisher _publisher;
	private readonly IPartyServiceClient _partyClient;
	private readonly ICatalogServiceClient _catalogClient;
	private readonly ILogger<BorrowingService> _logger;

	public BorrowingService(
		LendingDbContext context,
		IEventPublisher publisher,
		IPartyServiceClient partyClient,
		ICatalogServiceClient catalogClient,
		ILogger<BorrowingService> logger) {
		_context = context;
		_publisher = publisher;
		_partyClient = partyClient;
		_catalogClient = catalogClient;
		_logger = logger;
	}

	public async Task<BorrowingResponse> BorrowAsync(BorrowBookRequest request, CancellationToken ct) {
		// 1. Validate customer via Party.API
		var customer = await _partyClient.GetByIdAsync(request.CustomerId, ct)
			?? throw new DomainException($"Customer with ID {request.CustomerId} not found");

		if (!customer.Roles.Contains("Customer"))
			throw new DomainException("Party does not have Customer role");

		// 2. Check availability via Catalog.API
		var availability = await _catalogClient.GetBookAvailabilityAsync(request.BookId, ct)
			?? throw new DomainException($"Book with ID {request.BookId} not found");

		if (!availability.IsAvailable)
			throw new DomainException($"No copies available for book '{availability.Title}'");

		// 3. Check for duplicate active borrowing (local check)
		var existingBorrowing = await _context.Borrowings
			.FirstOrDefaultAsync(b =>
				b.BookId == request.BookId &&
				b.CustomerId == request.CustomerId &&
				b.ReturnedAt == null, ct);

		if (existingBorrowing != null)
			throw new DomainException("Customer already has an active borrowing for this book");

		// 4. Reserve the book in Catalog.API
		var reserved = await _catalogClient.ReserveBookAsync(request.BookId, ct);
		if (!reserved)
			throw new DomainException("Failed to reserve book — may no longer be available");

		try {
			// 5. Create borrowing record (with denormalized data)
			var customerName = $"{customer.FirstName} {customer.LastName}";
			var borrowing = new Borrowing(
				request.BookId, availability.Title,
				request.CustomerId, customerName);

			_context.Borrowings.Add(borrowing);
			await _context.SaveChangesAsync(ct);

			// 6. Publish event
			await _publisher.PublishAsync(new IntegrationEvent {
				EventType = "BookBorrowed",
				EntityType = "Borrowing",
				EntityId = borrowing.Id.ToString(),
				Action = "Borrowed",
				RelatedEntityIds = new Dictionary<string, string> {
					["BookId"] = request.BookId.ToString(),
					["CustomerId"] = request.CustomerId.ToString()
				},
				Payload = new {
					borrowing.Id,
					borrowing.BookId,
					borrowing.BookTitle,
					borrowing.CustomerId,
					borrowing.CustomerName,
					borrowing.BorrowedAt
				}
			}, ct);

			return MapToResponse(borrowing);
		} catch (Exception ex) {
			// COMPENSATION: if save fails, release the reservation
			_logger.LogError(ex, "Borrow failed after reserve. Releasing book {BookId}", request.BookId);
			await _catalogClient.ReleaseBookAsync(request.BookId, ct);
			throw;
		}
	}

	public async Task<BorrowingResponse> ReturnAsync(Guid bookId, Guid customerId, CancellationToken ct) {
		// 1. Find active borrowing (local)
		var borrowing = await _context.Borrowings
			.FirstOrDefaultAsync(b =>
				b.BookId == bookId &&
				b.CustomerId == customerId &&
				b.ReturnedAt == null, ct)
			?? throw new DomainException("No active borrowing found for this book and customer");

		// 2. Mark returned (domain logic)
		borrowing.MarkReturned();
		await _context.SaveChangesAsync(ct);

		// 3. Release the book in Catalog.API
		try {
			await _catalogClient.ReleaseBookAsync(bookId, ct);
		} catch (Exception ex) {
			// Log but don't fail the return — book is returned, availability
			// will self-heal or can be manually corrected
			_logger.LogWarning(ex, "Failed to release book {BookId} in Catalog. Availability may be stale.", bookId);
		}

		// 4. Publish event
		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "BookReturned",
			EntityType = "Borrowing",
			EntityId = borrowing.Id.ToString(),
			Action = "Returned",
			RelatedEntityIds = new Dictionary<string, string> {
				["BookId"] = bookId.ToString(),
				["CustomerId"] = customerId.ToString()
			},
			Payload = new {
				borrowing.Id,
				borrowing.BookId,
				borrowing.BookTitle,
				borrowing.CustomerId,
				borrowing.CustomerName,
				borrowing.BorrowedAt,
				borrowing.ReturnedAt
			}
		}, ct);

		return MapToResponse(borrowing);
	}

	public async Task<BorrowingResponse> GetByIdAsync(Guid id, CancellationToken ct) {
		var borrowing = await _context.Borrowings.FindAsync(new object[] { id }, ct)
			?? throw new NotFoundException("Borrowing", id);
		return MapToResponse(borrowing);
	}

	public async Task<IEnumerable<BorrowingResponse>> GetByCustomerAsync(Guid customerId, CancellationToken ct) {
		return await _context.Borrowings
			.Where(b => b.CustomerId == customerId)
			.OrderByDescending(b => b.BorrowedAt)
			.Select(b => MapToResponse(b))
			.ToListAsync(ct);
	}

	public async Task<IEnumerable<BorrowingResponse>> GetByBookAsync(Guid bookId, CancellationToken ct) {
		return await _context.Borrowings
			.Where(b => b.BookId == bookId)
			.OrderByDescending(b => b.BorrowedAt)
			.Select(b => MapToResponse(b))
			.ToListAsync(ct);
	}

	public async Task<IEnumerable<BorrowedBookSummaryResponse>> GetBorrowedSummaryAsync(CancellationToken ct) {
		// Pure local query — no cross-service calls thanks to denormalization
		return await _context.Borrowings
			.Where(b => b.ReturnedAt == null)
			.GroupBy(b => new { b.BookId, b.BookTitle })
			.Select(g => new BorrowedBookSummaryResponse {
				BookId = g.Key.BookId,
				BookTitle = g.Key.BookTitle,
				Borrowers = g.Select(b => new BorrowerInfo {
					CustomerId = b.CustomerId,
					CustomerName = b.CustomerName,
					BorrowedAt = b.BorrowedAt
				}).ToList()
			})
			.ToListAsync(ct);
	}

	private static BorrowingResponse MapToResponse(Borrowing b) => new() {
		Id = b.Id,
		BookId = b.BookId,
		BookTitle = b.BookTitle,
		CustomerId = b.CustomerId,
		CustomerName = b.CustomerName,
		BorrowedAt = b.BorrowedAt,
		ReturnedAt = b.ReturnedAt,
		IsActive = b.IsActive
	};
}
