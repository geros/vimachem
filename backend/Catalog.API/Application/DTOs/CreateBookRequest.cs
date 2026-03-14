namespace Catalog.API.Application.DTOs;

public record CreateBookRequest(
	string Title,
	string ISBN,
	Guid AuthorId,
	Guid CategoryId,
	int TotalCopies = 1
);
