namespace Clean.Domain.Entities;

public class DriverOffDay
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    public DateTime Date { get; set; }
    public string? Reason { get; set; }
}
