using System.Collections.Concurrent;
using System.Text.Encodings.Web;
namespace IdentityService.Email;
public static class EmailBodyBuilder
{
    private static readonly ConcurrentDictionary<string, string> Templates = new();
    public static string GenerateEmailBody(string template, string name, string code, int expirationMinutes)
    {
        var html = Templates.GetOrAdd(template, static key =>
        {
            using var stream = typeof(EmailBodyBuilder).Assembly.GetManifestResourceStream($"IdentityService.Email.Templates.{key}.html")
                ?? throw new InvalidOperationException("Email template not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
        return html.Replace("{{code}}", HtmlEncoder.Default.Encode(code)).Replace("{{name}}", HtmlEncoder.Default.Encode(name))
            .Replace("{{minutes}}", expirationMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
