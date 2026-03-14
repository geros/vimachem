using MongoDB.Driver;
using Audit.API.Domain;

namespace Audit.API.Infrastructure;

public sealed class EventRetentionJob : BackgroundService {
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<EventRetentionJob> _logger;
	private readonly TimeSpan _interval = TimeSpan.FromHours(24);
	private readonly TimeSpan _retention = TimeSpan.FromDays(365);

	public EventRetentionJob(
		IServiceProvider serviceProvider,
		ILogger<EventRetentionJob> logger) {
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		// Initial delay — don't run immediately on startup
		await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				using var scope = _serviceProvider.CreateScope();
				var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
				var collection = database.GetCollection<DomainEvent>("events");

				var cutoff = DateTime.UtcNow.Subtract(_retention);
				var filter = Builders<DomainEvent>.Filter.Lt(e => e.Timestamp, cutoff);

				var result = await collection.DeleteManyAsync(filter, stoppingToken);

				if (result.DeletedCount > 0)
					_logger.LogInformation(
						"Event retention cleanup: deleted {Count} events older than {Cutoff}",
						result.DeletedCount, cutoff);
				else
					_logger.LogDebug("Event retention cleanup: no events to delete");

			} catch (Exception ex) {
				_logger.LogError(ex, "Event retention cleanup failed");
			}

			await Task.Delay(_interval, stoppingToken);
		}
	}
}
