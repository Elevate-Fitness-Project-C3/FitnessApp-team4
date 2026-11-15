namespace IdentityService.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        AddDatabaseContext(services, configuration);
        return services;
    }
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDataProtection();
        services
            .AddIdentityCore<ApplicationUser>(ConfigureIdentityOptions)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        return services;
    }
    private static void AddDatabaseContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));
    }
    private static void ConfigureIdentityOptions(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.SignIn.RequireConfirmedEmail = true;
    }
}
