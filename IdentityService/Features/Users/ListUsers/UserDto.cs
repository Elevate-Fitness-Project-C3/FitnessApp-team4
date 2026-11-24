namespace IdentityService.Features.Users.ListUsers
{
    public sealed record UserDto(
        string Id,
        string FirstName,
        string LastName,
        string? Email,
        bool EmailConfirmed,
        string? PhoneNumber,
        bool IsActive,
        DateTimeOffset CreatedAt);
}