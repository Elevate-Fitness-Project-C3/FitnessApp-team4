using Microsoft.AspNetCore.Identity.UI.Services;
using IdentityService.Security.Tokens;
namespace IdentityService.Email;
public sealed class AuthEmailSender(EmailVerificationCodeStore codes, IEmailSender sender, ILogger<AuthEmailSender> logger)
{
    public async Task<bool> SendAsync(ApplicationUser user, bool reset = false)
    {
        var code = await codes.IssueAsync(user, reset);
        if (code is null) return false;
        var body = EmailBodyBuilder.GenerateEmailBody(reset ? "ForgetPassword" : "EmailConfirmation", user.FirstName, code);
        try
        {
            await sender.SendEmailAsync(user.Email!, reset ? "Fitness App: Reset password" : "Fitness App: Confirm email", body);
            return true;
        }
        catch (System.Net.Mail.SmtpException ex)
        {
            logger.LogWarning(ex, "Authentication email delivery failed for user {UserId}. SMTP status: {StatusCode}", user.Id, ex.StatusCode);
            return false;
        }
    }
}
