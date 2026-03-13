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
}
