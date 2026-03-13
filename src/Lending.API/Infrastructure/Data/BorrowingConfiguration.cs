using Lending.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.API.Infrastructure.Data;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing> {
	public void Configure(EntityTypeBuilder<Borrowing> builder) {
		builder.HasKey(b => b.Id);
		builder.Property(b => b.BookTitle).IsRequired().HasMaxLength(500);
		builder.Property(b => b.CustomerName).IsRequired().HasMaxLength(200);

		// Indexes for common queries
		builder.HasIndex(b => b.BookId);
		builder.HasIndex(b => b.CustomerId);
		builder.HasIndex(b => b.ReturnedAt); // filter active borrowings

		// Composite index for the duplicate check
		builder.HasIndex(b => new { b.BookId, b.CustomerId, b.ReturnedAt });
	}
}
