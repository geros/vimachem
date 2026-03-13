namespace Lending.API.HttpClients;

public sealed class PartyServiceClient : IPartyServiceClient {
	private readonly HttpClient _httpClient;
	private readonly ILogger<PartyServiceClient> _logger;

	public PartyServiceClient(HttpClient httpClient, ILogger<PartyServiceClient> logger) {
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task<PartyDto?> GetByIdAsync(Guid id, CancellationToken ct) {
		var response = await _httpClient.GetAsync($"/api/party/{id}", ct);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			return null;
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<PartyDto>(ct);
	}
}
