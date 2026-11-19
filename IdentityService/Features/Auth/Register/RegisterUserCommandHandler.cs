using IdentityService.Security.Tokens;
using IdentityService.Email;
namespace IdentityService.Features.Auth.Register;

internal sealed class RegisterUserCommandHandler(UserManager<ApplicationUser> userManager, AuthDbContext dbContext,
    AuthEmailQueue emailQueue, AuthEmailSignal emailSignal)
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            UserName = email,
            EmailConfirmed = false
        };

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var creation = await userManager.CreateAsync(user, request.Password);
                if (!creation.Succeeded)
                    return IdentityErrorMapper.ToResult<RegisterUserResponse>(creation);

                var assignment = await userManager.AddToRoleAsync(user, RoleNames.User);
                if (!assignment.Succeeded)
                    return IdentityErrorMapper.ToResult<RegisterUserResponse>(assignment);

                await emailQueue.EnqueueAsync(user.Id, OtpPurpose.ConfirmEmail, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation(DbIndexes.Email, DbIndexes.UserName))
            {
                foreach (var entry in exception.Entries.Where(e => ReferenceEquals(e.Entity, user)))
                    entry.State = EntityState.Detached;
                return UserErrors.EmailAlreadyExists;
            }
        }

        emailSignal.Notify();
        return Result<RegisterUserResponse>.Success(new RegisterUserResponse(
            UserId: user.Id, Email: user.Email!, FirstName: user.FirstName, LastName: user.LastName,
            EmailVerificationRequired: true, EmailQueued: true));
    }
}
