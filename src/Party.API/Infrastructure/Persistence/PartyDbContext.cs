using Microsoft.EntityFrameworkCore;
using Party.API.Domain;

namespace Party.API.Infrastructure.Persistence;

public class PartyDbContext : DbContext {
	public DbSet<Party> Parties => Set<Party>();
	public DbSet<PartyRole> PartyRoles => Set<PartyRole>();

	public PartyDbContext(DbContextOptions<PartyDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartyDbContext).Assembly);
	}
}
