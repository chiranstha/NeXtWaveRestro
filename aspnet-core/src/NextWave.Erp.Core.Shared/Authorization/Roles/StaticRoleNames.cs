namespace NextWave.Erp.Authorization.Roles;

public static class StaticRoleNames
{
    public static class Host
    {
        public const string Admin = "Admin";
    }

    public static class Tenants
    {
        public const string Admin = "Admin";

        public const string User = "User";

        public const string RestaurantManager = "RestaurantManager";

        public const string RestaurantCashier = "RestaurantCashier";

        public const string RestaurantWaiter = "RestaurantWaiter";

        public const string RestaurantKitchen = "RestaurantKitchen";

        public const string RestaurantInventory = "RestaurantInventory";
    }
}

