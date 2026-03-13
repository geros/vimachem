namespace Party.API.Application.DTOs;

public record CreatePartyRequest(
	string FirstName,
	string LastName,
	string Email
);
