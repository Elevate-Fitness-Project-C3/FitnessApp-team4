using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using IdentityService.Persistence.Configurations;

namespace IdentityService.Persistence;

public class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>, IDataProtectionKeyContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options): base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.SessionId });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
        });
        builder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.ToTable("EmailOutbox");
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId);
            entity.HasIndex(x => new { x.ProcessedOn, x.NextAttemptOn }).HasDatabaseName(DbIndexes.EmailOutboxDue);
            entity.HasIndex(x => new { x.UserId, x.Purpose }).IsUnique()
                  .HasFilter("[ProcessedOn] IS NULL").HasDatabaseName(DbIndexes.EmailOutboxPending);
        });
        builder.ApplyConfiguration(new ApplicationUserConfiguration());


        builder.Entity<IdentityRole>()
            .ToTable("Roles");

        builder.Entity<IdentityUserRole<string>>()
            .ToTable("UserRoles");

        builder.Entity<IdentityUserClaim<string>>()
            .ToTable("UserClaims");

        builder.Entity<IdentityUserLogin<string>>()
            .ToTable("UserLogins");

        builder.Entity<IdentityRoleClaim<string>>()
            .ToTable("RoleClaims");

        builder.Entity<IdentityUserToken<string>>()
            .ToTable("UserTokens");
    }
}
