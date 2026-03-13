using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Party.API.Application.DTOs;
using Party.API.Application.Interfaces;
using Party.API.Application.Services;
using Party.API.Domain;
using Party.API.Domain.Exceptions;
using Party.API.Infrastructure.Persistence;
using Shared.Events;
using Xunit;

namespace Party.API.Tests.Services;

public sealed class PartyServiceTests : IDisposable {
	private readonly PartyDbContext _context;
	private readonly Mock<IEventPublisher> _publisherMock;
	private readonly PartyService _service;

	public PartyServiceTests() {
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

	[Fact]
	public async Task CreateAsync_ShouldPersistAndPublishEvent() {
		var request = new CreatePartyRequest("George", "Orwell", "orwell@test.com");

		var result = await _service.CreateAsync(request, CancellationToken.None);

		result.Should().NotBeNull();
		result.FirstName.Should().Be("George");
		(await _context.Parties.CountAsync()).Should().Be(1);

		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e =>
				e.EventType == "PartyCreated" &&
				e.EntityType == "Party"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetByIdAsync_WhenNotFound_ShouldThrow() {
		var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task GetByIdAsync_WhenExists_ShouldReturnParty() {
		var party = new Party("Test", "User", "test@test.com");
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var result = await _service.GetByIdAsync(party.Id, CancellationToken.None);

		result.Should().NotBeNull();
		result.Id.Should().Be(party.Id);
		result.FirstName.Should().Be("Test");
	}

	[Fact]
	public async Task GetAllAsync_ShouldReturnAllParties() {
		_context.Parties.AddRange(
			new Party("User1", "Test", "user1@test.com"),
			new Party("User2", "Test", "user2@test.com")
		);
		await _context.SaveChangesAsync();

		var result = await _service.GetAllAsync(CancellationToken.None);

		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task AssignRoleAsync_ShouldAddRoleAndPublish() {
		var party = new Party("Test", "User", "test@test.com");
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var result = await _service.AssignRoleAsync(
			party.Id, RoleType.Customer, CancellationToken.None);

		result.Roles.Should().Contain("Customer");
		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "RoleAssigned"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DeleteAsync_ShouldRemoveAndPublish() {
		var party = new Party("Test", "User", "test@test.com");
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		await _service.DeleteAsync(party.Id, CancellationToken.None);

		(await _context.Parties.CountAsync()).Should().Be(0);
		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "PartyDeleted"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateAndPublish() {
		var party = new Party("Old", "Name", "old@test.com");
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var request = new UpdatePartyRequest("New", "Name", "new@test.com");
		var result = await _service.UpdateAsync(party.Id, request, CancellationToken.None);

		result.FirstName.Should().Be("New");
		result.LastName.Should().Be("Name");
		result.Email.Should().Be("new@test.com");
		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "PartyUpdated"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RemoveRoleAsync_ShouldRemoveAndPublish() {
		var party = new Party("Test", "User", "test@test.com");
		party.AssignRole(RoleType.Customer);
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var result = await _service.RemoveRoleAsync(party.Id, RoleType.Customer, CancellationToken.None);

		result.Roles.Should().BeEmpty();
		_publisherMock.Verify(p => p.PublishAsync(
			It.Is<IntegrationEvent>(e => e.EventType == "RoleRemoved"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RemoveRoleAsync_WhenNotFound_ShouldThrow() {
		var act = () => _service.RemoveRoleAsync(Guid.NewGuid(), RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task AssignRoleAsync_WhenDuplicate_ShouldThrow() {
		var party = new Party("Test", "User", "test@test.com");
		party.AssignRole(RoleType.Customer);
		_context.Parties.Add(party);
		await _context.SaveChangesAsync();

		var act = () => _service.AssignRoleAsync(party.Id, RoleType.Customer, CancellationToken.None);

		await act.Should().ThrowAsync<DomainException>();
	}
}
