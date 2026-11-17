using System.Net.Mail;
using IdentityService.Security.Options;
using IdentityService.Security.Tokens;
namespace IdentityService.Email;

public sealed class AuthEmailSender(IEmailSender sender, IOptions<OtpOptions> options,
    IOptions<EmailBrandingOptions> branding, ILogger<AuthEmailSender> logger)
{
    public async Task<bool> SendAsync(ApplicationUser user, OtpPurpose purpose, string code, CancellationToken cancellationToken)
    {
        var (template, subject) = purpose switch
        {
            OtpPurpose.ConfirmEmail => ("EmailConfirmation", branding.Value.ConfirmEmailSubject),
            OtpPurpose.ResetPassword => ("ForgotPassword", branding.Value.ResetPasswordSubject),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };

        try
        {
            var body = EmailBodyBuilder.GenerateEmailBody(template, user.FirstName, code, options.Value.ExpirationMinutes, branding.Value);
            await sender.SendEmailAsync(user.Email!, $"{branding.Value.ApplicationName}: {subject}", body, cancellationToken);
            return true;
        }
        catch (SmtpException ex)
        {
            // SMTP exception messages can contain the recipient address, so only the status code is logged.
            logger.LogWarning("Authentication e-mail delivery failed. UserId={UserId} Purpose={Purpose} SmtpStatus={Status}",
                user.Id, purpose, ex.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Authentication e-mail delivery timed out. UserId={UserId} Purpose={Purpose}", user.Id, purpose);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Authentication e-mail could not be built or sent. UserId={UserId} Purpose={Purpose}", user.Id, purpose);
            return false;
        }
    }
}
