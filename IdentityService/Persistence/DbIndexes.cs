namespace IdentityService.Persistence;

public static class DbIndexes
{
    public const string Email = "EmailIndex";
    public const string UserName = "UserNameIndex";
    public const string RoleName = "RoleNameIndex";
    public const string EmailOutboxDue = "IX_EmailOutbox_Due";
    public const string EmailOutboxPending = "UX_EmailOutbox_Pending";
    public const string RefreshTokensActive = "IX_RefreshTokens_Active";
    public const string RefreshTokensExpiresOn = "IX_RefreshTokens_ExpiresOn";
}
