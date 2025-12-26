using Microsoft.AspNetCore.Authorization;

namespace Clean.Application.Security.Permission;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string requiredPermission)
    {
        AuthenticationSchemes = "Bearer";
        Policy = requiredPermission;
    }
}