using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Clean.Domain.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; }  
    public UserRole Role { get; set; }
}