namespace IdentityService.Persistence.Seeding;

public static class PermissionCatalog
{
    public static readonly (string Code, string Arabic)[] Definitions =
    [
        ("Identity.Roles.Read", "عرض الأدوار"), ("Identity.Roles.Create", "إنشاء الأدوار"),
        ("Identity.Roles.Rename", "تسمية الأدوار"), ("Identity.Roles.Delete", "حذف الأدوار"),
        ("Identity.Roles.Permissions.Update", "إدارة منح الصلاحيات"),
        ("Identity.Permissions.Read", "عرض دليل الصلاحيات"),
        ("Identity.Users.Roles.Read", "عرض أدوار المستخدم"), ("Identity.Users.Roles.Assign", "إسناد الأدوار"),
        ("Identity.Users.Roles.Remove", "إزالة الأدوار"), ("Identity.Users.Permissions.Read", "عرض الصلاحيات الفعلية"),
        ("Identity.Users.Update", "إدارة نشاط الحساب"), ("Identity.AuditGrants.Manage", "إدارة نطاقات تقارير التدقيق"),
        ("Audit.Reports.Read", "قراءة تقارير التدقيق"), ("Audit.Reports.ReadSensitiveMetadata", "قراءة بيانات الطلب الحساسة")
    ];
    public static readonly string[] RecoveryPermissions =
    ["Identity.Roles.Read", "Identity.Roles.Create", "Identity.Roles.Permissions.Update", "Identity.Users.Roles.Assign", "Identity.Users.Roles.Remove", "Identity.Users.Update", "Identity.AuditGrants.Manage", "Audit.Reports.Read"];

    public static async Task SeedAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SecurityAdministrationLock.AcquireAsync(db, cancellationToken);
        (string Code, string? Parent, string Arabic)[] groups =
        [
            ("Identity", null, "إدارة الهوية"), ("Identity.Roles", "Identity", "الأدوار"),
            ("Identity.Users", "Identity", "المستخدمون"), ("Identity.Permissions", "Identity", "دليل الصلاحيات"),
            ("Identity.AuditGrants", "Identity", "نطاقات التقارير"), ("Audit", null, "التدقيق المركزي"),
            ("Audit.Reports", "Audit", "التقارير")
        ];
        foreach (var definition in groups)
        {
            var group = await db.Set<PermissionGroup>().FindAsync([definition.Code], cancellationToken);
            if (group is null) { group = new PermissionGroup { Code = definition.Code }; db.Add(group); }
            group.ParentCode = definition.Parent;
            group.DisplayNameAr = definition.Arabic;
            group.Owner = definition.Code.StartsWith("Audit", StringComparison.Ordinal) ? "AuditService" : "IdentityService";
        }
        var permissions = await db.Set<Permission>().ToDictionaryAsync(x => x.Code, cancellationToken);
        for (var index = 0; index < Definitions.Length; index++)
        {
            var definition = Definitions[index];
            if (!permissions.TryGetValue(definition.Code, out var permission))
            {
                permission = new Permission { Code = definition.Code, GroupCode = "Identity" };
                db.Add(permission);
            }
            permission.DisplayNameAr = definition.Arabic;
            permission.GroupCode = string.Join('.', definition.Code.Split('.').Take(2));
            permission.DisplayOrder = index;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
