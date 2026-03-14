using Catalog.API.Application.DTOs;
using FluentValidation;

namespace Catalog.API.Application.Validators;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest> {
	public UpdateCategoryValidator() {
		RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
	}
}
