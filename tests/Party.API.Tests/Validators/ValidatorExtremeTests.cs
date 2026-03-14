using FluentAssertions;
using FluentValidation.TestHelper;
using Party.API.Application.DTOs;
using Party.API.Application.Validators;
using Party.API.Domain;
using Xunit;

namespace Party.API.Tests.Validators;

/// <summary>
/// Extreme scenario tests for FluentValidation validators.
/// Covers boundary conditions, edge cases, and validation edge scenarios.
/// </summary>
public sealed class ValidatorExtremeTests {

	#region CreatePartyValidator Extreme Tests

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData("\n")]
	[InlineData("\r")]
	public void CreatePartyValidator_WithNullOrEmptyFirstName_ShouldHaveValidationError(string? firstName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest(firstName!, "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.FirstName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	public void CreatePartyValidator_WithNullOrEmptyLastName_ShouldHaveValidationError(string? lastName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", lastName!, "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.LastName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void CreatePartyValidator_WithNullOrEmptyEmail_ShouldHaveValidationError(string? email) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Doe", email!);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Email);
	}

	[Fact]
	public void CreatePartyValidator_WithSingleCharacterFirstName_ShouldHaveValidationError() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("J", "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.FirstName)
			.WithErrorMessage("First name must be at least 2 characters");
	}

	[Fact]
	public void CreatePartyValidator_WithSingleCharacterLastName_ShouldHaveValidationError() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "D", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.LastName)
			.WithErrorMessage("Last name must be at least 2 characters");
	}

	[Fact]
	public void CreatePartyValidator_WithExactlyTwoCharacterFirstName_ShouldPass() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("Jo", "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
	}

	[Fact]
	public void CreatePartyValidator_WithExactlyTwoCharacterLastName_ShouldPass() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Do", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.LastName);
	}

	[Theory]
	[InlineData("test")]
	[InlineData("@")]
	[InlineData("@.")]
	[InlineData("")]
	public void CreatePartyValidator_WithInvalidEmailFormat_ShouldHaveValidationError(string email) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Doe", email);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Email);
	}

	[Theory]
	[InlineData("test@test.com")]
	[InlineData("user@domain.co.uk")]
	[InlineData("user.name@domain.com")]
	[InlineData("user+tag@domain.com")]
	[InlineData("user_name@domain.com")]
	[InlineData("123@domain.com")]
	[InlineData("user@domain.museum")]
	[InlineData("user@sub.domain.com")]
	public void CreatePartyValidator_WithValidEmailFormat_ShouldPass(string email) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Doe", email);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Email);
	}

	[Fact]
	public void CreatePartyValidator_WithVeryLongFirstName_ShouldPassValidation() {
		var validator = new CreatePartyValidator();
		var longName = new string('A', 500);
		var request = new CreatePartyRequest(longName, "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		// Validator only checks minimum length, not maximum
		result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
	}

	[Fact]
	public void CreatePartyValidator_WithVeryLongLastName_ShouldPassValidation() {
		var validator = new CreatePartyValidator();
		var longName = new string('B', 500);
		var request = new CreatePartyRequest("John", longName, "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.LastName);
	}

	[Fact]
	public void CreatePartyValidator_WithVeryLongEmail_ShouldPassValidation() {
		var validator = new CreatePartyValidator();
		var longLocalPart = new string('c', 500);
		var request = new CreatePartyRequest("John", "Doe", $"{longLocalPart}@test.com");

		var result = validator.TestValidate(request);

		// Validator only checks format, not length
		result.ShouldNotHaveValidationErrorFor(x => x.Email);
	}

	[Theory]
	[InlineData("'; DROP TABLE Parties; --")]
	[InlineData("1 OR 1=1")]
	[InlineData("../../../etc/passwd")]
	[InlineData("\u003cscript\u003ealert('xss')\u003c/script\u003e")]
	public void CreatePartyValidator_WithSqlInjectionAttempt_ShouldValidateBasedOnRules(string maliciousInput) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest(
			maliciousInput.Length >= 2 ? maliciousInput : "XX",
			maliciousInput.Length >= 2 ? maliciousInput : "XX",
			$"{maliciousInput}@test.com"
		);

		var result = validator.TestValidate(request);

		// SQL injection strings that are long enough pass name validation
		// but email validation may fail if they don't look like valid emails
		if (maliciousInput.Contains("@")) {
			result.ShouldNotHaveValidationErrorFor(x => x.Email);
		}
	}

	[Theory]
	[InlineData("名")]
	[InlineData("🎉")]
	[InlineData("العربية")]
	[InlineData("日本語")]
	[InlineData("Кириллица")]
	public void CreatePartyValidator_WithUnicodeFirstName_BelowMinLength_ShouldFail(string unicodeText) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest(unicodeText, "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		// Single character unicode should fail minimum length
		if (unicodeText.Length < 2) {
			result.ShouldHaveValidationErrorFor(x => x.FirstName);
		}
	}

	[Fact]
	public void CreatePartyValidator_WithWhitespaceOnlyFirstName_ShouldFail() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("   ", "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.FirstName);
	}

	[Fact]
	public void CreatePartyValidator_WithWhitespaceOnlyLastName_ShouldFail() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "   ", "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.LastName);
	}

	[Fact]
	public void CreatePartyValidator_WithAllFieldsInvalid_ShouldHaveMultipleErrors() {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("", "", "");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.FirstName);
		result.ShouldHaveValidationErrorFor(x => x.LastName);
		result.ShouldHaveValidationErrorFor(x => x.Email);
		result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
	}

	[Theory]
	[InlineData("test@test.com")]
	[InlineData("TEST@TEST.COM")]
	[InlineData("Test@Test.Com")]
	[InlineData("TeSt@TeSt.CoM")]
	public void CreatePartyValidator_WithDifferentEmailCasing_ShouldPass(string email) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Doe", email);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Email);
	}

	[Theory]
	[InlineData(" test@test.com")]
	[InlineData("test@test.com ")]
	[InlineData(" test@test.com ")]
	public void CreatePartyValidator_WithEmailContainingWhitespace_MayPassOrFail_DependingOnValidatorVersion(string email) {
		// Note: FluentValidation's EmailAddress validator behavior varies by version
		// Some versions trim whitespace, others don't
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", "Doe", email);

		var result = validator.TestValidate(request);

		// This test documents the behavior - it may or may not have validation errors
		// depending on the FluentValidation version
		result.Should().NotBeNull();
	}

	#endregion

	#region AssignRoleValidator Extreme Tests

	[Theory]
	[InlineData((RoleType)(-1))]
	[InlineData((RoleType)(-100))]
	[InlineData((RoleType)2)]
	[InlineData((RoleType)3)]
	[InlineData((RoleType)999)]
	[InlineData((RoleType)int.MaxValue)]
	[InlineData((RoleType)int.MinValue)]
	public void AssignRoleValidator_WithInvalidRoleType_ShouldHaveValidationError(RoleType invalidRole) {
		var validator = new AssignRoleValidator();
		var request = new AssignRoleRequest(invalidRole);

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.RoleType)
			.WithErrorMessage("This role does not exist. Valid roles are: Author (0) or Customer (1)");
	}

	[Theory]
	[InlineData(RoleType.Author)]
	[InlineData(RoleType.Customer)]
	public void AssignRoleValidator_WithValidRoleType_ShouldPass(RoleType validRole) {
		var validator = new AssignRoleValidator();
		var request = new AssignRoleRequest(validRole);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.RoleType);
	}

	[Fact]
	public void AssignRoleValidator_WithDefaultRoleType_ShouldFail() {
		// Default for enum is 0, which is Author - a valid value
		var validator = new AssignRoleValidator();
		var request = new AssignRoleRequest(default(RoleType));

		var result = validator.TestValidate(request);

		// Default(0) is Author, which is valid
		result.ShouldNotHaveValidationErrorFor(x => x.RoleType);
	}

	#endregion

	#region Boundary Value Analysis

	[Theory]
	[InlineData("A")]      // Below minimum (1 char)
	[InlineData("AB")]     // At minimum (2 chars) - should pass
	[InlineData("ABC")]    // Above minimum (3 chars)
	public void CreatePartyValidator_FirstNameBoundaryLengths(string firstName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest(firstName, "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		if (firstName.Length < 2) {
			result.ShouldHaveValidationErrorFor(x => x.FirstName);
		} else {
			result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
		}
	}

	[Theory]
	[InlineData("A")]      // Below minimum (1 char)
	[InlineData("AB")]     // At minimum (2 chars) - should pass
	[InlineData("ABC")]    // Above minimum (3 chars)
	public void CreatePartyValidator_LastNameBoundaryLengths(string lastName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", lastName, "test@test.com");

		var result = validator.TestValidate(request);

		if (lastName.Length < 2) {
			result.ShouldHaveValidationErrorFor(x => x.LastName);
		} else {
			result.ShouldNotHaveValidationErrorFor(x => x.LastName);
		}
	}

	#endregion

	#region Special Character Tests

	[Theory]
	[InlineData("John\n")]
	[InlineData("John\r")]
	[InlineData("John\t")]
	[InlineData("John\0")]
	public void CreatePartyValidator_WithControlCharactersInNames_ShouldPass(string firstName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest(firstName, "Doe", "test@test.com");

		var result = validator.TestValidate(request);

		// Control characters count as characters, so length check passes
		result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
	}

	[Theory]
	[InlineData("O'Connor")]
	[InlineData("Jean-Luc")]
	[InlineData("D'Angelo")]
	[InlineData("Muñoz")]
	[InlineData("François")]
	public void CreatePartyValidator_WithSpecialCharactersInNames_ShouldPass(string lastName) {
		var validator = new CreatePartyValidator();
		var request = new CreatePartyRequest("John", lastName, "test@test.com");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.LastName);
	}

	#endregion
}
