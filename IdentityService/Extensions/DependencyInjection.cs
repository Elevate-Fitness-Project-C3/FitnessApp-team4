
namespace IdentityService.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR handlers from the Auth Service assembly.
        services.AddMediatR(cfg =>cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        // Register OpenAPI document generation.
        services.AddOpenApi();
        return services;
    }
    public static IServiceCollection AddPersistence(this IServiceCollection services,IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")??
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddDataProtection();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
