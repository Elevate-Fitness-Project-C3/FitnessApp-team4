using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuditService.Persistence;

public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(AuditDbContextFactory).Assembly.GetName().Name
        });
        var connection = AuditDatabaseConfiguration.GetConnectionString(builder.Configuration);
        return new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(connection, sql => sql.CommandTimeout(5))
            .EnableSensitiveDataLogging(false).EnableDetailedErrors(false).Options);
    }
}
