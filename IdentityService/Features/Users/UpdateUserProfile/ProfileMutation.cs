using IdentityService.Features.Users.ListUsers;
using IdentityService.Security.Authorization;

namespace IdentityService.Features.Users.UpdateUserProfile;

internal static class ProfileMutation
{
    internal static UserDto ToDto(ApplicationUser user) => new(user.Id, user.FirstName, user.LastName,
        user.Email, user.EmailConfirmed, user.PhoneNumber, user.IsActive, user.CreatedAt, user.ConcurrencyStamp!);

    internal static async Task<Result<UserDto>> ApplyAsync(UserManager<ApplicationUser> users, ApplicationUser user,
        string firstName, string lastName, string? phoneNumber, string concurrencyToken)
    {
        if (user.ConcurrencyStamp != concurrencyToken) return SecurityMutationChecks.Conflict;
        firstName = firstName.Trim();
        lastName = lastName.Trim();
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        if (user.FirstName == firstName && user.LastName == lastName && user.PhoneNumber == phoneNumber)
            return ToDto(user);
        user.FirstName = firstName;
        user.LastName = lastName;
        if (user.PhoneNumber != phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = false;
        }

        var result = await users.UpdateAsync(user);
        return result.Succeeded ? Result<UserDto>.Success(ToDto(user)) : IdentityErrorMapper.ToResult<UserDto>(result);
    }
}
