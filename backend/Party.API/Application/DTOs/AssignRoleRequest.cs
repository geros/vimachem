using Party.API.Domain;

namespace Party.API.Application.DTOs;

public record AssignRoleRequest(
	RoleType RoleType
);
