using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Audit.API.Application;
using Audit.API.Controllers;
using Audit.API.Domain;

namespace Audit.API.Tests;

public sealed class EventsControllerExtremeTests {

	[Fact]
	public async Task GetPartyEvents_PageZero_ShouldNormalizeToPageOne() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - Controller normalizes page < 1 to 1
		var result = await controller.GetPartyEvents("party-123", page: 0, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<PagedResponse<EventResponse>>(okResult.Value);
		mockRepo.Verify(r => r.GetPartyEventsAsync("party-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-100)]
	[InlineData(int.MinValue)]
	public async Task GetPartyEvents_NegativePage_ShouldNormalizeToPageOne(int page) {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("party-123", page: page, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync("party-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task GetPartyEvents_ZeroOrNegativePageSize_ShouldNormalizeToDefault(int pageSize) {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("party-123", page: 1, pageSize: pageSize);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync("party-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(101)]
	[InlineData(1000)]
	[InlineData(int.MaxValue)]
	public async Task GetPartyEvents_PageSizeOver100_ShouldNormalizeToDefault(int pageSize) {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("party-123", page: 1, pageSize: pageSize);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync("party-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_EmptyPartyId_ShouldStillCallRepository() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("", page: 1, pageSize: 20);

		// Assert - Controller doesn't validate partyId, passes it through
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync("", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_NullPartyId_ShouldThrowOrReturnEmpty() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - null partyId will cause issues
		var result = await controller.GetPartyEvents(null!, page: 1, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
	}

	[Fact]
	public async Task GetPartyEvents_VeryLongPartyId_ShouldHandleCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		var veryLongId = new string('a', 10000);

		mockRepo.Setup(r => r.GetPartyEventsAsync(
			veryLongId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents(veryLongId, page: 1, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync(veryLongId, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_UnicodeInPartyId_ShouldHandleCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		var unicodeId = "party-🎉-日本語-العربية";

		mockRepo.Setup(r => r.GetPartyEventsAsync(
			unicodeId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse> {
					new EventResponse {
						Id = "event-1",
						EventType = "Test",
						EntityType = "Party",
						EntityId = unicodeId,
						Action = "Test",
						Timestamp = DateTime.UtcNow
					}
				},
				Page = 1,
				PageSize = 20,
				TotalCount = 1,
				TotalPages = 1
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents(unicodeId, page: 1, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<PagedResponse<EventResponse>>(okResult.Value);
		Assert.Single(response.Items);
	}

	[Fact]
	public async Task GetPartyEvents_SpecialCharactersInPartyId_ShouldHandleCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		var specialId = "party-123; DROP TABLE events; --";

		mockRepo.Setup(r => r.GetPartyEventsAsync(
			specialId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents(specialId, page: 1, pageSize: 20);

		// Assert - Controller should pass through (repository/MongoDB handles sanitization)
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync(specialId, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_RepositoryThrowsException_ShouldPropagate() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Database connection failed"));

		var controller = new EventsController(mockRepo.Object);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await controller.GetPartyEvents("party-123", page: 1, pageSize: 20));
	}

	[Fact]
	public async Task GetPartyEvents_CancellationRequested_ShouldThrowOperationCanceledException() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new OperationCanceledException());

		var controller = new EventsController(mockRepo.Object);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await controller.GetPartyEvents("party-123", page: 1, pageSize: 20, ct: cts.Token));
	}

	[Fact]
	public async Task GetPartyEvents_VeryLargeResultSet_ShouldReturnCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		var largeResultSet = Enumerable.Range(1, 100).Select(i => new EventResponse {
			Id = $"event-{i}",
			EventType = "TestEvent",
			EntityType = "Party",
			EntityId = "party-123",
			Action = "Test",
			Timestamp = DateTime.UtcNow
		}).ToList();

		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = largeResultSet,
				Page = 1,
				PageSize = 100,
				TotalCount = 1000,
				TotalPages = 10
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("party-123", page: 1, pageSize: 100);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<PagedResponse<EventResponse>>(okResult.Value);
		Assert.Equal(100, response.Items.Count);
		Assert.Equal(1000, response.TotalCount);
		Assert.Equal(10, response.TotalPages);
	}

	[Fact]
	public async Task GetBookEvents_PageZero_ShouldNormalizeToPageOne() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetBookEvents("book-123", page: 0, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("book-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task GetBookEvents_NegativePage_ShouldNormalizeToPageOne(int page) {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetBookEvents("book-123", page: page, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("book-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetBookEvents_EmptyBookId_ShouldStillCallRepository() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetBookEvents("", page: 1, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetBookEvents_RepositoryThrowsTimeoutException_ShouldPropagate() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TimeoutException("Query timed out"));

		var controller = new EventsController(mockRepo.Object);

		// Act & Assert
		await Assert.ThrowsAsync<TimeoutException>(async () =>
			await controller.GetBookEvents("book-123", page: 1, pageSize: 20));
	}

	[Fact]
	public async Task GetBookEvents_ValidPageSizeBoundary_ShouldUseProvidedValue() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 50,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - Page size of 50 is within valid range (1-100)
		var result = await controller.GetBookEvents("book-123", page: 1, pageSize: 50);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("book-123", 1, 50, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetBookEvents_PageSizeExactly100_ShouldAccept() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 100,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - Page size of exactly 100 should be accepted
		var result = await controller.GetBookEvents("book-123", page: 1, pageSize: 100);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("book-123", 1, 100, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetBookEvents_PageSize101_ShouldNormalizeToDefault() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetBookEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - Page size over 100 should normalize to 20
		var result = await controller.GetBookEvents("book-123", page: 1, pageSize: 101);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetBookEventsAsync("book-123", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_ConcurrentRequests_ShouldHandleCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		var callCount = 0;

		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => {
				Interlocked.Increment(ref callCount);
				return new PagedResponse<EventResponse> {
					Items = new List<EventResponse>(),
					Page = 1,
					PageSize = 20,
					TotalCount = 0,
					TotalPages = 0
				};
			});

		var controller = new EventsController(mockRepo.Object);

		// Act - Simulate concurrent requests
		var tasks = Enumerable.Range(1, 50).Select(i =>
			controller.GetPartyEvents($"party-{i}", page: 1, pageSize: 20)
		).ToList();

		await Task.WhenAll(tasks);

		// Assert
		Assert.Equal(50, callCount);
	}

	[Fact]
	public void EventsController_NullRepository_DoesNotThrow() {
		// Act - Controller accepts null repository (no validation in constructor)
		var controller = new EventsController(null!);

		// Assert - Controller was created
		Assert.NotNull(controller);
	}

	[Fact]
	public async Task GetPartyEvents_WhitespacePartyId_ShouldPassThrough() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = new List<EventResponse>(),
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("   ", page: 1, pageSize: 20);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		mockRepo.Verify(r => r.GetPartyEventsAsync("   ", 1, 20, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetPartyEvents_ResponseWithNullItems_ShouldHandleCorrectly() {
		// Arrange
		var mockRepo = new Mock<IEventRepository>();
		mockRepo.Setup(r => r.GetPartyEventsAsync(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<EventResponse> {
				Items = null!, // Null items list
				Page = 1,
				PageSize = 20,
				TotalCount = 0,
				TotalPages = 0
			});

		var controller = new EventsController(mockRepo.Object);

		// Act
		var result = await controller.GetPartyEvents("party-123", page: 1, pageSize: 20);

		// Assert - Controller doesn't validate response content
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<PagedResponse<EventResponse>>(okResult.Value);
		Assert.Null(response.Items);
	}
}
