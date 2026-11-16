using IdentityService.Security.Keys;
using IdentityService.Security.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace IdentityService.Security.Tokens;

public sealed class JwtProvider(IOptions<JwtOptions> options, RsaKeyProvider rsaKeyProvider) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;
    private readonly TokenValidationParameters _refreshValidation = ValidationParameters(options.Value, rsaKeyProvider, false);
    public static TokenValidationParameters ValidationParameters(JwtOptions options, RsaKeyProvider rsaKeyProvider, bool validateLifetime = true) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateLifetime = validateLifetime,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = rsaKeyProvider.ValidationKey,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        NameClaimType = "sub",
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    public (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: rsaKeyProvider.SigningCredentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), checked(_options.AccessTokenExpirationMinutes * 60));
    }
    public string? ValidateToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(token, _refreshValidation.Clone(), out _);
            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch (SecurityTokenException) { return null; }
        catch (ArgumentException) { return null; }
    }
}
