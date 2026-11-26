namespace IdentityService.Security.Auditing;

public static class AuditActions
{
    public const string RoleCreated = "Identity.RoleCreated";
    public const string RoleRenamed = "Identity.RoleRenamed";
    public const string RoleDeleted = "Identity.RoleDeleted";
    public const string RolePermissionsUpdated = "Identity.RolePermissionsUpdated";
    public const string RoleAssigned = "Identity.RoleAssigned";
    public const string RoleRemoved = "Identity.RoleRemoved";
    public const string UserProfileUpdated = "Identity.UserProfileUpdated";
    public const string UserStatusUpdated = "Identity.UserStatusUpdated";
    public const string AuditGrantCreated = "Identity.AuditGrantCreated";
    public const string AuditGrantRemoved = "Identity.AuditGrantRemoved";
    public const string SecurityBootstrap = "Identity.SecurityBootstrap";
    public const string SecurityOperationDenied = "Identity.SecurityOperationDenied";
    public const string SecurityOperationFailed = "Identity.SecurityOperationFailed";

    public static readonly string[] All =
    [
        RoleCreated, RoleRenamed, RoleDeleted, RolePermissionsUpdated, RoleAssigned, RoleRemoved,
        UserStatusUpdated, UserProfileUpdated, AuditGrantCreated, AuditGrantRemoved, SecurityBootstrap,
        SecurityOperationDenied, SecurityOperationFailed
    ];
}
