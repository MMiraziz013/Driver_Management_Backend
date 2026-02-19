namespace Clean.Application.Security.Permission;

public static class PermissionConstants 
    {
        public const string ClaimType = "Permission";
        
        public static class Drivers
        {
            public const string View = "Permissions.Drivers.View";
            public const string Manage = "Permissions.Drivers.Manage";
            public const string ManageSelf = "Permissions.Drivers.ManageSelf";
            public const string ManageAll = "Permissions.Drivers.ManageAll";
        }

        public static class DriverAssignments
        {
            public const string View = "Permissions.DriverAssignments.View";
            public const string Manage = "Permissions.DriverAssignments.Manage";
            public const string ManageAll = "Permissions.DriverAssignments.ManageAll";
        }

        public static class DriverOffDays
        {
            public const string View = "Permissions.DriverOffDays.View";
            public const string Manage = "Permissions.DriverOffDays.Manage";
            public const string ManageSelf = "Permissions.DriverOffDays.ManageSelf";
            public const string ManageAll = "Permissions.DriverOffDays.ManageAll";

        }

        public static class DriverVacations
        {
            public const string View = "Permissions.DriverVacations.View";
            public const string Manage = "Permissions.DriverVacations.Manage";
            public const string ManageSelf = "Permissions.DriverVacations.ManageSelf";

        }

        public static class Filters
        {
            public const string View = "Permissions.Filters.View";
            public const string Manage = "Permissions.Filters.Manage";
        }

        public static class ReportPeriods
        {
            public const string View = "Permissions.ReportPeriods.View";
            public const string Manage = "Permissions.ReportPeriods.Manage";
            public const string ManageSelf = "Permissions.ReportPeriods.ManageSelf";
        }
        
        public static class ServiceTypes
        {
            public const string View = "Permissions.ServiceTypes.View";
            public const string Manage = "Permissions.ServiceTypes.Manage";
            public const string ManageSelf = "Permissions.ServiceTypes.ManageSelf";
        }
        
        public static class Trips
        {
            public const string View = "Permissions.Trips.View";
            public const string Manage = "Permissions.Trips.Manage";
            public const string ManageSelf = "Permissions.Trips.ManageSelf";
        }
        
        public static class Vehicles
        {
            public const string View = "Permissions.Vehicles.View";
            public const string Manage = "Permissions.Vehicles.Manage";
            public const string ManageSelf = "Permissions.Vehicles.ManageSelf";
        }
        
        public static class VehicleTypes
        {
            public const string View = "Permissions.VehicleTypes.View";
            public const string Manage = "Permissions.VehicleTypes.Manage";
            public const string ManageSelf = "Permissions.VehicleTypes.ManageSelf";
        }

        public static class Users
        {
            public const string ManageAll = "Permissions.Users.ManageAll";
            public const string ManageSelf = "Permissions.Users.ManageSelf";
            public const string View = "Permissions.Users.View";
        }
        
        public static class Gas
        {
            public const string Manage = "Permissions.Gas.Manage";
            public const string View = "Permissions.Gas.View";
        }
    }