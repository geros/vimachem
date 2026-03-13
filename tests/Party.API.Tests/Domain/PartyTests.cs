using FluentAssertions;
using Party.API.Domain;
using Party.API.Domain.Exceptions;
using Xunit;

namespace Party.API.Tests.Domain;

public sealed class PartyTests {
	[Fact]
	public void AssignRole_WhenNew_ShouldAddRole() {
		var party = new Party("John", "Doe", "john@test.com");

		party.AssignRole(RoleType.Customer);

		party.Roles.Should().HaveCount(1);
		party.HasRole(RoleType.Customer).Should().BeTrue();
	}

	[Fact]
	public void AssignRole_WhenDuplicate_ShouldThrow() {
		var party = new Party("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Author);

		var act = () => party.AssignRole(RoleType.Author);

		act.Should().Throw<DomainException>()
			.WithMessage("*already has role*");
	}

	[Fact]
	public void RemoveRole_WhenExists_ShouldRemove() {
		var party = new Party("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Customer);

		party.RemoveRole(RoleType.Customer);

		party.Roles.Should().BeEmpty();
		party.HasRole(RoleType.Customer).Should().BeFalse();
	}

	[Fact]
	public void RemoveRole_WhenMissing_ShouldThrow() {
		var party = new Party("John", "Doe", "john@test.com");

		var act = () => party.RemoveRole(RoleType.Author);

		act.Should().Throw<DomainException>()
			.WithMessage("*does not have role*");
	}

	[Fact]
	public void AssignBothRoles_ShouldAllowDualRole() {
		var party = new Party("Stephen", "King", "king@test.com");

		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);

		party.Roles.Should().HaveCount(2);
		party.HasRole(RoleType.Author).Should().BeTrue();
		party.HasRole(RoleType.Customer).Should().BeTrue();
	}

	[Fact]
	public void Update_ShouldSetUpdatedAt() {
		var party = new Party("John", "Doe", "john@test.com");
		party.UpdatedAt.Should().BeNull();

		party.Update("Jane", "Doe", "jane@test.com");

		party.FirstName.Should().Be("Jane");
		party.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_ShouldSetProperties() {
		var party = new Party("George", "Orwell", "orwell@test.com");

		party.FirstName.Should().Be("George");
		party.LastName.Should().Be("Orwell");
		party.Email.Should().Be("orwell@test.com");
		party.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		party.UpdatedAt.Should().BeNull();
	}

	[Fact]
	public void HasRole_WhenNoRoles_ShouldReturnFalse() {
		var party = new Party("John", "Doe", "john@test.com");

		party.HasRole(RoleType.Author).Should().BeFalse();
		party.HasRole(RoleType.Customer).Should().BeFalse();
	}

	[Fact]
	public void Update_ShouldUpdateAllFields() {
		var party = new Party("John", "Doe", "john@test.com");

		party.Update("Jane", "Smith", "jane@new.com");

		party.FirstName.Should().Be("Jane");
		party.LastName.Should().Be("Smith");
		party.Email.Should().Be("jane@new.com");
	}
}
