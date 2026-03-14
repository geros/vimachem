using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Party.API.Application.DTOs;
using Party.API.Application.Interfaces;
using Party.API.Application.Services;
using DomainParty = Party.API.Domain.Party;
using Party.API.Domain;
using Party.API.Domain.Exceptions;
using Party.API.Infrastructure.Persistence;
using Shared.Events;
using Xunit;

namespace Party.API.Tests.Services;

/// <summary>
/// Extreme scenario tests for PartyService.
/// Covers concurrency, stress tests, failure modes, and edge cases.
/// Note: InMemory database does not enforce all constraints like PostgreSQL would.
/// Some tests document expected PostgreSQL behavior that would fail with InMemory.
/// </summary>
public sealed class PartyServiceExtremeTests : IDisposable {
	private readonly PartyDbContext _context;
	private readonly Mock<IEventPublisher> _publisherMock;
	private readonly PartyService _service;

	public PartyServiceExtremeTests() {
		var options = new DbContextOptionsBuilder<PartyDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		_context = new PartyDbContext(options);
		_publisherMock = new Mock<IEventPublisher>();
		_service = new PartyService(_context, _publisherMock.Object);
	}

	public void Dispose() {
		_context.Dispose();
	}

	#region CreateAsync Extreme Scenarios

	[Fact(Skip = "InMemory database does not enforce unique constraints - would pass with PostgreSQL")]
	public async Task CreateAsync_WithDuplicateEmail_ShouldThrowDbUpdateException() {
		// Arrange
		var request1 = new CreatePartyRequest("John", "Doe", "duplicate@test.com");
		var request2 = new CreatePartyRequest("Jane", "Smith", "duplicate@test.com");

		await _service.CreateAsync(request1, CancellationToken.None);

		// Act
		var act = () => _service.CreateAsync(request2, CancellationToken.None);

		// Assert - With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact(Skip = "InMemory database does not enforce unique constraints - would pass with PostgreSQL")]
	public async Task CreateAsync_WithConcurrentSameEmail_ShouldThrowForSecond() {
		// Arrange - simulate concurrent creation with same email
		var email = "concurrent@test.com";
		var request1 = new CreatePartyRequest("John", "Doe", email);
		var request2 = new CreatePartyRequest("Jane", "Smith", email);

		// Create first party
		await _service.CreateAsync(request1, CancellationToken.None);

		// Try to create second with same email
		var act = () => _service.CreateAsync(request2, CancellationToken.None);

		// Assert - With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task CreateAsync_WithVeryLongNames_ShouldSucceed() {
		var longFirstName = new string('A', 100);  // Database max
		var longLastName = new string('B', 100);   // Database max
		var request = new CreatePartyRequest(longFirstName, longLastName, "test@test.com");

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.FirstName.Should().Be(longFirstName);
		result.LastName.Should().Be(longLastName);
	}

	[Fact]
	public async Task CreateAsync_WithMaxLengthEmail_ShouldSucceed() {
		var localPart = new string('c', 246); // 246 + 9 (@test.com) = 255
		var maxEmail = $"{localPart}@test.com";
		var request = new CreatePartyRequest("John", "Doe", maxEmail);

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.Email.Should().Be(maxEmail);
		result.Email.Should().HaveLength(255);
	}

	[Fact(Skip = "InMemory database does not enforce max length constraints - would pass with PostgreSQL")]
	public async Task CreateAsync_WithNamesExceedingDatabaseMax_ShouldThrowDbUpdateException() {
		var tooLongFirstName = new string('A', 101);  // Exceeds 100 char max
		var request = new CreatePartyRequest(tooLongFirstName, "Doe", "test@test.com");

		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact(Skip = "InMemory database does not enforce max length constraints - would pass with PostgreSQL")]
	public async Task CreateAsync_WithEmailExceedingDatabaseMax_ShouldThrowDbUpdateException() {
		var tooLongLocalPart = new string('c', 248); // 248 + 8 = 256, exceeds 255 max
		var tooLongEmail = $"{tooLongLocalPart}@test.com";
		var request = new CreatePartyRequest("John", "Doe", tooLongEmail);

		var act = () => _service.CreateAsync(request, CancellationToken.None);

		// With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Theory]
	[InlineData("test@")]
	[InlineData("@test.com")]
	[InlineData("invalid")]
	public async Task CreateAsync_WithInvalidEmailFormat_ShouldStillCreate_BecauseServiceDoesNotValidate(string email) {
		// Note: The service doesn't validate - it just passes data to domain
		// Validation should happen at controller/API layer
		var request = new CreatePartyRequest("John", "Doe", email);

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.Email.Should().Be(email);
	}

	[Fact]
	public async Task CreateAsync_WithSqlInjectionInNames_ShouldCreateAsLiteralString() {
		var sqlInjection = "'; DROP TABLE Parties; --";
		var request = new CreatePartyRequest(sqlInjection, sqlInjection, "test@test.com");

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.FirstName.Should().Be(sqlInjection);
		result.LastName.Should().Be(sqlInjection);
		// Verify table still exists by querying
		var count = await _context.Parties.CountAsync();
		count.Should().Be(1);
	}

	[Fact]
	public async Task CreateAsync_WithUnicodeCharacters_ShouldSucceed() {
		var request = new CreatePartyRequest(
			"日本語",
			"العربية",
			"test@xn--wgv71a.com" // Punycode for internationalized domain
		);

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.FirstName.Should().Be("日本語");
		result.LastName.Should().Be("العربية");
	}

	[Fact]
	public async Task CreateAsync_MultipleRapidCreations_ShouldSucceed() {
		var parties = new List<DomainParty>();

		for (int i = 0; i < 100; i++) {
			var request = new CreatePartyRequest(
				$"User{i}",
				$"Test{i}",
				$"user{i}@test.com"
			);
			var result = await _service.CreateAsync(request, CancellationToken.None);
			parties.Add(new DomainParty(result.FirstName, result.LastName, result.Email));
		}

		var count = await _context.Parties.CountAsync();
		count.Should().Be(100);
	}

	#endregion

	#region GetByIdAsync Extreme Scenarios

	[Fact]
	public async Task GetByIdAsync_WithEmptyGuid_ShouldThrowNotFoundException() {
		var act = () => _service.GetByIdAsync(Guid.Empty, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_WithMaxValueGuid_ShouldThrowNotFoundException() {
		var maxGuid = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

		var act = () => _service.GetByIdAsync(maxGuid, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_WithRandomGuid_ShouldThrowNotFoundException() {
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	#endregion

	#region UpdateAsync Extreme Scenarios

	[Fact]
	public async Task UpdateAsync_WithNonExistentParty_ShouldThrowNotFoundException() {
		var request = new UpdatePartyRequest("New", "Name", "new@test.com");

		var act = () => _service.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task UpdateAsync_WithEmptyGuid_ShouldThrowNotFoundException() {
		var request = new UpdatePartyRequest("New", "Name", "new@test.com");

		var act = () => _service.UpdateAsync(Guid.Empty, request, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact(Skip = "InMemory database does not enforce unique constraints - would pass with PostgreSQL")]
	public async Task UpdateAsync_WithDuplicateEmail_ShouldThrowDbUpdateException() {
		// Arrange
		var party1 = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);
		await _service.CreateAsync(
			new CreatePartyRequest("Jane", "Smith", "jane@test.com"),
			CancellationToken.None);

		// Act - try to update party1 with jane's email
		var request = new UpdatePartyRequest("John", "Doe", "jane@test.com");
		var act = () => _service.UpdateAsync(party1.Id, request, CancellationToken.None);

		// Assert - With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task UpdateAsync_WithSameEmail_ShouldSucceed() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var request = new UpdatePartyRequest("Johnny", "Doe", "john@test.com");
		var result = await _service.UpdateAsync(party.Id, request, CancellationToken.None);

		result.FirstName.Should().Be("Johnny");
		result.Email.Should().Be("john@test.com");
	}

	[Fact]
	public async Task UpdateAsync_WithVeryLongValues_ShouldSucceed() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var longFirstName = new string('A', 100);
		var longLastName = new string('B', 100);
		var request = new UpdatePartyRequest(longFirstName, longLastName, "john@test.com");

		var result = await _service.UpdateAsync(party.Id, request, CancellationToken.None);

		result.FirstName.Should().Be(longFirstName);
		result.LastName.Should().Be(longLastName);
	}

	[Fact(Skip = "InMemory database does not enforce max length constraints - would pass with PostgreSQL")]
	public async Task UpdateAsync_WithValuesExceedingMaxLength_ShouldThrowDbUpdateException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var tooLongName = new string('A', 101);
		var request = new UpdatePartyRequest(tooLongName, "Doe", "john@test.com");

		var act = () => _service.UpdateAsync(party.Id, request, CancellationToken.None);

		// With PostgreSQL: await act.Should().ThrowAsync<DbUpdateException>();
		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task UpdateAsync_MultipleUpdates_ShouldTrackEachUpdate() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		for (int i = 0; i < 10; i++) {
			var request = new UpdatePartyRequest($"Name{i}", "Doe", $"email{i}@test.com");
			await _service.UpdateAsync(party.Id, request, CancellationToken.None);
		}

		var finalParty = await _service.GetByIdAsync(party.Id, CancellationToken.None);
		finalParty.FirstName.Should().Be("Name9");
		finalParty.Email.Should().Be("email9@test.com");
	}

	#endregion

	#region DeleteAsync Extreme Scenarios

	[Fact]
	public async Task DeleteAsync_WithNonExistentParty_ShouldThrowNotFoundException() {
		var act = () => _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task DeleteAsync_WithEmptyGuid_ShouldThrowNotFoundException() {
		var act = () => _service.DeleteAsync(Guid.Empty, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task DeleteAsync_WithPartyThatHasRoles_ShouldCascadeDelete() {
		var party = new DomainParty("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		await _service.DeleteAsync(party.Id, CancellationToken.None);

		var partyCount = await _context.Parties.CountAsync();
		var roleCount = await _context.PartyRoles.CountAsync();
		partyCount.Should().Be(0);
		roleCount.Should().Be(0);
	}

	[Fact]
	public async Task DeleteAsync_AlreadyDeletedParty_ShouldThrowNotFoundException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		await _service.DeleteAsync(party.Id, CancellationToken.None);

		var act = () => _service.DeleteAsync(party.Id, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	#endregion

	#region AssignRoleAsync Extreme Scenarios

	[Fact]
	public async Task AssignRoleAsync_WithNonExistentParty_ShouldThrowNotFoundException() {
		var act = () => _service.AssignRoleAsync(Guid.NewGuid(), RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task AssignRoleAsync_WithEmptyGuid_ShouldThrowNotFoundException() {
		var act = () => _service.AssignRoleAsync(Guid.Empty, RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Theory]
	[InlineData((RoleType)(-1))]
	[InlineData((RoleType)2)]
	[InlineData((RoleType)999)]
	[InlineData((RoleType)int.MaxValue)]
	public async Task AssignRoleAsync_WithInvalidRoleType_ShouldThrowDomainException(RoleType invalidRole) {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var act = () => _service.AssignRoleAsync(party.Id, invalidRole, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}

	[Fact]
	public async Task AssignRoleAsync_WhenAllRolesAssigned_ShouldThrowDomainException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		await _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);
		await _service.AssignRoleAsync(party.Id, RoleType.Customer, CancellationToken.None);

		// Try to assign Author again
		var act = () => _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}

	[Fact]
	public async Task AssignRoleAsync_ConcurrentAssignments_ShouldHandleCorrectly() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		// First assignment should succeed
		await _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);

		// Second assignment of same role should fail
		var act = () => _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);
		await act.Should().ThrowAsync<DomainException>();

		// Third assignment of different role should succeed
		var result = await _service.AssignRoleAsync(party.Id, RoleType.Customer, CancellationToken.None);
		result.Roles.Should().HaveCount(2);
	}

	[Fact]
	public async Task AssignRoleAsync_MultiplePartiesSameRole_ShouldSucceed() {
		var party1 = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);
		var party2 = await _service.CreateAsync(
			new CreatePartyRequest("Jane", "Smith", "jane@test.com"),
			CancellationToken.None);

		await _service.AssignRoleAsync(party1.Id, RoleType.Author, CancellationToken.None);
		await _service.AssignRoleAsync(party2.Id, RoleType.Author, CancellationToken.None);

		var result1 = await _service.GetByIdAsync(party1.Id, CancellationToken.None);
		var result2 = await _service.GetByIdAsync(party2.Id, CancellationToken.None);

		result1.Roles.Should().Contain("Author");
		result2.Roles.Should().Contain("Author");
	}

	#endregion

	#region RemoveRoleAsync Extreme Scenarios

	[Fact]
	public async Task RemoveRoleAsync_WithNonExistentParty_ShouldThrowNotFoundException() {
		var act = () => _service.RemoveRoleAsync(Guid.NewGuid(), RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Theory]
	[InlineData((RoleType)(-1))]
	[InlineData((RoleType)2)]
	[InlineData((RoleType)999)]
	public async Task RemoveRoleAsync_WithInvalidRoleType_ShouldThrowDomainException(RoleType invalidRole) {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var act = () => _service.RemoveRoleAsync(party.Id, invalidRole, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}

	[Fact]
	public async Task RemoveRoleAsync_WhenRoleNotAssigned_ShouldThrowDomainException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		var act = () => _service.RemoveRoleAsync(party.Id, RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}

	[Fact]
	public async Task RemoveRoleAsync_AlreadyRemovedRole_ShouldThrowDomainException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		await _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);
		await _service.RemoveRoleAsync(party.Id, RoleType.Author, CancellationToken.None);

		var act = () => _service.RemoveRoleAsync(party.Id, RoleType.Author, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}

	#endregion

	#region GetAllAsync Extreme Scenarios

	[Fact]
	public async Task GetAllAsync_WithNoParties_ShouldReturnEmptyList() {
		var result = await _service.GetAllAsync(CancellationToken.None);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_WithLargeNumberOfParties_ShouldReturnAll() {
		// Create many parties
		for (int i = 0; i < 1000; i++) {
			_context.Parties.Add(new DomainParty(
				$"User{i}",
				$"Test{i}",
				$"user{i}@test.com"
			));
		}
		await _context.SaveChangesAsync();

		var result = await _service.GetAllAsync(CancellationToken.None);

		result.Should().HaveCount(1000);
	}

	[Fact]
	public async Task GetAllAsync_WithPartiesHavingManyRoles_ShouldReturnCorrectRoles() {
		// Note: System only supports 2 roles max per party
		var party = new DomainParty("John", "Doe", "john@test.com");
		party.AssignRole(RoleType.Author);
		party.AssignRole(RoleType.Customer);
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var result = await _service.GetAllAsync(CancellationToken.None);

		var partyResult = result.Single();
		partyResult.Roles.Should().HaveCount(2);
		partyResult.Roles.Should().Contain("Author");
		partyResult.Roles.Should().Contain("Customer");
	}

	#endregion

	#region CancellationToken Extreme Scenarios

	[Fact]
	public async Task CreateAsync_WithCancelledToken_ShouldThrowOperationCanceledException() {
		var cts = new CancellationTokenSource();
		cts.Cancel();
		var request = new CreatePartyRequest("John", "Doe", "john@test.com");

		var act = () => _service.CreateAsync(request, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task GetByIdAsync_WithCancelledToken_ShouldThrowOperationCanceledException() {
		var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = () => _service.GetByIdAsync(Guid.NewGuid(), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task GetAllAsync_WithCancelledToken_ShouldThrowOperationCanceledException() {
		var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = () => _service.GetAllAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	#endregion

	#region Event Publishing Extreme Scenarios

	[Fact]
	public async Task CreateAsync_WhenPublisherThrows_ShouldPropagateException() {
		_publisherMock.Setup(p => p.PublishAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Publisher failed"));

		var request = new CreatePartyRequest("John", "Doe", "john@test.com");

		var act = () => _service.CreateAsync(request, CancellationToken.None);

		await act.Should().ThrowAsync<Exception>().WithMessage("Publisher failed");
	}

	[Fact]
	public async Task UpdateAsync_WhenPublisherThrows_ShouldPropagateException() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		_publisherMock.Setup(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "PartyUpdated"),
			It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Publisher failed"));

		var request = new UpdatePartyRequest("Jane", "Doe", "jane@test.com");
		var act = () => _service.UpdateAsync(party.Id, request, CancellationToken.None);

		await act.Should().ThrowAsync<Exception>().WithMessage("Publisher failed");
	}

	#endregion

	#region Stress Tests

	[Fact]
	public async Task StressTest_MultipleOperationsOnSameParty() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		// Rapid role assign/remove cycles
		for (int i = 0; i < 50; i++) {
			await _service.AssignRoleAsync(party.Id, RoleType.Author, CancellationToken.None);
			await _service.RemoveRoleAsync(party.Id, RoleType.Author, CancellationToken.None);
		}

		var result = await _service.GetByIdAsync(party.Id, CancellationToken.None);
		result.Roles.Should().BeEmpty();
	}

	[Fact]
	public async Task StressTest_MultipleUpdatesToSameParty() {
		var party = await _service.CreateAsync(
			new CreatePartyRequest("John", "Doe", "john@test.com"),
			CancellationToken.None);

		// Rapid updates
		for (int i = 0; i < 100; i++) {
			var request = new UpdatePartyRequest(
				$"Name{i % 10}",
				$"Last{i % 5}",
				$"email{i}@test.com"
			);
			await _service.UpdateAsync(party.Id, request, CancellationToken.None);
		}

		var result = await _service.GetByIdAsync(party.Id, CancellationToken.None);
		result.FirstName.Should().Be("Name9");
		result.LastName.Should().Be("Last4");
	}

	#endregion
}
