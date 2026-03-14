using Catalog.API.Domain.Entities;
using Catalog.API.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.API.Tests.Domain;

public sealed class CategoryTests {
	[Fact]
	public void Constructor_WithValidName_ShouldCreate() {
		var category = new Category("Fiction");
		category.Name.Should().Be("Fiction");
		category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void Constructor_WithEmptyName_ShouldThrow() {
		var act = () => new Category("");
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}

	[Fact]
	public void Constructor_WithWhitespaceName_ShouldThrow() {
		var act = () => new Category("   ");
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}

	[Fact]
	public void Update_WithValidName_ShouldUpdate() {
		var category = new Category("Fiction");
		category.Update("Mystery");
		category.Name.Should().Be("Mystery");
	}

	[Fact]
	public void Update_WithEmptyName_ShouldThrow() {
		var category = new Category("Fiction");
		var act = () => category.Update("");
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}

	// EXTREME SCENARIOS - Edge Cases and Boundary Conditions

	[Fact]
	public void Constructor_WithSingleCharacterName_ShouldCreate() {
		var category = new Category("A");
		category.Name.Should().Be("A");
	}

	[Fact]
	public void Constructor_WithVeryLongName_ShouldCreate() {
		var longName = new string('A', 1000);
		var category = new Category(longName);
		category.Name.Should().Be(longName);
	}

	[Fact]
	public void Constructor_WithUnicodeName_ShouldCreate() {
		var unicodeName = "Ficci\u00f3n \ud83d\udcda \u5c0f\u8aac \u0642\u0635\u0635";
		var category = new Category(unicodeName);
		category.Name.Should().Be(unicodeName);
	}

	[Fact]
	public void Constructor_WithNameContainingSpecialCharacters_ShouldCreate() {
		var specialName = "Fiction <script>alert('xss')</script> \"' OR 1=1 --";
		var category = new Category(specialName);
		category.Name.Should().Be(specialName);
	}

	[Fact]
	public void Constructor_WithNameContainingNewlines_ShouldCreate() {
		var nameWithNewlines = "Fiction\nMystery\r\nThriller";
		var category = new Category(nameWithNewlines);
		category.Name.Should().Be(nameWithNewlines);
	}

	[Fact]
	public void Constructor_WithNameContainingTabs_ShouldCreate() {
		var nameWithTabs = "Fiction\tMystery\tThriller";
		var category = new Category(nameWithTabs);
		category.Name.Should().Be(nameWithTabs);
	}

	[Fact]
	public void Constructor_WithNameContainingOnlyWhitespace_ShouldThrow() {
		var act = () => new Category("   \t\n\r   ");
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}

	[Fact]
	public void Update_WithSingleCharacterName_ShouldUpdate() {
		var category = new Category("Fiction");
		category.Update("A");
		category.Name.Should().Be("A");
	}

	[Fact]
	public void Update_WithVeryLongName_ShouldUpdate() {
		var category = new Category("Fiction");
		var longName = new string('B', 1000);
		category.Update(longName);
		category.Name.Should().Be(longName);
	}

	[Fact]
	public void Update_WithSameName_ShouldSucceed() {
		var category = new Category("Fiction");
		category.Update("Fiction");
		category.Name.Should().Be("Fiction");
	}

	[Fact]
	public void Update_WithWhitespaceOnlyName_ShouldThrow() {
		var category = new Category("Fiction");
		var act = () => category.Update("   \t\n   ");
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}

	[Fact]
	public void Update_WithNullName_ShouldThrow() {
		var category = new Category("Fiction");
		var act = () => category.Update(null!);
		act.Should().Throw<DomainException>().WithMessage("*name is required*");
	}
}
