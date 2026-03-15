namespace Shared;

public static class SeedConstants {
	// Parties — shared across services
	public static readonly Guid OrwellId = Guid.Parse("11111111-1111-1111-1111-111111111111");
	public static readonly Guid ChristieId = Guid.Parse("22222222-2222-2222-2222-222222222222");
	public static readonly Guid DoeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
	public static readonly Guid KingId = Guid.Parse("44444444-4444-4444-4444-444444444444");

	// Categories
	public static readonly Guid FictionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
	public static readonly Guid MysteryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

	// Books
	public static readonly Guid Book1984Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
	public static readonly Guid BookAnimalFarmId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
	public static readonly Guid BookOrientExpressId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
	public static readonly Guid BookShiningId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

	// Additional author party IDs (index 0..19)
	// Format: 0000{i:D4}-0000-0000-0000-000000000001
	public static readonly Guid[] AuthorIds = Enumerable.Range(1, 20)
		.Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1))
		.ToArray();

	// Additional customer party IDs (index 0..29)
	// Format: 0000{i:D4}-0000-0000-0000-000000000002
	public static readonly Guid[] CustomerIds = Enumerable.Range(1, 30)
		.Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2))
		.ToArray();

	// Additional category IDs (index 0..5)
	// Format: 0000{i:D4}-0000-0000-0000-000000000003
	public static readonly Guid[] ExtraCategoryIds = Enumerable.Range(1, 6)
		.Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3))
		.ToArray();

	// Additional book IDs (index 0..95)
	// Format: 0000{i:D4}-0000-0000-0000-000000000004
	public static readonly Guid[] ExtraBookIds = Enumerable.Range(1, 96)
		.Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4))
		.ToArray();

	// Author names (parallel to AuthorIds)
	public static readonly string[] AuthorNames = [
		"Leo Tolstoy",          // 0
		"Jane Austen",          // 1
		"Ernest Hemingway",     // 2
		"F. Scott Fitzgerald",  // 3
		"Mark Twain",           // 4
		"Charles Dickens",      // 5
		"Frank Herbert",        // 6
		"Ray Bradbury",         // 7
		"Ursula K. Le Guin",    // 8
		"Philip K. Dick",       // 9
		"Toni Morrison",        // 10
		"Gabriel Marquez",      // 11
		"Haruki Murakami",      // 12
		"Fyodor Dostoevsky",    // 13
		"J.R.R. Tolkien",       // 14
		"J.K. Rowling",         // 15
		"Isaac Asimov",         // 16
		"Arthur C. Clarke",     // 17
		"John Steinbeck",       // 18
		"Herman Melville",      // 19
	];

	// Customer names (parallel to CustomerIds)
	public static readonly (string First, string Last, string Email)[] CustomerData = [
		("Alice",   "Johnson",   "alice.johnson@example.com"),
		("Bob",     "Smith",     "bob.smith@example.com"),
		("Carol",   "White",     "carol.white@example.com"),
		("David",   "Brown",     "david.brown@example.com"),
		("Emma",    "Davis",     "emma.davis@example.com"),
		("Frank",   "Wilson",    "frank.wilson@example.com"),
		("Grace",   "Lee",       "grace.lee@example.com"),
		("Henry",   "Martinez",  "henry.martinez@example.com"),
		("Isabella","Thompson",  "isabella.thompson@example.com"),
		("James",   "Anderson",  "james.anderson@example.com"),
		("Karen",   "Jackson",   "karen.jackson@example.com"),
		("Liam",    "Harris",    "liam.harris@example.com"),
		("Mia",     "Garcia",    "mia.garcia@example.com"),
		("Noah",    "Martinez",  "noah.martinez@example.com"),
		("Olivia",  "Robinson",  "olivia.robinson@example.com"),
		("Peter",   "Clark",     "peter.clark@example.com"),
		("Quinn",   "Rodriguez", "quinn.rodriguez@example.com"),
		("Rachel",  "Lewis",     "rachel.lewis@example.com"),
		("Sam",     "Walker",    "sam.walker@example.com"),
		("Taylor",  "Hall",      "taylor.hall@example.com"),
		("Uma",     "Allen",     "uma.allen@example.com"),
		("Victor",  "Young",     "victor.young@example.com"),
		("Wendy",   "Hernandez", "wendy.hernandez@example.com"),
		("Xavier",  "King",      "xavier.king@example.com"),
		("Yara",    "Wright",    "yara.wright@example.com"),
		("Zoe",     "Lopez",     "zoe.lopez@example.com"),
		("Aaron",   "Hill",      "aaron.hill@example.com"),
		("Bella",   "Scott",     "bella.scott@example.com"),
		("Carlos",  "Green",     "carlos.green@example.com"),
		("Diana",   "Adams",     "diana.adams@example.com"),
	];
}
