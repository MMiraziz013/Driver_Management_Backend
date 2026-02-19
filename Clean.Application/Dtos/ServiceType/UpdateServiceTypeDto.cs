namespace Clean.Application.Dtos.ServiceType;

public class UpdateServiceTypeDto
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}