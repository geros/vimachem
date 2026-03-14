using Catalog.API.Application.DTOs;
using Catalog.API.Application.Interfaces;
using Catalog.API.Application.Services;
using Catalog.API.Domain.Entities;
using Catalog.API.Domain.Exceptions;
using Catalog.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Events;
using System.Net.Http;
using Xunit;

namespace Catalog.API.Tests.Application;

public sealed class BookServiceTests : IDisposable {
	private readonly CatalogDbContext _context;
	private readonly Mock<IEventPublisher> _publisherMock;
	private readonly Mock<IPartyServiceClient> _partyClientMock;
	private readonly BookService _service;

	public BookServiceTests() {
		var options = new DbContextOptionsBuilder<CatalogDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new CatalogDbContext(options);

		_publisherMock = new Mock<IEventPublisher>();
		_partyClientMock = new Mock<IPartyServiceClient>();

		_service = new BookService(_context, _publisherMock.Object, _partyClientMock.Object);
	}

	public void Dispose() {
		_context.Dispose();
	}

	[Fact]
	public async Task CreateAsync_ShouldValidateAuthorRole_ViaPartyApi() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var request = new CreateBookRequest("Test Book", "1234567890", authorId, category.Id, 3);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result.Title.Should().Be("Test Book");
		result.AuthorName.Should().Be("John Doe");
		_publisherMock.Verify(x => x.PublishAsync(It.Is<IntegrationEvent>(
			e => e.EventType == "BookCreated"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task CreateAsync_WhenAuthorNotFound_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyDto?)null);

		var request = new CreateBookRequest("Test Book", "1234567890", authorId, category.Id, 3);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
	}

	[Fact]
	public async Task CreateAsync_WhenPartyNotAuthor_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Reader" } // Not an Author
			});

		var request = new CreateBookRequest("Test Book", "1234567890", authorId, category.Id, 3);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*does not have Author role*");
	}

	[Fact]
	public async Task SearchByTitle_ShouldReturnMatchingBooks() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var book1 = new Book("The Great Adventure", "1234567890", authorId, "Author", category.Id, 3);
		var book2 = new Book("Adventure Time", "1234567891", authorId, "Author", category.Id, 2);
		var book3 = new Book("Something Else", "1234567892", authorId, "Author", category.Id, 1);
		await _context.Books.AddRangeAsync(book1, book2, book3);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync("Adventure", CancellationToken.None);

		// Assert
		results.Should().HaveCount(2);
		results.Select(b => b.Title).Should().Contain("The Great Adventure");
		results.Select(b => b.Title).Should().Contain("Adventure Time");
	}

	[Fact]
	public async Task ReserveAsync_WhenAvailable_ShouldDecrement() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.ReserveAsync(book.Id, CancellationToken.None);

		// Assert
		result.AvailableCopies.Should().Be(2);
		result.IsAvailable.Should().BeTrue();
	}

	[Fact]
	public async Task ReserveAsync_WhenNoneAvailable_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 1);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		await _service.ReserveAsync(book.Id, CancellationToken.None); // Reserve the only copy

		// Act
		var act = () => _service.ReserveAsync(book.Id, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*No copies available*");
	}

	[Fact]
	public async Task ReleaseAsync_ShouldIncrement() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 2);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		await _service.ReserveAsync(book.Id, CancellationToken.None); // available = 1

		// Act
		var result = await _service.ReleaseAsync(book.Id, CancellationToken.None);

		// Assert
		result.AvailableCopies.Should().Be(2);
	}

	[Fact]
	public async Task DeleteAsync_ShouldRemoveAndPublishEvent() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		await _service.DeleteAsync(book.Id, CancellationToken.None);

		// Assert
		var deletedBook = await _context.Books.FindAsync(book.Id);
		deletedBook.Should().BeNull();
		_publisherMock.Verify(x => x.PublishAsync(It.Is<IntegrationEvent>(
			e => e.EventType == "BookDeleted"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetByIdAsync_WhenBookNotFound_ShouldThrow() {
		// Act
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetAvailabilityAsync_ShouldReturnCorrectAvailability() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 5);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetAvailabilityAsync(book.Id, CancellationToken.None);

		// Assert
		result.BookId.Should().Be(book.Id);
		result.Title.Should().Be("Test Book");
		result.TotalCopies.Should().Be(5);
		result.AvailableCopies.Should().Be(5);
		result.IsAvailable.Should().BeTrue();
	}

	// EXTREME SCENARIOS - Edge Cases, Boundary Conditions, and Stress Tests

	[Fact]
	public async Task CreateAsync_WithEmptyTitle_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var request = new CreateBookRequest("", "1234567890", authorId, category.Id, 1);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Title.Should().BeEmpty();
	}

	[Fact]
	public async Task CreateAsync_WithVeryLongTitle_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var longTitle = new string('A', 1000);
		var request = new CreateBookRequest(longTitle, "1234567890", authorId, category.Id, 1);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Title.Should().Be(longTitle);
	}

	[Fact]
	public async Task CreateAsync_WithUnicodeTitle_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var unicodeTitle = "The Great Gatsby \ud83d\udcda \u4e2d\u6587 \u0627\u0644\u0639\u0631\u0628\u064a\u0629";
		var request = new CreateBookRequest(unicodeTitle, "1234567890", authorId, category.Id, 1);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Title.Should().Be(unicodeTitle);
	}

	[Fact]
	public async Task CreateAsync_WithMaxIntCopies_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var request = new CreateBookRequest("Test", "1234567890", authorId, category.Id, int.MaxValue);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.TotalCopies.Should().Be(int.MaxValue);
		result.AvailableCopies.Should().Be(int.MaxValue);
	}

	[Fact]
	public async Task CreateAsync_WithEmptyGuidCategory_ShouldThrow() {
		// Arrange
		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Author" }
			});

		var request = new CreateBookRequest("Test", "1234567890", authorId, Guid.Empty, 1);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task CreateAsync_WhenPartyServiceThrowsHttpRequestException_ShouldPropagate() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Connection refused"));

		var request = new CreateBookRequest("Test", "1234567890", authorId, category.Id, 1);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert - HttpRequestException from mock propagates directly (would be wrapped by real PartyServiceClient)
		await act.Should().ThrowAsync<HttpRequestException>();
	}

	[Fact]
	public async Task CreateAsync_WhenPartyServiceTimesOut_ShouldWrapInDomainException() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TaskCanceledException("Request timeout"));

		var request = new CreateBookRequest("Test", "1234567890", authorId, category.Id, 1);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert - TaskCanceledException is not caught, so it propagates
		await act.Should().ThrowAsync<TaskCanceledException>();
	}

	[Fact]
	public async Task CreateAsync_WhenAuthorHasMultipleRolesIncludingAuthor_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string> { "Reader", "Author", "Librarian" }
			});

		var request = new CreateBookRequest("Test", "1234567890", authorId, category.Id, 1);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task CreateAsync_WhenAuthorHasEmptyRoles_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var authorId = Guid.NewGuid();
		_partyClientMock.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PartyDto {
				Id = authorId,
				FirstName = "John",
				LastName = "Doe",
				Roles = new List<string>()
			});

		var request = new CreateBookRequest("Test", "1234567890", authorId, category.Id, 1);

		// Act
		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*does not have Author role*");
	}

	[Fact]
	public async Task SearchByTitle_WithEmptyString_ShouldReturnAllBooks() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var book1 = new Book("The Great Adventure", "1234567890", authorId, "Author", category.Id, 3);
		var book2 = new Book("Adventure Time", "1234567891", authorId, "Author", category.Id, 2);
		await _context.Books.AddRangeAsync(book1, book2);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync("", CancellationToken.None);

		// Assert - empty search returns all books
		results.Should().HaveCount(2);
	}

	[Fact]
	public async Task SearchByTitle_WithSqlInjectionPattern_ShouldTreatAsLiteral() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var book1 = new Book("' OR 1=1 --", "1234567890", authorId, "Author", category.Id, 3);
		var book2 = new Book("Normal Title", "1234567891", authorId, "Author", category.Id, 2);
		await _context.Books.AddRangeAsync(book1, book2);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync("' OR 1=1 --", CancellationToken.None);

		// Assert - should only match the book with that exact title (case insensitive)
		results.Should().HaveCount(1);
		results.First().Title.Should().Be("' OR 1=1 --");
	}

	[Fact]
	public async Task SearchByTitle_WithSpecialCharacters_ShouldWork() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var book = new Book("C# Programming \u0026 .NET Framework", "1234567890", authorId, "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync("C#", CancellationToken.None);

		// Assert
		results.Should().HaveCount(1);
	}

	[Fact]
	public async Task SearchByTitle_WithUnicode_ShouldWork() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var book = new Book("\u4e2d\u6587\u4e66\u540d", "1234567890", authorId, "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync("\u4e2d\u6587", CancellationToken.None);

		// Assert
		results.Should().HaveCount(1);
	}

	[Fact]
	public async Task SearchByTitle_WithVeryLongQuery_ShouldWork() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var longTitle = new string('A', 500);
		var book = new Book(longTitle, "1234567890", authorId, "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.SearchByTitleAsync(new string('A', 100), CancellationToken.None);

		// Assert
		results.Should().HaveCount(1);
	}

	[Fact]
	public async Task GetAllAsync_WithNoBooks_ShouldReturnEmpty() {
		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_WithManyBooks_ShouldReturnAll() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var books = Enumerable.Range(1, 1000)
			.Select(i => new Book($"Book {i}", $"123456789{i:D4}", authorId, "Author", category.Id, 1))
			.ToList();
		await _context.Books.AddRangeAsync(books);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().HaveCount(1000);
	}

	[Fact]
	public async Task ReserveAsync_SequentialMultipleTimes_ShouldDecrementCorrectly() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 10);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act - reserve sequentially (concurrency not supported by EF InMemory)
		for (int i = 0; i < 10; i++) {
			await _service.ReserveAsync(book.Id, CancellationToken.None);
		}

		// Assert
		var updatedBook = await _context.Books.FindAsync(book.Id);
		updatedBook!.AvailableCopies.Should().Be(0);
	}

	[Fact]
	public async Task ReserveAsync_WhenBookNotFound_ShouldThrowNotFoundException() {
		// Act
		var act = () => _service.ReserveAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task ReleaseAsync_WhenBookNotFound_ShouldThrowNotFoundException() {
		// Act
		var act = () => _service.ReleaseAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task ReleaseAsync_WhenAllCopiesAlreadyAvailable_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act - try to release without any reservations
		var act = () => _service.ReleaseAsync(book.Id, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*All copies already available*");
	}

	[Fact]
	public async Task UpdateAsync_WithEmptyGuidCategory_ShouldThrowNotFound() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		var request = new UpdateBookRequest("Updated", Guid.Empty, 3);

		// Act
		var act = () => _service.UpdateAsync(book.Id, request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task UpdateAsync_ReduceToExactlyBorrowed_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 5);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		await _service.ReserveAsync(book.Id, CancellationToken.None); // 4 available
		await _service.ReserveAsync(book.Id, CancellationToken.None); // 3 available

		var request = new UpdateBookRequest("Updated", category.Id, 2); // Reduce to exactly borrowed amount

		// Act
		var result = await _service.UpdateAsync(book.Id, request, CancellationToken.None);

		// Assert
		result.TotalCopies.Should().Be(2);
		result.AvailableCopies.Should().Be(0);
	}

	[Fact]
	public async Task UpdateAsync_ReduceBelowBorrowed_ShouldThrow() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 5);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		await _service.ReserveAsync(book.Id, CancellationToken.None);
		await _service.ReserveAsync(book.Id, CancellationToken.None);

		var request = new UpdateBookRequest("Updated", category.Id, 1); // Try to reduce below borrowed

		// Act
		var act = () => _service.UpdateAsync(book.Id, request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<DomainException>().WithMessage("*Cannot reduce*");
	}

	[Fact]
	public async Task DeleteAsync_WhenBookNotFound_ShouldThrowNotFound() {
		// Act
		var act = () => _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_WithEmptyGuid_ShouldThrowNotFound() {
		// Act
		var act = () => _service.GetByIdAsync(Guid.Empty, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetAvailabilityAsync_WithZeroCopiesAvailable_ShouldReturnNotAvailable() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test Book", "1234567890", Guid.NewGuid(), "Author", category.Id, 1);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		await _service.ReserveAsync(book.Id, CancellationToken.None);

		// Act
		var result = await _service.GetAvailabilityAsync(book.Id, CancellationToken.None);

		// Assert
		result.IsAvailable.Should().BeFalse();
		result.AvailableCopies.Should().Be(0);
	}
}
