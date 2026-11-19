namespace IdentityService.Persistence;

public static class SecurityAdministrationLock
{
    public static Task AcquireAsync(AuthDbContext db, CancellationToken cancellationToken)
        => db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'identity-security-administration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51002, 'Security administration is busy.', 1;
            """, cancellationToken);
}
