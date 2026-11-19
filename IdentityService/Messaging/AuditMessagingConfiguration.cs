using MassTransit;

namespace IdentityService.Messaging;

public static class AuditMessagingConfiguration
{
    public static IServiceCollection AddAuditMessaging(this IServiceCollection services, IConfiguration configuration, bool development)
    {
        var enabled = configuration.GetValue<bool>("Messaging:Enabled");
        services.AddHealthChecks().AddCheck("audit-messaging-enabled", () => enabled
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Audit messaging is disabled."), tags: ["masstransit"]);
        if (!enabled) return services;
        var host = configuration["Messaging:Host"];
        var user = configuration["Messaging:UserName"];
        var password = configuration["Messaging:Password"];
        var tls = configuration.GetValue("Messaging:UseTls", true);
        var port = configuration.GetValue<ushort>("Messaging:Port", 5672);
        var virtualHost = configuration["Messaging:VirtualHost"] ?? "audit";
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password)
            || port == 0 || string.IsNullOrWhiteSpace(virtualHost) || !development && !tls)
            throw new InvalidOperationException("Identity audit messaging requires valid broker settings, explicit credentials, and TLS outside Development.");
        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<AuthDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(5);
            });
            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.OverrideDefaultBusEndpointQueueName($"identity-bus-{Guid.NewGuid():N}");
                rabbit.Host(host, port, virtualHost, settings =>
                {
                    settings.Username(user);
                    settings.Password(password);
                    if (tls) settings.UseSsl(ssl => ssl.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                });
            });
        });
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = false;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
