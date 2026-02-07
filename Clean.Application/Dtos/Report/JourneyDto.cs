namespace Clean.Application.Dtos.Report;

/// <summary>
/// Represents a grouped journey (one or more trips with same driver/vehicle)
/// </summary>
public class JourneyDto
{
    public int JourneyNumber { get; set; }
    
    /// <summary>
    /// Date of the journey
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Driver assigned to this journey
    /// </summary>
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    
    /// <summary>
    /// Vehicle used for this journey
    /// </summary>
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    
    /// <summary>
    /// Time of first trip's garage out
    /// </summary>
    public TimeSpan DepartureTime { get; set; }
    
    /// <summary>
    /// Time of last trip's garage in
    /// </summary>
    public TimeSpan ReturnTime { get; set; }
    
    /// <summary>
    /// Combined company names from all trips
    /// </summary>
    public string Companies { get; set; } = string.Empty;
    
    /// <summary>
    /// List of confirmation numbers included in this journey
    /// </summary>
    public List<string> ConfNumbers { get; set; } = new();
    
    /// <summary>
    /// Starting odometer reading (km)
    /// </summary>
    public double StartingMileage { get; set; }
    
    /// <summary>
    /// Ending odometer reading (km)
    /// </summary>
    public double EndingMileage { get; set; }
    
    /// <summary>
    /// Total distance traveled in this journey (km)
    /// </summary>
    public double TotalDistanceKm { get; set; }
    
    /// <summary>
    /// Total fuel consumed in this journey (liters)
    /// </summary>
    public double TotalFuelConsumed { get; set; }
    
    /// <summary>
    /// Number of trips combined into this journey
    /// </summary>
    public int TripCount { get; set; }
    
    /// <summary>
    /// Individual trips in this journey (for reference)
    /// </summary>
    public List<JourneyTripDto> Trips { get; set; } = new();
}

/// <summary>
/// Individual trip within a journey
/// </summary>
public class JourneyTripDto
{
    public int TripId { get; set; }
    public string ConfNumber { get; set; } = string.Empty;
    public TimeSpan GarageOutTime { get; set; }
    public TimeSpan GarageInTime { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RoutingDetails { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
    public string ServiceType { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating vehicle mileage
/// </summary>
public class UpdateVehicleMileageDto
{
    public int VehicleId { get; set; }
    public double NewMileage { get; set; }
}

/// <summary>
/// DTO for bulk mileage update
/// </summary>
public class BulkMileageUpdateDto
{
    public List<UpdateVehicleMileageDto> Updates { get; set; } = new();
}