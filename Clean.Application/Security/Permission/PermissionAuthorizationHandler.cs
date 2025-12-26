using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Clean.Application.Security.Permission;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;
    private readonly IDataContext _context;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<User> _userManager;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    // Check whether a given PermissionRequirement is satisfied or not for a particular context
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        _logger.LogWarning("Evaluating authorization requirement for permission {permission}", requirement.Permission);
        // Check if user has the required permission claim
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == "Permission" && c.Value == requirement.Permission);

        if (hasPermission)
        {
            _logger.LogInformation("User has permission {Permission}", requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User does not have permission {Permission}", requirement.Permission);
        }

        return Task.CompletedTask;
    }
}