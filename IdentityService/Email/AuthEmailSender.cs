using IdentityService.Security.Tokens;
namespace IdentityService.Email;
public sealed class AuthEmailSender(EmailSender sender, IOptions<IdentityService.Security.Options.EmailConfirmationTokenOptions> options, ILogger<AuthEmailSender> logger)
{
    public async Task SendAsync(ApplicationUser user, OtpPurpose purpose, string code, CancellationToken cancellationToken)
    {
        var reset = purpose == OtpPurpose.ResetPassword;
        var body = EmailBodyBuilder.GenerateEmailBody(reset ? "ForgetPassword" : "EmailConfirmation", user.FirstName, code, options.Value.ExpirationMinutes);
        try
        {
            await sender.SendEmailAsync(user.Email!, reset ? "Fitness App: Reset password" : "Fitness App: Confirm email", body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Authentication email delivery failed. UserId={UserId} ErrorType={ErrorType}", user.Id, ex.GetType().Name);
        }
    }
}
