using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Localization;
using Abp.MultiTenancy;

namespace NextWave.Erp.Authorization;

/// <summary>
/// Application's authorization provider.
/// Defines permissions for the application.
/// See <see cref="AppPermissions"/> for all permission names.
/// </summary>
public class AppAuthorizationProvider : AuthorizationProvider
{
    private readonly bool _isMultiTenancyEnabled;

    public AppAuthorizationProvider(bool isMultiTenancyEnabled)
    {
        _isMultiTenancyEnabled = isMultiTenancyEnabled;
    }

    public AppAuthorizationProvider(IMultiTenancyConfig multiTenancyConfig)
    {
        _isMultiTenancyEnabled = multiTenancyConfig.IsEnabled;
    }

    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

        var pages = context.GetPermissionOrNull(AppPermissions.Pages) ?? context.CreatePermission(AppPermissions.Pages, L("Pages"));
        pages.CreateChildPermission(AppPermissions.Pages_DemoUiComponents, L("DemoUiComponents"));


       

        var administration = pages.CreateChildPermission(AppPermissions.Pages_Administration, L("Administration"));

        var roles = administration.CreateChildPermission(AppPermissions.Pages_Administration_Roles, L("Roles"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Create, L("CreatingNewRole"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Edit, L("EditingRole"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Delete, L("DeletingRole"));

        var users = administration.CreateChildPermission(AppPermissions.Pages_Administration_Users, L("Users"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Create, L("CreatingNewUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Edit, L("EditingUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Delete, L("DeletingUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangePermissions, L("ChangingPermissions"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Impersonation, L("LoginForUsers"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Unlock, L("Unlock"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangeProfilePicture, L("UpdateUsersProfilePicture"));

        var languages = administration.CreateChildPermission(AppPermissions.Pages_Administration_Languages, L("Languages"));
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Create, L("CreatingNewLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Edit, L("EditingLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Delete, L("DeletingLanguages"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeTexts, L("ChangingTexts"));
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeDefaultLanguage, L("ChangeDefaultLanguage"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_AuditLogs, L("AuditLogs"));

        var organizationUnits = administration.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits, L("OrganizationUnits"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageOrganizationTree, L("ManagingOrganizationTree"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageMembers, L("ManagingMembers"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageRoles, L("ManagingRoles"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_UiCustomization, L("VisualSettings"));

        var webhooks = administration.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription, L("Webhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Create, L("CreatingWebhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Edit, L("EditingWebhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_ChangeActivity, L("ChangingWebhookActivity"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Detail, L("DetailingSubscription"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ListSendAttempts, L("ListingSendAttempts"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ResendWebhook, L("ResendingWebhook"));

        var dynamicProperties = administration.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties, L("DynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Create, L("CreatingDynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Edit, L("EditingDynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Delete, L("DeletingDynamicProperties"));

        var dynamicPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue, L("DynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Create, L("CreatingDynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Edit, L("EditingDynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Delete, L("DeletingDynamicPropertyValue"));

        var dynamicEntityProperties = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties, L("DynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Create, L("CreatingDynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Edit, L("EditingDynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Delete, L("DeletingDynamicEntityProperties"));

        var dynamicEntityPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue, L("EntityDynamicPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Create, L("CreatingDynamicEntityPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Edit, L("EditingDynamicEntityPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Delete, L("DeletingDynamicEntityPropertyValue"));

        var massNotification = administration.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification, L("MassNotifications"));
        massNotification.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification_Create, L("MassNotificationCreate"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_EntityChanges_FullHistory, L("EntityChanges_FullHistory"));

        //TENANT-SPECIFIC PERMISSIONS

        pages.CreateChildPermission(AppPermissions.Pages_Tenant_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Tenant);

        administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Tenant);
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_SubscriptionManagement, L("Subscription"), multiTenancySides: MultiTenancySides.Tenant);

        //HOST-SPECIFIC PERMISSIONS

        var editions = pages.CreateChildPermission(AppPermissions.Pages_Editions, L("Editions"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Create, L("CreatingNewEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Edit, L("EditingEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Delete, L("DeletingEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_MoveTenantsToAnotherEdition, L("MoveTenantsToAnotherEdition"), multiTenancySides: MultiTenancySides.Host);

        var tenants = pages.CreateChildPermission(AppPermissions.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Create, L("CreatingNewTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Edit, L("EditingTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_ChangeFeatures, L("ChangingFeatures"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Delete, L("DeletingTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Impersonation, L("LoginForTenants"), multiTenancySides: MultiTenancySides.Host);

        administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Host);

        var maintenance = administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Maintenance, L("Maintenance"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        maintenance.CreateChildPermission(AppPermissions.Pages_Administration_NewVersion_Create, L("SendNewVersionNotification"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_HangfireDashboard, L("HangfireDashboard"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Host);

        //ADDED PERMISSIONS START

        // Add your custom permissions here
        var accounting = pages.CreateChildPermission(AppPermissions.PagesAccounting, L("Accounting"));

        var accountLedgers = accounting.CreateChildPermission(AppPermissions.PagesAccountLedgers,
            L("AccountLedgers"), multiTenancySides: MultiTenancySides.Tenant);
        accountLedgers.CreateChildPermission(AppPermissions.PagesAccountLedgersCreate,
            L("CreateNewAccountLedger"), multiTenancySides: MultiTenancySides.Tenant);
        accountLedgers.CreateChildPermission(AppPermissions.PagesAccountLedgersEdit, L("EditAccountLedger"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountLedgers.CreateChildPermission(AppPermissions.PagesAccountLedgersDelete, L("DeleteAccountLedger"),
            multiTenancySides: MultiTenancySides.Tenant);

        var accountGroups = accounting.CreateChildPermission(AppPermissions.PagesAccountGroups, L("AccountGroups"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountGroups.CreateChildPermission(AppPermissions.PagesAccountGroupsCreate, L("CreateNewAccountGroup"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountGroups.CreateChildPermission(AppPermissions.PagesAccountGroupsEdit, L("EditAccountGroup"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountGroups.CreateChildPermission(AppPermissions.PagesAccountGroupsDelete, L("DeleteAccountGroup"),
            multiTenancySides: MultiTenancySides.Tenant);

        var financialYears = accounting.CreateChildPermission(AppPermissions.PagesFinancialYears,
            L("FinancialYears"), multiTenancySides: MultiTenancySides.Tenant);
        financialYears.CreateChildPermission(AppPermissions.PagesFinancialYearsCreate, L("CreateNewFinancialYear"),
            multiTenancySides: MultiTenancySides.Tenant);
        financialYears.CreateChildPermission(AppPermissions.PagesFinancialYearsEdit, L("EditFinancialYear"),
            multiTenancySides: MultiTenancySides.Tenant);
        financialYears.CreateChildPermission(AppPermissions.PagesFinancialYearsDelete, L("DeleteFinancialYear"),
            multiTenancySides: MultiTenancySides.Tenant);
        financialYears.CreateChildPermission(AppPermissions.PagesFinancialYearsSplit, L("SplitFinancialYear"),
            multiTenancySides: MultiTenancySides.Tenant);

        var taxes = accounting.CreateChildPermission(AppPermissions.PagesTaxes, L("Taxes"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxes.CreateChildPermission(AppPermissions.PagesTaxesCreate, L("CreateNewTax"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxes.CreateChildPermission(AppPermissions.PagesTaxesEdit, L("EditTax"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxes.CreateChildPermission(AppPermissions.PagesTaxesDelete, L("DeleteTax"),
            multiTenancySides: MultiTenancySides.Tenant);

        var voucherTypes = accounting.CreateChildPermission(AppPermissions.PagesVoucherTypes, L("VoucherType"),
               multiTenancySides: MultiTenancySides.Tenant);
        voucherTypes.CreateChildPermission(AppPermissions.PagesVoucherTypesCreate, L("CreateVoucherType"),
            multiTenancySides: MultiTenancySides.Tenant);
        voucherTypes.CreateChildPermission(AppPermissions.PagesVoucherTypesEdit, L("EditVoucherType"),
            multiTenancySides: MultiTenancySides.Tenant);
        voucherTypes.CreateChildPermission(AppPermissions.PagesVoucherTypesDelete, L("DeleteVoucherType"),
            multiTenancySides: MultiTenancySides.Tenant);

        var mergeLeder = accounting.CreateChildPermission(AppPermissions.PagesMergeLedger, L("MergeLedger"),
       multiTenancySides: MultiTenancySides.Tenant);

        var inventory = pages.CreateChildPermission(AppPermissions.PagesInventory, L("Inventory"),
               multiTenancySides: MultiTenancySides.Tenant);

        var products = inventory.CreateChildPermission(AppPermissions.PagesProducts, L("Products"),
            multiTenancySides: MultiTenancySides.Tenant);
        products.CreateChildPermission(AppPermissions.PagesProductsCreate, L("CreateNewProduct"),
            multiTenancySides: MultiTenancySides.Tenant);
        products.CreateChildPermission(AppPermissions.PagesProductsEdit, L("EditProduct"),
            multiTenancySides: MultiTenancySides.Tenant);
        products.CreateChildPermission(AppPermissions.PagesProductsDelete, L("DeleteProduct"),
            multiTenancySides: MultiTenancySides.Tenant);


        var units = inventory.CreateChildPermission(AppPermissions.PagesUnits, L("Units"),
               multiTenancySides: MultiTenancySides.Tenant);
        units.CreateChildPermission(AppPermissions.PagesUnitsCreate, L("CreateNewUnit"),
            multiTenancySides: MultiTenancySides.Tenant);
        units.CreateChildPermission(AppPermissions.PagesUnitsEdit, L("EditUnit"),
            multiTenancySides: MultiTenancySides.Tenant);
        units.CreateChildPermission(AppPermissions.PagesUnitsDelete, L("DeleteUnit"),
            multiTenancySides: MultiTenancySides.Tenant);

        var productGroups = inventory.CreateChildPermission(AppPermissions.PagesProductGroups, L("ProductGroups"),
            multiTenancySides: MultiTenancySides.Tenant);
        productGroups.CreateChildPermission(AppPermissions.PagesProductGroupsCreate, L("CreateNewProductGroup"),
            multiTenancySides: MultiTenancySides.Tenant);
        productGroups.CreateChildPermission(AppPermissions.PagesProductGroupsEdit, L("EditProductGroup"),
            multiTenancySides: MultiTenancySides.Tenant);
        productGroups.CreateChildPermission(AppPermissions.PagesProductGroupsDelete, L("DeleteProductGroup"),
            multiTenancySides: MultiTenancySides.Tenant);

        var productMerge = inventory.CreateChildPermission(AppPermissions.PagesProductMerge, L("ProductMerge"),
               multiTenancySides: MultiTenancySides.Tenant);

        var purchase = pages.CreateChildPermission(AppPermissions.PagesPurchase, L("Purchase"));

        var purchaseOrderMasters = purchase.CreateChildPermission(AppPermissions.PagesPurchaseOrderMasters,
                L("PurchaseOrder"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseOrderMasters.CreateChildPermission(AppPermissions.PagesPurchaseOrderMastersCreate,
            L("CreateNewPurchaseOrder"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseOrderMasters.CreateChildPermission(AppPermissions.PagesPurchaseOrderMastersEdit,
            L("EditPurchaseOrder"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseOrderMasters.CreateChildPermission(AppPermissions.PagesPurchaseOrderMastersDelete,
            L("DeletePurchaseOrder"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseOrderMasters.CreateChildPermission(AppPermissions.PagesPurchaseOrderMastersPrint,
            L("PrintPurchaseOrder"), multiTenancySides: MultiTenancySides.Tenant);

        var purchaseMasters = purchase.CreateChildPermission(AppPermissions.PagesPurchaseMasters, L("Purchase"),
              multiTenancySides: MultiTenancySides.Tenant);
        purchaseMasters.CreateChildPermission(AppPermissions.PagesPurchaseMastersCreate, L("CreateNewPurchase"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseMasters.CreateChildPermission(AppPermissions.PagesPurchaseMastersEdit, L("EditPurchase"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseMasters.CreateChildPermission(AppPermissions.PagesPurchaseMastersDelete, L("DeletePurchase"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseMasters.CreateChildPermission(AppPermissions.PagesPurchaseMastersPrint, L("PrintPurchase"),
            multiTenancySides: MultiTenancySides.Tenant);

        var purchaseReturns = purchase.CreateChildPermission(AppPermissions.PagesPurchaseReturns,
               L("PurchaseReturns"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseReturns.CreateChildPermission(AppPermissions.PagesPurchaseReturnsCreate,
            L("CreateNewPurchaseReturn"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseReturns.CreateChildPermission(AppPermissions.PagesPurchaseReturnsEdit, L("EditPurchaseReturn"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseReturns.CreateChildPermission(AppPermissions.PagesPurchaseReturnsDelete, L("DeletePurchaseReturn"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseReturns.CreateChildPermission(AppPermissions.PagesPurchaseReturnsPrint, L("PrintPurchaseReturn"),
            multiTenancySides: MultiTenancySides.Tenant);

        var sales = pages.CreateChildPermission(AppPermissions.PagesSales, L("Sales"));

        var salesMasters = sales.CreateChildPermission(AppPermissions.PagesSalesMasters, L("Sales"),
              multiTenancySides: MultiTenancySides.Tenant);
        salesMasters.CreateChildPermission(AppPermissions.PagesSalesMastersCreate, L("CreateNewSales"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesMasters.CreateChildPermission(AppPermissions.PagesSalesMastersEdit, L("EditSales"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesMasters.CreateChildPermission(AppPermissions.PagesSalesMastersDelete, L("DeleteSales"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesMasters.CreateChildPermission(AppPermissions.PagesSalesMastersPrint, L("PrintSales"),
            multiTenancySides: MultiTenancySides.Tenant);

        var salesReturnMasters = sales.CreateChildPermission(AppPermissions.PagesSalesReturnMasters,
               L("SalesReturn"), multiTenancySides: MultiTenancySides.Tenant);
        salesReturnMasters.CreateChildPermission(AppPermissions.PagesSalesReturnMastersCreate,
            L("CreateNewSalesReturn"), multiTenancySides: MultiTenancySides.Tenant);
        salesReturnMasters.CreateChildPermission(AppPermissions.PagesSalesReturnMastersEdit, L("EditSalesReturn"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesReturnMasters.CreateChildPermission(AppPermissions.PagesSalesReturnMastersDelete,
            L("DeleteSalesReturn"), multiTenancySides: MultiTenancySides.Tenant);
        salesReturnMasters.CreateChildPermission(AppPermissions.PagesSalesReturnMastersPrint, L("PrintSalesReturn"),
            multiTenancySides: MultiTenancySides.Tenant);

        var restaurant = pages.CreateChildPermission(AppPermissions.PagesRestaurant, L("Restaurant"),
            multiTenancySides: MultiTenancySides.Tenant);

        var restaurantSetup = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantSetup, L("RestaurantSetup"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantSetup.CreateChildPermission(AppPermissions.PagesRestaurantSetupCreate, L("CreateRestaurantSetup"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantSetup.CreateChildPermission(AppPermissions.PagesRestaurantSetupEdit, L("EditRestaurantSetup"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantSetup.CreateChildPermission(AppPermissions.PagesRestaurantSetupDelete, L("DeleteRestaurantSetup"),
            multiTenancySides: MultiTenancySides.Tenant);

        var restaurantMenu = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantMenu, L("RestaurantMenu"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantMenuCreate, L("CreateRestaurantMenu"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantMenuEdit, L("EditRestaurantMenu"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantMenuDelete, L("DeleteRestaurantMenu"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantMenuVariants, L("RestaurantMenuVariants"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantMenuModifiers, L("RestaurantMenuModifiers"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantMenu.CreateChildPermission(AppPermissions.PagesRestaurantItemAvailability, L("RestaurantItemAvailability"),
            multiTenancySides: MultiTenancySides.Tenant);

        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantRecipe, L("RestaurantRecipe"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantPos = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantPos, L("RestaurantPOS"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPos.CreateChildPermission(AppPermissions.PagesRestaurantPosDiscount, L("RestaurantPOSDiscount"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPos.CreateChildPermission(AppPermissions.PagesRestaurantPosVoid, L("RestaurantPOSVoid"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPos.CreateChildPermission(AppPermissions.PagesRestaurantPosTableTransfer, L("RestaurantPOSTableTransfer"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPos.CreateChildPermission(AppPermissions.PagesRestaurantPosSplitMerge, L("RestaurantPOSSplitMerge"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantBilling, L("RestaurantBilling"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantKotBot = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantKotBot, L("RestaurantKOTBOT"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantKotBot.CreateChildPermission(AppPermissions.PagesRestaurantKotBotReprint, L("RestaurantKOTBOTReprint"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantKds, L("RestaurantKDS"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantSync, L("RestaurantMobileSync"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantReports = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantReports, L("RestaurantReports"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantReports.CreateChildPermission(AppPermissions.PagesRestaurantReportsSales, L("RestaurantReportsSales"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantReports.CreateChildPermission(AppPermissions.PagesRestaurantReportsOperations, L("RestaurantReportsOperations"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantReports.CreateChildPermission(AppPermissions.PagesRestaurantReportsInventory, L("RestaurantReportsInventory"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantReports.CreateChildPermission(AppPermissions.PagesRestaurantReportsPayroll, L("RestaurantReportsPayroll"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantReports.CreateChildPermission(AppPermissions.PagesRestaurantReportsAuditFinance, L("RestaurantReportsAuditFinance"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantInventory = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantInventory, L("RestaurantInventory"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantInventory.CreateChildPermission(AppPermissions.PagesRestaurantInventorySupplierMapping, L("RestaurantSupplierMapping"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantInventory.CreateChildPermission(AppPermissions.PagesRestaurantInventoryStockAdjustment, L("RestaurantStockAdjustment"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantInventory.CreateChildPermission(AppPermissions.PagesRestaurantInventoryWastage, L("RestaurantWastage"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantInventory.CreateChildPermission(AppPermissions.PagesRestaurantInventoryReorder, L("RestaurantReorder"),
            multiTenancySides: MultiTenancySides.Tenant);

        var restaurantChannels = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantChannels, L("RestaurantChannels"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantChannels.CreateChildPermission(AppPermissions.PagesRestaurantChannelsManage, L("ManageRestaurantChannels"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantChannels.CreateChildPermission(AppPermissions.PagesRestaurantAggregators, L("RestaurantAggregators"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantChannels.CreateChildPermission(AppPermissions.PagesRestaurantPayouts, L("RestaurantPayouts"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantCustomerOrdering, L("RestaurantCustomerOrdering"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantReservations, L("RestaurantReservations"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurant.CreateChildPermission(AppPermissions.PagesRestaurantPrinterSetup, L("RestaurantPrinterSetup"),
            multiTenancySides: MultiTenancySides.Tenant);

        var restaurantPayroll = restaurant.CreateChildPermission(AppPermissions.PagesRestaurantPayroll, L("RestaurantPayroll"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantPayrollStaff = restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollStaff, L("RestaurantPayrollStaff"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayrollStaff.CreateChildPermission(AppPermissions.PagesRestaurantPayrollStaffAccess, L("RestaurantPayrollStaffAccess"),
            multiTenancySides: MultiTenancySides.Tenant);
        var restaurantPayrollAttendance = restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollAttendance, L("RestaurantPayrollAttendance"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayrollAttendance.CreateChildPermission(AppPermissions.PagesRestaurantPayrollAttendanceManage, L("RestaurantPayrollAttendanceManage"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollProcess, L("RestaurantPayrollProcess"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollApprove, L("RestaurantPayrollApprove"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollReports, L("RestaurantPayrollReports"),
            multiTenancySides: MultiTenancySides.Tenant);
        restaurantPayroll.CreateChildPermission(AppPermissions.PagesRestaurantPayrollOwnPayslip, L("RestaurantPayrollOwnPayslip"),
            multiTenancySides: MultiTenancySides.Tenant);



        var transaction = pages.CreateChildPermission(AppPermissions.PagesTransaction, L("Transaction"), multiTenancySides: MultiTenancySides.Tenant);


        var paymentMaster = transaction.CreateChildPermission(AppPermissions.PagesPaymentMasters, L("Payment"),
    multiTenancySides: MultiTenancySides.Tenant);
        paymentMaster.CreateChildPermission(AppPermissions.PagesPaymentMastersCreate,
            L("CreatePaymentMastersCreate"), multiTenancySides: MultiTenancySides.Tenant);
        paymentMaster.CreateChildPermission(AppPermissions.PagesPaymentMastersEdit, L("EditPayment"),
            multiTenancySides: MultiTenancySides.Tenant);
        paymentMaster.CreateChildPermission(AppPermissions.PagesPaymentMastersDelete, L("DeletePayment"),
            multiTenancySides: MultiTenancySides.Tenant);
        paymentMaster.CreateChildPermission(AppPermissions.PagesPaymentMastersPrint, L("PrintPayment"),
            multiTenancySides: MultiTenancySides.Tenant);

        var receiptMaster = transaction.CreateChildPermission(AppPermissions.PagesReceiptMasters, L("Receipt"),
               multiTenancySides: MultiTenancySides.Tenant);
        receiptMaster.CreateChildPermission(AppPermissions.PagesReceiptMastersCreate,
            L("CreateReceiptMastersCreate"), multiTenancySides: MultiTenancySides.Tenant);
        receiptMaster.CreateChildPermission(AppPermissions.PagesReceiptMastersEdit, L("EditReceipt"),
            multiTenancySides: MultiTenancySides.Tenant);
        receiptMaster.CreateChildPermission(AppPermissions.PagesReceiptMastersDelete, L("DeleteReceipt"),
            multiTenancySides: MultiTenancySides.Tenant);
        receiptMaster.CreateChildPermission(AppPermissions.PagesReceiptMastersPrint, L("PrintReceipt"),
            multiTenancySides: MultiTenancySides.Tenant);


        var journalMaster = transaction.CreateChildPermission(AppPermissions.PagesJournalMasters, L("Journal"),
                multiTenancySides: MultiTenancySides.Tenant);
        journalMaster.CreateChildPermission(AppPermissions.PagesJournalMastersCreate,
            L("CreateJournalMastersCreate"), multiTenancySides: MultiTenancySides.Tenant);
        journalMaster.CreateChildPermission(AppPermissions.PagesJournalMastersEdit, L("EditJournal"),
            multiTenancySides: MultiTenancySides.Tenant);
        journalMaster.CreateChildPermission(AppPermissions.PagesJournalMastersDelete, L("DeleteJournal"),
            multiTenancySides: MultiTenancySides.Tenant);
        journalMaster.CreateChildPermission(AppPermissions.PagesJournalMastersPrint, L("PrintJournal"),
            multiTenancySides: MultiTenancySides.Tenant);

        var contraMaster = transaction.CreateChildPermission(AppPermissions.PagesContraMasters, L("Contra"),
               multiTenancySides: MultiTenancySides.Tenant);
        contraMaster.CreateChildPermission(AppPermissions.PagesContraMastersCreate, L("CreateContraMastersCreate"),
            multiTenancySides: MultiTenancySides.Tenant);
        contraMaster.CreateChildPermission(AppPermissions.PagesContraMastersEdit, L("EditContra"),
            multiTenancySides: MultiTenancySides.Tenant);
        contraMaster.CreateChildPermission(AppPermissions.PagesContraMastersDelete, L("DeleteContra"),
            multiTenancySides: MultiTenancySides.Tenant);
        contraMaster.CreateChildPermission(AppPermissions.PagesContraMastersPrint, L("PrintContra"),
            multiTenancySides: MultiTenancySides.Tenant);

        var pdcPayables = transaction.CreateChildPermission(AppPermissions.PagesPDCPayables, L("PDCPayables"),
                multiTenancySides: MultiTenancySides.Tenant);
        pdcPayables.CreateChildPermission(AppPermissions.PagesPDCPayablesCreate, L("CreatePDCPayablesCreate"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcPayables.CreateChildPermission(AppPermissions.PagesPDCPayablesEdit, L("EditPDCPayables"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcPayables.CreateChildPermission(AppPermissions.PagesPDCPayablesDelete, L("DeletePDCPayables"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcPayables.CreateChildPermission(AppPermissions.PagesPDCPayablesPrint, L("PrintPDCPayables"),
            multiTenancySides: MultiTenancySides.Tenant);

        var pdcReceivables = transaction.CreateChildPermission(AppPermissions.PagesPDCReceivables,
               L("PDCReceivables"), multiTenancySides: MultiTenancySides.Tenant);
        pdcReceivables.CreateChildPermission(AppPermissions.PagesPDCReceivablesCreate,
            L("CreatePDCReceivablesCreate"), multiTenancySides: MultiTenancySides.Tenant);
        pdcReceivables.CreateChildPermission(AppPermissions.PagesPDCReceivablesEdit, L("EditPDCReceivables"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcReceivables.CreateChildPermission(AppPermissions.PagesPDCReceivablesDelete, L("DeletePDCReceivables"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcReceivables.CreateChildPermission(AppPermissions.PagesPDCReceivablesPrint, L("PrintPDCReceivables"),
            multiTenancySides: MultiTenancySides.Tenant);


        var pdcClearances = transaction.CreateChildPermission(AppPermissions.PagesPDCClearances, L("PDCClearances"),
                multiTenancySides: MultiTenancySides.Tenant);
        pdcClearances.CreateChildPermission(AppPermissions.PagesPDCClearancesCreate, L("CreatePDCClearancesCreate"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcClearances.CreateChildPermission(AppPermissions.PagesPDCClearancesEdit, L("EditPDCClearances"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcClearances.CreateChildPermission(AppPermissions.PagesPDCClearancesDelete, L("DeletePDCClearances"),
            multiTenancySides: MultiTenancySides.Tenant);
        pdcClearances.CreateChildPermission(AppPermissions.PagesPDCClearancesPrint, L("PrintPDCClearances"),
            multiTenancySides: MultiTenancySides.Tenant);

        var reporting = pages.CreateChildPermission(AppPermissions.PagesReporting, L("Reporting"),
               multiTenancySides: MultiTenancySides.Tenant);

        var accountReport = reporting.CreateChildPermission(AppPermissions.PagesAccountingReporting,
            L("AccountingReport"), multiTenancySides: MultiTenancySides.Tenant);
        accountReport.CreateChildPermission(AppPermissions.PagesAccountGroupReport, L("AccountGroupReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountReport.CreateChildPermission(AppPermissions.PagesAccountLedgerReport, L("AccountLedgerReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        accountReport.CreateChildPermission(AppPermissions.PagesBookReport, L("BookReport"),
            multiTenancySides: MultiTenancySides.Tenant);

        var inventoryReport = reporting.CreateChildPermission(AppPermissions.PagesInventoryReporting,
                L("InventoryReport"), multiTenancySides: MultiTenancySides.Tenant);
        inventoryReport.CreateChildPermission(AppPermissions.PagesStockReport, L("StockReport"),
               multiTenancySides: MultiTenancySides.Tenant);
        inventoryReport.CreateChildPermission(AppPermissions.PagesProductProfitReport, L("ProductProfitReport"),
                multiTenancySides: MultiTenancySides.Tenant);


        var purchaseReport = reporting.CreateChildPermission(AppPermissions.PagesPurchaseReporting,
                L("PurchaseReport"), multiTenancySides: MultiTenancySides.Tenant);
        purchaseReport.CreateChildPermission(AppPermissions.PagesPurchaseMasterReport, L("PurchaseMasterReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseReport.CreateChildPermission(AppPermissions.PagesPurchaseReturnReport, L("PurchaseReturnReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        purchaseReport.CreateChildPermission(AppPermissions.PagesProductWiseMonthlyReport, L("ProductWiseMonthlyPurchaseReport"),
            multiTenancySides: MultiTenancySides.Tenant);


        var salesReport = reporting.CreateChildPermission(AppPermissions.PagesSalesReporting, L("SalesReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesReport.CreateChildPermission(AppPermissions.PagesSalesMasterReport, L("SalesMasterReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesReport.CreateChildPermission(AppPermissions.PagesSalesReturnReport, L("SalesReturnReport"),
                multiTenancySides: MultiTenancySides.Tenant);
        salesReport.CreateChildPermission(AppPermissions.PagesMaterialSalesReport, L("MaterialSalesReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesReport.CreateChildPermission(AppPermissions.PagesLedgerWiseSalesReport, L("LedgerWiseSalesReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        salesReport.CreateChildPermission(AppPermissions.PagesProductWiseSalesReport, L("ProductWiseSalesReport"),
            multiTenancySides: MultiTenancySides.Tenant);
      

        var taxReport = reporting.CreateChildPermission(AppPermissions.PagesTaxReporting, L("TaxReporting"),
                multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesTaxPurchaseRegisterReport,
            L("TaxPurchaseRegisterReport"), multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesTaxSalesRegisterReport, L("TaxSalesRegisterReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesTdsReport, L("TdsReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesVatSummaryReport, L("VatSummaryReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesGetPayTaxReport, L("GetPayTaxReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesTaxableCustomerReport, L("TaxableCustomerReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesSalesAboveLakhsReport, L("PagesSalesAboveLakhsReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesPurchaseAboveLakhsReport, L("PagesPurchaseAboveLakhsReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesSalesTaxReport, L("PagesSalesTaxReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        taxReport.CreateChildPermission(AppPermissions.PagesPurchaseTaxReport, L("PagesPurchaseTaxReport"),
            multiTenancySides: MultiTenancySides.Tenant);


        var financialStatement =
               reporting.CreateChildPermission(AppPermissions.PagesFinancialStatement, L("FinancialStatement"), multiTenancySides: MultiTenancySides.Tenant);
        financialStatement.CreateChildPermission(AppPermissions.PagesBalanceSheetReport,
            L("BalanceSheetReport"), multiTenancySides: MultiTenancySides.Tenant);
        financialStatement.CreateChildPermission(AppPermissions.PagesProfitAndLossReport,
            L("ProfitAndLossReport"), multiTenancySides: MultiTenancySides.Tenant);
        financialStatement.CreateChildPermission(AppPermissions.PagesTrialBalanceReport,
            L("TrialBalanceReport"), multiTenancySides: MultiTenancySides.Tenant);
        financialStatement.CreateChildPermission(AppPermissions.PageFundFlowReport, L("FundFlowReport"),
            multiTenancySides: MultiTenancySides.Tenant);
        financialStatement.CreateChildPermission(AppPermissions.PagesCashFlowReport, L("CashFlowReport"),
            multiTenancySides: MultiTenancySides.Tenant);

    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, ErpConsts.LocalizationSourceName);
    }
}

