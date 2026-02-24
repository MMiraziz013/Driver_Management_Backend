namespace Clean.Application.Dtos.DriverVacation;

public class GetDriverVacationDto
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public string DriverName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }  // Computed: is vacation currently active?
    public bool IsPast { get; set; }     // Computed: has vacation ended?
    public bool IsFuture { get; set; }   // Computed: vacation hasn't started yet?
}

public class AddDriverVacationDto
{
    public int DriverId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDriverVacationDto
{
    public int Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}
