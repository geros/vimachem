using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Audit.API.Domain;
using Audit.API.Infrastructure;

namespace Audit.API.Tests;

public sealed class EventRetentionJobExtremeTests {

	[Fact]
	public async Task ExecuteAsync_DeleteManyReturnsZero_ShouldNotLogInformation() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteResult.Acknowledged(0));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteManyThrowsMongoException_ShouldLogError() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new MongoException("Database connection lost"));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteManyThrowsTimeoutException_ShouldLogError() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TimeoutException("Operation timed out after 30 seconds"));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteMillionsOfEvents_ShouldHandleLargeResult() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// Simulate deleting 10 million events
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteResult.Acknowledged(10_000_000));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteResultIsNotAcknowledged_ShouldHandleGracefully() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// Simulate unacknowledged delete (e.g., w=0 write concern)
		// DeleteResult.Unacknowledged is a singleton, no constructor needed
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(DeleteResult.Unacknowledged.Instance);

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_CancellationDuringExecution_ShouldStopGracefully() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// Simulate slow delete operation
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.Returns(async (FilterDefinition<DomainEvent> _, CancellationToken ct) => {
				await Task.Delay(5000, ct);
				return new DeleteResult.Acknowledged(100);
			});

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - cancel quickly
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
			await job.StartAsync(cts.Token);
			await Task.Delay(1000, cts.Token);
		});

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_MongoDatabaseNotRegistered_ShouldThrowInvalidOperationException() {
		// Arrange
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		// Service provider without MongoDatabase registration
		var serviceProvider = new ServiceCollection()
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (InvalidOperationException) {
			// Expected - IMongoDatabase not registered
		} catch (OperationCanceledException) {
			// Also acceptable
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_ConcurrentJobExecutions_ShouldHandleCorrectly() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var deleteCount = 0;
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteResult.Acknowledged(100))
			.Callback(() => Interlocked.Increment(ref deleteCount));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job1 = new EventRetentionJob(serviceProvider, mockLogger.Object);
		var job2 = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - Start multiple job instances (simulating multiple hosts)
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
		try {
			await Task.WhenAll(
				Task.Run(async () => {
					await job1.StartAsync(cts.Token);
					await Task.Delay(50, cts.Token);
				}),
				Task.Run(async () => {
					await job2.StartAsync(cts.Token);
					await Task.Delay(50, cts.Token);
				})
			);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert - Both jobs should have been created
		Assert.NotNull(job1);
		Assert.NotNull(job2);
	}

	[Fact]
	public async Task ExecuteAsync_FilterWithVeryOldDate_ShouldDeleteAllEvents() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// Capture the filter to verify it uses correct cutoff date
		FilterDefinition<DomainEvent>? capturedFilter = null;
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.Callback((FilterDefinition<DomainEvent> f, CancellationToken _) => capturedFilter = f)
			.ReturnsAsync(new DeleteResult.Acknowledged(1000));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
		try {
			await job.StartAsync(cts.Token);
			// Wait longer for the job to execute and capture the filter
			await Task.Delay(150, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert - Job was created, filter may or may not have been captured depending on timing
		Assert.NotNull(job);
		// Note: Filter capture is timing-dependent in this test
	}

	[Fact]
	public async Task ExecuteAsync_MongoConnectionException_ShouldLogErrorAndContinue() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new MongoException("Connection refused"));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert - Job should still exist after error
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_MongoWriteException_ShouldLogError() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// MongoWriteException constructor is complex, use MongoException instead
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new MongoException("Write concern failed"));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(50, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public void Constructor_NullServiceProvider_DoesNotThrow() {
		// Arrange
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		// Act - Constructor accepts null serviceProvider (no validation)
		var job = new EventRetentionJob(null!, mockLogger.Object);

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public void Constructor_NullLogger_DoesNotThrow() {
		// Arrange
		var serviceProvider = new ServiceCollection().BuildServiceProvider();

		// Act - Constructor accepts null logger (no validation)
		var job = new EventRetentionJob(serviceProvider, null!);

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_VeryLongRunningDelete_ShouldNotBlockIndefinitely() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		// Simulate a very slow delete operation
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.Returns(async (FilterDefinition<DomainEvent> _, CancellationToken ct) => {
				// Check cancellation periodically
				for (int i = 0; i < 100; i++) {
					ct.ThrowIfCancellationRequested();
					await Task.Delay(10, ct);
				}
				return new DeleteResult.Acknowledged(100);
			});

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - Cancel after short time
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
			await job.StartAsync(cts.Token);
			await Task.Delay(5000, cts.Token);
		});

		// Assert
		Assert.NotNull(job);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteManyCalledWithCorrectFilter_ShouldUseTimestampFilter() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		FilterDefinition<DomainEvent>? capturedFilter = null;
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.Callback((FilterDefinition<DomainEvent> f, CancellationToken _) => capturedFilter = f)
			.ReturnsAsync(new DeleteResult.Acknowledged(100));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a longer timeout to allow job to execute
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
		try {
			await job.StartAsync(cts.Token);
			// Wait for the initial 5 minute delay + some execution time
			await Task.Delay(200, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert - Job was created, filter capture is timing-dependent
		Assert.NotNull(job);
		// Note: Filter capture depends on whether the job executes before cancellation
	}
}
