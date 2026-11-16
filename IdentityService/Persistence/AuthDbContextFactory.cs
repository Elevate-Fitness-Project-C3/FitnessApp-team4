using Microsoft.EntityFrameworkCore.Design;
namespace IdentityService.Persistence;
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables().AddCommandLine(args).Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            connection = "Server=invalid.invalid;Database=IdentityDb;Integrated Security=True;Connect Timeout=2";
        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlServer(connection).Options);
    }
}
