
using Microsoft.Data.SqlClient;
namespace IdentityService.Persistence.Seeding;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        if (await roleManager.RoleExistsAsync(RoleNames.User))
            return;
        try
        {
            var result = await roleManager.CreateAsync(new IdentityRole(RoleNames.User));
            if (result.Succeeded)
                return;
            if (IsDuplicateRoleError(result) && await roleManager.RoleExistsAsync(RoleNames.User))
                return;
            throw CreateSeedingException(RoleNames.User, result);
        }
        catch (DbUpdateException exception) when (IsDuplicateRoleViolation(exception))
        {
            foreach (var entry in exception.Entries)
            {
                entry.State = EntityState.Detached;
            }
            if (!await roleManager.RoleExistsAsync(RoleNames.User))
                throw;
        }
    }
    private static bool IsDuplicateRoleError(IdentityResult result)
    {
        return result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateRoleName));
    }
    private static bool IsDuplicateRoleViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException && sqlException.Number is 2601 or 2627 && sqlException.Message.Contains("RoleNameIndex", StringComparison.OrdinalIgnoreCase);
    }
    private static InvalidOperationException CreateSeedingException(string roleName, IdentityResult result)
    {
        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        return new InvalidOperationException($"Failed to seed role '{roleName}': {errors}");
    }
}
