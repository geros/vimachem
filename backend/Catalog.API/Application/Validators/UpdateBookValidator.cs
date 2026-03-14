using Catalog.API.Application.DTOs;
using FluentValidation;

namespace Catalog.API.Application.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest> {
	public UpdateBookValidator() {
		RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
		RuleFor(x => x.CategoryId).NotEmpty();
		RuleFor(x => x.TotalCopies).GreaterThan(0);
	}
}
