using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class Filter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public FilterEntity Entity { get; set; }
    public string Field { get; set; } = string.Empty;
    
    public FilterOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;

    public FilterAction Action { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
