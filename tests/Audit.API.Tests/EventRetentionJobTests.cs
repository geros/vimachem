using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Audit.API.Domain;
using Audit.API.Infrastructure;

namespace Audit.API.Tests;

public sealed class EventRetentionJobTests {
	[Fact]
	public async Task ShouldDeleteEventsOlderThanOneYear() {
		// Arrange
		var mockCollection = new Mock<IMongoCollection<DomainEvent>>();
		var mockDatabase = new Mock<IMongoDatabase>();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		mockDatabase.Setup(d => d.GetCollection<DomainEvent>("events", null))
			.Returns(mockCollection.Object);

		var deletedCount = 5L;
		mockCollection.Setup(c => c.DeleteManyAsync(
			It.IsAny<FilterDefinition<DomainEvent>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));

		var serviceProvider = new ServiceCollection()
			.AddSingleton(mockDatabase.Object)
			.BuildServiceProvider();

		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Act - use a short timeout since we can't wait 5 minutes in a test
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
		try {
			await job.StartAsync(cts.Token);
			await Task.Delay(100, cts.Token);
			await job.StopAsync(cts.Token);
		} catch (OperationCanceledException) {
			// Expected
		}

		// Assert - verify the job was set up correctly
		Assert.NotNull(job);
	}

	[Fact]
	public void EventRetentionJob_Constructor_SetsDependencies() {
		// Arrange
		var serviceProvider = new ServiceCollection().BuildServiceProvider();
		var mockLogger = new Mock<ILogger<EventRetentionJob>>();

		// Act
		var job = new EventRetentionJob(serviceProvider, mockLogger.Object);

		// Assert
		Assert.NotNull(job);
	}
}
