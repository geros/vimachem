using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Infrastructure.Persistence;

public class CatalogDbContext : DbContext {
	public DbSet<Book> Books => Set<Book>();
	public DbSet<Category> Categories => Set<Category>();

	public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
	}
}
