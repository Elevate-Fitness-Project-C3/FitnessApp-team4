namespace IdentityService.Security.Tokens;
public interface IJwtProvider
{
    (string Token, int ExpiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles);
    string? ValidateToken(string token);
}
