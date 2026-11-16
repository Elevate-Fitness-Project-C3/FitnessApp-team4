namespace IdentityService.Identity;
public sealed class RefreshToken
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresOn { get; set; }
    public DateTime? RevokedOn { get; set; }
}
