using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Catalog.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(CatalogDbContext context) {
		if (await context.Categories.AnyAsync()) return;

		// Categories - create with specific IDs
		var fiction = new Category("Fiction") { Id = SeedConstants.FictionId };
		var mystery = new Category("Mystery") { Id = SeedConstants.MysteryId };
		context.Categories.AddRange(fiction, mystery);
		await context.SaveChangesAsync();

		// Books — AuthorIds reference Party.API's SeedConstants
		var book1984 = new Book("1984", "9780451524935",
			SeedConstants.OrwellId, "George Orwell",
			SeedConstants.FictionId, 3) { Id = SeedConstants.Book1984Id };
		var bookAnimalFarm = new Book("Animal Farm", "9780451526342",
			SeedConstants.OrwellId, "George Orwell",
			SeedConstants.FictionId, 2) { Id = SeedConstants.BookAnimalFarmId };
		var bookOrientExpress = new Book("Murder on the Orient Express", "9780062693662",
			SeedConstants.ChristieId, "Agatha Christie",
			SeedConstants.MysteryId, 2) { Id = SeedConstants.BookOrientExpressId };
		var bookShining = new Book("The Shining", "9780307743657",
			SeedConstants.KingId, "Stephen King",
			SeedConstants.MysteryId, 1) { Id = SeedConstants.BookShiningId };

		context.Books.AddRange(book1984, bookAnimalFarm, bookOrientExpress, bookShining);
		await context.SaveChangesAsync();
	}
}
