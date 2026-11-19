using IdentityService.Security.Auditing;

namespace IdentityService.Security.Authorization;

public sealed class SecurityMutationOrchestrator(AuthDbContext db, SecurityMutationChecks checks, AuditActorContext actor)
{
    public async Task<Result<T>> ExecuteAsync<T>(string permission,
        Func<Task<Result<T>>> mutateAsync, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync(permission, cancellationToken);
        if (error is not null)
            return error.Value;

        var result = await mutateAsync();
        if (result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
            actor.MarkMutationCommitted();
        }

        return result;
    }
}
