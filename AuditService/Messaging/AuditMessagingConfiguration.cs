using AuditService.Persistence;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuditService.Messaging;

public static class AuditMessagingConfiguration
{
    public static IServiceCollection AddAuditMessaging(this IServiceCollection services, IConfiguration configuration, bool development)
    {
        var options = configuration.GetSection("Messaging").Get<AuditMessagingOptions>() ?? new();
        services.AddHealthChecks().AddCheck("audit-messaging-enabled", () => options.Enabled
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Audit messaging is disabled."), tags: ["masstransit"]);
        if (!options.Enabled) return services;
        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.UserName)
            || string.IsNullOrWhiteSpace(options.Password) || options.Sources.Length == 0
            || (!development && !options.UseTls)
            || options.Port == 0 || string.IsNullOrWhiteSpace(options.VirtualHost)
            || options.Sources.Any(x => !ValidSource(x.SourceService) || !AuditEventValidation.ValidProfile(x))
            || options.Sources.Select(x => x.SourceService).Distinct(StringComparer.Ordinal).Count() != options.Sources.Length)
            throw new InvalidOperationException("Audit messaging requires explicit broker settings, unique and valid source profiles, action policies, and TLS outside Development.");
        services.AddSingleton<IOptions<AuditMessagingOptions>>(Options.Create(options));
        services.AddScoped<AuditEventIngestor>();
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<AuditEventRecordedConsumer>();
            bus.AddEntityFrameworkOutbox<AuditDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.DuplicateDetectionWindow = TimeSpan.FromDays(1);
                outbox.QueryDelay = TimeSpan.FromSeconds(5);
            });
            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.OverrideDefaultBusEndpointQueueName($"audit-bus-{Guid.NewGuid():N}");
                rabbit.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.UserName);
                    host.Password(options.Password);
                    if (options.UseTls) host.UseSsl(ssl => ssl.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                });
                foreach (var source in options.Sources)
                {
                    rabbit.ReceiveEndpoint($"audit-ingest-{source.SourceService}-v1", endpoint =>
                    {
                        endpoint.Durable = true;
                        endpoint.AutoDelete = false;
                        endpoint.SetQuorumQueue();
                        endpoint.ConfigureConsumeTopology = false;
                        endpoint.Bind($"audit.{source.SourceService}.v1");
                        endpoint.PrefetchCount = 16;
                        endpoint.ConcurrentMessageLimit = 8;
                        endpoint.PublishFaults = false;
                        endpoint.UseMessageRetry(retry =>
                        {
                            retry.Handle<SqlException>();
                            retry.Handle<DbUpdateException>();
                            retry.Handle<DbUpdateConcurrencyException>();
                            retry.Handle<TimeoutException>();
                            retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                        });
                        endpoint.UseEntityFrameworkOutbox<AuditDbContext>(context);
                        endpoint.ConfigureConsumer<AuditEventRecordedConsumer>(context);
                    });
                }
            });
        });
        services.Configure<MassTransitHostOptions>(host =>
        {
            host.WaitUntilStarted = false;
            host.StartTimeout = TimeSpan.FromSeconds(30);
            host.StopTimeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }

    public static bool ValidSource(string? value) => value is { Length: > 0 and <= 64 }
        && value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}
