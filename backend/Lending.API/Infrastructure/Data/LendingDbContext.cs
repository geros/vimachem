using Lending.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lending.API.Infrastructure.Data;

public class LendingDbContext : DbContext {
	public DbSet<Borrowing> Borrowings => Set<Borrowing>();

	public LendingDbContext(DbContextOptions<LendingDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(LendingDbContext).Assembly);
	}
}
