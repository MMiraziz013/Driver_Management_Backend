using System.ComponentModel.DataAnnotations;

namespace Clean.Domain.Enums;

public enum UserRole
{
    [Display(Name = "Employee")]
    Employee = 1,
    [Display(Name = "Admin")]
    Admin = 2
}