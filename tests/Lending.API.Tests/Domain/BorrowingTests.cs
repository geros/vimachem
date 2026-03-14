using FluentAssertions;
using Lending.API.Domain;
using Xunit;

namespace Lending.API.Tests.Domain;

public sealed class BorrowingTests {
	[Fact]
	public void Constructor_ShouldSetFieldsCorrectly() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.IsActive.Should().BeTrue();
		borrowing.ReturnedAt.Should().BeNull();
		borrowing.BookTitle.Should().Be("1984");
	}

	[Fact]
	public void MarkReturned_ShouldSetReturnedAt() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.MarkReturned();
		borrowing.ReturnedAt.Should().NotBeNull();
		borrowing.IsActive.Should().BeFalse();
	}

	[Fact]
	public void MarkReturned_WhenAlreadyReturned_ShouldThrow() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.MarkReturned();

		var act = () => borrowing.MarkReturned();
		act.Should().Throw<DomainException>().WithMessage("*already returned*");
	}

	// EXTREME SCENARIOS

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	public void Constructor_WithEmptyOrWhitespaceBookTitle_ShouldAllow(string bookTitle) {
		// Edge case: System allows empty/whitespace titles - may need validation
		var act = () => new Borrowing(Guid.NewGuid(), bookTitle, Guid.NewGuid(), "John Doe");
		act.Should().NotThrow();
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	public void Constructor_WithEmptyOrWhitespaceCustomerName_ShouldAllow(string customerName) {
		// Edge case: System allows empty/whitespace customer names - may need validation
		var act = () => new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), customerName);
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_WithVeryLongBookTitle_ShouldHandle() {
		// Boundary: Very long title (10,000 characters)
		var longTitle = new string('A', 10000);
		var borrowing = new Borrowing(Guid.NewGuid(), longTitle, Guid.NewGuid(), "John Doe");
		borrowing.BookTitle.Should().Be(longTitle);
	}

	[Fact]
	public void Constructor_WithVeryLongCustomerName_ShouldHandle() {
		// Boundary: Very long customer name (10,000 characters)
		var longName = new string('B', 10000);
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), longName);
		borrowing.CustomerName.Should().Be(longName);
	}

	[Fact]
	public void Constructor_WithUnicodeBookTitle_ShouldHandle() {
		// Edge case: Unicode characters in title
		var unicodeTitle = "????????? ?????? (War and Peace) - ?????? ?????? - 1984 - ???";
		var borrowing = new Borrowing(Guid.NewGuid(), unicodeTitle, Guid.NewGuid(), "John Doe");
		borrowing.BookTitle.Should().Be(unicodeTitle);
	}

	[Fact]
	public void Constructor_WithUnicodeCustomerName_ShouldHandle() {
		// Edge case: Unicode characters in customer name
		var unicodeName = "????? ????? (Yoko Ono) - ??? (Li Wei) - Jos? Garc?a";
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), unicodeName);
		borrowing.CustomerName.Should().Be(unicodeName);
	}

	[Fact]
	public void Constructor_WithSpecialCharactersInBookTitle_ShouldHandle() {
		// Edge case: Special characters that might cause issues
		var specialTitle = "<script>alert('xss')</script> & \"quoted\" 'apos' \n\t\r";
		var borrowing = new Borrowing(Guid.NewGuid(), specialTitle, Guid.NewGuid(), "John Doe");
		borrowing.BookTitle.Should().Be(specialTitle);
	}

	[Fact]
	public void Constructor_WithEmptyGuid_ShouldAllow() {
		// Edge case: Empty GUIDs - should this be allowed?
		var emptyGuid = Guid.Empty;
		var borrowing = new Borrowing(emptyGuid, "1984", emptyGuid, "John Doe");
		borrowing.BookId.Should().Be(emptyGuid);
		borrowing.CustomerId.Should().Be(emptyGuid);
	}

	[Fact]
	public void MarkReturned_MultipleCalls_ShouldThrowEachTime() {
		// Edge case: Ensure idempotency failure is consistent
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.MarkReturned();

		// First retry
		var act1 = () => borrowing.MarkReturned();
		act1.Should().Throw<DomainException>().WithMessage("*already returned*");

		// Second retry
		var act2 = () => borrowing.MarkReturned();
		act2.Should().Throw<DomainException>().WithMessage("*already returned*");

		// Third retry
		var act3 = () => borrowing.MarkReturned();
		act3.Should().Throw<DomainException>().WithMessage("*already returned*");
	}

	[Fact]
	public void IsActive_AfterConstruction_ShouldBeTrue() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.IsActive.Should().BeTrue();
	}

	[Fact]
	public void IsActive_AfterMarkReturned_ShouldBeFalse() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.MarkReturned();
		borrowing.IsActive.Should().BeFalse();
	}

	[Fact]
	public void BorrowedAt_ShouldBeUtcNow() {
		var before = DateTime.UtcNow.AddSeconds(-1);
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		var after = DateTime.UtcNow.AddSeconds(1);

		borrowing.BorrowedAt.Should().BeAfter(before);
		borrowing.BorrowedAt.Should().BeBefore(after);
	}

	[Fact]
	public void ReturnedAt_AfterMarkReturned_ShouldBeUtc() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		borrowing.MarkReturned();

		borrowing.ReturnedAt.Should().NotBeNull();
		borrowing.ReturnedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
	}

	[Fact]
	public void MarkReturned_ReturnedAtShouldBeAfterBorrowedAt() {
		var borrowing = new Borrowing(Guid.NewGuid(), "1984", Guid.NewGuid(), "John Doe");
		var borrowedAt = borrowing.BorrowedAt;

		// Simulate time passing
		Thread.Sleep(10);
		borrowing.MarkReturned();

		borrowing.ReturnedAt.Should().BeAfter(borrowedAt);
	}
}
