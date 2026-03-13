using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Catalog.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(CatalogDbContext context) {
		if (await context.Categories.AnyAsync()) return;

		// Categories
		var fiction = new Category("Fiction");
		var mystery = new Category("Mystery");
		context.Categories.AddRange(fiction, mystery);
		await context.SaveChangesAsync();

		// Set deterministic IDs using EF Core's entry modification
		context.Entry(fiction).Property(c => c.Id).CurrentValue = SeedConstants.FictionId;
		context.Entry(mystery).Property(c => c.Id).CurrentValue = SeedConstants.MysteryId;
		await context.SaveChangesAsync();

		// Books — AuthorIds reference Party.API's SeedConstants
		var book1984 = new Book("1984", "9780451524935",
			SeedConstants.OrwellId, "George Orwell",
			SeedConstants.FictionId, 3);
		var bookAnimalFarm = new Book("Animal Farm", "9780451526342",
			SeedConstants.OrwellId, "George Orwell",
			SeedConstants.FictionId, 2);
		var bookOrientExpress = new Book("Murder on the Orient Express", "9780062693662",
			SeedConstants.ChristieId, "Agatha Christie",
			SeedConstants.MysteryId, 2);
		var bookShining = new Book("The Shining", "9780307743657",
			SeedConstants.KingId, "Stephen King",
			SeedConstants.MysteryId, 1);

		context.Books.AddRange(book1984, bookAnimalFarm, bookOrientExpress, bookShining);
		await context.SaveChangesAsync();

		// Set deterministic IDs for books
		context.Entry(book1984).Property(b => b.Id).CurrentValue = SeedConstants.Book1984Id;
		context.Entry(bookAnimalFarm).Property(b => b.Id).CurrentValue = SeedConstants.BookAnimalFarmId;
		context.Entry(bookOrientExpress).Property(b => b.Id).CurrentValue = SeedConstants.BookOrientExpressId;
		context.Entry(bookShining).Property(b => b.Id).CurrentValue = SeedConstants.BookShiningId;
		await context.SaveChangesAsync();
	}
}
