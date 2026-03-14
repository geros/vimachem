using Catalog.API.Domain.Entities;
using Catalog.API.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.API.Tests.Domain;

public sealed class BookTests {
	[Fact]
	public void Constructor_ShouldSetAvailableCopiesEqualToTotal() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		book.AvailableCopies.Should().Be(3);
	}

	[Fact]
	public void Constructor_WithZeroCopies_ShouldThrow() {
		var act = () => new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 0);
		act.Should().Throw<DomainException>().WithMessage("*must be positive*");
	}

	[Fact]
	public void Reserve_WhenAvailable_ShouldDecrement() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		book.Reserve();
		book.AvailableCopies.Should().Be(2);
	}

	[Fact]
	public void Reserve_WhenNoneAvailable_ShouldThrow() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		book.Reserve(); // 0 left

		var act = () => book.Reserve();
		act.Should().Throw<DomainException>().WithMessage("*No copies available*");
	}

	[Fact]
	public void Release_ShouldIncrement() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 2);
		book.Reserve(); // 1 left
		book.Release(); // back to 2
		book.AvailableCopies.Should().Be(2);
	}

	[Fact]
	public void Release_WhenAllAvailable_ShouldThrow() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 2);

		var act = () => book.Release();
		act.Should().Throw<DomainException>().WithMessage("*All copies already available*");
	}

	[Fact]
	public void Update_ShouldAdjustAvailableCopies() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		book.Reserve(); // available = 2, borrowed = 1

		book.Update("Test Updated", Guid.NewGuid(), 5);

		book.TotalCopies.Should().Be(5);
		book.AvailableCopies.Should().Be(4); // 5 - 1 borrowed
	}

	[Fact]
	public void Update_CannotReduceBelowBorrowed_ShouldThrow() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		book.Reserve();
		book.Reserve(); // 2 borrowed, 1 available

		var act = () => book.Update("Test", Guid.NewGuid(), 1); // can't go below 2
		act.Should().Throw<DomainException>().WithMessage("*Cannot reduce*");
	}

	[Fact]
	public void Update_ShouldSetUpdatedAt() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		book.UpdatedAt.Should().BeNull();

		book.Update("Test Updated", Guid.NewGuid(), 5);

		book.UpdatedAt.Should().NotBeNull();
		book.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	// EXTREME SCENARIOS - Edge Cases and Boundary Conditions

	[Fact]
	public void Constructor_WithEmptyTitle_ShouldCreate() {
		// Title is not validated in domain - only at application layer
		var act = () => new Book("", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithNullTitle_ShouldCreate() {
		// Title is not validated in domain - only at application layer
		var act = () => new Book(null!, "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithVeryLongTitle_ShouldCreate() {
		var longTitle = new string('A', 10000);
		var act = () => new Book(longTitle, "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithEmptyIsbn_ShouldCreate() {
		// ISBN is not validated in domain - only at application layer
		var act = () => new Book("Test", "", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithNullIsbn_ShouldCreate() {
		// ISBN is not validated in domain - only at application layer
		var act = () => new Book("Test", null!, Guid.NewGuid(), "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithEmptyAuthorName_ShouldCreate() {
		var act = () => new Book("Test", "1234567890", Guid.NewGuid(), "", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithMaxIntCopies_ShouldCreate() {
		var act = () => new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), int.MaxValue);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithNegativeCopies_ShouldThrow() {
		var act = () => new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), -1);
		act.Should().Throw<DomainException>().WithMessage("*must be positive*");
	}

	[Fact]
	public void Constructor_WithEmptyGuidAuthorId_ShouldCreate() {
		var act = () => new Book("Test", "1234567890", Guid.Empty, "Author", Guid.NewGuid(), 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithEmptyGuidCategoryId_ShouldCreate() {
		var act = () => new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.Empty, 1);
		act.Should().NotThrow();
	}

	[Fact]
	public void Reserve_MultipleTimesUpToAvailable_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 100);

		for (int i = 0; i < 100; i++) {
			book.Reserve();
		}

		book.AvailableCopies.Should().Be(0);
		book.IsAvailable().Should().BeFalse();
	}

	[Fact]
	public void Reserve_WhenExactlyOneAvailable_ShouldSucceedThenThrow() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);

		book.Reserve();
		book.AvailableCopies.Should().Be(0);

		var act = () => book.Reserve();
		act.Should().Throw<DomainException>().WithMessage("*No copies available*");
	}

	[Fact]
	public void Release_MultipleTimesUpToTotal_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 100);

		// Reserve all copies
		for (int i = 0; i < 100; i++) {
			book.Reserve();
		}

		// Release all copies
		for (int i = 0; i < 100; i++) {
			book.Release();
		}

		book.AvailableCopies.Should().Be(100);
	}

	[Fact]
	public void Update_WithSameValues_ShouldStillUpdateTimestamp() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		var originalCategoryId = book.CategoryId;

		book.Update("Test", originalCategoryId, 3);

		book.Title.Should().Be("Test");
		book.TotalCopies.Should().Be(3);
		book.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void Update_ReduceToExactlyBorrowedAmount_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 5);
		book.Reserve(); // 4 available, 1 borrowed
		book.Reserve(); // 3 available, 2 borrowed

		book.Update("Test", book.CategoryId, 2); // Reduce to exactly borrowed amount

		book.TotalCopies.Should().Be(2);
		book.AvailableCopies.Should().Be(0);
	}

	[Fact]
	public void Update_WithVeryLargeTotalCopies_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 1);

		book.Update("Test", book.CategoryId, int.MaxValue);

		book.TotalCopies.Should().Be(int.MaxValue);
		book.AvailableCopies.Should().Be(int.MaxValue);
	}

	[Fact]
	public void Update_WithUnicodeTitle_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		var unicodeTitle = "The Great Gatsby \ud83d\udcda \u4e2d\u6587 \u0627\u0644\u0639\u0631\u0628\u064a\u0629";

		book.Update(unicodeTitle, book.CategoryId, 3);

		book.Title.Should().Be(unicodeTitle);
	}

	[Fact]
	public void Update_WithTitleContainingSpecialCharacters_ShouldSucceed() {
		var book = new Book("Test", "1234567890", Guid.NewGuid(), "Author", Guid.NewGuid(), 3);
		var specialTitle = "Book <script>alert('xss')</script> \"' OR 1=1 --";

		book.Update(specialTitle, book.CategoryId, 3);

		book.Title.Should().Be(specialTitle);
	}
}

// Extension method for test readability
internal static class BookExtensions {
	public static bool IsAvailable(this Book book) => book.AvailableCopies > 0;
}
