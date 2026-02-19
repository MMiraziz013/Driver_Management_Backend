using Clean.Application.Security.Permission;

namespace Clean.Application.Services.Permission;

public static class RolePermissionService
{
    private static readonly Dictionary<string, List<string>> RolePermissions = new()
    {
        {
            RoleConstants.Admin, new List<string>
            {
                PermissionConstants.Users.ManageAll,
                PermissionConstants.Users.ManageSelf,
                PermissionConstants.Users.View,
                
                PermissionConstants.Drivers.View,
                PermissionConstants.Drivers.Manage,
                PermissionConstants.Drivers.ManageAll,
                
                PermissionConstants.DriverAssignments.View,
                PermissionConstants.DriverAssignments.Manage,
                PermissionConstants.DriverAssignments.ManageAll,
                
                PermissionConstants.DriverOffDays.View,
                PermissionConstants.DriverOffDays.Manage,
                PermissionConstants.DriverOffDays.ManageSelf,
                PermissionConstants.DriverOffDays.ManageAll,
                
                PermissionConstants.DriverVacations.View,
                PermissionConstants.DriverVacations.Manage,
                PermissionConstants.DriverVacations.ManageSelf,
                
                PermissionConstants.Filters.View,
                PermissionConstants.Filters.Manage,
                
                PermissionConstants.ReportPeriods.View,
                PermissionConstants.ReportPeriods.Manage,
                PermissionConstants.ReportPeriods.ManageSelf,
                
                PermissionConstants.ServiceTypes.View,
                PermissionConstants.ServiceTypes.Manage,
                PermissionConstants.ServiceTypes.ManageSelf,
                
                PermissionConstants.Trips.View,
                PermissionConstants.Trips.Manage,
                PermissionConstants.Trips.ManageSelf,
                
                PermissionConstants.Vehicles.View,
                PermissionConstants.Vehicles.Manage,
                PermissionConstants.Vehicles.ManageSelf,
                
                PermissionConstants.VehicleTypes.View,
                PermissionConstants.VehicleTypes.Manage,
                PermissionConstants.VehicleTypes.ManageSelf,
                
                PermissionConstants.Gas.View,
                PermissionConstants.Gas.Manage,
                
            }
        },
        {
            RoleConstants.Employee, new List<string>
            {
                PermissionConstants.Users.ManageSelf,
                PermissionConstants.Users.View,
                
                PermissionConstants.Drivers.View,
                
                PermissionConstants.DriverAssignments.View,
                PermissionConstants.DriverAssignments.Manage,
                
                PermissionConstants.DriverOffDays.View,
                PermissionConstants.DriverOffDays.Manage,
                
                PermissionConstants.DriverVacations.View,
                PermissionConstants.DriverVacations.Manage,
                
                PermissionConstants.Filters.View,
                
                PermissionConstants.ReportPeriods.View,
                PermissionConstants.ReportPeriods.ManageSelf,
                
                PermissionConstants.ServiceTypes.View,
                PermissionConstants.ServiceTypes.Manage,
                PermissionConstants.ServiceTypes.ManageSelf,
                
                PermissionConstants.Trips.View,
                PermissionConstants.Trips.ManageSelf,
                
                PermissionConstants.Vehicles.View,
                PermissionConstants.Vehicles.Manage,
                PermissionConstants.Vehicles.ManageSelf,
                
                PermissionConstants.VehicleTypes.View,
                PermissionConstants.VehicleTypes.Manage,
                PermissionConstants.VehicleTypes.ManageSelf,

            }
        },
    };

    public static IEnumerable<string> GetPermissionsByRoles(IEnumerable<string> roles)
    {
        return roles
            .SelectMany(role => RolePermissions.TryGetValue(role, out var permissions)
                ? permissions
                : Enumerable.Empty<string>())
            .Distinct()
            .ToList();
    }
    
    public static IEnumerable<string> GetAllRoles()
    {
        return RolePermissions.Keys;
    }
}
