using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using IdentityService.Security.Options;
using IdentityService.Security.Tokens;
using IdentityService.Email;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Reflection;
using System.Text;
namespace IdentityService.Common;
public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(config => { config.RegisterServicesFromAssembly(assembly); config.AddOpenBehavior(typeof(ValidationBehavior<,>)); });
        services.AddValidatorsFromAssembly(assembly);
        services.AddMapster();
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddDataProtection().SetApplicationName("IdentityService").PersistKeysToDbContext<AuthDbContext>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Tokens.EmailConfirmationTokenProvider = EmailConfirmationTokenProvider.ProviderName;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AuthDbContext>().AddSignInManager().AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider>(EmailConfirmationTokenProvider.ProviderName);
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName)
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience) && Encoding.UTF8.GetByteCount(x.SigningKey) >= 32 && x.AccessTokenExpirationMinutes is > 0 and <= 1440 && x.RefreshTokenExpirationDays is > 0 and <= 365, "Invalid JWT configuration.").ValidateOnStart();
        services.AddOptions<EmailConfirmationTokenOptions>().BindConfiguration(EmailConfirmationTokenOptions.SectionName)
            .Validate(x => x.ExpirationMinutes == 10, "Email confirmation must expire after 10 minutes.").ValidateOnStart();
        services.AddOptions<EmailConfirmationTokenProviderOptions>().Configure<IOptions<EmailConfirmationTokenOptions>>((x, configured) =>
        {
            x.Name = EmailConfirmationTokenProvider.ProviderName;
            x.TokenLifespan = TimeSpan.FromMinutes(configured.Value.ExpirationMinutes);
        });
        services.Configure<DataProtectionTokenProviderOptions>(x => x.TokenLifespan = TimeSpan.FromHours(24));
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<RefreshTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtOptions>>((x, configured) =>
        {
            x.MapInboundClaims = false;
            x.IncludeErrorDetails = false;
            x.TokenValidationParameters = JwtProvider.ValidationParameters(configured.Value);
        });
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("Auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
        services.AddOptions<EmailOptions>().BindConfiguration("Email").Validate(x => !string.IsNullOrWhiteSpace(x.Host) && x.Port is > 0 and <= 65535 && System.Net.Mail.MailAddress.TryCreate(x.From, out _), "Invalid email configuration.").ValidateOnStart();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddScoped<EmailVerificationCodeStore>();
        services.AddScoped<AuthEmailSender>();
        return services;
    }
}
