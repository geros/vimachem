using Catalog.API.Application.Interfaces;
using Catalog.API.Domain.Exceptions;
using System.Net.Http.Json;

namespace Catalog.API.HttpClients;

public sealed class PartyServiceClient : IPartyServiceClient {
	private readonly HttpClient _httpClient;
	private readonly ILogger<PartyServiceClient> _logger;

	public PartyServiceClient(HttpClient httpClient, ILogger<PartyServiceClient> logger) {
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task<PartyDto?> GetByIdAsync(Guid id, CancellationToken ct) {
		try {
			var response = await _httpClient.GetAsync($"/api/parties/{id}", ct);
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				return null;
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadFromJsonAsync<PartyDto>(ct);
		} catch (HttpRequestException ex) {
			_logger.LogError(ex, "Failed to reach Party.API for party {PartyId}", id);
			throw new DomainException("Party service is currently unavailable");
		}
	}
}
