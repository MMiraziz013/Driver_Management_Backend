namespace Clean.Application.Dtos.Driver;

public class AssignmentFinalizationResultDto
{
    public int PeriodId { get; set; }
    public DateTime FinalizedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsPreview { get; set; } = false;
    public int DriversUpdated { get; set; }
    public int DriversWithWarnings { get; set; }
    public List<DriverStateDto> DriverStates { get; set; } = [];
}