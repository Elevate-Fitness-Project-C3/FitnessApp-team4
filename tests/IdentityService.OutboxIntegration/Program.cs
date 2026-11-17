using System.Collections.Concurrent;
using IdentityService.Email;
using IdentityService.Identity;
using IdentityService.Persistence;
using IdentityService.Security.Options;
using IdentityService.Security.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var connection = Environment.GetEnvironmentVariable("OUTBOX_TEST_CONNECTION") ?? throw new Exception("Missing isolated test connection.");
if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("IdentityOutboxTest_", StringComparison.Ordinal))
    throw new InvalidOperationException("Use a dedicated database named IdentityOutboxTest_*.");
var services = new ServiceCollection();
var sender = new DelayedSender();
var settings = new EmailOutboxOptions { LeaseSeconds = 2, RenewalSeconds = 1, ProcessingTimeoutSeconds = 20 };
var otp = Options.Create(new OtpOptions { ResendCooldownSeconds = 0 });
services.AddLogging();
services.AddDbContext<AuthDbContext>(x => x.UseSqlServer(connection));
services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();
services.AddDataProtection().UseEphemeralDataProtectionProvider();
services.AddSingleton(Options.Create(settings));
services.AddSingleton(otp);
services.AddSingleton(Options.Create(new EmailBrandingOptions { ApplicationName = "<New & Brand>", TeamName = "New <Team>" }));
services.AddSingleton<IEmailSender>(sender);
services.AddSingleton<AuthEmailSignal>();
services.AddScoped<UserLockScope>();
services.AddScoped<EmailVerificationCodeStore>();
services.AddScoped<AuthEmailSender>();
services.AddScoped<AuthEmailQueue>();
await using var provider = services.BuildServiceProvider();
await using (var scope = provider.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();

var signal = provider.GetRequiredService<AuthEmailSignal>();
var scopes = provider.GetRequiredService<IServiceScopeFactory>();
using var worker1 = new AuthEmailWorker(scopes, signal, otp, Options.Create(settings), NullLogger<AuthEmailWorker>.Instance);
using var worker2 = new AuthEmailWorker(scopes, signal, otp, Options.Create(settings), NullLogger<AuthEmailWorker>.Instance);
await worker1.StartAsync(default);
await worker2.StartAsync(default);
using var pumpCancellation = new CancellationTokenSource();
var pump = Task.Run(async () =>
{
    try { while (true) { signal.Notify(); await Task.Delay(100, pumpCancellation.Token); } }
    catch (OperationCanceledException) { }
});
try
{
    var first = await AddMessageAsync();
    await WaitAsync(async () => (await ReadAsync(first)).ProcessedOn != null);
    Check(sender.Calls == 1, "Two workers delivered the same leased message.");
    Check(sender.Bodies.Single().Contains("&lt;New &amp; Brand&gt;") && sender.Bodies.Single().Contains("New &lt;Team&gt;"), "Branding not HTML encoded.");
    Check(!sender.Bodies.Single().Contains("Fitness"), "Legacy branding leaked.");
    Console.WriteLine("PASS: two workers, 5-second send/2-second lease, renewal prevents takeover; branded HTML encoded.");

    sender.Block = true;
    var second = await AddMessageAsync();
    await WaitAsync(() => Task.FromResult(sender.Calls == 2));
    var replacement = Guid.NewGuid();
    await using (var scope = provider.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.EmailOutbox.Where(x => x.Id == second).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.LockToken, (Guid?)replacement)
            .SetProperty(x => x.LockedUntil, (DateTime?)DateTime.UtcNow.AddMinutes(1)));
    }
    await sender.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(8));
    await Task.Delay(300);
    var lost = await ReadAsync(second);
    Check(lost.LockToken == replacement && lost.Attempts == 0 && lost.ProcessedOn == null, "Old owner updated successor state.");
    Console.WriteLine("PASS: lease loss cancels sending; old owner cannot complete, defer, or retry successor's row.");
    await using (var scope = provider.CreateAsyncScope())
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().EmailOutbox.Where(x => x.Id == second)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedOn, (DateTime?)DateTime.UtcNow));
}
finally
{
    pumpCancellation.Cancel();
    await pump;
    await worker1.StopAsync(default);
    await worker2.StopAsync(default);
}

var failed = await AddMessageAsync(4);
await using (var scope = provider.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var check = new EmailOutboxHealthCheck(db, Options.Create(settings));
    Check((await check.CheckHealthAsync(new HealthCheckContext())).Status == HealthStatus.Degraded, "Exhaustion not detected.");
    var sql = await File.ReadAllTextAsync("IdentityService/Operations/RetryFailedEmail.sql");
    sql = sql.Replace("DECLARE @MessageId bigint = NULL;", $"DECLARE @MessageId bigint = {failed};");
    await db.Database.ExecuteSqlRawAsync(sql);
    var retried = await ReadAsync(failed);
    Check(retried.Attempts == 0 && retried.LockToken == null, "Manual retry did not reactivate row.");
    Check((await check.CheckHealthAsync(new HealthCheckContext())).Status == HealthStatus.Healthy, "Health did not recover.");
    await db.EmailOutbox.Where(x => x.Id == failed).ExecuteUpdateAsync(s => s
        .SetProperty(x => x.Attempts, 4).SetProperty(x => x.LockToken, (Guid?)Guid.NewGuid())
        .SetProperty(x => x.LockedUntil, (DateTime?)DateTime.UtcNow.AddMinutes(1)));
    try { await db.Database.ExecuteSqlRawAsync(sql); throw new Exception("Active lease was requeued."); }
    catch (SqlException ex) when (ex.Number == 50002) { }
    Check((await ReadAsync(failed)).Attempts == 4, "Refused retry still changed attempts.");
    Console.WriteLine("PASS: degraded health, atomic operator retry, healthy recovery, active lease protected.");
}

async Task<long> AddMessageAsync(int attempts = 0)
{
    await using var scope = provider.CreateAsyncScope();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var email = $"outbox-{Guid.NewGuid():N}@example.test";
    var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = "User", EmailConfirmed = true };
    Check((await users.CreateAsync(user)).Succeeded, "Test user creation failed.");
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var message = new EmailOutboxMessage { UserId = user.Id, Purpose = OtpPurpose.ResetPassword, Attempts = attempts,
        CreatedOn = DateTime.UtcNow, NextAttemptOn = DateTime.UtcNow };
    db.EmailOutbox.Add(message);
    await db.SaveChangesAsync();
    return message.Id;
}
async Task<EmailOutboxMessage> ReadAsync(long id)
{
    await using var scope = provider.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<AuthDbContext>().EmailOutbox.AsNoTracking().SingleAsync(x => x.Id == id);
}
static async Task WaitAsync(Func<Task<bool>> condition)
{
    var until = DateTime.UtcNow.AddSeconds(18);
    while (!await condition()) { if (DateTime.UtcNow > until) throw new Exception("Test timed out."); await Task.Delay(100); }
}
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

sealed class DelayedSender : IEmailSender
{
    public int Calls;
    public volatile bool Block;
    public ConcurrentBag<string> Bodies { get; } = new();
    public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Calls);
        Bodies.Add(htmlMessage);
        try { await Task.Delay(Block ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5), cancellationToken); }
        catch (OperationCanceledException) { Canceled.TrySetResult(); throw; }
    }
}
