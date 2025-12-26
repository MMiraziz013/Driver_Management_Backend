using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ClassLibrary1.Data.Seeds;

public class SeedAdminUser : IDataSeeder
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public SeedAdminUser(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        const string adminRole = "Admin";
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "myPassword1$";

        if (!await _roleManager.RoleExistsAsync(adminRole))
            await _roleManager.CreateAsync(new IdentityRole<int>(adminRole));

        var admin = await _userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new User
            {
                UserName = "admin",
                FirstName = "Admin",
                LastName = "User",
                Email = adminEmail,
                EmailConfirmed = true,
                Role = UserRole.Admin,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(admin, adminRole);
        }
    }
}
