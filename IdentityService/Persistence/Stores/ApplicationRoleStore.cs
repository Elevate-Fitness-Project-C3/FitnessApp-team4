using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IdentityService.Persistence.Stores;

public sealed class ApplicationRoleStore
    : RoleStore<IdentityRole, AuthDbContext, string>
{
    public ApplicationRoleStore(
        AuthDbContext dbContext,
        IdentityErrorDescriber errorDescriber)
        : base(dbContext, errorDescriber)
    {
        AutoSaveChanges = false;
    }
}