namespace Lending.API.HttpClients;

public interface ICatalogServiceClient {
	Task<BookAvailabilityDto?> GetBookAvailabilityAsync(Guid id, CancellationToken ct);
	Task<bool> ReserveBookAsync(Guid bookId, CancellationToken ct);
	Task<bool> ReleaseBookAsync(Guid bookId, CancellationToken ct);
}

public record BookAvailabilityDto {
	public Guid BookId { get; init; }
	public string Title { get; init; } = string.Empty;
	public int TotalCopies { get; init; }
	public int AvailableCopies { get; init; }
	public bool IsAvailable { get; init; }
}
