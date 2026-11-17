using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuditService.Security;

public sealed record AuditReadScope(string ApplicationId, string? TenantId, string SourceService);
public sealed record AuditAccess(string UserId, bool SensitiveMetadata, AuditReadScope[] Scopes);
public sealed record AuditAccessResult(int StatusCode, AuditAccess? Access);

public sealed class AuditReportAuthorization(HttpClient client, IConfiguration configuration, IHostEnvironment environment)
{
    public async Task<AuditAccessResult> ResolveAsync(HttpContext context)
    {
        var token = context.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(token, out var bearer) || !string.Equals(bearer.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(bearer.Parameter)) return new(401, null);
        var key = configuration["Reporting:ServiceKey"];
        if (!Uri.TryCreate(configuration["Reporting:IdentityBaseUrl"], UriKind.Absolute, out var baseUri)
            || !environment.IsDevelopment() && baseUri.Scheme != "https" || string.IsNullOrWhiteSpace(key) || key.Length < 32)
            return new(503, null);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/internal/audit/authorization"));
            request.Headers.Authorization = bearer;
            request.Headers.Add("X-Audit-Service-Key", key);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            if ((int)response.StatusCode is 401 or 403) return new((int)response.StatusCode, null);
            if (!response.IsSuccessStatusCode) return new(503, null);
            var access = await response.Content.ReadFromJsonAsync<AuditAccess>(context.RequestAborted);
            if (access is null || string.IsNullOrWhiteSpace(access.UserId) || access.Scopes is null || access.Scopes.Length > 100
                || access.Scopes.Any(x => string.IsNullOrWhiteSpace(x.ApplicationId) || string.IsNullOrWhiteSpace(x.SourceService))) return new(503, null);
            return access.Scopes.Length == 0 ? new(403, null) : new(200, access);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return new(503, null);
        }
    }
}
