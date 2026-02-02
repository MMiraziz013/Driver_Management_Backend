namespace Clean.Domain.Entities;

public class CachedLocation
{
    public int Id { get; set; }
    public string AddressName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CachedAt { get; set; }
}