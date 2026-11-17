using IdentityService.Security.Tokens;
namespace IdentityService.Email;
public sealed class AuthEmailWorker(IServiceScopeFactory scopes, ILogger<AuthEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var codes = scope.ServiceProvider.GetRequiredService<EmailVerificationCodeStore>();
                var sender = scope.ServiceProvider.GetRequiredService<AuthEmailSender>();
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);
                var item = await db.Set<IdentityUserToken<string>>().AsNoTracking()
                    .Where(x => x.LoginProvider == AuthEmailQueue.Provider).OrderBy(x => x.UserId).ThenBy(x => x.Name)
                    .FirstOrDefaultAsync(stoppingToken);
                if (item is null)
                {
                    await tx.CommitAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }
                await RefreshTokenSessionLock.AcquireAsync(db, item.UserId, stoppingToken);
                var claimed = await db.Set<IdentityUserToken<string>>().Where(x => x.UserId == item.UserId
                    && x.LoginProvider == item.LoginProvider && x.Name == item.Name && x.Value == item.Value).ExecuteDeleteAsync(stoppingToken);
                if (claimed != 1)
                    continue;
                var user = await users.FindByIdAsync(item.UserId);
                string? code = null;
                var validPurpose = Enum.TryParse<OtpPurpose>(item.Name, out var purpose);
                if (user is not null && validPurpose && user.EmailConfirmed == (purpose == OtpPurpose.ResetPassword))
                    code = await codes.IssueAsync(user, purpose, stoppingToken);
                await tx.CommitAsync(stoppingToken);
                if (user is not null && code is not null)
                    await sender.SendAsync(user, purpose, code, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError("Authentication email worker failed. ErrorType={ErrorType}", ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
