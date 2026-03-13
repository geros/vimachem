namespace Lending.API.Application.DTOs;

public record BorrowBookRequest(
	Guid BookId,
	Guid CustomerId
);
