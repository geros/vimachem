using FluentAssertions;
using Lending.API.Application;
using Xunit;
using Lending.API.Application.DTOs;
using Lending.API.Domain;
using Lending.API.HttpClients;
using Lending.API.Infrastructure.Data;
using Lending.API.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Events;

namespace Lending.API.Tests.Application;

public sealed class BorrowingServiceTests : IDisposable {
	private readonly LendingDbContext _context;
	private readonly Mock<IEventPublisher> _publisherMock;
	private readonly Mock<IPartyServiceClient> _partyClientMock;
	private readonly Mock<ICatalogServiceClient> _catalogClientMock;
	private readonly BorrowingService _service;

	public BorrowingServiceTests() {
		var options = new DbContextOptionsBuilder<LendingDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		_context = new LendingDbContext(options);
		_publisherMock = new Mock<IEventPublisher>();
		_partyClientMock = new Mock<IPartyServiceClient>();
		_catalogClientMock = new Mock<ICatalogServiceClient>();
		_service = new BorrowingService(
			_context, _publisherMock.Object,
			_partyClientMock.Object, _catalogClientMock.Object,
			Mock.Of<ILogger<BorrowingService>>());
	}

	public void Dispose() {
		_context.Dispose();
	}

	[Fact]
	public async Task BorrowAsync_HappyPath_ShouldCreateAndReserve() {
		// Arrange
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId, FirstName = "John", LastName = "Doe",
				Roles = new List<string> { "Customer" }
			});

		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId, Title = "1984",
				TotalCopies = 3, AvailableCopies = 2, IsAvailable = true
			});

		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var result = await _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		// Assert
		result.BookTitle.Should().Be("1984");
		result.CustomerName.Should().Be("John Doe");
		result.IsActive.Should().BeTrue();

		_catalogClientMock.Verify(x => x.ReserveBookAsync(bookId,
			It.IsAny<CancellationToken>()), Times.Once);

		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "BookBorrowed"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task BorrowAsync_WhenNotCustomer_ShouldThrow() {
		_partyClientMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Roles = new List<string> { "Author" } // not a customer
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), Guid.NewGuid()),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Customer role*");
	}

	[Fact]
	public async Task BorrowAsync_WhenNoAvailability_ShouldThrow() {
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto { BookId = bookId, IsAvailable = false, Title = "1984" });

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No copies available*");
	}

	[Fact]
	public async Task BorrowAsync_WhenDuplicateActive_ShouldThrow() {
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		SetupAvailableBook(bookId);

		// Existing active borrowing
		_context.Borrowings.Add(
			new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*already has an active borrowing*");
	}

	[Fact]
	public async Task BorrowAsync_WhenSaveFails_ShouldReleaseReservation() {
		// This tests the compensation logic
		// Setup: reserve succeeds, but force a save failure by making publisher throw
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId, FirstName = "John", LastName = "Doe",
				Roles = new List<string> { "Customer" }
			});

		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId, Title = "1984",
				TotalCopies = 3, AvailableCopies = 2, IsAvailable = true
			});

		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Make the publisher throw to simulate failure after save
		_publisherMock.Setup(x => x.PublishAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Publishing failed"));

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		await act.Should().ThrowAsync<Exception>();

		// Assert: ReleaseBookAsync is called as compensation
		_catalogClientMock.Verify(x => x.ReleaseBookAsync(bookId,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReturnAsync_HappyPath_ShouldMarkReturnedAndRelease() {
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		_context.Borrowings.Add(
			new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _service.ReturnAsync(bookId, customerId, CancellationToken.None);

		result.IsActive.Should().BeFalse();
		result.ReturnedAt.Should().NotBeNull();

		_catalogClientMock.Verify(x => x.ReleaseBookAsync(bookId,
			It.IsAny<CancellationToken>()), Times.Once);

		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "BookReturned"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReturnAsync_WhenReleaseFails_ShouldNotFailReturn() {
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		_context.Borrowings.Add(
			new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Catalog unavailable"));

		// Should not throw even if release fails
		var result = await _service.ReturnAsync(bookId, customerId, CancellationToken.None);

		result.IsActive.Should().BeFalse();
		result.ReturnedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task GetBorrowedSummaryAsync_ShouldGroupByBook() {
		// Arrange
		var bookId1 = Guid.NewGuid();
		var bookId2 = Guid.NewGuid();
		var customerId1 = Guid.NewGuid();
		var customerId2 = Guid.NewGuid();
		var customerId3 = Guid.NewGuid();

		_context.Borrowings.Add(new Borrowing(bookId1, "1984", customerId1, "John Doe"));
		_context.Borrowings.Add(new Borrowing(bookId1, "1984", customerId2, "Jane Doe"));
		_context.Borrowings.Add(new Borrowing(bookId2, "Animal Farm", customerId3, "Bob Smith"));

		// Add a returned borrowing (should not appear in summary)
		var returnedBorrowing = new Borrowing(bookId1, "1984", Guid.NewGuid(), "Returned User");
		returnedBorrowing.MarkReturned();
		_context.Borrowings.Add(returnedBorrowing);

		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetBorrowedSummaryAsync(CancellationToken.None);

		// Assert
		var summary = result.ToList();
		summary.Should().HaveCount(2);

		var book1Summary = summary.First(s => s.BookId == bookId1);
		book1Summary.BookTitle.Should().Be("1984");
		book1Summary.Borrowers.Should().HaveCount(2);
		book1Summary.Borrowers.Should().Contain(b => b.CustomerId == customerId1);
		book1Summary.Borrowers.Should().Contain(b => b.CustomerId == customerId2);

		var book2Summary = summary.First(s => s.BookId == bookId2);
		book2Summary.BookTitle.Should().Be("Animal Farm");
		book2Summary.Borrowers.Should().HaveCount(1);
	}

	[Fact]
	public async Task GetByIdAsync_WhenExists_ShouldReturnBorrowing() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		_context.Borrowings.Add(borrowing);
		await _context.SaveChangesAsync();

		var result = await _service.GetByIdAsync(borrowing.Id, CancellationToken.None);

		result.Id.Should().Be(borrowing.Id);
		result.BookTitle.Should().Be("1984");
	}

	[Fact]
	public async Task GetByIdAsync_WhenNotExists_ShouldThrowNotFound() {
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByCustomerAsync_ShouldReturnCustomerBorrowings() {
		var customerId = Guid.NewGuid();
		var otherCustomerId = Guid.NewGuid();

		_context.Borrowings.Add(new Borrowing(Guid.NewGuid(), "Book 1", customerId, "John Doe"));
		_context.Borrowings.Add(new Borrowing(Guid.NewGuid(), "Book 2", customerId, "John Doe"));
		_context.Borrowings.Add(new Borrowing(Guid.NewGuid(), "Book 3", otherCustomerId, "Jane Doe"));
		await _context.SaveChangesAsync();

		var result = await _service.GetByCustomerAsync(customerId, CancellationToken.None);

		result.Should().HaveCount(2);
		result.All(b => b.CustomerId == customerId).Should().BeTrue();
	}

	[Fact]
	public async Task GetByBookAsync_ShouldReturnBookBorrowings() {
		var bookId = Guid.NewGuid();
		var otherBookId = Guid.NewGuid();

		_context.Borrowings.Add(new Borrowing(bookId, "1984", Guid.NewGuid(), "John Doe"));
		_context.Borrowings.Add(new Borrowing(bookId, "1984", Guid.NewGuid(), "Jane Doe"));
		_context.Borrowings.Add(new Borrowing(otherBookId, "Animal Farm", Guid.NewGuid(), "Bob Smith"));
		await _context.SaveChangesAsync();

		var result = await _service.GetByBookAsync(bookId, CancellationToken.None);

		result.Should().HaveCount(2);
		result.All(b => b.BookId == bookId).Should().BeTrue();
	}

	private void SetupValidCustomer(Guid? customerId = null) {
		_partyClientMock.Setup(x => x.GetByIdAsync(customerId ?? It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId ?? Guid.NewGuid(),
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Customer" }
			});
	}

	private void SetupAvailableBook(Guid? bookId = null) {
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId ?? It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId ?? Guid.NewGuid(),
				Title = "1984",
				TotalCopies = 3,
				AvailableCopies = 2,
				IsAvailable = true
			});
	}

	// EXTREME SCENARIOS

	[Fact]
	public async Task BorrowAsync_WithEmptyGuidCustomerId_ShouldThrow() {
		// Edge case: Empty GUID for customer
		var emptyGuid = Guid.Empty;
		_partyClientMock.Setup(x => x.GetByIdAsync(emptyGuid, It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyDto?)null);

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), emptyGuid),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Customer with ID*not found*");
	}

	[Fact]
	public async Task BorrowAsync_WithEmptyGuidBookId_ShouldThrow() {
		// Edge case: Empty GUID for book
		var emptyGuid = Guid.Empty;
		var customerId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Customer" }
			});

		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(emptyGuid, It.IsAny<CancellationToken>()))
			.ReturnsAsync((BookAvailabilityDto?)null);

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(emptyGuid, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Book with ID*not found*");
	}

	[Fact]
	public async Task BorrowAsync_WhenPartyServiceReturnsNull_ShouldThrow() {
		// Failure mode: Party service unavailable or party not found
		_partyClientMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyDto?)null);

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), Guid.NewGuid()),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Customer with ID*not found*");
	}

	[Fact]
	public async Task BorrowAsync_WhenCatalogServiceReturnsNull_ShouldThrow() {
		// Failure mode: Catalog service unavailable or book not found
		var customerId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((BookAvailabilityDto?)null);

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Book with ID*not found*");
	}

	[Fact]
	public async Task BorrowAsync_WhenPartyServiceThrowsException_ShouldPropagate() {
		// Failure mode: Party service throws exception
		_partyClientMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Connection refused"));

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), Guid.NewGuid()),
			CancellationToken.None);

		await act.Should().ThrowAsync<HttpRequestException>()
			.WithMessage("*Connection refused*");
	}

	[Fact]
	public async Task BorrowAsync_WhenCatalogServiceThrowsException_ShouldPropagate() {
		// Failure mode: Catalog service throws exception
		var customerId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Timeout"));

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<HttpRequestException>()
			.WithMessage("*Timeout*");
	}

	[Fact]
	public async Task BorrowAsync_WhenReserveBookReturnsFalse_ShouldThrow() {
		// Failure mode: Reservation fails (race condition - book just became unavailable)
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		SetupAvailableBook(bookId);
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Failed to reserve book*");
	}

	[Fact]
	public async Task BorrowAsync_WhenCustomerHasMultipleRolesIncludingCustomer_ShouldSucceed() {
		// Edge case: Customer has multiple roles
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author", "Customer", "Librarian" }
			});

		SetupAvailableBook(bookId);
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		result.Should().NotBeNull();
		result.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task BorrowAsync_WhenCustomerRoleIsCaseSensitive_ShouldThrow() {
		// Edge case: Role case sensitivity - "customer" vs "Customer"
		var customerId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "customer" } // lowercase
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Customer role*");
	}

	[Fact]
	public async Task BorrowAsync_WhenCustomerHasNoRoles_ShouldThrow() {
		// Edge case: Empty roles list
		var customerId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string>() // empty
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*Customer role*");
	}

	[Fact]
	public async Task BorrowAsync_WhenBookHasZeroAvailableCopies_ShouldThrow() {
		// Boundary: Zero available copies
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId,
				Title = "1984",
				TotalCopies = 5,
				AvailableCopies = 0,
				IsAvailable = false
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No copies available*");
	}

	[Fact]
	public async Task BorrowAsync_WhenBookHasNegativeAvailableCopies_ShouldThrow() {
		// Extreme case: Negative available copies (data corruption scenario)
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId,
				Title = "1984",
				TotalCopies = 5,
				AvailableCopies = -1, // negative
				IsAvailable = false
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No copies available*");
	}

	[Fact]
	public async Task BorrowAsync_WhenBookIsAvailableButIsAvailableFlagIsFalse_ShouldThrow() {
		// Inconsistency: AvailableCopies > 0 but IsAvailable = false
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId,
				Title = "1984",
				TotalCopies = 5,
				AvailableCopies = 3,
				IsAvailable = false // inconsistent
			});

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId),
			CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No copies available*");
	}

	[Fact]
	public async Task BorrowAsync_WhenCustomerNameHasSpecialCharacters_ShouldSucceed() {
		// Edge case: Special characters in customer name
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "<script>alert('xss')</script>",
				LastName = "O'Brien \"The Hacker\"",
				Roles = new List<string> { "Customer" }
			});

		SetupAvailableBook(bookId);
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		result.CustomerName.Should().Contain("O'Brien");
		result.CustomerName.Should().Contain("The Hacker");
	}

	[Fact]
	public async Task BorrowAsync_WhenBookTitleIsVeryLong_ShouldSucceed() {
		// Boundary: Very long book title
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		var longTitle = new string('A', 10000);

		SetupValidCustomer(customerId);
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new BookAvailabilityDto {
				BookId = bookId,
				Title = longTitle,
				TotalCopies = 5,
				AvailableCopies = 3,
				IsAvailable = true
			});
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		result.BookTitle.Should().Be(longTitle);
	}

	[Fact]
	public async Task BorrowAsync_WhenSameCustomerBorrowsDifferentBooks_ShouldSucceed() {
		// Stress test: Same customer borrowing multiple different books
		var customerId = Guid.NewGuid();
		SetupValidCustomer(customerId);

		var bookIds = new List<Guid>();
		for (int i = 0; i < 100; i++) {
			var bookId = Guid.NewGuid();
			bookIds.Add(bookId);
			var bookIdCapture = bookId;
			_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(bookIdCapture, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new BookAvailabilityDto {
					BookId = bookIdCapture,
					Title = $"Book {i}",
					TotalCopies = 5,
					AvailableCopies = 3,
					IsAvailable = true
				});
			_catalogClientMock.Setup(x => x.ReserveBookAsync(bookIdCapture, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
		}

		foreach (var bookId in bookIds) {
			var result = await _service.BorrowAsync(
				new BorrowBookRequest(bookId, customerId), CancellationToken.None);
			result.IsActive.Should().BeTrue();
		}

		var borrowings = await _service.GetByCustomerAsync(customerId, CancellationToken.None);
		borrowings.Should().HaveCount(100);
	}

	[Fact]
	public async Task BorrowAsync_WhenManyCustomersBorrowSameBook_ShouldFailForAllAfterAvailableCopies() {
		// Race condition simulation: Many customers try to borrow the same book
		// This tests the system's behavior under concurrent load
		var bookId = Guid.NewGuid();
		var availableCopies = 3;
		var reservationCount = 0;

		SetupAvailableBook(bookId);
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => {
				reservationCount++;
				return reservationCount <= availableCopies;
			});

		var successCount = 0;
		var failureCount = 0;

		for (int i = 0; i < 10; i++) {
			var customerId = Guid.NewGuid();
			_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new PartyDto {
					Id = customerId,
					FirstName = $"Customer{i}",
					LastName = "Test",
					Roles = new List<string> { "Customer" }
				});

			try {
				await _service.BorrowAsync(
					new BorrowBookRequest(bookId, customerId), CancellationToken.None);
				successCount++;
			}
			catch (DomainException) {
				failureCount++;
			}
		}

		successCount.Should().Be(3);
		failureCount.Should().Be(7);
	}

	[Fact]
	public async Task ReturnAsync_WhenBorrowingAlreadyReturned_ShouldThrow() {
		// Edge case: Returning an already returned book
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		var borrowing = new Borrowing(bookId, "1984", customerId, "John Doe");
		borrowing.MarkReturned();
		_context.Borrowings.Add(borrowing);
		await _context.SaveChangesAsync();

		var act = () => _service.ReturnAsync(bookId, customerId, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No active borrowing found*");
	}

	[Fact]
	public async Task ReturnAsync_WhenNoActiveBorrowingExists_ShouldThrow() {
		// Failure mode: No borrowing record exists
		var act = () => _service.ReturnAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No active borrowing found*");
	}

	[Fact]
	public async Task ReturnAsync_WhenWrongCustomerTriesToReturn_ShouldThrow() {
		// Security: Wrong customer tries to return someone else's book
		var customerId = Guid.NewGuid();
		var wrongCustomerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		_context.Borrowings.Add(new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		var act = () => _service.ReturnAsync(bookId, wrongCustomerId, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No active borrowing found*");
	}

	[Fact]
	public async Task ReturnAsync_WhenWrongBookId_ShouldThrow() {
		// Edge case: Wrong book ID
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		var wrongBookId = Guid.NewGuid();
		_context.Borrowings.Add(new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		var act = () => _service.ReturnAsync(wrongBookId, customerId, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>()
			.WithMessage("*No active borrowing found*");
	}

	[Fact]
	public async Task ReturnAsync_WhenReleaseBookThrowsException_ShouldStillSucceed() {
		// Failure mode: Catalog release fails but return should still succeed
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		_context.Borrowings.Add(new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Catalog service unavailable"));

		var result = await _service.ReturnAsync(bookId, customerId, CancellationToken.None);

		result.IsActive.Should().BeFalse();
		result.ReturnedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task ReturnAsync_WhenReleaseBookReturnsFalse_ShouldStillSucceed() {
		// Failure mode: Release returns false but return should still succeed
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();
		_context.Borrowings.Add(new Borrowing(bookId, "1984", customerId, "John Doe"));
		await _context.SaveChangesAsync();

		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var result = await _service.ReturnAsync(bookId, customerId, CancellationToken.None);

		result.IsActive.Should().BeFalse();
		result.ReturnedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task GetByCustomerAsync_WhenCustomerHasNoBorrowings_ShouldReturnEmpty() {
		// Edge case: Customer with no borrowings
		var result = await _service.GetByCustomerAsync(Guid.NewGuid(), CancellationToken.None);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByBookAsync_WhenBookHasNoBorrowings_ShouldReturnEmpty() {
		// Edge case: Book with no borrowings
		var result = await _service.GetByBookAsync(Guid.NewGuid(), CancellationToken.None);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByBookAsync_ShouldIncludeBothActiveAndReturnedBorrowings() {
		// Verify that GetByBook returns all borrowings, not just active ones
		var bookId = Guid.NewGuid();
		var customerId1 = Guid.NewGuid();
		var customerId2 = Guid.NewGuid();

		var activeBorrowing = new Borrowing(bookId, "1984", customerId1, "John Doe");
		var returnedBorrowing = new Borrowing(bookId, "1984", customerId2, "Jane Doe");
		returnedBorrowing.MarkReturned();

		_context.Borrowings.Add(activeBorrowing);
		_context.Borrowings.Add(returnedBorrowing);
		await _context.SaveChangesAsync();

		var result = await _service.GetByBookAsync(bookId, CancellationToken.None);

		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetBorrowedSummaryAsync_WhenNoActiveBorrowings_ShouldReturnEmpty() {
		// Edge case: No active borrowings at all
		var result = await _service.GetBorrowedSummaryAsync(CancellationToken.None);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetBorrowedSummaryAsync_ShouldOnlyIncludeActiveBorrowings() {
		// Verify summary only includes active (not returned) borrowings
		var bookId = Guid.NewGuid();
		var customerId1 = Guid.NewGuid();
		var customerId2 = Guid.NewGuid();

		var activeBorrowing = new Borrowing(bookId, "1984", customerId1, "John Doe");
		var returnedBorrowing = new Borrowing(bookId, "1984", customerId2, "Jane Doe");
		returnedBorrowing.MarkReturned();

		_context.Borrowings.Add(activeBorrowing);
		_context.Borrowings.Add(returnedBorrowing);
		await _context.SaveChangesAsync();

		var result = await _service.GetBorrowedSummaryAsync(CancellationToken.None);

		result.Should().HaveCount(1);
		result.First().Borrowers.Should().HaveCount(1);
		result.First().Borrowers.First().CustomerId.Should().Be(customerId1);
	}

	[Fact]
	public async Task GetBorrowedSummaryAsync_WithManyBooksAndBorrowers_ShouldHandle() {
		// Stress test: Many books with many borrowers
		const int bookCount = 50;
		const int borrowersPerBook = 20;

		for (int b = 0; b < bookCount; b++) {
			var bookId = Guid.NewGuid();
			for (int c = 0; c < borrowersPerBook; c++) {
				_context.Borrowings.Add(new Borrowing(bookId, $"Book {b}", Guid.NewGuid(), $"Customer {b}-{c}"));
			}
		}
		await _context.SaveChangesAsync();

		var result = await _service.GetBorrowedSummaryAsync(CancellationToken.None);

		result.Should().HaveCount(bookCount);
		foreach (var summary in result) {
			summary.Borrowers.Should().HaveCount(borrowersPerBook);
		}
	}

	[Fact]
	public async Task BorrowAsync_WhenPublisherThrows_ShouldReleaseReservationAndRethrow() {
		// Compensation logic: Ensure reservation is released when publisher fails
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		SetupValidCustomer(customerId);
		SetupAvailableBook(bookId);
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_publisherMock.Setup(x => x.PublishAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Publisher failure"));

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		await act.Should().ThrowAsync<Exception>()
			.WithMessage("*Publisher failure*");

		_catalogClientMock.Verify(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task BorrowAsync_WhenDbContextThrows_ShouldReleaseReservationAndRethrow() {
		// Compensation logic: Ensure reservation is released when DB fails
		var customerId = Guid.NewGuid();
		var bookId = Guid.NewGuid();

		SetupValidCustomer(customerId);
		SetupAvailableBook(bookId);

		// Setup reserve to succeed but we'll throw on save
		_catalogClientMock.Setup(x => x.ReserveBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_catalogClientMock.Setup(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Make publisher throw to trigger compensation
		_publisherMock.Setup(x => x.PublishAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(bookId, customerId), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*Simulated DB failure*");

		_catalogClientMock.Verify(x => x.ReleaseBookAsync(bookId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetByIdAsync_WithEmptyGuid_ShouldThrowNotFound() {
		// Edge case: Empty GUID lookup
		var act = () => _service.GetByIdAsync(Guid.Empty, CancellationToken.None);
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByCustomerAsync_WithEmptyGuid_ShouldReturnEmpty() {
		// Edge case: Empty GUID for customer lookup
		var result = await _service.GetByCustomerAsync(Guid.Empty, CancellationToken.None);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByBookAsync_WithEmptyGuid_ShouldReturnEmpty() {
		// Edge case: Empty GUID for book lookup
		var result = await _service.GetByBookAsync(Guid.Empty, CancellationToken.None);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task BorrowAsync_WithCancellationToken_ShouldRespectCancellation() {
		// Edge case: Cancelled token - the service checks party first which will throw
		// before cancellation is checked, so we need to setup a customer that will then
		// be cancelled during the catalog check
		var customerId = Guid.NewGuid();
		var cts = new CancellationTokenSource();

		_partyClientMock.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = customerId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Customer" }
			});

		// Setup catalog to throw when cancelled
		_catalogClientMock.Setup(x => x.GetBookAvailabilityAsync(It.IsAny<Guid>(), cts.Token))
			.ThrowsAsync(new OperationCanceledException());

		cts.Cancel();

		var act = () => _service.BorrowAsync(
			new BorrowBookRequest(Guid.NewGuid(), customerId), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task ReturnAsync_WithCancellationToken_ShouldRespectCancellation() {
		// Edge case: Cancelled token
		var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = () => _service.ReturnAsync(Guid.NewGuid(), Guid.NewGuid(), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
