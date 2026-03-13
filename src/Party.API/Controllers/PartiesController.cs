using Microsoft.AspNetCore.Mvc;
using Party.API.Application.DTOs;
using Party.API.Application.Interfaces;
using Party.API.Domain;

namespace Party.API.Controllers;

[ApiController]
[Route("api/parties")]
[Produces("application/json")]
public class PartiesController : ControllerBase {
	private readonly IPartyService _service;

	public PartiesController(IPartyService service) => _service = service;

	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<PartyResponse>), 200)]
	public async Task<IActionResult> GetAll(CancellationToken ct) {
		var result = await _service.GetAllAsync(ct);
		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(PartyResponse), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetById(Guid id, CancellationToken ct) {
		var result = await _service.GetByIdAsync(id, ct);
		return Ok(result);
	}

	[HttpPost]
	[ProducesResponseType(typeof(PartyResponse), 201)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Create(
		[FromBody] CreatePartyRequest request, CancellationToken ct) {
		var result = await _service.CreateAsync(request, ct);
		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
	}

	[HttpPut("{id:guid}")]
	[ProducesResponseType(typeof(PartyResponse), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Update(
		Guid id, [FromBody] UpdatePartyRequest request, CancellationToken ct) {
		var result = await _service.UpdateAsync(id, request, ct);
		return Ok(result);
	}

	[HttpDelete("{id:guid}")]
	[ProducesResponseType(204)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
		await _service.DeleteAsync(id, ct);
		return NoContent();
	}

	[HttpPost("{id:guid}/roles")]
	[ProducesResponseType(typeof(PartyResponse), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> AssignRole(
		Guid id, [FromBody] AssignRoleRequest request, CancellationToken ct) {
		var result = await _service.AssignRoleAsync(id, request.RoleType, ct);
		return Ok(result);
	}

	[HttpDelete("{id:guid}/roles/{roleType}")]
	[ProducesResponseType(typeof(PartyResponse), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> RemoveRole(
		Guid id, RoleType roleType, CancellationToken ct) {
		var result = await _service.RemoveRoleAsync(id, roleType, ct);
		return Ok(result);
	}
}
