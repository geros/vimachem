using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainParty = Party.API.Domain.Party;

namespace Party.API.Infrastructure.Persistence.Configurations;

public class PartyConfiguration : IEntityTypeConfiguration<DomainParty> {
	public void Configure(EntityTypeBuilder<DomainParty> builder) {
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

		builder.Metadata.FindNavigation(nameof(DomainParty.Roles))
			?.SetPropertyAccessMode(PropertyAccessMode.Field);
	}
}
