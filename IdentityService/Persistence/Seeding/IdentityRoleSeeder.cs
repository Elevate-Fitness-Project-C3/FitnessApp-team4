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
            if (result.Succeeded || result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateRoleName)))
                return;
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed role '{RoleNames.User}': {errors}");
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(DbIndexes.RoleName))
        {
            foreach (var entry in exception.Entries) entry.State = EntityState.Detached;
            if (!await roleManager.RoleExistsAsync(RoleNames.User)) throw;
        }
    }
}
