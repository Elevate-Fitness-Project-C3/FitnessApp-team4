using Microsoft.EntityFrameworkCore.Design;
namespace IdentityService.Persistence;
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(AuthDbContextFactory).Assembly.GetName().Name
        });
        var connection = IdentityDatabaseConfiguration.GetConnectionString(builder.Configuration);
        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlServer(connection).Options);
    }
}
