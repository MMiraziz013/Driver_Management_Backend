namespace Clean.Application.Abstractions;

public interface IDistanceService
{
    double CalculateDistance(string routeString, double durationHours);
    string CleanAddress(string rawAddress);
}