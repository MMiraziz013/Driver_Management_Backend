namespace Clean.Application.Dtos.Company;

public class CompanyCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? Color { get; set; }
    public int CompanyCount { get; set; }
}

public class CreateCompanyCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? Color { get; set; }
}

public class UpdateCompanyCategoryDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Color { get; set; }
}

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CompanyCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
    public string? Aliases { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime FirstSeenAt { get; set; }
}

public class UpdateCompanyDto
{
    public int? CompanyCategoryId { get; set; }
    public string? Aliases { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class BulkAssignCategoryDto
{
    public List<int> CompanyIds { get; set; } = new();
    public int CategoryId { get; set; }
}