using System.Net;
using System.Reflection;
using System.Security.Claims;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Role;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Services.Permission;

public class PermissionService : IPermissionService
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IDataContext _context;

    public PermissionService(RoleManager<IdentityRole<int>> roleManager, IDataContext context)
    {
        _roleManager = roleManager;
        _context = context;
    } 
    
    //get all permissions by roleId
    public async Task<PaginatedResponse<RoleClaimDto>> GetPermissionsByRoleId(GetRolePermissionFilter filter)
    {
        var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
        var role = await _roleManager.FindByIdAsync(filter.RoleId);
        
        var allPermissions = GetPermissions(typeof(PermissionConstants));

        if (role != null)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var permission in allPermissions)
            {
                permission.RoleId = role.Id.ToString();
                permission.Selected = roleClaims.Any(c => c.Type == PermissionConstants.ClaimType && c.Value == permission.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string searchTerm = filter.Search.ToLower();
            allPermissions = allPermissions
                .Where(p => p.Value.ToLower().Contains(searchTerm))
                .ToList();
        }
        
        //pagination
        var totalRecords = allPermissions.Count;
        var pagedPermissions = allPermissions
            .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
            .Take(validFilter.PageSize)
            .ToList();
        
        return new PaginatedResponse<RoleClaimDto>(
            pagedPermissions,
            filter.PageNumber,
            filter.PageSize,
            totalRecords
        );


    }
    
    //update permissions by roleId
    public async Task<Response<RoleClaimDto>> UpdatePermission( RoleClaimDto permission)
    {
        if (string.IsNullOrWhiteSpace(permission.RoleId))
            return new Response<RoleClaimDto>(HttpStatusCode.BadRequest,"Invalid permission data.");

        var role = await _roleManager.FindByIdAsync(permission.RoleId);
        if (role == null)
            return new Response<RoleClaimDto>(HttpStatusCode.BadRequest,$"Role with ID '{permission.RoleId}' not found.");

        var claims = await _roleManager.GetClaimsAsync(role);
        var existing = claims.FirstOrDefault(c => c.Type == permission.Type && c.Value == permission.Value);
        

        if (permission.Selected)
        {
            if (existing == null)
            {
               var  result = await _roleManager.AddClaimAsync(role, new Claim(permission.Type, permission.Value));
                if (!result.Succeeded)
                    return new Response<RoleClaimDto>(HttpStatusCode.InternalServerError, result.Errors.Select(e => e.Description).ToList());
            }
        }
        else
        {
            if (existing != null)
            {
                var result = await _roleManager.RemoveClaimAsync(role, existing); // use existing claim!
                if (!result.Succeeded)
                    return new Response<RoleClaimDto>(HttpStatusCode.InternalServerError, result.Errors.Select(e => e.Description).ToList());
            }
        }

        return new Response<RoleClaimDto>(HttpStatusCode.OK, permission);  
    }

    public async Task<Response<List<RoleDto>>> GetRoles()
    {
        
        var roles = await _roleManager.Roles.Select(x=>new RoleDto(){Id = x.Id.ToString(), Name = x.Name}).ToListAsync();
        return new Response<List<RoleDto>>(HttpStatusCode.OK, roles);
    }

    private List<RoleClaimDto> GetPermissions(Type policy)
    {
        var nestedTypes = policy.GetNestedTypes(BindingFlags.Public);
        var allPermissions = new List<RoleClaimDto>();
        if (nestedTypes.Length > 0)
        {
            foreach (var nested in nestedTypes)
            {
                FieldInfo[] fields = nested.GetFields(BindingFlags.Static | BindingFlags.Public);

                foreach (FieldInfo fi in fields)
                {
                    allPermissions.Add(new RoleClaimDto(PermissionConstants.ClaimType, fi.GetValue(null).ToString()));
                }
            }
        }
        else
        {
            FieldInfo[] fields = policy.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo fi in fields)
            {
                allPermissions.Add(new RoleClaimDto(PermissionConstants.ClaimType, fi.GetValue(null)?.ToString()!));
            }
        }


        return allPermissions;

    }
}