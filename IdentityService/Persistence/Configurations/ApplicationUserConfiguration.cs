using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {

        builder.ToTable("Users");
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.NormalizedEmail)
               .HasDatabaseName("EmailIndex")
               .IsUnique()
               .HasFilter("[NormalizedEmail] IS NOT NULL");
    }
}
