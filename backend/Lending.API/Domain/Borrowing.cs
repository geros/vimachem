namespace Lending.API.Domain;

public class Borrowing {
	public Guid Id { get; private set; } = Guid.NewGuid();
	public Guid BookId { get; private set; }
	public string BookTitle { get; private set; } = string.Empty;
	public Guid CustomerId { get; private set; }
	public string CustomerName { get; private set; } = string.Empty;
	public DateTime BorrowedAt { get; private set; } = DateTime.UtcNow;
	public DateTime? ReturnedAt { get; private set; }

	public bool IsActive => ReturnedAt == null;

	private Borrowing() { }

	public Borrowing(Guid bookId, string bookTitle, Guid customerId, string customerName) {
		BookId = bookId;
		BookTitle = bookTitle;
		CustomerId = customerId;
		CustomerName = customerName;
	}

	public void MarkReturned() {
		if (ReturnedAt != null)
			throw new DomainException("Borrowing already returned");
		ReturnedAt = DateTime.UtcNow;
	}
}
