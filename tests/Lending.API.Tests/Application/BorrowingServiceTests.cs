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
}
