using Party.API.Application.DTOs;
using Party.API.Domain;

namespace Party.API.Application.Interfaces;

public interface IPartyService {
	Task<IEnumerable<PartyResponse>> GetAllAsync(CancellationToken ct);
	Task<PartyResponse> GetByIdAsync(Guid id, CancellationToken ct);
	Task<PartyResponse> CreateAsync(CreatePartyRequest request, CancellationToken ct);
	Task<PartyResponse> UpdateAsync(Guid id, UpdatePartyRequest request, CancellationToken ct);
	Task DeleteAsync(Guid id, CancellationToken ct);
	Task<PartyResponse> AssignRoleAsync(Guid partyId, RoleType roleType, CancellationToken ct);
	Task<PartyResponse> RemoveRoleAsync(Guid partyId, RoleType roleType, CancellationToken ct);
}
