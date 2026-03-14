using FluentValidation;
using Party.API.Application.DTOs;
using Party.API.Domain;

namespace Party.API.Application.Validators;

public class AssignRoleValidator : AbstractValidator<AssignRoleRequest> {
	public AssignRoleValidator() {
		RuleFor(x => x.RoleType)
			.Must(IsValidRoleType)
			.WithMessage("This role does not exist. Valid roles are: Author (0) or Customer (1)");
	}

	private static bool IsValidRoleType(RoleType roleType) {
		return roleType is RoleType.Author or RoleType.Customer;
	}
}
