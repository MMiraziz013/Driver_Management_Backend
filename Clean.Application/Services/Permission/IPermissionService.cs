using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Role;

namespace Clean.Application.Services.Permission;

public interface IPermissionService
{
    Task<PaginatedResponse<RoleClaimDto>> GetPermissionsByRoleId(GetRolePermissionFilter filter);
    Task<Response<RoleClaimDto>> UpdatePermission(RoleClaimDto permission);
    Task<Response<List<RoleDto>>> GetRoles();
}