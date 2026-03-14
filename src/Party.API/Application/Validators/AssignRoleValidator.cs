using FluentValidation;
using Party.API.Application.DTOs;

namespace Party.API.Application.Validators;

public class AssignRoleValidator : AbstractValidator<AssignRoleRequest> {
	public AssignRoleValidator() {
		RuleFor(x => x.RoleType)
			.IsInEnum().WithMessage("Invalid role type. Must be Author (0) or Customer (1)");
	}
}
