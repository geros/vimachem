using Catalog.API.Domain.Exceptions;

namespace Catalog.API.Domain.Entities;

public class Book {
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; private set; } = string.Empty;
	public string ISBN { get; private set; } = string.Empty;
	public Guid AuthorId { get; private set; }
	public string AuthorName { get; private set; } = string.Empty;
	public Guid CategoryId { get; private set; }
	public Category? Category { get; private set; }
	public int TotalCopies { get; private set; }
	public int AvailableCopies { get; private set; }
	public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; private set; }

	private Book() { }

	public Book(string title, string isbn, Guid authorId, string authorName,
		Guid categoryId, int totalCopies) {
		if (totalCopies <= 0)
			throw new DomainException("TotalCopies must be positive");

		Title = title;
		ISBN = isbn;
		AuthorId = authorId;
		AuthorName = authorName;
		CategoryId = categoryId;
		TotalCopies = totalCopies;
		AvailableCopies = totalCopies;
	}

	public void Update(string title, Guid categoryId, int totalCopies) {
		var currentlyBorrowed = TotalCopies - AvailableCopies;
		if (totalCopies < currentlyBorrowed)
			throw new DomainException(
				$"Cannot reduce TotalCopies below {currentlyBorrowed} (currently borrowed)");

		Title = title;
		CategoryId = categoryId;
		var delta = totalCopies - TotalCopies;
		TotalCopies = totalCopies;
		AvailableCopies += delta;
		UpdatedAt = DateTime.UtcNow;
	}

	public void Reserve() {
		if (AvailableCopies <= 0)
			throw new DomainException($"No copies available for book '{Title}'");
		AvailableCopies--;
	}

	public void Release() {
		if (AvailableCopies >= TotalCopies)
			throw new DomainException($"All copies already available for book '{Title}'");
		AvailableCopies++;
	}
}
