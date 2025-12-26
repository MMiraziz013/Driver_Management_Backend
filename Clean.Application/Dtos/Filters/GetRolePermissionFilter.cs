using System.ComponentModel.DataAnnotations;

namespace Clean.Application.Dtos.Filters;

public class GetRolePermissionFilter : PaginationFilter
{
    [Required]
    public string RoleId { get; set; }

    public GetRolePermissionFilter() : base()
    {

    }
    public string? Search { get; set; }
    public GetRolePermissionFilter(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {

    }
}