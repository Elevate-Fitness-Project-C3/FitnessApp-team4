using Microsoft.Data.SqlClient;

namespace IdentityService.Persistence;

internal static class IdentityDatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");

        SqlConnectionStringBuilder settings;
        try
        {
            settings = new SqlConnectionStringBuilder(connection);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not a valid SQL Server connection string.");
        }

        if (!settings.IntegratedSecurity && !string.IsNullOrEmpty(settings.UserID))
        {
            var password = configuration["Database:Password"];
            if (!string.IsNullOrEmpty(password)) settings.Password = password;
            if (string.IsNullOrEmpty(settings.Password))
                throw new InvalidOperationException("Configure Database:Password for the Identity database login.");
        }

        return settings.ConnectionString;
    }
}
