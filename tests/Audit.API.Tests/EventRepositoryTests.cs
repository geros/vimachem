using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Audit.API.Domain;
using Audit.API.Infrastructure;

namespace Audit.API.Tests;

public sealed class EventRepositoryTests {
	[Fact]
	public async Task GetPartyEvents_ShouldIncludeDirectAndRelated() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var partyId = "party-123";
		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "PartyCreated",
				EntityType = "Party",
				EntityId = partyId,
				Action = "Create",
				Timestamp = DateTime.UtcNow
			},
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "BookBorrowed",
				EntityType = "Borrowing",
				EntityId = "borrowing-456",
				Action = "Borrow",
				RelatedEntityIds = new Dictionary<string, string> {
					{ "CustomerId", partyId }
				},
				Timestamp = DateTime.UtcNow
			}
		};

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(events);

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(2L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync(partyId, 1, 20, CancellationToken.None);

		// Assert
		Assert.Equal(2, result.Items.Count);
		Assert.Equal(2L, result.TotalCount);
		Assert.Equal(1, result.Page);
		Assert.Equal(20, result.PageSize);
		Assert.Equal(1, result.TotalPages);
	}

	[Fact]
	public async Task GetBookEvents_ShouldReturnPaginated() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var bookId = "book-789";
		var events = Enumerable.Range(1, 10).Select(i => new DomainEvent {
			Id = ObjectId.GenerateNewId().ToString(),
			EventType = "BookCreated",
			EntityType = "Book",
			EntityId = bookId,
			Action = "Create",
			Timestamp = DateTime.UtcNow
		}).ToList();

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(events);

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(25L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetBookEventsAsync(bookId, 1, 10, CancellationToken.None);

		// Assert
		Assert.Equal(10, result.Items.Count);
		Assert.Equal(25L, result.TotalCount);
		Assert.Equal(1, result.Page);
		Assert.Equal(10, result.PageSize);
		Assert.Equal(3, result.TotalPages); // 25 items / 10 per page = 3 pages
	}

	[Fact]
	public async Task GetBookEvents_SecondPage_ShouldReturnCorrectItems() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var bookId = "book-789";
		var events = Enumerable.Range(11, 10).Select(i => new DomainEvent {
			Id = ObjectId.GenerateNewId().ToString(),
			EventType = "BookUpdated",
			EntityType = "Book",
			EntityId = bookId,
			Action = "Update",
			Timestamp = DateTime.UtcNow
		}).ToList();

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(events);

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(25L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetBookEventsAsync(bookId, 2, 10, CancellationToken.None);

		// Assert
		Assert.Equal(10, result.Items.Count);
		Assert.Equal(25L, result.TotalCount);
		Assert.Equal(2, result.Page);
		Assert.Equal(10, result.PageSize);
		Assert.Equal(3, result.TotalPages);
	}

	[Fact]
	public async Task SaveEventAsync_ShouldInsertEvent() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		mockCollection.Setup(c => c.InsertOneAsync(
			It.IsAny<DomainEvent>(),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var repository = new EventRepository(mockDatabase.Object);

		var @event = new DomainEvent {
			EventType = "TestEvent",
			EntityType = "Test",
			EntityId = "test-123",
			Action = "Test",
			Timestamp = DateTime.UtcNow
		};

		// Act
		await repository.SaveEventAsync(@event, CancellationToken.None);

		// Assert
		mockCollection.Verify(c => c.InsertOneAsync(
			It.Is<DomainEvent>(e => e.EventType == "TestEvent"),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
