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
        var connection = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before running EF Core tools.");
        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlServer(connection).Options);
    }
}
