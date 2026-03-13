using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lending.API.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LendingDbContext> {
	public LendingDbContext CreateDbContext(string[] args) {
		var optionsBuilder = new DbContextOptionsBuilder<LendingDbContext>();
		optionsBuilder.UseNpgsql("Host=localhost;Database=lending_db;Username=postgres;Password=postgres");
		return new LendingDbContext(optionsBuilder.Options);
	}
}
