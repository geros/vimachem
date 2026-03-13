using Party.API.Domain.Exceptions;

namespace Party.API.Domain;

public class Party {
	public Guid Id { get; private set; } = Guid.NewGuid();
	public string FirstName { get; private set; } = string.Empty;
	public string LastName { get; private set; } = string.Empty;
	public string Email { get; private set; } = string.Empty;
	public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; private set; }

	private readonly List<PartyRole> _roles = new();
	public IReadOnlyCollection<PartyRole> Roles => _roles.AsReadOnly();

	private Party() { }

	public Party(string firstName, string lastName, string email) {
		FirstName = firstName;
		LastName = lastName;
		Email = email;
	}

	public void Update(string firstName, string lastName, string email) {
		FirstName = firstName;
		LastName = lastName;
		Email = email;
		UpdatedAt = DateTime.UtcNow;
	}

	public void AssignRole(RoleType roleType) {
		if (_roles.Any(r => r.RoleType == roleType))
			throw new DomainException($"Party already has role {roleType}");
		_roles.Add(new PartyRole(Id, roleType));
	}

	public void RemoveRole(RoleType roleType) {
		var role = _roles.FirstOrDefault(r => r.RoleType == roleType)
			?? throw new DomainException($"Party does not have role {roleType}");
		_roles.Remove(role);
	}

	public bool HasRole(RoleType roleType) => _roles.Any(r => r.RoleType == roleType);
}
