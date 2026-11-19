using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Persistence.Configurations;

public static class SecurityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<PermissionGroup>(entity =>
        {
            entity.ToTable("PermissionGroups");
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(160).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.ParentCode).HasMaxLength(160).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.Owner).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DisplayNameAr).HasMaxLength(256).IsRequired();
            entity.HasOne<PermissionGroup>().WithMany().HasForeignKey(x => x.ParentCode).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(160).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.GroupCode).HasMaxLength(160).UseCollation("Latin1_General_100_BIN2").IsRequired();
            entity.Property(x => x.DisplayNameAr).HasMaxLength(256).IsRequired();
            entity.HasOne<PermissionGroup>().WithMany().HasForeignKey(x => x.GroupCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.GroupCode, x.IsActive, x.DisplayOrder });
        });
        builder.Entity<SecurityAuditRecord>(entity =>
        {
            entity.ToTable("SecurityAuditLogs", table => table.HasCheckConstraint("CK_SecurityAuditLogs_Json", "ISJSON([EventPayload]) = 1"));
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.ActorUserId).HasMaxLength(450);
            entity.Property(x => x.Action).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(450);
            entity.Property(x => x.EventPayload).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(x => new { x.TimestampUtc, x.EventId });
            entity.HasIndex(x => new { x.ActorUserId, x.TimestampUtc });
        });
        builder.Entity<AuditReportGrant>(entity =>
        {
            entity.ToTable("AuditReportGrants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ApplicationId).HasMaxLength(100).UseCollation("Latin1_General_100_BIN2").IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(128).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.SourceService).HasMaxLength(64).UseCollation("Latin1_General_100_BIN2").IsRequired();
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.ApplicationId, x.TenantId, x.SourceService }).IsUnique().HasFilter(null);
        });
        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property(x => x.ClaimType).HasMaxLength(128).UseCollation("Latin1_General_100_BIN2");
            entity.Property(x => x.ClaimValue).HasMaxLength(256).UseCollation("Latin1_General_100_BIN2");
            entity.HasIndex(x => new { x.RoleId, x.ClaimValue }).IsUnique().HasFilter("[ClaimType] = N'permission'").HasDatabaseName("UX_RoleClaims_Permission");
        });
        foreach (var type in new[] { typeof(ApplicationUser), typeof(IdentityRole), typeof(Permission), typeof(PermissionGroup) })
        {
            var entity = builder.Entity(type);
            entity.Property<string>("CreatedBy").HasMaxLength(450);
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset(7)");
            entity.Property<string>("LastModifiedBy").HasMaxLength(450);
            entity.Property<DateTimeOffset?>("LastModifiedAt").HasColumnType("datetimeoffset(7)");
        }
    }
}
