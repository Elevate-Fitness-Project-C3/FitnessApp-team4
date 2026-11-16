using IdentityService.Email;
using IdentityService.Features.Users;
using IdentityService.Features.Users.AssignUserRole;
using IdentityService.Features.Users.CheckEmailExists;
using IdentityService.Features.Users.CreateUser;
namespace IdentityService.Features.Auth.Register;

internal sealed class RegisterUserCommandHandler(ISender sender, AuthDbContext dbContext, AuthEmailSender emailSender) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var emailExistsResult = await sender.Send(new CheckEmailExistsQuery(request.Email), cancellationToken);
        if (emailExistsResult.IsError)
            return Result<RegisterUserResponse>.Failure(emailExistsResult.Errors);
        if (emailExistsResult.Value)
            return UserErrors.EmailAlreadyExists;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var createUserResult = await sender.Send(new CreateUserCommand(request.FirstName, request.LastName, request.Email, request.Password), cancellationToken);
        if (createUserResult.IsError)
            return Result<RegisterUserResponse>.Failure(createUserResult.Errors);
        var user = createUserResult.Value;
        var assignRoleResult = await sender.Send(new AssignUserRoleCommand(user, RoleNames.User), cancellationToken);
        if (assignRoleResult.IsError)
            return Result<RegisterUserResponse>.Failure(assignRoleResult.Errors);
        var response = new RegisterUserResponse(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            EmailVerificationRequired: !user.EmailConfirmed);
        await transaction.CommitAsync(cancellationToken);
        var sent = await emailSender.SendAsync(user);
        return Result<RegisterUserResponse>.Success(response with { EmailSent = sent });
    }
}
