using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Driver;

public class AddDriverDto
{
    public string FullName { get; set; } = null!;
    public DateOnly BirthYear { get; set; }
    public string Address { get; set; } = null!;
    public DriverCategory DriverCategories { get; set; }
    
    public EmploymentType EmploymentType { get; set; }
}