using System.Net.Http.Headers;
using AuditService.Messaging;

namespace AuditService.Features.AuditEvents.Reports;

public sealed record AuditReadScope(string ApplicationId, string? TenantId, string SourceService);
public sealed record AuditAccess(string UserId, bool SensitiveMetadata, AuditReadScope[] Scopes);
public sealed record AuditAccessResult(int StatusCode, AuditAccess? Access);

public sealed class AuditReportAuthorization(HttpClient client, IConfiguration configuration, IHostEnvironment environment,
    ILogger<AuditReportAuthorization> logger)
{
    public async Task<AuditAccessResult> ResolveAsync(HttpContext context)
    {
        var token = context.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(token, out var bearer) || !string.Equals(bearer.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(bearer.Parameter)) return new(401, null);
        var key = configuration["Reporting:ServiceKey"];
        if (!Uri.TryCreate(configuration["Reporting:IdentityBaseUrl"], UriKind.Absolute, out var baseUri)
            || baseUri.Host.Length == 0 || baseUri.UserInfo.Length != 0
            || (baseUri.Scheme != "https" && (!environment.IsDevelopment() || baseUri.Scheme != "http"))
            || string.IsNullOrWhiteSpace(key) || key.Length is < 32 or > 256 || key.Any(c => c is < '!' or > '~'))
            return new(503, null);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            deadline.CancelAfter(client.Timeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/internal/audit/authorization"));
            request.Headers.Authorization = bearer;
            request.Headers.Add("X-Audit-Service-Key", key);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if ((int)response.StatusCode is 401 or 403) return new((int)response.StatusCode, null);
            if (!response.IsSuccessStatusCode) return new(503, null);
            var access = await response.Content.ReadFromJsonAsync<AuditAccess>(deadline.Token);
            if (access is null || string.IsNullOrWhiteSpace(access.UserId) || access.Scopes is null || access.Scopes.Length > 100
                || access.Scopes.Any(x => x is null || !AuditEventValidation.ValidIdentifier(x.ApplicationId, 100)
                    || !AuditMessagingConfiguration.ValidSource(x.SourceService) || !AuditEventValidation.ValidText(x.TenantId, 128, false)))
                return new(503, null);
            return access.Scopes.Length == 0 ? new(403, null) : new(200, access);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException
            || exception is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Audit authorization request failed. TraceId={TraceId} ExceptionType={ExceptionType}",
                System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier, exception.GetType().Name);
            return new(503, null);
        }
    }
}
