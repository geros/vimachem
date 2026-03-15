using MongoDB.Bson;
using MongoDB.Driver;
using Audit.API.Domain;
using Shared;

namespace Audit.API.Infrastructure;

public static class DataSeeder {
	private static readonly (Guid Id, string Name, string Type)[] AllParties = BuildParties();
	private static readonly (Guid Id, string Title, Guid AuthorId, string AuthorName)[] AllBooks = BuildBooks();

	public static async Task SeedAsync(IMongoDatabase database) {
		var collection = database.GetCollection<DomainEvent>("events");
		if (await collection.CountDocumentsAsync(FilterDefinition<DomainEvent>.Empty) > 0) return;

		var events = new List<DomainEvent>();
		var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// ── PartyCreated events (54 parties) ─────────────────────────────────
		for (var i = 0; i < AllParties.Length; i++) {
			var (id, name, roleType) = AllParties[i];
			events.Add(new DomainEvent {
				EventType = "party.created",
				EntityType = "Party",
				EntityId = id.ToString(),
				Action = "Created",
				RelatedEntityIds = new Dictionary<string, string>(),
				Payload = new BsonDocument {
					["id"] = id.ToString(),
					["name"] = name,
					["roleType"] = roleType
				},
				Timestamp = baseDate.AddHours(i * 2)
			});
		}

		// ── BookCreated events (100 books) ───────────────────────────────────
		for (var i = 0; i < AllBooks.Length; i++) {
			var (id, title, authorId, authorName) = AllBooks[i];
			events.Add(new DomainEvent {
				EventType = "book.created",
				EntityType = "Book",
				EntityId = id.ToString(),
				Action = "Created",
				RelatedEntityIds = new Dictionary<string, string> {
					["AuthorId"] = authorId.ToString()
				},
				Payload = new BsonDocument {
					["id"] = id.ToString(),
					["title"] = title,
					["authorId"] = authorId.ToString(),
					["authorName"] = authorName
				},
				Timestamp = baseDate.AddDays(30).AddHours(i)
			});
		}

		// ── BookBorrowed events (200 borrowings) ─────────────────────────────
		var customers = BuildCustomers();
		var borrowDate = baseDate.AddDays(60);
		for (var i = 0; i < 200; i++) {
			var customer = customers[i % customers.Length];
			var book = AllBooks[(i * 3) % AllBooks.Length];
			var borrowedAt = borrowDate.AddDays(i * 4);
			var borrowingId = new Guid(i + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5);

			events.Add(new DomainEvent {
				EventType = "book.borrowed",
				EntityType = "Borrowing",
				EntityId = borrowingId.ToString(),
				Action = "Borrowed",
				RelatedEntityIds = new Dictionary<string, string> {
					["BookId"] = book.Id.ToString(),
					["CustomerId"] = customer.Id.ToString()
				},
				Payload = new BsonDocument {
					["borrowingId"] = borrowingId.ToString(),
					["bookId"] = book.Id.ToString(),
					["bookTitle"] = book.Title,
					["customerId"] = customer.Id.ToString(),
					["customerName"] = customer.Name
				},
				Timestamp = borrowedAt
			});

			// 150 of 200 are returned
			if (i < 150) {
				var returnedAt = borrowedAt.AddDays(7 + (i % 21));
				events.Add(new DomainEvent {
					EventType = "book.returned",
					EntityType = "Borrowing",
					EntityId = borrowingId.ToString(),
					Action = "Returned",
					RelatedEntityIds = new Dictionary<string, string> {
						["BookId"] = book.Id.ToString(),
						["CustomerId"] = customer.Id.ToString()
					},
					Payload = new BsonDocument {
						["borrowingId"] = borrowingId.ToString(),
						["bookId"] = book.Id.ToString(),
						["bookTitle"] = book.Title,
						["customerId"] = customer.Id.ToString(),
						["customerName"] = customer.Name
					},
					Timestamp = returnedAt
				});
			}
		}

		await collection.InsertManyAsync(events);
	}

	private static (Guid Id, string Name, string Type)[] BuildParties() {
		var list = new List<(Guid, string, string)> {
			(SeedConstants.OrwellId,  "George Orwell",   "Author"),
			(SeedConstants.ChristieId,"Agatha Christie",  "Author"),
			(SeedConstants.DoeId,     "John Doe",         "Customer"),
			(SeedConstants.KingId,    "Stephen King",     "Author,Customer"),
		};
		for (var i = 0; i < SeedConstants.AuthorNames.Length; i++)
			list.Add((SeedConstants.AuthorIds[i], SeedConstants.AuthorNames[i], "Author"));
		for (var i = 0; i < SeedConstants.CustomerData.Length; i++) {
			var (first, last, _) = SeedConstants.CustomerData[i];
			list.Add((SeedConstants.CustomerIds[i], $"{first} {last}", "Customer"));
		}
		return list.ToArray();
	}

	private static (Guid Id, string Name)[] BuildCustomers() {
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

	private static (Guid Id, string Title, Guid AuthorId, string AuthorName)[] BuildBooks() {
		var a = SeedConstants.AuthorIds;
		var n = SeedConstants.AuthorNames;
		var e = SeedConstants.ExtraBookIds;
		var orw = SeedConstants.OrwellId;
		var chr = SeedConstants.ChristieId;
		var kng = SeedConstants.KingId;

		return [
			(SeedConstants.Book1984Id,           "1984",                                  orw,   "George Orwell"),
			(SeedConstants.BookAnimalFarmId,      "Animal Farm",                           orw,   "George Orwell"),
			(SeedConstants.BookOrientExpressId,   "Murder on the Orient Express",          chr,   "Agatha Christie"),
			(SeedConstants.BookShiningId,         "The Shining",                           kng,   "Stephen King"),
			(e[0],  "War and Peace",                             a[0],  n[0]),
			(e[1],  "Anna Karenina",                             a[0],  n[0]),
			(e[2],  "Pride and Prejudice",                       a[1],  n[1]),
			(e[3],  "Sense and Sensibility",                     a[1],  n[1]),
			(e[4],  "A Farewell to Arms",                        a[2],  n[2]),
			(e[5],  "The Old Man and the Sea",                   a[2],  n[2]),
			(e[6],  "The Great Gatsby",                          a[3],  n[3]),
			(e[7],  "Huckleberry Finn",                          a[4],  n[4]),
			(e[8],  "Great Expectations",                        a[5],  n[5]),
			(e[9],  "A Tale of Two Cities",                      a[5],  n[5]),
			(e[10], "Of Mice and Men",                           a[18], n[18]),
			(e[11], "The Grapes of Wrath",                       a[18], n[18]),
			(e[12], "Death on the Nile",                         chr,   "Agatha Christie"),
			(e[13], "The ABC Murders",                           chr,   "Agatha Christie"),
			(e[14], "And Then There Were None",                  chr,   "Agatha Christie"),
			(e[15], "It",                                        kng,   "Stephen King"),
			(e[16], "Misery",                                    kng,   "Stephen King"),
			(e[17], "The Metamorphosis",                         a[7],  n[7]),
			(e[18], "Crime and Punishment",                      a[13], n[13]),
			(e[19], "The Brothers Karamazov",                    a[13], n[13]),
			(e[20], "Beloved",                                   a[10], n[10]),
			(e[21], "Moby Dick",                                 a[19], n[19]),
			(e[22], "One Hundred Years of Solitude",             a[11], n[11]),
			(e[23], "Norwegian Wood",                            a[12], n[12]),
			(e[24], "Dune",                                      a[6],  n[6]),
			(e[25], "Dune Messiah",                              a[6],  n[6]),
			(e[26], "Fahrenheit 451",                            a[7],  n[7]),
			(e[27], "The Martian Chronicles",                    a[7],  n[7]),
			(e[28], "The Left Hand of Darkness",                 a[8],  n[8]),
			(e[29], "The Dispossessed",                          a[8],  n[8]),
			(e[30], "Do Androids Dream of Electric Sheep?",      a[9],  n[9]),
			(e[31], "The Man in the High Castle",                a[9],  n[9]),
			(e[32], "Foundation",                                a[16], n[16]),
			(e[33], "I, Robot",                                  a[16], n[16]),
			(e[34], "Foundation and Empire",                     a[16], n[16]),
			(e[35], "2001: A Space Odyssey",                     a[17], n[17]),
			(e[36], "Childhood's End",                           a[17], n[17]),
			(e[37], "Rendezvous with Rama",                      a[17], n[17]),
			(e[38], "Brave New World",                           a[2],  n[2]),
			(e[39], "Slaughterhouse-Five",                       a[4],  n[4]),
			(e[40], "The Fellowship of the Ring",                a[14], n[14]),
			(e[41], "The Two Towers",                            a[14], n[14]),
			(e[42], "The Return of the King",                    a[14], n[14]),
			(e[43], "The Hobbit",                                a[14], n[14]),
			(e[44], "Harry Potter and the Philosopher's Stone",  a[15], n[15]),
			(e[45], "Harry Potter and the Chamber of Secrets",   a[15], n[15]),
			(e[46], "Harry Potter and the Prisoner of Azkaban",  a[15], n[15]),
			(e[47], "Harry Potter and the Goblet of Fire",       a[15], n[15]),
			(e[48], "Harry Potter and the Order of the Phoenix", a[15], n[15]),
			(e[49], "Harry Potter and the Half-Blood Prince",    a[15], n[15]),
			(e[50], "Harry Potter and the Deathly Hallows",      a[15], n[15]),
			(e[51], "Pet Sematary",                              kng,   "Stephen King"),
			(e[52], "The Dark Tower: The Gunslinger",            kng,   "Stephen King"),
			(e[53], "The Stand",                                 kng,   "Stephen King"),
			(e[54], "Carrie",                                    kng,   "Stephen King"),
			(e[55], "Doctor Sleep",                              kng,   "Stephen King"),
			(e[56], "East of Eden",                              a[18], n[18]),
			(e[57], "For Whom the Bell Tolls",                   a[2],  n[2]),
			(e[58], "The Sun Also Rises",                        a[2],  n[2]),
			(e[59], "Tender Is the Night",                       a[3],  n[3]),
			(e[60], "This Side of Paradise",                     a[3],  n[3]),
			(e[61], "Mrs Dalloway",                              a[1],  n[1]),
			(e[62], "To the Lighthouse",                         a[1],  n[1]),
			(e[63], "Ulysses",                                   a[8],  n[8]),
			(e[64], "The Sound and the Fury",                    a[9],  n[9]),
			(e[65], "As I Lay Dying",                            a[9],  n[9]),
			(e[66], "My Experiments with Truth",                 a[0],  n[0]),
			(e[67], "Long Walk to Freedom",                      a[10], n[10]),
			(e[68], "The Story of My Life",                      a[1],  n[1]),
			(e[69], "I Know Why the Caged Bird Sings",           a[10], n[10]),
			(e[70], "Confessions",                               a[11], n[11]),
			(e[71], "The Diary of a Young Girl",                 a[12], n[12]),
			(e[72], "Night",                                     a[13], n[13]),
			(e[73], "Becoming",                                  a[8],  n[8]),
			(e[74], "Born a Crime",                              a[19], n[19]),
			(e[75], "Open: An Autobiography",                    a[18], n[18]),
			(e[76], "Sapiens",                                   a[4],  n[4]),
			(e[77], "Homo Deus",                                 a[4],  n[4]),
			(e[78], "Guns, Germs, and Steel",                    a[5],  n[5]),
			(e[79], "The Silk Roads",                            a[6],  n[6]),
			(e[80], "A Short History of Nearly Everything",      a[7],  n[7]),
			(e[81], "The Art of War",                            a[0],  n[0]),
			(e[82], "The Prince",                                a[5],  n[5]),
			(e[83], "Democracy in America",                      a[11], n[11]),
			(e[84], "The Rise and Fall of the Third Reich",      a[13], n[13]),
			(e[85], "1776",                                      a[18], n[18]),
			(e[86], "Emma",                                      a[1],  n[1]),
			(e[87], "Persuasion",                                a[1],  n[1]),
			(e[88], "Northanger Abbey",                          a[1],  n[1]),
			(e[89], "Wuthering Heights",                         a[2],  n[2]),
			(e[90], "Jane Eyre",                                 a[2],  n[2]),
			(e[91], "Rebecca",                                   a[3],  n[3]),
			(e[92], "Doctor Zhivago",                            a[0],  n[0]),
			(e[93], "Love in the Time of Cholera",               a[11], n[11]),
			(e[94], "The Notebook",                              a[12], n[12]),
			(e[95], "Kafka on the Shore",                        a[12], n[12]),
		];
	}
}
