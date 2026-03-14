namespace Party.API.Application.DTOs;

public record UpdatePartyRequest(
	string FirstName,
	string LastName,
	string Email
);
