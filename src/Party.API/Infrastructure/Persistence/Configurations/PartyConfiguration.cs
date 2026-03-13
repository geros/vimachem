using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Party.API.Domain;

namespace Party.API.Infrastructure.Persistence.Configurations;

public class PartyConfiguration : IEntityTypeConfiguration<Party> {
	public void Configure(EntityTypeBuilder<Party> builder) {
		builder.HasKey(p => p.Id);

		builder.Property(p => p.FirstName)
			.IsRequired().HasMaxLength(100);

		builder.Property(p => p.LastName)
			.IsRequired().HasMaxLength(100);

		builder.Property(p => p.Email)
			.IsRequired().HasMaxLength(255);

		builder.HasIndex(p => p.Email).IsUnique();

		// Private backing field for Roles
		builder.HasMany(p => p.Roles)
			.WithOne()
			.HasForeignKey(r => r.PartyId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(p => p.Roles)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
