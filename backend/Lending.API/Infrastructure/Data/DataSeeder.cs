using Lending.API.Domain;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Lending.API.Infrastructure.Data;

public static class DataSeeder {
	// All seeded book IDs with their titles
	private static readonly (Guid Id, string Title)[] Books = BuildBooks();

	// All seeded customer IDs with full names
	private static readonly (Guid Id, string Name)[] Customers = BuildCustomers();

	public static async Task SeedAsync(LendingDbContext context) {
		if (await context.Borrowings.AnyAsync()) return;

		var borrowings = new List<Borrowing>();
		var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var borrowingIndex = 0;

		// 150 returned borrowings
		for (var i = 0; i < 150; i++) {
			var customer = Customers[borrowingIndex % Customers.Length];
			var book = Books[(borrowingIndex * 3) % Books.Length];
			var borrowedAt = baseDate.AddDays(borrowingIndex * 4);
			var returnedAt = borrowedAt.AddDays(7 + (borrowingIndex % 21));

			var b = new Borrowing(book.Id, book.Title, customer.Id, customer.Name);
			SetDate(b, "BorrowedAt", borrowedAt);
			SetDate(b, "ReturnedAt", returnedAt);
			borrowings.Add(b);
			borrowingIndex++;
		}

		// 50 active borrowings
		for (var i = 0; i < 50; i++) {
			var customer = Customers[(borrowingIndex + i) % Customers.Length];
			var book = Books[((borrowingIndex + i) * 7) % Books.Length];
			var borrowedAt = baseDate.AddDays(600 + i * 2);

			var b = new Borrowing(book.Id, book.Title, customer.Id, customer.Name);
			SetDate(b, "BorrowedAt", borrowedAt);
			borrowings.Add(b);
		}

		context.Borrowings.AddRange(borrowings);
		await context.SaveChangesAsync();
	}

	private static void SetDate(Borrowing borrowing, string propertyName, DateTime value) {
		var prop = typeof(Borrowing).GetProperty(propertyName);
		prop?.SetValue(borrowing, value);
	}

	private static (Guid, string)[] BuildBooks() {
		var list = new List<(Guid, string)> {
			(SeedConstants.Book1984Id,         "1984"),
			(SeedConstants.BookAnimalFarmId,   "Animal Farm"),
			(SeedConstants.BookOrientExpressId,"Murder on the Orient Express"),
			(SeedConstants.BookShiningId,      "The Shining"),
		};

		var extraTitles = new[] {
			"War and Peace", "Anna Karenina", "Pride and Prejudice", "Sense and Sensibility",
			"A Farewell to Arms", "The Old Man and the Sea", "The Great Gatsby", "Huckleberry Finn",
			"Great Expectations", "A Tale of Two Cities", "Of Mice and Men", "The Grapes of Wrath",
			"Death on the Nile", "The ABC Murders", "And Then There Were None", "It",
			"Misery", "The Metamorphosis", "Crime and Punishment", "The Brothers Karamazov",
			"Beloved", "Moby Dick", "One Hundred Years of Solitude", "Norwegian Wood",
			"Dune", "Dune Messiah", "Fahrenheit 451", "The Martian Chronicles",
			"The Left Hand of Darkness", "The Dispossessed", "Do Androids Dream of Electric Sheep?",
			"The Man in the High Castle", "Foundation", "I, Robot", "Foundation and Empire",
			"2001: A Space Odyssey", "Childhood's End", "Rendezvous with Rama",
			"Brave New World", "Slaughterhouse-Five",
			"The Fellowship of the Ring", "The Two Towers", "The Return of the King", "The Hobbit",
			"Harry Potter and the Philosopher's Stone", "Harry Potter and the Chamber of Secrets",
			"Harry Potter and the Prisoner of Azkaban", "Harry Potter and the Goblet of Fire",
			"Harry Potter and the Order of the Phoenix", "Harry Potter and the Half-Blood Prince",
			"Harry Potter and the Deathly Hallows", "Pet Sematary",
			"The Dark Tower: The Gunslinger", "The Stand",
			"Carrie", "Doctor Sleep", "East of Eden", "For Whom the Bell Tolls",
			"The Sun Also Rises", "Tender Is the Night", "This Side of Paradise",
			"Mrs Dalloway", "To the Lighthouse", "Ulysses", "The Sound and the Fury",
			"As I Lay Dying",
			"My Experiments with Truth", "Long Walk to Freedom", "The Story of My Life",
			"I Know Why the Caged Bird Sings", "Confessions", "The Diary of a Young Girl",
			"Night", "Becoming", "Born a Crime", "Open: An Autobiography",
			"Sapiens", "Homo Deus", "Guns, Germs, and Steel", "The Silk Roads",
			"A Short History of Nearly Everything", "The Art of War", "The Prince",
			"Democracy in America", "The Rise and Fall of the Third Reich", "1776",
			"Emma", "Persuasion", "Northanger Abbey", "Wuthering Heights", "Jane Eyre",
			"Rebecca", "Doctor Zhivago", "Love in the Time of Cholera",
			"The Notebook", "Kafka on the Shore",
		};

		for (var i = 0; i < extraTitles.Length; i++)
			list.Add((SeedConstants.ExtraBookIds[i], extraTitles[i]));

		return list.ToArray();
	}

	private static (Guid, string)[] BuildCustomers() {
		var list = new List<(Guid, string)> {
			(SeedConstants.DoeId,  "John Doe"),
			(SeedConstants.KingId, "Stephen King"),
		};
		for (var i = 0; i < SeedConstants.CustomerData.Length; i++) {
			var (first, last, _) = SeedConstants.CustomerData[i];
			list.Add((SeedConstants.CustomerIds[i], $"{first} {last}"));
		}
		return list.ToArray();
	}
}
