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
}
