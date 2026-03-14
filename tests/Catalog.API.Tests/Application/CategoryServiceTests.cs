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

public sealed class CategoryServiceTests : IDisposable {
	private readonly CatalogDbContext _context;
	private readonly Mock<IEventPublisher> _publisherMock;
	private readonly CategoryService _service;

	public CategoryServiceTests() {
		var options = new DbContextOptionsBuilder<CatalogDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new CatalogDbContext(options);
		_publisherMock = new Mock<IEventPublisher>();
		_service = new CategoryService(_context, _publisherMock.Object);
	}

	public void Dispose() {
		_context.Dispose();
	}

	[Fact]
	public async Task CreateAsync_ShouldCreateAndPublishEvent() {
		// Arrange
		var request = new CreateCategoryRequest("Mystery");

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result.Name.Should().Be("Mystery");
		_publisherMock.Verify(x => x.PublishAsync(It.Is<IntegrationEvent>(
			e => e.EventType == "CategoryCreated"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetAllAsync_ShouldReturnAllCategories() {
		// Arrange
		await _context.Categories.AddRangeAsync(
			new Category("Fiction"),
			new Category("Mystery")
		);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetByIdAsync_WhenExists_ShouldReturnCategory() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		// Act
		var result = await _service.GetByIdAsync(category.Id, CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(category.Id);
		result.Name.Should().Be("Fiction");
	}

	[Fact]
	public async Task GetByIdAsync_WhenNotFound_ShouldThrow() {
		// Act
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateAndPublishEvent() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var request = new UpdateCategoryRequest("Mystery");

		// Act
		var result = await _service.UpdateAsync(category.Id, request, CancellationToken.None);

		// Assert
		result.Name.Should().Be("Mystery");
		_publisherMock.Verify(x => x.PublishAsync(It.Is<IntegrationEvent>(
			e => e.EventType == "CategoryUpdated"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DeleteAsync_ShouldRemoveAndPublishEvent() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		// Act
		await _service.DeleteAsync(category.Id, CancellationToken.None);

		// Assert
		var deleted = await _context.Categories.FindAsync(category.Id);
		deleted.Should().BeNull();
		_publisherMock.Verify(x => x.PublishAsync(It.Is<IntegrationEvent>(
			e => e.EventType == "CategoryDeleted"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetAllAsync_ShouldIncludeBookCount() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		var result = results.FirstOrDefault(c => c.Id == category.Id);
		result.Should().NotBeNull();
		result!.BookCount.Should().Be(1);
	}

	// EXTREME SCENARIOS - Edge Cases, Boundary Conditions, and Stress Tests

	[Fact]
	public async Task CreateAsync_WithSingleCharacterName_ShouldSucceed() {
		// Arrange
		var request = new CreateCategoryRequest("A");

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Name.Should().Be("A");
	}

	[Fact]
	public async Task CreateAsync_WithVeryLongName_ShouldSucceed() {
		// Arrange
		var longName = new string('A', 1000);
		var request = new CreateCategoryRequest(longName);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Name.Should().Be(longName);
	}

	[Fact]
	public async Task CreateAsync_WithUnicodeName_ShouldSucceed() {
		// Arrange
		var unicodeName = "Ficci\u00f3n \ud83d\udcda \u5c0f\u8aac \u0642\u0635\u0635";
		var request = new CreateCategoryRequest(unicodeName);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Name.Should().Be(unicodeName);
	}

	[Fact]
	public async Task CreateAsync_WithSpecialCharacters_ShouldSucceed() {
		// Arrange
		var specialName = "Fiction <script>alert('xss')</script> \"' OR 1=1 --";
		var request = new CreateCategoryRequest(specialName);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Name.Should().Be(specialName);
	}

	[Fact]
	public async Task CreateAsync_WithNameContainingNewlines_ShouldSucceed() {
		// Arrange
		var nameWithNewlines = "Fiction\nMystery\r\nThriller";
		var request = new CreateCategoryRequest(nameWithNewlines);

		// Act
		var result = await _service.CreateAsync(request, CancellationToken.None);

		// Assert
		result.Name.Should().Be(nameWithNewlines);
	}

	[Fact]
	public async Task GetAllAsync_WithNoCategories_ShouldReturnEmpty() {
		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_WithManyCategories_ShouldReturnAll() {
		// Arrange
		var categories = Enumerable.Range(1, 1000)
			.Select(i => new Category($"Category {i}"))
			.ToList();
		await _context.Categories.AddRangeAsync(categories);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().HaveCount(1000);
	}

	[Fact]
	public async Task GetByIdAsync_WithEmptyGuid_ShouldThrowNotFound() {
		// Act
		var act = () => _service.GetByIdAsync(Guid.Empty, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_WithNonExistentId_ShouldThrowNotFound() {
		// Act
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task UpdateAsync_WithEmptyGuid_ShouldThrowNotFound() {
		// Arrange
		var request = new UpdateCategoryRequest("Updated");

		// Act
		var act = () => _service.UpdateAsync(Guid.Empty, request, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task UpdateAsync_WithSameName_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var request = new UpdateCategoryRequest("Fiction");

		// Act
		var result = await _service.UpdateAsync(category.Id, request, CancellationToken.None);

		// Assert
		result.Name.Should().Be("Fiction");
	}

	[Fact]
	public async Task UpdateAsync_WithVeryLongName_ShouldSucceed() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		await _context.SaveChangesAsync();

		var longName = new string('B', 1000);
		var request = new UpdateCategoryRequest(longName);

		// Act
		var result = await _service.UpdateAsync(category.Id, request, CancellationToken.None);

		// Assert
		result.Name.Should().Be(longName);
	}

	[Fact]
	public async Task DeleteAsync_WithEmptyGuid_ShouldThrowNotFound() {
		// Act
		var act = () => _service.DeleteAsync(Guid.Empty, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentId_ShouldThrowNotFound() {
		// Act
		var act = () => _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task DeleteAsync_WithBooksInCategory_ShouldThrowDueToRestrictBehavior() {
		// Arrange - EF Core is configured with DeleteBehavior.Restrict
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", category.Id, 3);
		await _context.Books.AddAsync(book);
		await _context.SaveChangesAsync();

		// Act
		var act = () => _service.DeleteAsync(category.Id, CancellationToken.None);

		// Assert - Should throw because of restrict delete behavior
		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task GetAllAsync_WithCategoryHavingManyBooks_ShouldReturnCorrectCount() {
		// Arrange
		var category = new Category("Fiction");
		await _context.Categories.AddAsync(category);

		var authorId = Guid.NewGuid();
		var books = Enumerable.Range(1, 100)
			.Select(i => new Book($"Book {i}", $"123456789{i:D4}", authorId, "Author", category.Id, 1))
			.ToList();
		await _context.Books.AddRangeAsync(books);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		var result = results.FirstOrDefault(c => c.Id == category.Id);
		result.Should().NotBeNull();
		result!.BookCount.Should().Be(100);
	}

	[Fact]
	public async Task GetAllAsync_WithMultipleCategoriesAndBooks_ShouldReturnCorrectCounts() {
		// Arrange
		var fiction = new Category("Fiction");
		var mystery = new Category("Mystery");
		var empty = new Category("Empty");
		await _context.Categories.AddRangeAsync(fiction, mystery, empty);

		var authorId = Guid.NewGuid();
		var fictionBooks = Enumerable.Range(1, 50)
			.Select(i => new Book($"Fiction Book {i}", $"111111111{i:D4}", authorId, "Author", fiction.Id, 1))
			.ToList();
		var mysteryBooks = Enumerable.Range(1, 25)
			.Select(i => new Book($"Mystery Book {i}", $"222222222{i:D4}", authorId, "Author", mystery.Id, 1))
			.ToList();
		await _context.Books.AddRangeAsync(fictionBooks);
		await _context.Books.AddRangeAsync(mysteryBooks);
		await _context.SaveChangesAsync();

		// Act
		var results = await _service.GetAllAsync(CancellationToken.None);

		// Assert
		results.Should().HaveCount(3);
		results.First(c => c.Name == "Fiction").BookCount.Should().Be(50);
		results.First(c => c.Name == "Mystery").BookCount.Should().Be(25);
		results.First(c => c.Name == "Empty").BookCount.Should().Be(0);
	}
}
