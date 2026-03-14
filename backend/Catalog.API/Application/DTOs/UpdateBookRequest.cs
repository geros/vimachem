namespace Catalog.API.Application.DTOs;

public record UpdateBookRequest(
	string Title,
	Guid CategoryId,
	int TotalCopies
);
