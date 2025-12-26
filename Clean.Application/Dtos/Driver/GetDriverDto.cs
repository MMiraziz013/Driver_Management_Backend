using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Driver;

public class GetDriverDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public int Age { get; set; }
    public string Address { get; set; } = null!;
    public EmploymentType EmploymentType { get; set; }
    public DriverCategory LicenseCategory { get; set; } = new ();
    public bool IsActive { get; set; }

}