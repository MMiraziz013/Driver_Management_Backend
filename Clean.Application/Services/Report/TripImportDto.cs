using MiniExcelLibs.Attributes;

public class TripImportDto
{
    [ExcelColumnName("Conf#")]
    public string ConfNumber { get; set; }

    [ExcelColumnName("PU Date")]
    public string PickUpDate { get; set; }

    [ExcelColumnName("Garage Out")]
    public string GarageOutTime { get; set; }

    [ExcelColumnName("Garage In")]
    public string GarageInTime { get; set; }

    [ExcelColumnName("Routing Details")]
    public string RoutingDetails { get; set; }

    [ExcelColumnName("Vehicle Type")]
    public string VehicleType { get; set; }

    [ExcelColumnName("Service Type")]
    public string ServiceType { get; set; }
    
    [ExcelColumnName("Company")]
    public string CompanyName { get; set; }
    
    [ExcelColumnName("Driver")]
    public string Driver { get; set; }

    [ExcelColumnName("Car")]
    public string Car { get; set; }
    
    [ExcelColumnName("Pmt Method")]
    public string PmtMethod { get; set; }
}