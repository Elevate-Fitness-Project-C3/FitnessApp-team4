using Microsoft.Data.SqlClient;
namespace IdentityService.Persistence.Seeding;
internal static class DatabaseInitializer
{
    private static readonly HashSet<int> NotReadyErrors = [-2, 2, 53, 64, 233, 10053, 10054, 10060, 10061, 17142];
    private const int MaxStartupAttempts = 20;
    public static async Task InitializeAsync(WebApplication app)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            try
            {
                if ((await db.Database.GetPendingMigrationsAsync(app.Lifetime.ApplicationStopping)).Any())
                    throw new InvalidOperationException("Apply IdentityService migrations manually with dotnet ef database update before startup.");
                await IdentityRoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
                await PermissionCatalog.SeedAsync(db, app.Lifetime.ApplicationStopping);
                return;
            }
            catch (SqlException ex) when (attempt < MaxStartupAttempts && ex.Errors.Cast<SqlError>().Any(e => NotReadyErrors.Contains(e.Number)))
            {
                app.Logger.LogWarning("Database is not ready. Retry {Attempt}.", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(3), app.Lifetime.ApplicationStopping);
            }
        }
    }
}
