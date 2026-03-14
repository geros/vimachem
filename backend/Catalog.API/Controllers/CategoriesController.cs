using Catalog.API.Application.DTOs;
using Catalog.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/catalog/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase {
	private readonly ICategoryService _service;

	public CategoriesController(ICategoryService service) => _service = service;

	[HttpGet]
	public async Task<IActionResult> GetAll(CancellationToken ct) =>
		Ok(await _service.GetAllAsync(ct));

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
		Ok(await _service.GetByIdAsync(id, ct));

	[HttpPost]
	[ProducesResponseType(typeof(CategoryResponse), 201)]
	public async Task<IActionResult> Create(
		[FromBody] CreateCategoryRequest request, CancellationToken ct) {
		var result = await _service.CreateAsync(request, ct);
		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Update(
		Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct) =>
		Ok(await _service.UpdateAsync(id, request, ct));

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
		await _service.DeleteAsync(id, ct);
		return NoContent();
	}
}
