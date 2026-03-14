using Microsoft.EntityFrameworkCore;
using Party.API.Application.DTOs;
using Party.API.Application.Interfaces;
using Party.API.Domain;
using Party.API.Domain.Exceptions;
using Party.API.Infrastructure.Persistence;
using Shared.Events;

namespace Party.API.Application.Services;

public sealed class PartyService : IPartyService {
	private readonly PartyDbContext _context;
	private readonly IEventPublisher _publisher;

	public PartyService(PartyDbContext context, IEventPublisher publisher) {
		_context = context;
		_publisher = publisher;
	}

	public async Task<IEnumerable<PartyResponse>> GetAllAsync(CancellationToken ct) {
		var parties = await _context.Parties
			.AsNoTracking()
			.Include(p => p.Roles)
			.ToListAsync(ct);

		return parties.Select(MapToResponse);
	}

	public async Task<PartyResponse> GetByIdAsync(Guid id, CancellationToken ct) {
		var party = await _context.Parties
			.AsNoTracking()
			.Include(p => p.Roles)
			.FirstOrDefaultAsync(p => p.Id == id, ct)
			?? throw new NotFoundException("Party", id);

		return MapToResponse(party);
	}

	public async Task<PartyResponse> CreateAsync(CreatePartyRequest request, CancellationToken ct) {
		var party = new Domain.Party(request.FirstName, request.LastName, request.Email);

		_context.Parties.Add(party);
		await _context.SaveChangesAsync(ct);

		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "PartyCreated",
			EntityType = "Party",
			EntityId = party.Id.ToString(),
			Action = "Created",
			RelatedEntityIds = new Dictionary<string, string>(),
			Payload = new {
				party.Id,
				party.FirstName,
				party.LastName,
				party.Email,
				Roles = party.Roles.Select(r => r.RoleType.ToString()).ToList()
			}
		}, ct);

		return MapToResponse(party);
	}

	public async Task<PartyResponse> UpdateAsync(Guid id, UpdatePartyRequest request, CancellationToken ct) {
		var party = await _context.Parties
			.Include(p => p.Roles)
			.FirstOrDefaultAsync(p => p.Id == id, ct)
			?? throw new NotFoundException("Party", id);

		party.Update(request.FirstName, request.LastName, request.Email);
		await _context.SaveChangesAsync(ct);

		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "PartyUpdated",
			EntityType = "Party",
			EntityId = party.Id.ToString(),
			Action = "Updated",
			RelatedEntityIds = new Dictionary<string, string>(),
			Payload = new {
				party.Id,
				party.FirstName,
				party.LastName,
				party.Email,
				Roles = party.Roles.Select(r => r.RoleType.ToString()).ToList()
			}
		}, ct);

		return MapToResponse(party);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct) {
		var party = await _context.Parties
			.FirstOrDefaultAsync(p => p.Id == id, ct)
			?? throw new NotFoundException("Party", id);

		_context.Parties.Remove(party);
		await _context.SaveChangesAsync(ct);

		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "PartyDeleted",
			EntityType = "Party",
			EntityId = party.Id.ToString(),
			Action = "Deleted",
			RelatedEntityIds = new Dictionary<string, string>(),
			Payload = new {
				party.Id,
				party.FirstName,
				party.LastName,
				party.Email
			}
		}, ct);
	}

	public async Task<PartyResponse> AssignRoleAsync(Guid partyId, RoleType roleType, CancellationToken ct) {
		var party = await _context.Parties
			.Include(p => p.Roles)
			.FirstOrDefaultAsync(p => p.Id == partyId, ct)
			?? throw new NotFoundException("Party", partyId);

		var role = party.AssignRole(roleType);
		_context.PartyRoles.Add(role);
		await _context.SaveChangesAsync(ct);

		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "RoleAssigned",
			EntityType = "Party",
			EntityId = party.Id.ToString(),
			Action = "RoleAssigned",
			RelatedEntityIds = new Dictionary<string, string> { ["RoleType"] = roleType.ToString() },
			Payload = new {
				party.Id,
				RoleType = roleType.ToString(),
				Roles = party.Roles.Select(r => r.RoleType.ToString()).ToList()
			}
		}, ct);

		return MapToResponse(party);
	}

	public async Task<PartyResponse> RemoveRoleAsync(Guid partyId, RoleType roleType, CancellationToken ct) {
		var party = await _context.Parties
			.Include(p => p.Roles)
			.FirstOrDefaultAsync(p => p.Id == partyId, ct)
			?? throw new NotFoundException("Party", partyId);

		party.RemoveRole(roleType);
		await _context.SaveChangesAsync(ct);

		await _publisher.PublishAsync(new IntegrationEvent {
			EventType = "RoleRemoved",
			EntityType = "Party",
			EntityId = party.Id.ToString(),
			Action = "RoleRemoved",
			RelatedEntityIds = new Dictionary<string, string> { ["RoleType"] = roleType.ToString() },
			Payload = new {
				party.Id,
				RoleType = roleType.ToString(),
				Roles = party.Roles.Select(r => r.RoleType.ToString()).ToList()
			}
		}, ct);

		return MapToResponse(party);
	}

	private static PartyResponse MapToResponse(Domain.Party party) {
		return new PartyResponse {
			Id = party.Id,
			FirstName = party.FirstName,
			LastName = party.LastName,
			Email = party.Email,
			Roles = party.Roles.Select(r => r.RoleType.ToString()).ToList(),
			CreatedAt = party.CreatedAt,
			UpdatedAt = party.UpdatedAt
		};
	}
}
