using IdentityService.Security.Tokens;
namespace IdentityService.Identity;

public sealed class EmailOutboxMessage
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public OtpPurpose Purpose { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime NextAttemptOn { get; set; }
    public int Attempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public Guid? LockToken { get; set; }
    public DateTime? ProcessedOn { get; set; }
}
