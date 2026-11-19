namespace IdentityService.Features.AuditGrants.SetAuditGrant;

public sealed record AuditGrantRequest(string UserId, string ApplicationId, string? TenantId, string SourceService);
