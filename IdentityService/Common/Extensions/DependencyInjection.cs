using MapsterMapper;
using System.Reflection;
namespace IdentityService.Common.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        AddMediatRServices(services, assembly);
        AddValidationServices(services, assembly);
        AddMapping(services, assembly);
        return services;
    }
    private static void AddMediatRServices(IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
    }
    private static void AddValidationServices(IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
    }
    private static void AddMapping(IServiceCollection services, Assembly assembly)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }
}
