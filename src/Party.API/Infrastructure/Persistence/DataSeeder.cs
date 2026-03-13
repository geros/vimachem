using Microsoft.EntityFrameworkCore;
using Party.API.Domain;
using Shared;

namespace Party.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(PartyDbContext context) {
		if (await context.Parties.AnyAsync()) return;

		var orwell = new Party("George", "Orwell", "orwell@books.com");
		SetId(orwell, SeedConstants.OrwellId);
		orwell.AssignRole(RoleType.Author);

		var christie = new Party("Agatha", "Christie", "christie@books.com");
		SetId(christie, SeedConstants.ChristieId);
		christie.AssignRole(RoleType.Author);

		var doe = new Party("John", "Doe", "john@example.com");
		SetId(doe, SeedConstants.DoeId);
		doe.AssignRole(RoleType.Customer);

		var king = new Party("Stephen", "King", "king@books.com");
		SetId(king, SeedConstants.KingId);
		king.AssignRole(RoleType.Author);
		king.AssignRole(RoleType.Customer);

		context.Parties.AddRange(orwell, christie, doe, king);
		await context.SaveChangesAsync();
	}

	private static void SetId(Party party, Guid id) {
		var entry = party.GetType().GetProperty("Id");
		entry?.SetValue(party, id);
	}
}
