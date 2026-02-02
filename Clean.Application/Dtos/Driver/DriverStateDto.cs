namespace Clean.Application.Dtos.Driver;

public class DriverStateDto
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public DateTime? LastTripEndTime { get; set; }
    public double HoursWorkedThisPeriod { get; set; }
    public int TripsThisPeriod { get; set; }
    public double IncompleteWeekHours { get; set; }
    public int ConsecutiveDaysWorked { get; set; }
    public DateTime? LastRestDay { get; set; }
    public double HoursSinceLastTrip { get; set; }
    public bool HasSufficientRest { get; set; }
    public List<string> Warnings { get; set; } = [];
}