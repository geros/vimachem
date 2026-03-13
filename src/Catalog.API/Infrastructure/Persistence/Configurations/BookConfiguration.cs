using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.API.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book> {
	public void Configure(EntityTypeBuilder<Book> builder) {
		builder.HasKey(b => b.Id);
		builder.Property(b => b.Title).IsRequired().HasMaxLength(500);
		builder.Property(b => b.ISBN).IsRequired().HasMaxLength(13);
		builder.Property(b => b.AuthorName).IsRequired().HasMaxLength(200);

		builder.HasIndex(b => b.ISBN).IsUnique();
		builder.HasIndex(b => b.Title); // for search
		builder.HasIndex(b => b.AuthorId); // for filtering by author

		builder.HasOne(b => b.Category)
			.WithMany()
			.HasForeignKey(b => b.CategoryId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
