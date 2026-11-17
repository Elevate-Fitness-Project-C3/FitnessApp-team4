namespace IdentityService.Identity;

public sealed class Permission
{
    public string Code { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public string DisplayNameAr { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PermissionGroup
{
    public string Code { get; set; } = "";
    public string? ParentCode { get; set; }
    public string Owner { get; set; } = "IdentityService";
    public string DisplayNameAr { get; set; } = "";
    public int DisplayOrder { get; set; }
}
