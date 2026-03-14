using Catalog.API.Application.DTOs;
using FluentValidation;

namespace Catalog.API.Application.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest> {
	public CreateCategoryValidator() {
		RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
	}
}
