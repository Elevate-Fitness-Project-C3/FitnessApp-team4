using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ConfirmEmail;
internal sealed class ConfirmEmailCommandHandler(UserManager<ApplicationUser> users, AuthDbContext db, EmailVerificationCodeStore codes) : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null) return AuthErrors.InvalidCode;
        if (user.EmailConfirmed) return Error.Conflict("Auth.AlreadyConfirmed", "Email already confirmed.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var token = await codes.VerifyAsync(user, request.Code, false, cancellationToken);
        if (token is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthErrors.InvalidCode;
        }
        var result = await users.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return AuthErrors.InvalidCode;
        await transaction.CommitAsync(cancellationToken);
        return Result.Success;
    }
}
