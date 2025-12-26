using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class Driver
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    public DateOnly BirthDay { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped] // Tell EF to ignore this
    public int Age
    {
        get
        {
            if (BirthDay == default) return 0; // Guard against default dates
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            int age = today.Year - BirthDay.Year;
            if (today < BirthDay.AddYears(age)) age--;
            return age;
        }
    }
    public string Address { get; set; } = null!;

    public DriverCategory Category { get; set; }
    public EmploymentType EmploymentType { get; set; }

    public int WeeklyWorkLimit { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    public List<DriverVacation> Vacations { get; set; } = [];
    public List<DriverOffDay> OffDays { get; set; } = [];
    public List<DriverAssignment> Assignments { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}