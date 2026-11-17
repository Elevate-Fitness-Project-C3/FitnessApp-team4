using AuditService.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditService.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    private const string IdentifierCollation = "Latin1_General_100_BIN2";

    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", "dbo", table =>
        {
            table.HasCheckConstraint("CK_AuditEvents_EventId", "[EventId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint("CK_AuditEvents_Identifiers", "LEN([SourceService]) > 0 AND LEN([ApplicationId]) > 0 AND LEN([Action]) > 0 AND LEN([EntityType]) > 0 AND LEN([CorrelationId]) > 0 AND ([TenantId] IS NULL OR LEN([TenantId]) > 0) AND ([EntityId] IS NULL OR LEN([EntityId]) > 0)");
            table.HasCheckConstraint("CK_AuditEvents_Actor", "([ActorKind] = 1 AND [ActorUserId] IS NOT NULL AND LEN([ActorUserId]) > 0 AND [ActorSystemId] IS NULL) OR ([ActorKind] = 2 AND [ActorSystemId] IS NOT NULL AND LEN([ActorSystemId]) > 0 AND [ActorUserId] IS NULL AND [ActorUserNameSnapshot] IS NULL) OR ([ActorKind] = 3 AND [ActorUserId] IS NULL AND [ActorSystemId] IS NULL AND [ActorUserNameSnapshot] IS NULL)");
            table.HasCheckConstraint("CK_AuditEvents_Status", "[Status] IN (1, 2, 3) AND ([Status] <> 1 OR [EntityId] IS NOT NULL)");
            table.HasCheckConstraint("CK_AuditEvents_Utc", "DATEPART(TZOFFSET, [TimestampUtc]) = 0 AND DATEPART(TZOFFSET, [IngestedAtUtc]) = 0 AND [TimestampUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset) AND [IngestedAtUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset)");
            table.HasCheckConstraint("CK_AuditEvents_OldValues", "[OldValues] IS NULL OR ISJSON([OldValues], OBJECT) = 1");
            table.HasCheckConstraint("CK_AuditEvents_NewValues", "[NewValues] IS NULL OR ISJSON([NewValues], OBJECT) = 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(x => x.EventId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(x => x.SourceService).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired();
        builder.Property(x => x.ApplicationId).HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("nvarchar(128)").HasMaxLength(128).UseCollation(IdentifierCollation).IsRequired(false);
        builder.Property(x => x.ActorKind).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnType("nvarchar(450)").HasMaxLength(450).UseCollation(IdentifierCollation).IsRequired(false);
        builder.Property(x => x.ActorUserNameSnapshot).HasColumnType("nvarchar(256)").HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.ActorSystemId).HasColumnType("varchar(128)").HasMaxLength(128).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired(false);
        builder.Property(x => x.Action).HasColumnType("varchar(160)").HasMaxLength(160).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired();
        builder.Property(x => x.EntityType).HasColumnType("varchar(128)").HasMaxLength(128).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired();
        builder.Property(x => x.EntityId).HasColumnType("nvarchar(450)").HasMaxLength(450).UseCollation(IdentifierCollation).IsRequired(false);
        builder.Property(x => x.TimestampUtc).HasColumnType("datetimeoffset(7)").HasPrecision(7).IsRequired();
        builder.Property(x => x.IngestedAtUtc).HasColumnType("datetimeoffset(7)").HasPrecision(7).IsRequired();
        builder.Property(x => x.IpAddress).HasColumnType("varchar(45)").HasMaxLength(45).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.DeviceName).HasColumnType("nvarchar(128)").HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.UserAgent).HasColumnType("nvarchar(1024)").HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.CorrelationId).HasColumnType("varchar(128)").HasMaxLength(128).IsUnicode(false).UseCollation(IdentifierCollation).IsRequired();
        builder.Property(x => x.OldValues).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.NewValues).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();

        builder.HasIndex(x => new { x.SourceService, x.EventId }).IsUnique().HasDatabaseName("UX_AuditEvents_Source_EventId");
        builder.HasIndex(x => new { x.ApplicationId, x.TenantId, x.TimestampUtc, x.Id }).IsDescending(false, false, true, true).HasDatabaseName("IX_AuditEvents_Scope_Time");
        builder.HasIndex(x => new { x.ApplicationId, x.TenantId, x.SourceService, x.TimestampUtc, x.Id }).IsDescending(false, false, false, true, true).HasDatabaseName("IX_AuditEvents_Scope_Source_Time");
        builder.HasIndex(x => new { x.ApplicationId, x.TenantId, x.ActorUserId, x.TimestampUtc, x.Id }).IsDescending(false, false, false, true, true).HasDatabaseName("IX_AuditEvents_Scope_Actor_Time");
        builder.HasIndex(x => new { x.ApplicationId, x.TenantId, x.EntityType, x.EntityId, x.TimestampUtc, x.Id }).IsDescending(false, false, false, false, true, true).HasDatabaseName("IX_AuditEvents_Scope_Entity_Time");
        builder.HasIndex(x => new { x.ApplicationId, x.TenantId, x.CorrelationId, x.Id }).HasDatabaseName("IX_AuditEvents_Scope_Correlation");
    }
}
