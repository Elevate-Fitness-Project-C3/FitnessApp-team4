using System.Collections.Concurrent;
using System.Text.Encodings.Web;
namespace IdentityService.Email;
public static class EmailBodyBuilder
{
    private static readonly ConcurrentDictionary<string, string> Templates = new();
    public static string GenerateEmailBody(string template, string name, string code, int expirationMinutes, EmailBrandingOptions branding)
    {
        var html = Templates.GetOrAdd(template, static key =>
        {
            using var stream = typeof(EmailBodyBuilder).Assembly.GetManifestResourceStream($"IdentityService.Email.Templates.{key}.html")
                ?? throw new InvalidOperationException("Email template not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
        return System.Text.RegularExpressions.Regex.Replace(html, @"\{\{(code|name|minutes|applicationName|teamName)\}\}", match =>
            HtmlEncoder.Default.Encode(match.Groups[1].Value switch
            {
                "code" => code,
                "name" => name,
                "applicationName" => branding.ApplicationName,
                "teamName" => branding.TeamName,
                _ => expirationMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));
    }
}
