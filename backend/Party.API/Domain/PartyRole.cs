namespace Party.API.Domain;

public class PartyRole {
	public Guid Id { get; private set; } = Guid.NewGuid();
	public Guid PartyId { get; private set; }
	public RoleType RoleType { get; private set; }
	public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

	private PartyRole() { }

	public PartyRole(Guid partyId, RoleType roleType) {
		PartyId = partyId;
		RoleType = roleType;
	}
}
