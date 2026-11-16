using IdentityService.Security.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace IdentityService.Security.Tokens;
public sealed class JwtProvider(IOptions<JwtOptions> options) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;
    public static TokenValidationParameters ValidationParameters(JwtOptions options, bool validateLifetime = true) => new()
    {
        ValidateIssuer = true, ValidIssuer = options.Issuer,
        ValidateAudience = true, ValidAudience = options.Audience,
        ValidateLifetime = validateLifetime, RequireExpirationTime = true,
        RequireSignedTokens = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = "sub", RoleClaimType = "role", ClockSkew = TimeSpan.FromSeconds(30)
    };
    public (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("sub", user.Id), new("jti", Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now, now.AddMinutes(_options.AccessTokenExpirationMinutes), credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), checked(_options.AccessTokenExpirationMinutes * 60));
    }
    public string? ValidateToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, ValidationParameters(_options, false), out _);
            return principal.FindFirst("sub")?.Value;
        }
        catch (SecurityTokenException) { return null; }
        catch (ArgumentException) { return null; }
    }
}
