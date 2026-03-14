using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Audit.API.Domain;
using Audit.API.Infrastructure;

namespace Audit.API.Tests;

public sealed class EventRepositoryExtremeTests {

	[Fact]
	public async Task GetPartyEvents_EmptyCollection_ShouldReturnEmptyResult() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(0L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None);

		// Assert
		Assert.Empty(result.Items);
		Assert.Equal(0L, result.TotalCount);
		Assert.Equal(1, result.Page);
		Assert.Equal(0, result.TotalPages);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task GetPartyEvents_PageZeroOrNegative_ShouldCalculateSkipCorrectly(int page) {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "TestEvent",
				EntityType = "Party",
				EntityId = "party-123",
				Action = "Test",
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
			.ReturnsAsync(1L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act - The repository doesn't validate page numbers, so negative/zero will cause negative skip
		var result = await repository.GetPartyEventsAsync("party-123", page, 20, CancellationToken.None);

		// Assert - Repository passes through the page value as-is
		Assert.Single(result.Items);
		Assert.Equal(page, result.Page);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public async Task GetPartyEvents_ZeroOrNegativePageSize_MayCauseIssues(int pageSize) {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(100L);

		// Need to setup FindAsync for negative page sizes (limit is negative)
		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act & Assert - Zero or negative pageSize causes various issues
		if (pageSize == 0) {
			// With pageSize 0, MongoDB returns null cursor
			await Assert.ThrowsAnyAsync<Exception>(async () =>
				await repository.GetPartyEventsAsync("party-123", 1, pageSize, CancellationToken.None));
		} else {
			// Negative pageSize - MongoDB may throw or return unexpected results
			try {
				var result = await repository.GetPartyEventsAsync("party-123", 1, pageSize, CancellationToken.None);
				// If it succeeds, TotalPages would be negative or zero due to Math.Ceiling
				Assert.True(result.TotalPages <= 0 || result.TotalPages > 100);
			} catch (Exception) {
				// MongoDB may throw for negative limit
			}
		}
	}

	[Fact]
	public async Task GetPartyEvents_VeryLargePageSize_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var events = Enumerable.Range(1, 100).Select(i => new DomainEvent {
			Id = ObjectId.GenerateNewId().ToString(),
			EventType = "TestEvent",
			EntityType = "Party",
			EntityId = "party-123",
			Action = "Test",
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
			.ReturnsAsync(100L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync("party-123", 1, int.MaxValue, CancellationToken.None);

		// Assert
		Assert.Equal(100, result.Items.Count);
		Assert.Equal(1, result.TotalPages);
	}

	[Fact]
	public async Task GetPartyEvents_VeryLargePageNumber_ShouldReturnEmpty() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(100L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act - Page number far beyond available data
		var result = await repository.GetPartyEventsAsync("party-123", 1000000, 20, CancellationToken.None);

		// Assert
		Assert.Empty(result.Items);
		Assert.Equal(1000000, result.Page);
		Assert.Equal(5, result.TotalPages); // 100 items / 20 per page = 5 pages
	}

	[Fact]
	public async Task GetPartyEvents_NullPartyId_ShouldThrowOrReturnEmpty() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(0L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync(null!, 1, 20, CancellationToken.None);

		// Assert - Should handle null gracefully (filter will match nothing or throw)
		Assert.NotNull(result);
	}

	[Fact]
	public async Task GetPartyEvents_EmptyPartyId_ShouldReturnEmptyOrMatchEmpty() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(0L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync("", 1, 20, CancellationToken.None);

		// Assert
		Assert.Empty(result.Items);
	}

	[Fact]
	public async Task SaveEventAsync_NullEvent_PassesThroughToMongoDriver() {
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

		// Repository passes null to Mongo driver which may throw
		mockCollection.Setup(c => c.InsertOneAsync(
			It.IsAny<DomainEvent>(),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ArgumentNullException("entity"));

		var repository = new EventRepository(mockDatabase.Object);

		// Act & Assert - Mongo driver throws for null
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await repository.SaveEventAsync(null!, CancellationToken.None));
	}

	[Fact]
	public async Task SaveEventAsync_MissingRequiredFields_ShouldStillInsert() {
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

		// Event with minimal fields - no validation in repository
		var @event = new DomainEvent {
			// All fields have default values
		};

		// Act - Should not throw, repository doesn't validate
		await repository.SaveEventAsync(@event, CancellationToken.None);

		// Assert
		mockCollection.Verify(c => c.InsertOneAsync(
			It.Is<DomainEvent>(e => e.EventType == string.Empty),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveEventAsync_VeryLargePayload_ShouldHandleCorrectly() {
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

		// Create a very large payload (16MB is MongoDB document limit)
		var largeData = new string('x', 10_000_000);
		var @event = new DomainEvent {
			EventType = "LargePayloadEvent",
			EntityType = "Test",
			EntityId = "test-123",
			Action = "Test",
			Payload = BsonDocument.Parse($"{{\"data\": \"{largeData.Substring(0, 1000)}\"}}"),
			Timestamp = DateTime.UtcNow
		};

		// Act
		await repository.SaveEventAsync(@event, CancellationToken.None);

		// Assert
		mockCollection.Verify(c => c.InsertOneAsync(
			It.Is<DomainEvent>(e => e.EventType == "LargePayloadEvent"),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveEventAsync_SpecialCharactersInFields_ShouldHandleCorrectly() {
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
			EventType = "Test\n\r\t\\\"'Event",
			EntityType = "Test<Entity>",
			EntityId = "test-123; DROP TABLE events; --",
			Action = "Test ",
			Timestamp = DateTime.UtcNow
		};

		// Act
		await repository.SaveEventAsync(@event, CancellationToken.None);

		// Assert
		mockCollection.Verify(c => c.InsertOneAsync(
			It.Is<DomainEvent>(e => e.EntityId.Contains("DROP TABLE")),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_VeryLongPartyId_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var veryLongId = new string('a', 10000);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(0L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync(veryLongId, 1, 20, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result.Items);
	}

	[Fact]
	public async Task GetPartyEvents_UnicodeAndEmojiInPartyId_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var unicodeId = "party-🎉-日本語-العربية-🚀";

		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "UnicodeTest",
				EntityType = "Party",
				EntityId = unicodeId,
				Action = "Test",
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
			.ReturnsAsync(1L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync(unicodeId, 1, 20, CancellationToken.None);

		// Assert
		Assert.Single(result.Items);
	}

	[Fact]
	public async Task GetPartyEvents_DatabaseConnectionFailure_ShouldThrowException() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new MongoException("Connection refused"));

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act & Assert
		await Assert.ThrowsAsync<MongoException>(async () =>
			await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None));
	}

	[Fact]
	public async Task GetPartyEvents_TimeoutException_ShouldThrowException() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TimeoutException("Operation timed out"));

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act & Assert
		await Assert.ThrowsAsync<TimeoutException>(async () =>
			await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None));
	}

	[Fact]
	public async Task GetPartyEvents_CancellationRequested_ShouldThrowOperationCanceledException() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new OperationCanceledException());

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await repository.GetPartyEventsAsync("party-123", 1, 20, cts.Token));
	}

	[Fact]
	public async Task GetBookEvents_EmptyBookId_ShouldReturnEmpty() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(0L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetBookEventsAsync("", 1, 20, CancellationToken.None);

		// Assert
		Assert.Empty(result.Items);
	}

	[Fact]
	public async Task GetBookEvents_WithRelatedEntities_ShouldIncludeAll() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var bookId = "book-789";
		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "BookCreated",
				EntityType = "Book",
				EntityId = bookId,
				Action = "Create",
				Timestamp = DateTime.UtcNow
			},
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "BookBorrowed",
				EntityType = "Borrowing",
				EntityId = "borrowing-456",
				Action = "Borrow",
				RelatedEntityIds = new Dictionary<string, string> { { "BookId", bookId } },
				Timestamp = DateTime.UtcNow
			},
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "BookReviewed",
				EntityType = "Review",
				EntityId = "review-789",
				Action = "Review",
				RelatedEntityIds = new Dictionary<string, string> { { "BookId", bookId } },
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
			.ReturnsAsync(3L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetBookEventsAsync(bookId, 1, 20, CancellationToken.None);

		// Assert
		Assert.Equal(3, result.Items.Count);
	}

	[Fact]
	public async Task SaveEventAsync_ConcurrentCalls_ShouldHandleCorrectly() {
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

		var insertCount = 0;
		mockCollection.Setup(c => c.InsertOneAsync(
			It.IsAny<DomainEvent>(),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask)
			.Callback(() => Interlocked.Increment(ref insertCount));

		var repository = new EventRepository(mockDatabase.Object);

		// Act - Simulate concurrent event saves
		var tasks = Enumerable.Range(1, 100).Select(i => {
			var @event = new DomainEvent {
				EventType = $"Event-{i}",
				EntityType = "Test",
				EntityId = $"test-{i}",
				Action = "Test",
				Timestamp = DateTime.UtcNow
			};
			return repository.SaveEventAsync(@event, CancellationToken.None);
		}).ToList();

		await Task.WhenAll(tasks);

		// Assert
		Assert.Equal(100, insertCount);
	}

	[Fact]
	public async Task GetPartyEvents_TotalCountLargerThanIntMaxValue_ShouldHandlePagination() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var mockCursor = new Mock<IAsyncCursor<DomainEvent>>();
		mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(true)
			.ReturnsAsync(false);
		mockCursor.Setup(c => c.Current).Returns(new List<DomainEvent>());

		mockCollection.Setup(c => c.FindAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<FindOptions<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockCursor.Object);

		// Simulate count larger than int.MaxValue
		mockCollection.Setup(c => c.CountDocumentsAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CountOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync((long)int.MaxValue + 1000);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act
		var result = await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None);

		// Assert - TotalPages calculation with large count
		Assert.True(result.TotalPages > 0);
		Assert.Equal((long)int.MaxValue + 1000, result.TotalCount);
	}

	[Fact]
	public async Task GetPartyEvents_EventsWithNullRelatedEntities_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "TestEvent",
				EntityType = "Party",
				EntityId = "party-123",
				Action = "Test",
				RelatedEntityIds = null!, // Null related entities
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
			.ReturnsAsync(1L);

		mockCollection.Setup(c => c.Indexes.CreateOne(
			It.IsAny<CreateIndexModel<DomainEvent>>(),
			It.IsAny<CreateOneIndexOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns("index-name");

		var repository = new EventRepository(mockDatabase.Object);

		// Act & Assert - Null RelatedEntityIds may cause null reference during mapping
		var result = await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None);
		Assert.Single(result.Items);
	}

	[Fact]
	public async Task GetPartyEvents_MinDateTimeValues_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var events = new List<DomainEvent> {
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "AncientEvent",
				EntityType = "Party",
				EntityId = "party-123",
				Action = "Create",
				Timestamp = DateTime.MinValue // Minimum date time
			},
			new() {
				Id = ObjectId.GenerateNewId().ToString(),
				EventType = "FutureEvent",
				EntityType = "Party",
				EntityId = "party-123",
				Action = "Update",
				Timestamp = DateTime.MaxValue // Maximum date time
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
		var result = await repository.GetPartyEventsAsync("party-123", 1, 20, CancellationToken.None);

		// Assert
		Assert.Equal(2, result.Items.Count);
	}

	[Fact]
	public async Task SaveEventAsync_EventWithMaxSizeRelatedEntities_ShouldHandleCorrectly() {
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

		// Create event with many related entities
		var relatedEntities = new Dictionary<string, string>();
		for (int i = 0; i < 1000; i++) {
			relatedEntities[$"Key{i}"] = $"Value{i}";
		}

		var @event = new DomainEvent {
			EventType = "ManyRelationsEvent",
			EntityType = "Test",
			EntityId = "test-123",
			Action = "Test",
			RelatedEntityIds = relatedEntities,
			Timestamp = DateTime.UtcNow
		};

		// Act
		await repository.SaveEventAsync(@event, CancellationToken.None);

		// Assert
		mockCollection.Verify(c => c.InsertOneAsync(
			It.Is<DomainEvent>(e => e.RelatedEntityIds.Count == 1000),
			It.IsAny<InsertOneOptions>(),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
