using Microsoft.Data.SqlClient;

namespace AuditService.Persistence;

internal static class AuditDatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("AuditDb");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:AuditDb through a secure configuration provider.");
        try
        {
            var parsed = new SqlConnectionStringBuilder(connection);
            if (string.IsNullOrWhiteSpace(parsed.DataSource) || string.IsNullOrWhiteSpace(parsed.InitialCatalog))
                throw new ArgumentException();
            if (parsed.ConnectTimeout == 0 || parsed.ConnectTimeout > 5) parsed.ConnectTimeout = 5;
            return parsed.ConnectionString;
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("ConnectionStrings:AuditDb must specify a valid SQL Server and an explicit dedicated database.");
        }
    }
}
