using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Party.API.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PartyDbContext> {
	public PartyDbContext CreateDbContext(string[] args) {
		var optionsBuilder = new DbContextOptionsBuilder<PartyDbContext>();
		optionsBuilder.UseNpgsql("Host=localhost;Database=party_db;Username=postgres;Password=postgres");
		return new PartyDbContext(optionsBuilder.Options);
	}
}
