using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using IdentityService.Persistence.Configurations;
using MassTransit;

namespace IdentityService.Persistence;

public class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>, IDataProtectionKeyContext
{
    private readonly IdentityService.Security.Auditing.AuditActorContext? actor;
    public AuthDbContext(DbContextOptions<AuthDbContext> options, IdentityService.Security.Auditing.AuditActorContext? actor = null): base(options)
    {
        this.actor = actor;
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
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
        SecurityConfiguration.Configure(builder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditMetadata();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditMetadata();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditMetadata()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is SecurityAuditRecord && entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Security audit history is append-only.");
            if (entry.Entity is not (ApplicationUser or IdentityRole or Permission or PermissionGroup)) continue;
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedBy").CurrentValue = actor is null ? "system:identity" : actor.EntityActorId;
                entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
                entry.Property("LastModifiedBy").CurrentValue = null;
                entry.Property("LastModifiedAt").CurrentValue = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var name in new[] { "CreatedBy", "CreatedAt" })
                {
                    entry.Property(name).CurrentValue = entry.Property(name).OriginalValue;
                    entry.Property(name).IsModified = false;
                }
                var changed = entry.Properties.Any(p => p.IsModified && p.Metadata.Name is not ("CreatedBy" or "CreatedAt" or "LastModifiedBy" or "LastModifiedAt" or "ConcurrencyStamp") && !Equals(p.CurrentValue, p.OriginalValue));
                if (changed)
                {
                    entry.Property("LastModifiedBy").CurrentValue = actor?.EntityActorId;
                    entry.Property("LastModifiedAt").CurrentValue = DateTimeOffset.UtcNow;
                }
                else
                {
                    entry.Property("LastModifiedBy").CurrentValue = entry.Property("LastModifiedBy").OriginalValue;
                    entry.Property("LastModifiedAt").CurrentValue = entry.Property("LastModifiedAt").OriginalValue;
                }
            }
        }
    }
}
