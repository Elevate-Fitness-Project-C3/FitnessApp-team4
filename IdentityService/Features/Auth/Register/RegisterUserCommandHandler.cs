using IdentityService.Security.Tokens;
using IdentityService.Email;
using IdentityService.Features.Users;
namespace IdentityService.Features.Auth.Register;

internal sealed class RegisterUserCommandHandler(UserManager<ApplicationUser> userManager, AuthDbContext dbContext, AuthEmailQueue emailSender)
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
                var creationResult = await userManager.CreateAsync(user, request.Password);
                if (!creationResult.Succeeded)
                    return MapCreationErrors(creationResult);
                var assignmentResult = await userManager.AddToRoleAsync(user, RoleNames.User);
                if (!assignmentResult.Succeeded)
                    return MapAssignmentErrors(assignmentResult);
                await emailSender.EnqueueAsync(user.Id, OtpPurpose.ConfirmEmail, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation("EmailIndex", "UserNameIndex"))
            {
                foreach (var entry in exception.Entries)
                {
                    if (ReferenceEquals(entry.Entity, user))
                        entry.State = EntityState.Detached;
                }
                return UserErrors.EmailAlreadyExists;
            }
        }
        var response = new RegisterUserResponse(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            EmailVerificationRequired: !user.EmailConfirmed);
        return Result<RegisterUserResponse>.Success(response with { EmailQueued = true });
    }
    private static Result<RegisterUserResponse> MapCreationErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors
            .Select(error => error.Code switch
            {
                nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)
                    => UserErrors.EmailAlreadyExists,
                _ => Error.Validation($"Identity.{error.Code}", error.Description)
            })
            .Distinct()
            .ToList();
        return Result<RegisterUserResponse>.Failure(errors);
    }
    private static Result<RegisterUserResponse> MapAssignmentErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors
            .Select(error => error.Code switch
            {
                nameof(IdentityErrorDescriber.UserAlreadyInRole) or nameof(IdentityErrorDescriber.ConcurrencyFailure)
                    => Error.Conflict($"Identity.{error.Code}", error.Description),
                nameof(IdentityErrorDescriber.DefaultError)
                    => Error.Failure($"Identity.{error.Code}", "Failed to assign the role."),
                _ => Error.Validation($"Identity.{error.Code}", error.Description)
            })
            .Distinct()
            .ToList();
        return Result<RegisterUserResponse>.Failure(errors);
    }
}
