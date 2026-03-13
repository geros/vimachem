namespace Catalog.API.Application.Interfaces;

public interface IPartyServiceClient {
	Task<PartyDto?> GetByIdAsync(Guid id, CancellationToken ct);
}

public record PartyDto {
	public Guid Id { get; init; }
	public string FirstName { get; init; } = string.Empty;
	public string LastName { get; init; } = string.Empty;
	public List<string> Roles { get; init; } = new();
}
