namespace IdentityService.Features.AuditGrants.SetAuditGrant;

public sealed record SetAuditGrantCommand(string UserId, string ApplicationId, string? TenantId, string SourceService, bool Remove) : IRequest<Result<Success>>;
