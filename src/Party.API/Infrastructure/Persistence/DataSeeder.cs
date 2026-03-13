using Microsoft.EntityFrameworkCore;
using Party.API.Domain;
using DomainParty = Party.API.Domain.Party;
using Shared;

namespace Party.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(PartyDbContext context) {
		if (await context.Parties.AnyAsync()) return;

		var orwell = new DomainParty("George", "Orwell", "orwell@books.com");
		SetId(orwell, SeedConstants.OrwellId);
		orwell.AssignRole(RoleType.Author);

		var christie = new DomainParty("Agatha", "Christie", "christie@books.com");
		SetId(christie, SeedConstants.ChristieId);
		christie.AssignRole(RoleType.Author);

		var doe = new DomainParty("John", "Doe", "john@example.com");
		SetId(doe, SeedConstants.DoeId);
		doe.AssignRole(RoleType.Customer);

		var king = new DomainParty("Stephen", "King", "king@books.com");
		SetId(king, SeedConstants.KingId);
		king.AssignRole(RoleType.Author);
		king.AssignRole(RoleType.Customer);

		context.Parties.AddRange(orwell, christie, doe, king);
		await context.SaveChangesAsync();
	}

	private static void SetId(DomainParty party, Guid id) {
		var entry = party.GetType().GetProperty("Id");
		entry?.SetValue(party, id);
	}
}
