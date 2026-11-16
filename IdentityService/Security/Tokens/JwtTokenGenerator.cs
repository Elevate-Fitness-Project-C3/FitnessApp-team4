using IdentityService.Security.Keys;
using IdentityService.Security.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdentityService.Security.Tokens;

public sealed class JwtTokenGenerator(
    IOptions<JwtOptions> options, RsaKeyProvider rsaKeyProvider)
{
    private readonly JwtOptions _options = options.Value;
    public (string Token, int ExpiresIn) GenerateAccessToken(
        ApplicationUser user, IEnumerable<string> roles)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: rsaKeyProvider.SigningCredentials);
        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            checked(_options.AccessTokenExpirationMinutes * 60)
        );
    }
}