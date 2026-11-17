using AuditService.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditService.Persistence.Configurations;

public sealed class AuditEventReceiptConfiguration : IEntityTypeConfiguration<AuditEventReceipt>
{
    public void Configure(EntityTypeBuilder<AuditEventReceipt> builder)
    {
        builder.ToTable("AuditEventReceipts", "dbo", table =>
        {
            table.HasCheckConstraint("CK_AuditEventReceipts_Identity", "LEN([SourceService]) > 0 AND [EventId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint("CK_AuditEventReceipts_Utc", "DATEPART(TZOFFSET, [FirstIngestedAtUtc]) = 0 AND [FirstIngestedAtUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset)");
        });
        builder.HasKey(x => new { x.SourceService, x.EventId });
        builder.Property(x => x.SourceService).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        builder.Property(x => x.EventId).HasColumnType("uniqueidentifier").ValueGeneratedNever().IsRequired();
        builder.Property(x => x.PayloadHash).HasColumnType("binary(32)").HasMaxLength(32).IsFixedLength().IsRequired();
        builder.Property(x => x.FirstIngestedAtUtc).HasColumnType("datetimeoffset(7)").HasPrecision(7).IsRequired();
        // No event foreign key: receipts must survive historical event archival.
    }
}
