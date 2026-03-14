using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Party.API.Domain;

namespace Party.API.Infrastructure.Persistence.Configurations;

public class PartyRoleConfiguration : IEntityTypeConfiguration<PartyRole> {
	public void Configure(EntityTypeBuilder<PartyRole> builder) {
		builder.HasKey(r => r.Id);

		builder.Property(r => r.RoleType)
			.HasConversion<int>();

		// Prevent duplicate roles per party
		builder.HasIndex(r => new { r.PartyId, r.RoleType }).IsUnique();
	}
}
