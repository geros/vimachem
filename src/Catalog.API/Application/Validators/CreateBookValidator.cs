using Catalog.API.Application.DTOs;
using FluentValidation;

namespace Catalog.API.Application.Validators;

public class CreateBookValidator : AbstractValidator<CreateBookRequest> {
	public CreateBookValidator() {
		RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
		RuleFor(x => x.ISBN).NotEmpty().Matches(@"^\d{10}(\d{3})?$")
			.WithMessage("ISBN must be 10 or 13 digits");
		RuleFor(x => x.AuthorId).NotEmpty();
		RuleFor(x => x.CategoryId).NotEmpty();
		RuleFor(x => x.TotalCopies).GreaterThan(0);
	}
}
