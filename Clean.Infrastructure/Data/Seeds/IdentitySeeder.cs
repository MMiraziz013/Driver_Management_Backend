using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Security.Permission;
using Clean.Application.Services.Permission;
using Microsoft.AspNetCore.Identity;

namespace ClassLibrary1.Data.Seeds;

public class IdentitySeeder : IDataSeeder
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public IdentitySeeder(RoleManager<IdentityRole<int>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedPermissionsAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new List<string>
        {
            RoleConstants.Admin,
            RoleConstants.Employee,
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    private async Task SeedPermissionsAsync()
    {
        foreach (var roleName in RolePermissionService.GetAllRoles())
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) continue;

            var permissions = RolePermissionService.GetPermissionsByRoles(new[] { roleName });
            var existingClaims = await _roleManager.GetClaimsAsync(role);
 
            foreach (var permission in permissions)
            {
                if (!existingClaims.Any(c => c.Type == PermissionConstants.ClaimType && c.Value == permission))
                {
                    await _roleManager.AddClaimAsync(role, new Claim(PermissionConstants.ClaimType, permission));
                }
            }
        }
    }
}