using System.Text.Encodings.Web;
namespace IdentityService.Email;
public static class EmailBodyBuilder
{
    public static string GenerateEmailBody(string template, string name, string code)
    {
        using var stream = typeof(EmailBodyBuilder).Assembly.GetManifestResourceStream($"IdentityService.Email.Templates.{template}.html")
            ?? throw new InvalidOperationException("Email template not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("{{code}}", HtmlEncoder.Default.Encode(code)).Replace("{{name}}", HtmlEncoder.Default.Encode(name));
    }
}
