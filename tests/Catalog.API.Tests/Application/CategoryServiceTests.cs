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
}
