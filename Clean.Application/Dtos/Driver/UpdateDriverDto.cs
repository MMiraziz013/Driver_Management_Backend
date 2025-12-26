using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Driver;

public class UpdateDriverDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public DateOnly BirthYear { get; set; }
    public string Address { get; set; } = null!;
    public List<DriverCategory> DriverCategories { get; set; } = new();
    public EmploymentType EmploymentType { get; set; }
}