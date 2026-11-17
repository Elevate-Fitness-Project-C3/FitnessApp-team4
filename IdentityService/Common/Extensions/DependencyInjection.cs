using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using IdentityService.Security.Options;
using IdentityService.Security.Tokens;
using IdentityService.Security.Keys;
using IdentityService.Email;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
namespace IdentityService.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        services.AddApplication();
        services.AddHttpContextAccessor();
        services.AddScoped<IdentityService.Security.Auditing.AuditActorContext>();
        services.AddScoped<IdentityService.Security.Auditing.SecurityAuditWriter>();
        services.AddScoped<IdentityService.Features.Roles.SecurityMutationChecks>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, IdentityService.Security.Authorization.ApiAuthorizationResultHandler>();
        services.AddScoped<IdentityService.Security.Authorization.EffectivePermissionResolver>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, IdentityService.Security.Authorization.PermissionPolicyProvider>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, IdentityService.Security.Authorization.PermissionAuthorizationHandler>();
        services.AddPersistence(configuration, isDevelopment);
        services.AddIdentityConfiguration();
        services.AddJwtConfiguration();
        services.AddAuthRateLimiting(configuration);
        services.AddEmailConfiguration();
        IdentityService.Messaging.AuditMessagingConfiguration.AddAuditMessaging(services, configuration, isDevelopment);
        return services;
    }
    private static void AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(config => { config.RegisterServicesFromAssembly(assembly); config.AddOpenBehavior(typeof(ValidationBehavior<,>)); });
        services.AddValidatorsFromAssembly(assembly);
    }
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Database connection configuration is required.");
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<UserLockScope>();
        var protection = services.AddDataProtection().SetApplicationName("IdentityService").PersistKeysToDbContext<AuthDbContext>();
        var certificatePath = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
            protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificatePath, configuration["DataProtection:CertificatePassword"]));
        else if (!isDevelopment)
            throw new InvalidOperationException("Data protection certificate is required outside Development.");
    }
    private static void AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Tokens.EmailConfirmationTokenProvider = EmailConfirmationTokenProvider.ProviderName;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider>(EmailConfirmationTokenProvider.ProviderName);
    }
    private static void AddJwtConfiguration(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName)
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer)
                && !string.IsNullOrWhiteSpace(x.Audience)
                && x.AccessTokenExpirationMinutes is > 0 and <= 1440
                && x.RefreshTokenExpirationDays is > 0 and <= 365
                && x.MaxActiveRefreshTokens is >= 1 and <= 100,
                "Invalid JWT configuration.").ValidateOnStart();
        services.AddOptions<RsaKeyOptions>().BindConfiguration(RsaKeyOptions.SectionName)
            .Validate(x => !string.IsNullOrWhiteSpace(x.PrivateKeyPath)
                && !string.IsNullOrWhiteSpace(x.PublicKeyPath),
                "Invalid RSA key configuration.").ValidateOnStart();
        services.AddSingleton<RsaKeyProvider>();
        services.AddOptions<OtpOptions>().BindConfiguration(OtpOptions.SectionName)
            .Validate(x => x.ExpirationMinutes is >= 2 and <= 60
                && x.MaxAttempts is >= 1 and <= 10
                && x.ResendCooldownSeconds is >= 0 and <= 3600,
                "Invalid OTP configuration.").ValidateOnStart();
        services.AddOptions<EmailConfirmationTokenProviderOptions>().Configure<IOptions<OtpOptions>>((x, configured) =>
        {
            x.Name = EmailConfirmationTokenProvider.ProviderName;
            x.TokenLifespan = TimeSpan.FromMinutes(configured.Value.ExpirationMinutes);
        });
        services.Configure<DataProtectionTokenProviderOptions>(x => x.TokenLifespan = TimeSpan.FromHours(24));
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<RefreshTokenIssuer>();
        services.AddHostedService<RefreshTokenCleanupWorker>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, RsaKeyProvider>((x, configured, rsaKeyProvider) =>
            {
                x.MapInboundClaims = false;
                x.IncludeErrorDetails = false;
                x.TokenValidationParameters = JwtProvider.ValidationParameters(configured.Value, rsaKeyProvider);
                x.Events.OnTokenValidated = async context =>
                {
                    var id = context.Principal?.FindFirst("sub")?.Value;
                    var db = context.HttpContext.RequestServices.GetRequiredService<AuthDbContext>();
                    if (id is null || !await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive, context.HttpContext.RequestAborted))
                        context.Fail("The account is not active.");
                };
            }).ValidateOnStart();
        services.AddAuthorization();
    }
    private static void AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(proxy));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = IdentityService.Common.Responses.ApiResponse<object>.Fail("Too many requests.", StatusCodes.Status429TooManyRequests);
                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
            options.AddPolicy(RateLimitPolicies.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
    }
    private static void AddEmailConfiguration(this IServiceCollection services)
    {
        services.AddOptions<EmailBrandingOptions>().BindConfiguration("EmailBranding")
            .Validate(x => !string.IsNullOrWhiteSpace(x.ApplicationName) && !string.IsNullOrWhiteSpace(x.TeamName)
                && !string.IsNullOrWhiteSpace(x.ConfirmEmailSubject) && !string.IsNullOrWhiteSpace(x.ResetPasswordSubject)
                && !($"{x.ApplicationName}{x.ConfirmEmailSubject}{x.ResetPasswordSubject}".Contains('\r'))
                && !($"{x.ApplicationName}{x.ConfirmEmailSubject}{x.ResetPasswordSubject}".Contains('\n')),
                "Invalid email branding.").ValidateOnStart();
        services.AddOptions<EmailOutboxOptions>().BindConfiguration("EmailOutbox")
            .Validate(x => x.MaxDeliveryAttempts is >= 1 and <= 20 && x.LeaseSeconds is >= 2 and <= 600
                && x.RenewalSeconds >= 1 && x.RenewalSeconds <= x.LeaseSeconds / 2
                && x.ProcessingTimeoutSeconds >= x.LeaseSeconds && x.ProcessingTimeoutSeconds <= 3600,
                "Invalid outbox configuration.").ValidateOnStart();
        services.AddHealthChecks().AddCheck<EmailOutboxHealthCheck>("email-outbox");
        services.AddOptions<EmailOptions>().BindConfiguration("Email")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Host) && x.Port is > 0 and <= 65535
                && System.Net.Mail.MailAddress.TryCreate(x.From, out _)
                && (string.IsNullOrWhiteSpace(x.UserName) == string.IsNullOrWhiteSpace(x.Password)),
                "Invalid email configuration.").ValidateOnStart();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddScoped<AuthEmailQueue>();
        services.AddSingleton<AuthEmailSignal>();
        services.AddHostedService<AuthEmailWorker>();
        services.AddScoped<EmailVerificationCodeStore>();
        services.AddScoped<AuthEmailSender>();
    }
}
