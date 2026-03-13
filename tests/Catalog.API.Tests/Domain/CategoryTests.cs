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
}
