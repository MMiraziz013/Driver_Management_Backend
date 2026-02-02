using Clean.Application.Abstractions;
using Clean.Domain.Enums;
using ClosedXML.Excel;

namespace Clean.Application.Services.Report;

/// <summary>
/// Handles exporting assignment reports to Excel
/// </summary>
public class ReportExportService
{
    private readonly IUnitOfWork _uow;

    public ReportExportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<byte[]> ExportReportAsync(int periodId)
    {
        var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
        if (period == null) return [];

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Assignment Report");

        // Create headers
        var headers = new[]
        {
            "ConfNumber", "Date", "Garage Out", "Garage In", "Service Type", "Company",
            "Route", "Distance (km)", "Vehicle Type", "Driver", "Plate #", "Status", 
            "Payment Method", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Fill data
        int rowNum = 2;
        foreach (var trip in period.Trips.OrderBy(t => t.PickUpDate).ThenBy(t => t.GarageOutTime))
        {
            var assignment = trip.Assignments.FirstOrDefault();
            bool isConflict = assignment?.HasConflict ?? true;
            bool isFieldTrip = assignment?.AssignmentType == AssignmentType.Manual;
            bool noCoordinates = !trip.CoordinatesResolved;

            ws.Cell(rowNum, 1).Value = trip.ConfNumber;
            ws.Cell(rowNum, 2).Value = trip.PickUpDate.ToShortDateString();
            ws.Cell(rowNum, 3).Value = trip.GarageOutTime.ToString(@"hh\:mm");
            ws.Cell(rowNum, 4).Value = trip.GarageInTime.ToString(@"hh\:mm");
            ws.Cell(rowNum, 5).Value = trip.ServiceType?.Name ?? "N/A";
            ws.Cell(rowNum, 6).Value = trip.CompanyName;
            ws.Cell(rowNum, 7).Value = trip.RoutingDetails;
            ws.Cell(rowNum, 8).Value = trip.DistanceKm?.ToString() ?? "N/A";
            ws.Cell(rowNum, 9).Value = trip.VehicleType.Name;
            ws.Cell(rowNum, 10).Value = assignment?.Driver?.FullName ?? "UNASSIGNED";
            ws.Cell(rowNum, 11).Value = assignment?.Vehicle?.PlateNumber ?? "N/A";

            string status = isConflict ? "CONFLICT" : (isFieldTrip ? "FIELD TRIP" : "ASSIGNED");
            ws.Cell(rowNum, 12).Value = status;

            ws.Cell(rowNum, 13).Value = trip.PmtMethod ?? "Unknown";

            string notes = assignment?.Notes ?? "";
            if (noCoordinates)
            {
                notes = (string.IsNullOrEmpty(notes) ? "" : notes + "; ") + "⚠️ Coordinates not resolved";
            }
            ws.Cell(rowNum, 14).Value = notes;

            // Color coding
            if (isConflict)
            {
                ws.Range(rowNum, 1, rowNum, 14).Style.Fill.BackgroundColor = XLColor.IndianRed;
                ws.Range(rowNum, 1, rowNum, 14).Style.Font.FontColor = XLColor.White;
            }
            else if (noCoordinates)
            {
                ws.Range(rowNum, 1, rowNum, 14).Style.Fill.BackgroundColor = XLColor.Yellow;
            }
            else if (isFieldTrip)
            {
                ws.Range(rowNum, 1, rowNum, 14).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            rowNum++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}