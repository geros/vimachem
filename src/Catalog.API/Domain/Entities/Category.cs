using Catalog.API.Domain.Exceptions;

namespace Catalog.API.Domain.Entities;

public class Category {
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; private set; } = string.Empty;
	public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
	public List<Book> Books { get; private set; } = new();

	private Category() { }

	public Category(string name) {
		if (string.IsNullOrWhiteSpace(name))
			throw new DomainException("Category name is required");
		Name = name;
	}

	public void Update(string name) {
		if (string.IsNullOrWhiteSpace(name))
			throw new DomainException("Category name is required");
		Name = name;
	}
}
