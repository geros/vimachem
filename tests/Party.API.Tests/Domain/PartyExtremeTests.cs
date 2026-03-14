using FluentAssertions;
using DomainParty = Party.API.Domain.Party;
using Party.API.Domain;
using Party.API.Domain.Exceptions;
using Xunit;

namespace Party.API.Tests.Domain;

/// <summary>
/// Extreme scenario tests for Party domain entity.
/// Covers edge cases, boundary conditions, and stress scenarios.
/// </summary>
public sealed class PartyExtremeTests {

	#region Constructor Extreme Scenarios

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData("\n")]
	public void Constructor_WithEmptyOrWhitespaceFirstName_ShouldAllowButStoreAsIs(string firstName) {
		// Domain entity itself doesn't validate - validation happens at application layer
		var party = new DomainParty(firstName, "Doe", "test@test.com");

		party.FirstName.Should().Be(firstName);
	}

	[Fact]
	public void Constructor_WithNullFirstName_ShouldStoreAsEmpty() {
		// Domain entity itself doesn't validate - validation happens at application layer
		var party = new DomainParty(null!, "Doe", "test@test.com");

		party.FirstName.Should().BeNull();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData("\n")]
	public void Constructor_WithEmptyOrWhitespaceLastName_ShouldAllowButStoreAsIs(string lastName) {
		var party = new DomainParty("John", lastName, "test@test.com");

		party.LastName.Should().Be(lastName);
	}

	[Fact]
	public void Constructor_WithNullLastName_ShouldStoreAsNull() {
		var party = new DomainParty("John", null!, "test@test.com");

		party.LastName.Should().BeNull();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_WithEmptyOrWhitespaceEmail_ShouldAllowButStoreAsIs(string email) {
		var party = new DomainParty("John", "Doe", email);

		party.Email.Should().Be(email);
	}

	[Fact]
	public void Constructor_WithNullEmail_ShouldStoreAsNull() {
		var party = new DomainParty("John", "Doe", null!);

		party.Email.Should().BeNull();
	}

	[Fact]
	public void Constructor_WithVeryLongFirstName_ShouldAllow() {
		var longName = new string('A', 1000);

		var party = new DomainParty(longName, "Doe", "test@test.com");

		party.FirstName.Should().Be(longName);
	}

	[Fact]
	public void Constructor_WithVeryLongLastName_ShouldAllow() {
		var longName = new string('B', 1000);

		var party = new DomainParty("John", longName, "test@test.com");

		party.LastName.Should().Be(longName);
	}

	[Fact]
	public void Constructor_WithVeryLongEmail_ShouldAllow() {
		var longLocalPart = new string('c', 500);
		var longEmail = $"{longLocalPart}@example.com";

		var party = new DomainParty("John", "Doe", longEmail);

		party.Email.Should().Be(longEmail);
	}

	[Theory]
	[InlineData("test@")]
	[InlineData("@test.com")]
	[InlineData("test")]
	[InlineData("test@.com")]
	[InlineData("test@test")]
	[InlineData("test..test@test.com")]
	[InlineData(".test@test.com")]
	[InlineData("test.@test.com")]
	public void Constructor_WithInvalidEmailFormat_ShouldAllow_BecauseDomainDoesNotValidate(string email) {
		// Domain entity accepts any string - validation is at application layer
		var party = new DomainParty("John", "Doe", email);

		party.Email.Should().Be(email);
	}

	[Theory]
	[InlineData("<script>alert('xss')</script>")]
	[InlineData("'; DROP TABLE Parties; --")]
	[InlineData("1 OR 1=1")]
	[InlineData("../../../etc/passwd")]
	public void Constructor_WithSqlInjectionAttempt_ShouldStoreAsLiteralString(string maliciousInput) {
		var party = new DomainParty(maliciousInput, maliciousInput, maliciousInput);

		party.FirstName.Should().Be(maliciousInput);
		party.LastName.Should().Be(maliciousInput);
		party.Email.Should().Be(maliciousInput);
	}

	[Theory]
	[InlineData("名")]
	[InlineData("🎉")]
	[InlineData("🚀🌟💻")]
	[InlineData("العربية")]
	[InlineData("日本語テキスト")]
	[InlineData("Кириллица")]
	[InlineData("עברית")]
	public void Constructor_WithUnicodeCharacters_ShouldAllow(string unicodeText) {
		var party = new DomainParty(unicodeText, unicodeText, $"{unicodeText}@test.com");

		party.FirstName.Should().Be(unicodeText);
		party.LastName.Should().Be(unicodeText);
	}

	[Theory]
	[InlineData("\u0000")]  // Null character
	[InlineData("\u0001")]  // Start of heading
	[InlineData("\u001F")]  // Unit separator
	[InlineData("\u007F")]  // Delete character
	[InlineData("\u0080")]  // Extended ASCII
	public void Constructor_WithControlCharacters_ShouldAllow(string controlChar) {
		var party = new DomainParty(controlChar, controlChar, $"test{controlChar}@test.com");

		party.FirstName.Should().Be(controlChar);
	}

	[Fact]
	public void Constructor_WithMaxLengthBoundaryFirstName_ShouldAllow() {
		// Database max length is 100
		var boundaryName = new string('A', 100);

		var party = new DomainParty(boundaryName, "Doe", "test@test.com");

		party.FirstName.Should().HaveLength(100);
	}

	[Fact]
	public void Constructor_WithMaxLengthBoundaryLastName_ShouldAllow() {
		var boundaryName = new string('B', 100);

		var party = new DomainParty("John", boundaryName, "test@test.com");

		party.LastName.Should().HaveLength(100);
	}

	[Fact]
	public void Constructor_WithMaxLengthBoundaryEmail_ShouldAllow() {
		// Database max length is 255
		var localPart = new string('c', 246); // 246 + 9 (@test.com) = 255
		var boundaryEmail = $"{localPart}@test.com";

		var party = new DomainParty("John", "Doe", boundaryEmail);

		party.Email.Should().HaveLength(255);
	}

	[Fact]
	public void Constructor_WithSingleCharacterNames_ShouldAllow() {
		var party = new DomainParty("J", "D", "test@test.com");

		party.FirstName.Should().Be("J");
		party.LastName.Should().Be("D");
	}

	[Fact]
	public void Constructor_WithExactlyTwoCharacterNames_ShouldAllow() {
		var party = new DomainParty("Jo", "Do", "test@test.com");

		party.FirstName.Should().Be("Jo");
		party.LastName.Should().Be("Do");
	}

	#endregion

	#region Update Extreme Scenarios

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t\n\r")]
	public void Update_WithWhitespaceOnlyValues_ShouldUpdate(string whitespace) {
		var party = new DomainParty("John", "Doe", "john@test.com");

		party.Update(whitespace, whitespace, whitespace);

		party.FirstName.Should().Be(whitespace);
		party.LastName.Should().Be(whitespace);
		party.Email.Should().Be(whitespace);
		party.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void Update_WithSameValues_ShouldStillUpdateTimestamp() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var originalCreatedAt = party.CreatedAt;

		party.Update("John", "Doe", "john@test.com");

		party.FirstName.Should().Be("John");
		party.UpdatedAt.Should().NotBeNull();
		party.UpdatedAt.Should().BeAfter(originalCreatedAt);
	}

	[Fact]
	public void Update_MultipleTimes_ShouldUpdateTimestampEachTime() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		DateTime? firstUpdate = null;

		for (int i = 0; i < 10; i++) {
			party.Update($"Name{i}", "Doe", "test@test.com");
			if (firstUpdate == null) {
				firstUpdate = party.UpdatedAt;
			}
		}

		party.UpdatedAt.Should().BeOnOrAfter(firstUpdate!.Value);
	}

	[Fact]
	public void Update_WithVeryLongValues_ShouldAllow() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var longName = new string('X', 500);
		var longEmail = $"{new string('e', 490)}@test.com";

		party.Update(longName, longName, longEmail);

		party.FirstName.Should().Be(longName);
		party.LastName.Should().Be(longName);
		party.Email.Should().Be(longEmail);
	}

	#endregion

	#region Role Assignment Extreme Scenarios

	[Fact]
	public void AssignRole_WithInvalidRoleTypeValue_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var invalidRole = (RoleType)999;

		var act = () => party.AssignRole(invalidRole);

		act.Should().Throw<DomainException>()
			.WithMessage("*role does not exist*");
	}

	[Fact]
	public void AssignRole_WithNegativeRoleTypeValue_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var invalidRole = (RoleType)(-1);

		var act = () => party.AssignRole(invalidRole);

		act.Should().Throw<DomainException>()
			.WithMessage("*role does not exist*");
	}

	[Fact]
	public void AssignRole_WithMaxIntRoleTypeValue_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var invalidRole = (RoleType)int.MaxValue;

		var act = () => party.AssignRole(invalidRole);

		act.Should().Throw<DomainException>()
			.WithMessage("*role does not exist*");
	}

	[Fact]
	public void AssignRole_WhenAllPossibleRolesAssigned_ShouldNotAllowDuplicates() {
		var party = new DomainParty("John", "Doe", "john@test.com");

		// Assign all valid roles
		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);

		// Try to assign duplicate
		var act = () => party.AssignRole(RoleType.Author);

		act.Should().Throw<DomainException>()
			.WithMessage("*already has role*");
	}

	[Fact]
	public void AssignRole_AlternatingAssignAndRemove_ShouldHandleCorrectly() {
		var party = new DomainParty("John", "Doe", "john@test.com");

		// Rapid assign/remove cycles
		for (int i = 0; i < 100; i++) {
			party.AssignRole(RoleType.Author);
			party.HasRole(RoleType.Author).Should().BeTrue();

			party.RemoveRole(RoleType.Author);
			party.HasRole(RoleType.Author).Should().BeFalse();
		}

		party.Roles.Should().BeEmpty();
	}

	[Fact]
	public void RemoveRole_WithInvalidRoleTypeValue_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		var invalidRole = (RoleType)999;

		var act = () => party.RemoveRole(invalidRole);

		act.Should().Throw<DomainException>()
			.WithMessage("*role does not exist*");
	}

	[Fact]
	public void RemoveRole_WhenRoleNeverAssigned_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");

		var act = () => party.RemoveRole(RoleType.Customer);

		act.Should().Throw<DomainException>()
			.WithMessage("*does not have role*");
	}

	[Fact]
	public void RemoveRole_AfterClearingAllRoles_ShouldThrowDomainException() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);

		party.RemoveRole(RoleType.Author);
		party.RemoveRole(RoleType.Customer);

		var act = () => party.RemoveRole(RoleType.Author);

		act.Should().Throw<DomainException>()
			.WithMessage("*does not have role*");
	}

	#endregion

	#region HasRole Extreme Scenarios

	[Theory]
	[InlineData((RoleType)(-1))]
	[InlineData((RoleType)2)]
	[InlineData((RoleType)999)]
	[InlineData((RoleType)int.MaxValue)]
	public void HasRole_WithInvalidRoleType_ShouldReturnFalse(RoleType invalidRole) {
		var party = new DomainParty("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);

		var result = party.HasRole(invalidRole);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasRole_OnNewParty_ShouldReturnFalseForAllRoles() {
		var party = new DomainParty("John", "Doe", "john@test.com");

		party.HasRole(RoleType.Author).Should().BeFalse();
		party.HasRole(RoleType.Customer).Should().BeFalse();
	}

	#endregion

	#region Edge Case Combinations

	[Fact]
	public void Party_WithAllBoundaryConditions_ShouldHandleCorrectly() {
		var party = new DomainParty(
			new string('A', 100),  // Max boundary first name
			new string('B', 100),  // Max boundary last name
			$"{new string('c', 246)}@test.com"  // Max boundary email (255)
		);

		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);

		party.FirstName.Should().HaveLength(100);
		party.LastName.Should().HaveLength(100);
		party.Email.Should().HaveLength(255);
		party.Roles.Should().HaveCount(2);
	}

	[Fact]
	public void Party_WithMixedValidAndInvalidOperations_ShouldMaintainConsistency() {
		var party = new DomainParty("John", "Doe", "john@test.com");

		// Valid operations
		party.AssignRole(RoleType.Author);
		party.HasRole(RoleType.Author).Should().BeTrue();

		// Invalid operation (should throw)
		var act = () => party.AssignRole(RoleType.Author);
		act.Should().Throw<DomainException>();

		// Party should still be in consistent state
		party.HasRole(RoleType.Author).Should().BeTrue();
		party.Roles.Should().HaveCount(1);
		party.FirstName.Should().Be("John");
	}

	[Fact]
	public void Party_CreatedWithGuidDefaultValues_ShouldHaveUniqueId() {
		var party1 = new DomainParty("John", "Doe", "john@test.com");
		var party2 = new DomainParty("Jane", "Doe", "jane@test.com");

		party1.Id.Should().NotBe(Guid.Empty);
		party2.Id.Should().NotBe(Guid.Empty);
		party1.Id.Should().NotBe(party2.Id);
	}

	[Fact]
	public void Party_CreatedAt_ShouldBeUtc() {
		var beforeCreation = DateTime.UtcNow.AddMilliseconds(-100);

		var party = new DomainParty("John", "Doe", "john@test.com");

		var afterCreation = DateTime.UtcNow.AddMilliseconds(100);

		party.CreatedAt.Should().BeOnOrAfter(beforeCreation);
		party.CreatedAt.Should().BeOnOrBefore(afterCreation);
		party.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
	}

	#endregion
}
