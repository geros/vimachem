using Lending.API.Application.DTOs;
using Lending.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lending.API.Controllers;

[ApiController]
[Route("api/lending")]
[Produces("application/json")]
public class BorrowingsController : ControllerBase {
	private readonly IBorrowingService _service;

	public BorrowingsController(IBorrowingService service) => _service = service;

	[HttpPost("borrow")]
	[ProducesResponseType(typeof(BorrowingResponse), 201)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Borrow(
		[FromBody] BorrowBookRequest request, CancellationToken ct) {
		var result = await _service.BorrowAsync(request, ct);
		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
	}

	[HttpPost("{bookId:guid}/return")]
	[ProducesResponseType(typeof(BorrowingResponse), 200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Return(
		Guid bookId, [FromBody] ReturnBookRequest request, CancellationToken ct) {
		var result = await _service.ReturnAsync(bookId, request.CustomerId, ct);
		return Ok(result);
	}

	[HttpGet("summary")]
	[ProducesResponseType(typeof(IEnumerable<BorrowedBookSummaryResponse>), 200)]
	public async Task<IActionResult> GetSummary(CancellationToken ct) =>
		Ok(await _service.GetBorrowedSummaryAsync(ct));

	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(BorrowingResponse), 200)]
	public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
		Ok(await _service.GetByIdAsync(id, ct));

	[HttpGet("by-customer/{customerId:guid}")]
	[ProducesResponseType(typeof(IEnumerable<BorrowingResponse>), 200)]
	public async Task<IActionResult> GetByCustomer(
		Guid customerId, CancellationToken ct) =>
		Ok(await _service.GetByCustomerAsync(customerId, ct));

	[HttpGet("by-book/{bookId:guid}")]
	[ProducesResponseType(typeof(IEnumerable<BorrowingResponse>), 200)]
	public async Task<IActionResult> GetByBook(
		Guid bookId, CancellationToken ct) =>
		Ok(await _service.GetByBookAsync(bookId, ct));
}
