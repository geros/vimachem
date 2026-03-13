using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.API.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category> {
	public void Configure(EntityTypeBuilder<Category> builder) {
		builder.HasKey(c => c.Id);
		builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
		builder.HasIndex(c => c.Name).IsUnique();

		builder.HasMany(c => c.Books)
			.WithOne(b => b.Category)
			.HasForeignKey(b => b.CategoryId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
