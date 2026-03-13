using Microsoft.EntityFrameworkCore;
using Party.API.Domain;
using DomainParty = Party.API.Domain.Party;

namespace Party.API.Infrastructure.Persistence;

public class PartyDbContext : DbContext {
	public DbSet<DomainParty> Parties => Set<DomainParty>();
	public DbSet<PartyRole> PartyRoles => Set<PartyRole>();

	public PartyDbContext(DbContextOptions<PartyDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartyDbContext).Assembly);
	}
}
