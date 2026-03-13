namespace Lending.API.HttpClients;

public sealed class CatalogServiceClient : ICatalogServiceClient {
	private readonly HttpClient _httpClient;
	private readonly ILogger<CatalogServiceClient> _logger;

	public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger) {
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task<BookAvailabilityDto?> GetBookAvailabilityAsync(Guid id, CancellationToken ct) {
		var response = await _httpClient.GetAsync($"/api/catalog/books/{id}/availability", ct);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			return null;
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<BookAvailabilityDto>(ct);
	}

	public async Task<bool> ReserveBookAsync(Guid bookId, CancellationToken ct) {
		var response = await _httpClient.PutAsync($"/api/catalog/books/{bookId}/reserve", null, ct);
		return response.IsSuccessStatusCode;
	}

	public async Task<bool> ReleaseBookAsync(Guid bookId, CancellationToken ct) {
		var response = await _httpClient.PutAsync($"/api/catalog/books/{bookId}/release", null, ct);
		return response.IsSuccessStatusCode;
	}
}
