using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Catalog.API.Infrastructure.Persistence;

public static class DataSeeder {
	public static async Task SeedAsync(CatalogDbContext context) {
		if (await context.Categories.AnyAsync()) return;

		// ── Categories ───────────────────────────────────────────────────────
		var fiction    = new Category("Fiction")         { Id = SeedConstants.FictionId };
		var mystery    = new Category("Mystery")         { Id = SeedConstants.MysteryId };
		var sciFi      = new Category("Science Fiction") { Id = SeedConstants.ExtraCategoryIds[0] };
		var biography  = new Category("Biography")       { Id = SeedConstants.ExtraCategoryIds[1] };
		var history    = new Category("History")         { Id = SeedConstants.ExtraCategoryIds[2] };
		var romance    = new Category("Romance")         { Id = SeedConstants.ExtraCategoryIds[3] };
		var thriller   = new Category("Thriller")        { Id = SeedConstants.ExtraCategoryIds[4] };
		var fantasy    = new Category("Fantasy")         { Id = SeedConstants.ExtraCategoryIds[5] };
		context.Categories.AddRange(fiction, mystery, sciFi, biography, history, romance, thriller, fantasy);
		await context.SaveChangesAsync();

		var books = new List<Book>();

		// ── Original 4 books ─────────────────────────────────────────────────
		books.Add(new Book("1984",                             "9780451524935", SeedConstants.OrwellId,   "George Orwell",   SeedConstants.FictionId, 3) { Id = SeedConstants.Book1984Id });
		books.Add(new Book("Animal Farm",                      "9780451526342", SeedConstants.OrwellId,   "George Orwell",   SeedConstants.FictionId, 2) { Id = SeedConstants.BookAnimalFarmId });
		books.Add(new Book("Murder on the Orient Express",     "9780062693662", SeedConstants.ChristieId, "Agatha Christie", SeedConstants.MysteryId, 2) { Id = SeedConstants.BookOrientExpressId });
		books.Add(new Book("The Shining",                      "9780307743657", SeedConstants.KingId,     "Stephen King",    SeedConstants.MysteryId, 1) { Id = SeedConstants.BookShiningId });

		// Helper aliases
		var eid  = SeedConstants.ExtraBookIds;
		var aid  = SeedConstants.AuthorIds;
		var aname = SeedConstants.AuthorNames;
		var cid  = SeedConstants.ExtraCategoryIds;
		var orw  = SeedConstants.OrwellId;
		var chr  = SeedConstants.ChristieId;
		var kng  = SeedConstants.KingId;

		// ── Fiction (12 books) ────────────────────────────────────────────────
		books.Add(new Book("War and Peace",              "9781000000001", aid[0],  aname[0],  SeedConstants.FictionId, 3) { Id = eid[0] });
		books.Add(new Book("Anna Karenina",              "9781000000002", aid[0],  aname[0],  SeedConstants.FictionId, 2) { Id = eid[1] });
		books.Add(new Book("Pride and Prejudice",        "9781000000003", aid[1],  aname[1],  SeedConstants.FictionId, 4) { Id = eid[2] });
		books.Add(new Book("Sense and Sensibility",      "9781000000004", aid[1],  aname[1],  SeedConstants.FictionId, 2) { Id = eid[3] });
		books.Add(new Book("A Farewell to Arms",         "9781000000005", aid[2],  aname[2],  SeedConstants.FictionId, 3) { Id = eid[4] });
		books.Add(new Book("The Old Man and the Sea",    "9781000000006", aid[2],  aname[2],  SeedConstants.FictionId, 2) { Id = eid[5] });
		books.Add(new Book("The Great Gatsby",           "9781000000007", aid[3],  aname[3],  SeedConstants.FictionId, 4) { Id = eid[6] });
		books.Add(new Book("Huckleberry Finn",           "9781000000008", aid[4],  aname[4],  SeedConstants.FictionId, 3) { Id = eid[7] });
		books.Add(new Book("Great Expectations",         "9781000000009", aid[5],  aname[5],  SeedConstants.FictionId, 2) { Id = eid[8] });
		books.Add(new Book("A Tale of Two Cities",       "9781000000010", aid[5],  aname[5],  SeedConstants.FictionId, 3) { Id = eid[9] });
		books.Add(new Book("Of Mice and Men",            "9781000000011", aid[18], aname[18], SeedConstants.FictionId, 2) { Id = eid[10] });
		books.Add(new Book("The Grapes of Wrath",        "9781000000012", aid[18], aname[18], SeedConstants.FictionId, 3) { Id = eid[11] });

		// ── Mystery (12 books) ────────────────────────────────────────────────
		books.Add(new Book("Death on the Nile",          "9781000000013", chr,     "Agatha Christie",     SeedConstants.MysteryId, 2) { Id = eid[12] });
		books.Add(new Book("The ABC Murders",             "9781000000014", chr,     "Agatha Christie",     SeedConstants.MysteryId, 3) { Id = eid[13] });
		books.Add(new Book("And Then There Were None",    "9781000000015", chr,     "Agatha Christie",     SeedConstants.MysteryId, 4) { Id = eid[14] });
		books.Add(new Book("It",                          "9781000000016", kng,     "Stephen King",        SeedConstants.MysteryId, 2) { Id = eid[15] });
		books.Add(new Book("Misery",                      "9781000000017", kng,     "Stephen King",        SeedConstants.MysteryId, 3) { Id = eid[16] });
		books.Add(new Book("The Metamorphosis",           "9781000000018", aid[7],  aname[7],              SeedConstants.MysteryId, 2) { Id = eid[17] });
		books.Add(new Book("Crime and Punishment",        "9781000000019", aid[13], aname[13],             SeedConstants.MysteryId, 4) { Id = eid[18] });
		books.Add(new Book("The Brothers Karamazov",      "9781000000020", aid[13], aname[13],             SeedConstants.MysteryId, 2) { Id = eid[19] });
		books.Add(new Book("Beloved",                     "9781000000021", aid[10], aname[10],             SeedConstants.MysteryId, 2) { Id = eid[20] });
		books.Add(new Book("Moby Dick",                   "9781000000022", aid[19], aname[19],             SeedConstants.MysteryId, 2) { Id = eid[21] });
		books.Add(new Book("One Hundred Years of Solitude","9781000000023", aid[11], aname[11],            SeedConstants.MysteryId, 3) { Id = eid[22] });
		books.Add(new Book("Norwegian Wood",              "9781000000024", aid[12], aname[12],             SeedConstants.MysteryId, 2) { Id = eid[23] });

		// ── Science Fiction (16 books) ────────────────────────────────────────
		books.Add(new Book("Dune",                                  "9781000000025", aid[6],  aname[6],  cid[0], 3) { Id = eid[24] });
		books.Add(new Book("Dune Messiah",                          "9781000000026", aid[6],  aname[6],  cid[0], 2) { Id = eid[25] });
		books.Add(new Book("Fahrenheit 451",                        "9781000000027", aid[7],  aname[7],  cid[0], 4) { Id = eid[26] });
		books.Add(new Book("The Martian Chronicles",                 "9781000000028", aid[7],  aname[7],  cid[0], 2) { Id = eid[27] });
		books.Add(new Book("The Left Hand of Darkness",             "9781000000029", aid[8],  aname[8],  cid[0], 2) { Id = eid[28] });
		books.Add(new Book("The Dispossessed",                      "9781000000030", aid[8],  aname[8],  cid[0], 2) { Id = eid[29] });
		books.Add(new Book("Do Androids Dream of Electric Sheep?",  "9781000000031", aid[9],  aname[9],  cid[0], 3) { Id = eid[30] });
		books.Add(new Book("The Man in the High Castle",            "9781000000032", aid[9],  aname[9],  cid[0], 2) { Id = eid[31] });
		books.Add(new Book("Foundation",                            "9781000000033", aid[16], aname[16], cid[0], 3) { Id = eid[32] });
		books.Add(new Book("I, Robot",                              "9781000000034", aid[16], aname[16], cid[0], 2) { Id = eid[33] });
		books.Add(new Book("Foundation and Empire",                 "9781000000035", aid[16], aname[16], cid[0], 2) { Id = eid[34] });
		books.Add(new Book("2001: A Space Odyssey",                 "9781000000036", aid[17], aname[17], cid[0], 3) { Id = eid[35] });
		books.Add(new Book("Childhood's End",                       "9781000000037", aid[17], aname[17], cid[0], 2) { Id = eid[36] });
		books.Add(new Book("Rendezvous with Rama",                  "9781000000038", aid[17], aname[17], cid[0], 2) { Id = eid[37] });
		books.Add(new Book("Brave New World",                       "9781000000039", aid[2],  aname[2],  cid[0], 3) { Id = eid[38] });
		books.Add(new Book("Slaughterhouse-Five",                   "9781000000040", aid[4],  aname[4],  cid[0], 2) { Id = eid[39] });

		// ── Fantasy (14 books) ────────────────────────────────────────────────
		books.Add(new Book("The Fellowship of the Ring",            "9781000000041", aid[14], aname[14], cid[5], 5) { Id = eid[40] });
		books.Add(new Book("The Two Towers",                        "9781000000042", aid[14], aname[14], cid[5], 4) { Id = eid[41] });
		books.Add(new Book("The Return of the King",                "9781000000043", aid[14], aname[14], cid[5], 4) { Id = eid[42] });
		books.Add(new Book("The Hobbit",                            "9781000000044", aid[14], aname[14], cid[5], 5) { Id = eid[43] });
		books.Add(new Book("Harry Potter and the Philosopher's Stone","9781000000045", aid[15], aname[15], cid[5], 5) { Id = eid[44] });
		books.Add(new Book("Harry Potter and the Chamber of Secrets","9781000000046", aid[15], aname[15], cid[5], 4) { Id = eid[45] });
		books.Add(new Book("Harry Potter and the Prisoner of Azkaban","9781000000047", aid[15], aname[15], cid[5], 4) { Id = eid[46] });
		books.Add(new Book("Harry Potter and the Goblet of Fire",   "9781000000048", aid[15], aname[15], cid[5], 3) { Id = eid[47] });
		books.Add(new Book("Harry Potter and the Order of the Phoenix","9781000000049", aid[15], aname[15], cid[5], 3) { Id = eid[48] });
		books.Add(new Book("Harry Potter and the Half-Blood Prince", "9781000000050", aid[15], aname[15], cid[5], 3) { Id = eid[49] });
		books.Add(new Book("Harry Potter and the Deathly Hallows",  "9781000000051", aid[15], aname[15], cid[5], 4) { Id = eid[50] });
		books.Add(new Book("Pet Sematary",                          "9781000000052", kng,     "Stephen King",    cid[5], 2) { Id = eid[51] });
		books.Add(new Book("The Dark Tower: The Gunslinger",        "9781000000053", kng,     "Stephen King",    cid[5], 2) { Id = eid[52] });
		books.Add(new Book("The Stand",                             "9781000000054", kng,     "Stephen King",    cid[5], 3) { Id = eid[53] });

		// ── Thriller (12 books) ───────────────────────────────────────────────
		books.Add(new Book("Carrie",                                "9781000000055", kng,     "Stephen King",    cid[4], 2) { Id = eid[54] });
		books.Add(new Book("Doctor Sleep",                          "9781000000056", kng,     "Stephen King",    cid[4], 2) { Id = eid[55] });
		books.Add(new Book("East of Eden",                          "9781000000057", aid[18], aname[18],         cid[4], 3) { Id = eid[56] });
		books.Add(new Book("For Whom the Bell Tolls",               "9781000000058", aid[2],  aname[2],          cid[4], 2) { Id = eid[57] });
		books.Add(new Book("The Sun Also Rises",                    "9781000000059", aid[2],  aname[2],          cid[4], 3) { Id = eid[58] });
		books.Add(new Book("Tender Is the Night",                   "9781000000060", aid[3],  aname[3],          cid[4], 2) { Id = eid[59] });
		books.Add(new Book("This Side of Paradise",                 "9781000000061", aid[3],  aname[3],          cid[4], 2) { Id = eid[60] });
		books.Add(new Book("Mrs Dalloway",                          "9781000000062", aid[1],  aname[1],          cid[4], 1) { Id = eid[61] });
		books.Add(new Book("To the Lighthouse",                     "9781000000063", aid[1],  aname[1],          cid[4], 2) { Id = eid[62] });
		books.Add(new Book("Ulysses",                               "9781000000064", aid[8],  aname[8],          cid[4], 1) { Id = eid[63] });
		books.Add(new Book("The Sound and the Fury",                "9781000000065", aid[9],  aname[9],          cid[4], 2) { Id = eid[64] });
		books.Add(new Book("As I Lay Dying",                        "9781000000066", aid[9],  aname[9],          cid[4], 2) { Id = eid[65] });

		// ── Biography (10 books) ──────────────────────────────────────────────
		books.Add(new Book("My Experiments with Truth",             "9781000000067", aid[0],  aname[0], cid[1], 2) { Id = eid[66] });
		books.Add(new Book("Long Walk to Freedom",                  "9781000000068", aid[10], aname[10], cid[1], 3) { Id = eid[67] });
		books.Add(new Book("The Story of My Life",                  "9781000000069", aid[1],  aname[1], cid[1], 2) { Id = eid[68] });
		books.Add(new Book("I Know Why the Caged Bird Sings",       "9781000000070", aid[10], aname[10], cid[1], 3) { Id = eid[69] });
		books.Add(new Book("Confessions",                           "9781000000071", aid[11], aname[11], cid[1], 2) { Id = eid[70] });
		books.Add(new Book("The Diary of a Young Girl",             "9781000000072", aid[12], aname[12], cid[1], 4) { Id = eid[71] });
		books.Add(new Book("Night",                                 "9781000000073", aid[13], aname[13], cid[1], 3) { Id = eid[72] });
		books.Add(new Book("Becoming",                              "9781000000074", aid[8],  aname[8], cid[1], 4) { Id = eid[73] });
		books.Add(new Book("Born a Crime",                          "9781000000075", aid[19], aname[19], cid[1], 2) { Id = eid[74] });
		books.Add(new Book("Open: An Autobiography",                "9781000000076", aid[18], aname[18], cid[1], 2) { Id = eid[75] });

		// ── History (10 books) ────────────────────────────────────────────────
		books.Add(new Book("Sapiens",                               "9781000000077", aid[4],  aname[4],  cid[2], 4) { Id = eid[76] });
		books.Add(new Book("Homo Deus",                             "9781000000078", aid[4],  aname[4],  cid[2], 3) { Id = eid[77] });
		books.Add(new Book("Guns, Germs, and Steel",                "9781000000079", aid[5],  aname[5],  cid[2], 2) { Id = eid[78] });
		books.Add(new Book("The Silk Roads",                        "9781000000080", aid[6],  aname[6],  cid[2], 2) { Id = eid[79] });
		books.Add(new Book("A Short History of Nearly Everything",  "9781000000081", aid[7],  aname[7],  cid[2], 3) { Id = eid[80] });
		books.Add(new Book("The Art of War",                        "9781000000082", aid[0],  aname[0],  cid[2], 5) { Id = eid[81] });
		books.Add(new Book("The Prince",                            "9781000000083", aid[5],  aname[5],  cid[2], 3) { Id = eid[82] });
		books.Add(new Book("Democracy in America",                  "9781000000084", aid[11], aname[11], cid[2], 2) { Id = eid[83] });
		books.Add(new Book("The Rise and Fall of the Third Reich",  "9781000000085", aid[13], aname[13], cid[2], 2) { Id = eid[84] });
		books.Add(new Book("1776",                                  "9781000000086", aid[18], aname[18], cid[2], 3) { Id = eid[85] });

		// ── Romance (10 books) ────────────────────────────────────────────────
		books.Add(new Book("Emma",                                  "9781000000087", aid[1],  aname[1],  cid[3], 3) { Id = eid[86] });
		books.Add(new Book("Persuasion",                            "9781000000088", aid[1],  aname[1],  cid[3], 2) { Id = eid[87] });
		books.Add(new Book("Northanger Abbey",                      "9781000000089", aid[1],  aname[1],  cid[3], 2) { Id = eid[88] });
		books.Add(new Book("Wuthering Heights",                     "9781000000090", aid[2],  aname[2],  cid[3], 3) { Id = eid[89] });
		books.Add(new Book("Jane Eyre",                             "9781000000091", aid[2],  aname[2],  cid[3], 3) { Id = eid[90] });
		books.Add(new Book("Rebecca",                               "9781000000092", aid[3],  aname[3],  cid[3], 2) { Id = eid[91] });
		books.Add(new Book("Doctor Zhivago",                        "9781000000093", aid[0],  aname[0],  cid[3], 2) { Id = eid[92] });
		books.Add(new Book("Love in the Time of Cholera",           "9781000000094", aid[11], aname[11], cid[3], 3) { Id = eid[93] });
		books.Add(new Book("The Notebook",                          "9781000000095", aid[12], aname[12], cid[3], 4) { Id = eid[94] });
		books.Add(new Book("Kafka on the Shore",                    "9781000000096", aid[12], aname[12], cid[3], 3) { Id = eid[95] });

		context.Books.AddRange(books);
		await context.SaveChangesAsync();
	}
}
