using Microsoft.EntityFrameworkCore;
using Party.API.Domain;
using DomainParty = Party.API.Domain.Party;
using Shared;

namespace Party.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(PartyDbContext context) {
		if (await context.Parties.AnyAsync()) return;

		var parties = new List<DomainParty>();

		// ── Original 4 parties ──────────────────────────────────────────────
		var orwell = new DomainParty("George", "Orwell", "orwell@books.com");
		SetId(orwell, SeedConstants.OrwellId);
		orwell.AssignRole(RoleType.Author);
		parties.Add(orwell);

		var christie = new DomainParty("Agatha", "Christie", "christie@books.com");
		SetId(christie, SeedConstants.ChristieId);
		christie.AssignRole(RoleType.Author);
		parties.Add(christie);

		var doe = new DomainParty("John", "Doe", "john@example.com");
		SetId(doe, SeedConstants.DoeId);
		doe.AssignRole(RoleType.Customer);
		parties.Add(doe);

		var king = new DomainParty("Stephen", "King", "king@books.com");
		SetId(king, SeedConstants.KingId);
		king.AssignRole(RoleType.Author);
		king.AssignRole(RoleType.Customer);
		parties.Add(king);

		// ── 20 additional authors ────────────────────────────────────────────
		var authorEmails = new[] {
			"leo.tolstoy@books.com",
			"jane.austen@books.com",
			"ernest.hemingway@books.com",
			"scott.fitzgerald@books.com",
			"mark.twain@books.com",
			"charles.dickens@books.com",
			"frank.herbert@books.com",
			"ray.bradbury@books.com",
			"ursula.leguin@books.com",
			"philip.dick@books.com",
			"toni.morrison@books.com",
			"gabriel.marquez@books.com",
			"haruki.murakami@books.com",
			"fyodor.dostoevsky@books.com",
			"tolkien@books.com",
			"jk.rowling@books.com",
			"isaac.asimov@books.com",
			"arthur.clarke@books.com",
			"john.steinbeck@books.com",
			"herman.melville@books.com",
		};

		for (var i = 0; i < SeedConstants.AuthorNames.Length; i++) {
			var parts = SeedConstants.AuthorNames[i].Split(' ');
			var firstName = parts[0];
			var lastName = string.Join(' ', parts[1..]);
			var author = new DomainParty(firstName, lastName, authorEmails[i]);
			SetId(author, SeedConstants.AuthorIds[i]);
			author.AssignRole(RoleType.Author);
			parties.Add(author);
		}

		// ── 30 additional customers ──────────────────────────────────────────
		for (var i = 0; i < SeedConstants.CustomerData.Length; i++) {
			var (first, last, email) = SeedConstants.CustomerData[i];
			var customer = new DomainParty(first, last, email);
			SetId(customer, SeedConstants.CustomerIds[i]);
			customer.AssignRole(RoleType.Customer);
			parties.Add(customer);
		}

		context.Parties.AddRange(parties);
		await context.SaveChangesAsync();
	}

	private static void SetId(DomainParty party, Guid id) {
		var entry = party.GetType().GetProperty("Id");
		entry?.SetValue(party, id);
	}
}
