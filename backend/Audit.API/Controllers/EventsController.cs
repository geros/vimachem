using Microsoft.AspNetCore.Mvc;
using Audit.API.Application;

namespace Audit.API.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase {
	private readonly IEventRepository _repository;

	public EventsController(IEventRepository repository) => _repository = repository;

	[HttpGet("parties/{partyId}")]
	[ProducesResponseType(typeof(PagedResponse<EventResponse>), 200)]
	public async Task<IActionResult> GetPartyEvents(
		string partyId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken ct = default) {
		if (page < 1) page = 1;
		if (pageSize < 1 || pageSize > 100) pageSize = 20;

		var result = await _repository.GetPartyEventsAsync(partyId, page, pageSize, ct);
		return Ok(result);
	}

	[HttpGet("books/{bookId}")]
	[ProducesResponseType(typeof(PagedResponse<EventResponse>), 200)]
	public async Task<IActionResult> GetBookEvents(
		string bookId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken ct = default) {
		if (page < 1) page = 1;
		if (pageSize < 1 || pageSize > 100) pageSize = 20;

		var result = await _repository.GetBookEventsAsync(bookId, page, pageSize, ct);
		return Ok(result);
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResponse<EventResponse>), 200)]
	public async Task<IActionResult> GetAllEvents(
		[FromQuery] string? entityType = null,
		[FromQuery] string? action = null,
		[FromQuery] string? entityId = null,
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken ct = default) {
		if (page < 1) page = 1;
		if (pageSize < 1 || pageSize > 100) pageSize = 20;

		var filter = new EventFilter {
			EntityType = entityType,
			Action = action,
			EntityId = entityId,
			From = from,
			To = to
		};

		var result = await _repository.GetAllEventsAsync(filter, page, pageSize, ct);
		return Ok(result);
	}
}
