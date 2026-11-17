namespace IdentityService.Security.Tokens;
public interface IJwtProvider
{
    (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles);
    Task<string?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
