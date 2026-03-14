using FluentValidation;
using Party.API.Application.DTOs;

namespace Party.API.Application.Validators;

public class CreatePartyValidator : AbstractValidator<CreatePartyRequest> {
	public CreatePartyValidator() {
		RuleFor(x => x.FirstName)
			.NotEmpty().WithMessage("First name is required")
			.MinimumLength(2).WithMessage("First name must be at least 2 characters");

		RuleFor(x => x.LastName)
			.NotEmpty().WithMessage("Last name is required")
			.MinimumLength(2).WithMessage("Last name must be at least 2 characters");

		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required")
			.EmailAddress().WithMessage("Invalid email format");
	}
}
