using IdentityService.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IdentityService.Persistence.Stores;

public sealed class ApplicationUserStore
    : UserStore<ApplicationUser, IdentityRole, AuthDbContext, string>
{
    public ApplicationUserStore(
        AuthDbContext dbContext,
        IdentityErrorDescriber errorDescriber)
        : base(dbContext, errorDescriber)
    {
        AutoSaveChanges = false;
    }
}