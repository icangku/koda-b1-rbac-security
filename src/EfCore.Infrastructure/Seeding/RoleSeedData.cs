using EfCore.Application.Security;
using EfCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Infrastructure.Seeding;

public static class RoleSeedData
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<AppUser> userManager,
        string? bootstrapAdminEmail)
    {
        string[] requiredRoles =
        [
            AppRoles.Customer,
            AppRoles.Admin
        ];

        foreach (var roleName in requiredRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Name = roleName
                });

            EnsureSucceeded(
                result,
                $"Membuat role {roleName}");
        }

        var existingUsers = await userManager.Users.ToListAsync();

        foreach (var user in existingUsers)
        {
            var roles = await userManager.GetRolesAsync(user);

            if (roles.Count > 0)
            {
                continue;
            }

            var result = await userManager.AddToRoleAsync(
                user,
                AppRoles.Customer);

            EnsureSucceeded(
                result,
                $"Memberikan role CUSTOMER ke user {user.Id}");
        }

        if (string.IsNullOrWhiteSpace(bootstrapAdminEmail))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(
            bootstrapAdminEmail.Trim());

        if (admin is null)
        {
            return;
        }

        if (!await userManager.IsInRoleAsync(
            admin,
            AppRoles.Admin))
        {
            var result = await userManager.AddToRoleAsync(
                admin,
                AppRoles.Admin);

            EnsureSucceeded(
                result,
                "Memberikan role ADMIN");
        }

        if (await userManager.IsInRoleAsync(
            admin,
            AppRoles.Customer))
        {
            var result = await userManager.RemoveFromRoleAsync(
                admin,
                AppRoles.Customer);

            EnsureSucceeded(
                result,
                "Menghapus role CUSTOMER dari administrator");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var description = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"{operation} gagal: {description}");
    }
}
