using System.Net;
using System.Net.Mail;
namespace IdentityService.Email;
public sealed class EmailSender(IOptions<EmailOptions> options)
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.UserName))
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        using var message = new MailMessage(settings.From, email, subject, htmlMessage) { IsBodyHtml = true };
        await client.SendMailAsync(message, cancellationToken);
    }
}
