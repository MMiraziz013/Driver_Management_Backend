namespace Clean.Domain.Entities;

public class DriverVacation
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}