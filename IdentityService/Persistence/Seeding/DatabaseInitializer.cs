using Microsoft.Data.SqlClient;
namespace IdentityService.Persistence.Seeding;
internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var migrate = app.Configuration.GetValue<bool>("DevelopmentSetup:ApplyMigrations");
        if (migrate && !app.Environment.IsDevelopment())
            throw new InvalidOperationException("Automatic migrations are restricted to Development.");
        for (var attempt = 0; ; attempt++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            try
            {
                if (migrate) await db.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
                else if ((await db.Database.GetPendingMigrationsAsync(app.Lifetime.ApplicationStopping)).Any())
                    throw new InvalidOperationException("Pending database migrations must be applied before startup.");
                await IdentityRoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
                return;
            }
            catch (SqlException ex) when (migrate && attempt < 20 && ex.Errors.Cast<SqlError>().Any(e => e.Number is -2 or 2 or 53 or 64 or 233 or 10053 or 10054 or 10060 or 10061 or 17142))
            {
                app.Logger.LogWarning("Database is not ready. Retry {Attempt}.", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(3), app.Lifetime.ApplicationStopping);
            }
        }
    }
}
