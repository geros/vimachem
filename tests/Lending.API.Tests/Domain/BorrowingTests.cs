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
}
