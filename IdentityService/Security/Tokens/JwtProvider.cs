using IdentityService.Security.Keys;
using IdentityService.Security.Options;
using Microsoft.IdentityModel.JsonWebTokens;
namespace IdentityService.Security.Tokens;

public sealed class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    private readonly RsaKeyProvider _rsaKeyProvider;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly TokenValidationParameters _expiredTokenValidation;

    public JwtProvider(IOptions<JwtOptions> options, RsaKeyProvider rsaKeyProvider)
    {
        _options = options.Value;
        _rsaKeyProvider = rsaKeyProvider;
        _expiredTokenValidation = ValidationParameters(_options, rsaKeyProvider, validateLifetime: false);
    }

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
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id,
            ["jti"] = Guid.NewGuid().ToString()
        };
        var roleList = roles.ToArray();
        if (roleList.Length == 1) claims["role"] = roleList[0];
        else if (roleList.Length > 1) claims["role"] = roleList;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_options.AccessTokenExpirationMinutes),
            SigningCredentials = _rsaKeyProvider.SigningCredentials,
            Claims = claims
        };
        return (_handler.CreateToken(descriptor), checked(_options.AccessTokenExpirationMinutes * 60));
    }

    public async Task<string?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _handler.ValidateTokenAsync(token, _expiredTokenValidation);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsValid)
            return null;
        return (result.ClaimsIdentity.FindFirst("sub") ?? result.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))?.Value;
    }
}
