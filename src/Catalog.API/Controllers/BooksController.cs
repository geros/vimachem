using Catalog.API.Application.DTOs;
using Catalog.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/catalog/books")]
[Produces("application/json")]
public class BooksController : ControllerBase {
	private readonly IBookService _service;

	public BooksController(IBookService service) => _service = service;

	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken ct) =>
		Ok(await _service.GetAllAsync(ct));

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
		Ok(await _service.GetByIdAsync(id, ct));

	[HttpGet("search")]
	public async Task<IActionResult> SearchByTitle(
		[FromQuery] string title, CancellationToken ct) =>
		Ok(await _service.SearchByTitleAsync(title, ct));

	[HttpGet("{id:guid}/availability")]
	public async Task<IActionResult> GetAvailability(Guid id, CancellationToken ct) =>
		Ok(await _service.GetAvailabilityAsync(id, ct));

	[HttpPost]
	[ProducesResponseType(typeof(BookResponse), 201)]
	public async Task<IActionResult> Create(
		[FromBody] CreateBookRequest request, CancellationToken ct) {
		var result = await _service.CreateAsync(request, ct);
		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Update(
		Guid id, [FromBody] UpdateBookRequest request, CancellationToken ct) =>
		Ok(await _service.UpdateAsync(id, request, ct));

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
		await _service.DeleteAsync(id, ct);
		return NoContent();
	}

	// Internal endpoints — called by Lending.API
	[HttpPut("{id:guid}/reserve")]
	public async Task<IActionResult> Reserve(Guid id, CancellationToken ct) =>
		Ok(await _service.ReserveAsync(id, ct));

	[HttpPut("{id:guid}/release")]
	public async Task<IActionResult> Release(Guid id, CancellationToken ct) =>
		Ok(await _service.ReleaseAsync(id, ct));
}
