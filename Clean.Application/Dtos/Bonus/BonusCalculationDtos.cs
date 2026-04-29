namespace Clean.Application.Dtos.Bonus;

public class BonusCalculationRequestDto
{
    public List<int> PeriodIds { get; set; } = new();
}

public class BonusCalculationResultDto
{
    public List<int> PeriodIds { get; set; } = new();
    public string PeriodNames { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public decimal GrandTotal { get; set; }
    public int TotalDrivers { get; set; }
    public int TotalTrips { get; set; }
    public double TotalHoursWorked { get; set; }
    
    public List<DriverBonusResultDto> DriverResults { get; set; } = new();
}

public class DriverBonusResultDto
{
    public string DriverName { get; set; } = string.Empty;
    public decimal TotalBonus { get; set; }
    public double TotalHoursWorked { get; set; }
    public int TotalTrips { get; set; }
    public int TotalDaysWorked { get; set; }
    public double AverageHoursPerDay { get; set; }
    
    // Per service type breakdown
    public List<ServiceTypeBonusBreakdownDto> ServiceTypeBreakdowns { get; set; } = new();
    
    // Stats
    public TripStatDto? LongestTrip { get; set; }
    public TripStatDto? FurthestTrip { get; set; }
}

public class ServiceTypeBonusBreakdownDto
{
    public string ServiceTypeName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public double TotalHours { get; set; }
    public int TotalDays { get; set; }
    public decimal BonusAmount { get; set; }
    
    // Vehicle type breakdown
    public int PremiumVehicleTrips { get; set; }
    public int StandardVehicleTrips { get; set; }
    
    // Duration bracket counts
    public int TripsUnder2Hours { get; set; }
    public int TripsUnder4Hours { get; set; }
    public int Trips4To6Hours { get; set; }
    public int Trips6To8Hours { get; set; }
    public int Trips8To10Hours { get; set; }
    public int Trips10To12Hours { get; set; }
    public int Trips12To14Hours { get; set; }
    public int TripsOver14Hours { get; set; }
    
}

public class TripStatDto
{
    public string ConfNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public double DurationHours { get; set; }
    public double DistanceKm { get; set; }
}