namespace Clean.Domain.Entities;

public class ServiceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<Trip> Trips { get; set; } = [];
}
