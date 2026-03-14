namespace Party.API.Application.DTOs;

public record PartyResponse {
	public Guid Id { get; init; }
	public string FirstName { get; init; } = string.Empty;
	public string LastName { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public List<string> Roles { get; init; } = new();
	public DateTime CreatedAt { get; init; }
	public DateTime? UpdatedAt { get; init; }
}
