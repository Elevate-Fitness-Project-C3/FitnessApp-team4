using AuditService.Auditing;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace AuditService.Persistence;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AuditEventReceipt> AuditEventReceipts => Set<AuditEventReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        modelBuilder.AddInboxStateEntity(entity => entity.ToTable("InboxState", "dbo"));
        modelBuilder.AddOutboxMessageEntity(entity => entity.ToTable("OutboxMessage", "dbo"));
        modelBuilder.AddOutboxStateEntity(entity => entity.ToTable("OutboxState", "dbo"));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectHistoryChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejectHistoryChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejectHistoryChanges()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(entry =>
                entry.Entity is AuditEvent or AuditEventReceipt &&
                entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit history and receipts are append-only. Use the controlled archival process.");
    }
}
