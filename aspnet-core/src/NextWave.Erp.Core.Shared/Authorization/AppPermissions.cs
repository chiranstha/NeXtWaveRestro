namespace NextWave.Erp.Authorization;

/// <summary>
/// Defines string constants for application's permission names.
/// <see cref="AppAuthorizationProvider"/> for permission definitions.
/// </summary>
public static class AppPermissions
{
    //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

    public const string Pages = "Pages";


    // Financial Statement State
    public const string PagesFinancialStatement = "Pages.FinancialStatement";

    public const string PagesProfitAndLossReport = "Pages.ProfitAndLossReport";
    public const string PagesTrialBalanceReport = "Pages.TrialBalanceReport";
    public const string PagesBalanceSheetReport = "Pages.PagesBalanceSheetReport";
    public const string PageFundFlowReport = "Pages.PageFundFlowReport";
    public const string PagesCashFlowReport = "Pages.PagesCashFlowReport";

    public const string Pages_DemoUiComponents = "Pages.DemoUiComponents";
    public const string Pages_Administration = "Pages.Administration";

    public const string Pages_Administration_Roles = "Pages.Administration.Roles";
    public const string Pages_Administration_Roles_Create = "Pages.Administration.Roles.Create";
    public const string Pages_Administration_Roles_Edit = "Pages.Administration.Roles.Edit";
    public const string Pages_Administration_Roles_Delete = "Pages.Administration.Roles.Delete";

    public const string Pages_Administration_Users = "Pages.Administration.Users";
    public const string Pages_Administration_Users_Create = "Pages.Administration.Users.Create";
    public const string Pages_Administration_Users_Edit = "Pages.Administration.Users.Edit";
    public const string Pages_Administration_Users_Delete = "Pages.Administration.Users.Delete";
    public const string Pages_Administration_Users_ChangePermissions = "Pages.Administration.Users.ChangePermissions";
    public const string Pages_Administration_Users_Impersonation = "Pages.Administration.Users.Impersonation";
    public const string Pages_Administration_Users_Unlock = "Pages.Administration.Users.Unlock";
    public const string Pages_Administration_Users_ChangeProfilePicture = "Pages.Administration.Users.ChangeProfilePicture";

    public const string Pages_Administration_Languages = "Pages.Administration.Languages";
    public const string Pages_Administration_Languages_Create = "Pages.Administration.Languages.Create";
    public const string Pages_Administration_Languages_Edit = "Pages.Administration.Languages.Edit";
    public const string Pages_Administration_Languages_Delete = "Pages.Administration.Languages.Delete";
    public const string Pages_Administration_Languages_ChangeTexts = "Pages.Administration.Languages.ChangeTexts";
    public const string Pages_Administration_Languages_ChangeDefaultLanguage = "Pages.Administration.Languages.ChangeDefaultLanguage";

    public const string Pages_Administration_AuditLogs = "Pages.Administration.AuditLogs";

    public const string Pages_Administration_OrganizationUnits = "Pages.Administration.OrganizationUnits";
    public const string Pages_Administration_OrganizationUnits_ManageOrganizationTree = "Pages.Administration.OrganizationUnits.ManageOrganizationTree";
    public const string Pages_Administration_OrganizationUnits_ManageMembers = "Pages.Administration.OrganizationUnits.ManageMembers";
    public const string Pages_Administration_OrganizationUnits_ManageRoles = "Pages.Administration.OrganizationUnits.ManageRoles";

    public const string Pages_Administration_HangfireDashboard = "Pages.Administration.HangfireDashboard";

    public const string Pages_Administration_UiCustomization = "Pages.Administration.UiCustomization";

    public const string Pages_Administration_WebhookSubscription = "Pages.Administration.WebhookSubscription";
    public const string Pages_Administration_WebhookSubscription_Create = "Pages.Administration.WebhookSubscription.Create";
    public const string Pages_Administration_WebhookSubscription_Edit = "Pages.Administration.WebhookSubscription.Edit";
    public const string Pages_Administration_WebhookSubscription_ChangeActivity = "Pages.Administration.WebhookSubscription.ChangeActivity";
    public const string Pages_Administration_WebhookSubscription_Detail = "Pages.Administration.WebhookSubscription.Detail";
    public const string Pages_Administration_Webhook_ListSendAttempts = "Pages.Administration.Webhook.ListSendAttempts";
    public const string Pages_Administration_Webhook_ResendWebhook = "Pages.Administration.Webhook.ResendWebhook";

    public const string Pages_Administration_DynamicProperties = "Pages.Administration.DynamicProperties";
    public const string Pages_Administration_DynamicProperties_Create = "Pages.Administration.DynamicProperties.Create";
    public const string Pages_Administration_DynamicProperties_Edit = "Pages.Administration.DynamicProperties.Edit";
    public const string Pages_Administration_DynamicProperties_Delete = "Pages.Administration.DynamicProperties.Delete";

    public const string Pages_Administration_DynamicPropertyValue = "Pages.Administration.DynamicPropertyValue";
    public const string Pages_Administration_DynamicPropertyValue_Create = "Pages.Administration.DynamicPropertyValue.Create";
    public const string Pages_Administration_DynamicPropertyValue_Edit = "Pages.Administration.DynamicPropertyValue.Edit";
    public const string Pages_Administration_DynamicPropertyValue_Delete = "Pages.Administration.DynamicPropertyValue.Delete";

    public const string Pages_Administration_DynamicEntityProperties = "Pages.Administration.DynamicEntityProperties";
    public const string Pages_Administration_DynamicEntityProperties_Create = "Pages.Administration.DynamicEntityProperties.Create";
    public const string Pages_Administration_DynamicEntityProperties_Edit = "Pages.Administration.DynamicEntityProperties.Edit";
    public const string Pages_Administration_DynamicEntityProperties_Delete = "Pages.Administration.DynamicEntityProperties.Delete";

    public const string Pages_Administration_DynamicEntityPropertyValue = "Pages.Administration.DynamicEntityPropertyValue";
    public const string Pages_Administration_DynamicEntityPropertyValue_Create = "Pages.Administration.DynamicEntityPropertyValue.Create";
    public const string Pages_Administration_DynamicEntityPropertyValue_Edit = "Pages.Administration.DynamicEntityPropertyValue.Edit";
    public const string Pages_Administration_DynamicEntityPropertyValue_Delete = "Pages.Administration.DynamicEntityPropertyValue.Delete";

    public const string Pages_Administration_MassNotification = "Pages.Administration.MassNotification";
    public const string Pages_Administration_MassNotification_Create = "Pages.Administration.MassNotification.Create";

    public const string Pages_Administration_NewVersion_Create = "Pages.Administration.NewVersion.Create";

    public const string Pages_Administration_EntityChanges_FullHistory = "Pages.Administration.EntityChanges.FullHistory";

    //TENANT-SPECIFIC PERMISSIONS

    public const string Pages_Tenant_Dashboard = "Pages.Tenant.Dashboard";

    public const string Pages_Administration_Tenant_Settings = "Pages.Administration.Tenant.Settings";

    public const string Pages_Administration_Tenant_SubscriptionManagement = "Pages.Administration.Tenant.SubscriptionManagement";

    //HOST-SPECIFIC PERMISSIONS

    public const string Pages_Editions = "Pages.Editions";
    public const string Pages_Editions_Create = "Pages.Editions.Create";
    public const string Pages_Editions_Edit = "Pages.Editions.Edit";
    public const string Pages_Editions_Delete = "Pages.Editions.Delete";
    public const string Pages_Editions_MoveTenantsToAnotherEdition = "Pages.Editions.MoveTenantsToAnotherEdition";

    public const string Pages_Tenants = "Pages.Tenants";
    public const string Pages_Tenants_Create = "Pages.Tenants.Create";
    public const string Pages_Tenants_Edit = "Pages.Tenants.Edit";
    public const string Pages_Tenants_ChangeFeatures = "Pages.Tenants.ChangeFeatures";
    public const string Pages_Tenants_Delete = "Pages.Tenants.Delete";
    public const string Pages_Tenants_Impersonation = "Pages.Tenants.Impersonation";

    public const string Pages_Administration_Host_Maintenance = "Pages.Administration.Host.Maintenance";
    public const string Pages_Administration_Host_Settings = "Pages.Administration.Host.Settings";
    public const string Pages_Administration_Host_Dashboard = "Pages.Administration.Host.Dashboard";


    // Accounting
    public const string PagesAccounting = "Pages.Accounting";

    public const string PagesAccountGroups = "Pages.AccountGroups";
    public const string PagesAccountGroupsCreate = "Pages.AccountGroups.Create";
    public const string PagesAccountGroupsEdit = "Pages.AccountGroups.Edit";
    public const string PagesAccountGroupsDelete = "Pages.AccountGroups.Delete";

    public const string PagesAccountLedgers = "Pages.AccountLedgers";
    public const string PagesAccountLedgersCreate = "Pages.AccountLedgers.Create";
    public const string PagesAccountLedgersEdit = "Pages.AccountLedgers.Edit";
    public const string PagesAccountLedgersDelete = "Pages.AccountLedgers.Delete";

    public const string PagesFinancialYears = "Pages.FinancialYears";
    public const string PagesFinancialYearsCreate = "Pages.FinancialYears.Create";
    public const string PagesFinancialYearsEdit = "Pages.FinancialYears.Edit";
    public const string PagesFinancialYearsDelete = "Pages.FinancialYears.Delete";
    public const string PagesFinancialYearsSplit = "Pages.FinancialYears.Split";

    public const string PagesTaxes = "Pages.Taxes";
    public const string PagesTaxesCreate = "Pages.Taxes.Create";
    public const string PagesTaxesEdit = "Pages.Taxes.Edit";
    public const string PagesTaxesDelete = "Pages.Taxes.Delete";

    public const string PagesVoucherTypes = "Pages.VoucherTypes";
    public const string PagesVoucherTypesCreate = "Pages.VoucherTypes.Create";
    public const string PagesVoucherTypesEdit = "Pages.VoucherTypes.Edit";
    public const string PagesVoucherTypesDelete = "Pages.VoucherTypes.Delete";

    public const string PagesMergeLedger = "Pages.MergeLedger";


    // Inventory
    public const string PagesInventory = "Pages.Inventory";

    public const string PagesUnits = "Pages.Units";
    public const string PagesUnitsCreate = "Pages.Units.Create";
    public const string PagesUnitsEdit = "Pages.Units.Edit";
    public const string PagesUnitsDelete = "Pages.Units.Delete";

    public const string PagesProductGroups = "Pages.ProductGroups";
    public const string PagesProductGroupsCreate = "Pages.ProductGroups.Create";
    public const string PagesProductGroupsEdit = "Pages.ProductGroups.Edit";
    public const string PagesProductGroupsDelete = "Pages.ProductGroups.Delete";

    public const string PagesProducts = "Pages.Products";
    public const string PagesProductsCreate = "Pages.Products.Create";
    public const string PagesProductsEdit = "Pages.Products.Edit";
    public const string PagesProductsDelete = "Pages.Products.Delete";

    public const string PagesProductMerge = "Pages.ProductMerge";
    // Purchase
    public const string PagesPurchase = "Pages.Purchase";

    public const string PagesPurchaseOrderMasters = "Pages.PurchaseOrderMasters";
    public const string PagesPurchaseOrderMastersCreate = "Pages.PurchaseOrderMasters.Create";
    public const string PagesPurchaseOrderMastersEdit = "Pages.PurchaseOrderMasters.Edit";
    public const string PagesPurchaseOrderMastersDelete = "Pages.PurchaseOrderMasters.Delete";
    public const string PagesPurchaseOrderMastersPrint = "Pages.PurchaseOrderMasters.Print";

    public const string PagesPurchaseMasters = "Pages.PurchaseMasters";
    public const string PagesPurchaseMastersCreate = "Pages.PurchaseMasters.Create";
    public const string PagesPurchaseMastersEdit = "Pages.PurchaseMasters.Edit";
    public const string PagesPurchaseMastersDelete = "Pages.PurchaseMasters.Delete";
    public const string PagesPurchaseMastersPrint = "Pages.PurchaseMasters.Print";

    public const string PagesPurchaseReturns = "Pages.PurchaseReturns";
    public const string PagesPurchaseReturnsCreate = "Pages.PurchaseReturns.Create";
    public const string PagesPurchaseReturnsEdit = "Pages.PurchaseReturns.Edit";
    public const string PagesPurchaseReturnsDelete = "Pages.PurchaseReturns.Delete";
    public const string PagesPurchaseReturnsPrint = "Pages.PurchaseReturns.Print";

    //Sales
    public const string PagesSales = "Pages.Sales";

    public const string PagesSalesMasters = "Pages.SalesMasters";
    public const string PagesSalesMastersCreate = "Pages.SalesMasters.Create";
    public const string PagesSalesMastersEdit = "Pages.SalesMasters.Edit";
    public const string PagesSalesMastersDelete = "Pages.SalesMasters.Delete";
    public const string PagesSalesMastersPrint = "Pages.SalesMasters.Print";
    public const string PagesSalesPosCreate = "Pages.SalesMasters.PosCreate";
    public const string PagesSalesPosPrint = "Pages.SalesMasters.PosPrint";

    public const string PagesSalesReturnMasters = "Pages.SalesReturnMasters";
    public const string PagesSalesReturnMastersCreate = "Pages.SalesReturnMasters.Create";
    public const string PagesSalesReturnMastersEdit = "Pages.SalesReturnMasters.Edit";
    public const string PagesSalesReturnMastersDelete = "Pages.SalesReturnMasters.Delete";
    public const string PagesSalesReturnMastersPrint = "Pages.SalesReturnMasters.Print";

    //Restaurant
    public const string PagesRestaurant = "Pages.Restaurant";
    public const string PagesRestaurantSetup = "Pages.Restaurant.Setup";
    public const string PagesRestaurantSetupCreate = "Pages.Restaurant.Setup.Create";
    public const string PagesRestaurantSetupEdit = "Pages.Restaurant.Setup.Edit";
    public const string PagesRestaurantSetupDelete = "Pages.Restaurant.Setup.Delete";
    public const string PagesRestaurantMenu = "Pages.Restaurant.Menu";
    public const string PagesRestaurantMenuCreate = "Pages.Restaurant.Menu.Create";
    public const string PagesRestaurantMenuEdit = "Pages.Restaurant.Menu.Edit";
    public const string PagesRestaurantMenuDelete = "Pages.Restaurant.Menu.Delete";
    public const string PagesRestaurantMenuVariants = "Pages.Restaurant.Menu.Variants";
    public const string PagesRestaurantMenuModifiers = "Pages.Restaurant.Menu.Modifiers";
    public const string PagesRestaurantItemAvailability = "Pages.Restaurant.ItemAvailability";
    public const string PagesRestaurantRecipe = "Pages.Restaurant.Recipe";
    public const string PagesRestaurantPos = "Pages.Restaurant.Pos";
    public const string PagesRestaurantPosDiscount = "Pages.Restaurant.Pos.Discount";
    public const string PagesRestaurantPosVoid = "Pages.Restaurant.Pos.Void";
    public const string PagesRestaurantPosTableTransfer = "Pages.Restaurant.Pos.TableTransfer";
    public const string PagesRestaurantPosSplitMerge = "Pages.Restaurant.Pos.SplitMerge";
    public const string PagesRestaurantBilling = "Pages.Restaurant.Billing";
    public const string PagesRestaurantKotBot = "Pages.Restaurant.KotBot";
    public const string PagesRestaurantKotBotReprint = "Pages.Restaurant.KotBot.Reprint";
    public const string PagesRestaurantKds = "Pages.Restaurant.Kds";
    public const string PagesRestaurantSync = "Pages.Restaurant.Sync";
    public const string PagesRestaurantReports = "Pages.Restaurant.Reports";
    public const string PagesRestaurantInventory = "Pages.Restaurant.Inventory";
    public const string PagesRestaurantInventorySupplierMapping = "Pages.Restaurant.Inventory.SupplierMapping";
    public const string PagesRestaurantInventoryStockAdjustment = "Pages.Restaurant.Inventory.StockAdjustment";
    public const string PagesRestaurantInventoryWastage = "Pages.Restaurant.Inventory.Wastage";
    public const string PagesRestaurantInventoryReorder = "Pages.Restaurant.Inventory.Reorder";
    public const string PagesRestaurantChannels = "Pages.Restaurant.Channels";
    public const string PagesRestaurantChannelsManage = "Pages.Restaurant.Channels.Manage";
    public const string PagesRestaurantAggregators = "Pages.Restaurant.Aggregators";
    public const string PagesRestaurantPayouts = "Pages.Restaurant.Payouts";
    public const string PagesRestaurantCustomerOrdering = "Pages.Restaurant.CustomerOrdering";

    //Transaction
    public const string PagesTransaction = "Pages.Transaction";

    public const string PagesContraMasters = "Pages.ContraMasters";
    public const string PagesContraMastersCreate = "Pages.ContraMasters.Create";
    public const string PagesContraMastersEdit = "Pages.ContraMasters.Edit";
    public const string PagesContraMastersDelete = "Pages.ContraMasters.Delete";
    public const string PagesContraMastersPrint = "Pages.ContraMasters.Print";

    public const string PagesPaymentMasters = "Pages.PaymentMasters";
    public const string PagesPaymentMastersCreate = "Pages.PaymentMasters.Create";
    public const string PagesPaymentMastersEdit = "Pages.PaymentMasters.Edit";
    public const string PagesPaymentMastersDelete = "Pages.PaymentMasters.Delete";
    public const string PagesPaymentMastersPrint = "Pages.PaymentMasters.Print";

    public const string PagesReceiptMasters = "Pages.ReceiptMasters";
    public const string PagesReceiptMastersCreate = "Pages.ReceiptMasters.Create";
    public const string PagesReceiptMastersEdit = "Pages.ReceiptMasters.Edit";
    public const string PagesReceiptMastersDelete = "Pages.ReceiptMasters.Delete";
    public const string PagesReceiptMastersPrint = "Pages.ReceiptMasters.Print";

    public const string PagesJournalMasters = "Pages.JournalMasters";
    public const string PagesJournalMastersCreate = "Pages.JournalMasters.Create";
    public const string PagesJournalMastersEdit = "Pages.JournalMasters.Edit";
    public const string PagesJournalMastersDelete = "Pages.JournalMasters.Delete";
    public const string PagesJournalMastersPrint = "Pages.JournalMasters.Print";


    public const string PagesPDCPayables = "Pages.PDCPayables";
    public const string PagesPDCPayablesCreate = "Pages.PDCPayables.Create";
    public const string PagesPDCPayablesEdit = "Pages.PDCPayables.Edit";
    public const string PagesPDCPayablesDelete = "Pages.PDCPayables.Delete";
    public const string PagesPDCPayablesPrint = "Pages.PDCPayables.Print";

    public const string PagesPDCReceivables = "Pages.PDCReceivables";
    public const string PagesPDCReceivablesCreate = "Pages.PDCReceivables.Create";
    public const string PagesPDCReceivablesEdit = "Pages.PDCReceivables.Edit";
    public const string PagesPDCReceivablesDelete = "Pages.PDCReceivables.Delete";
    public const string PagesPDCReceivablesPrint = "Pages.PDCReceivables.Print";

    public const string PagesPDCClearances = "Pages.PDCClearances";
    public const string PagesPDCClearancesCreate = "Pages.PDCClearances.Create";
    public const string PagesPDCClearancesEdit = "Pages.PDCClearances.Edit";
    public const string PagesPDCClearancesDelete = "Pages.PDCClearances.Delete";
    public const string PagesPDCClearancesPrint = "Pages.PDCClearances.Print";


    public const string PagesReporting = "Pages.Reporting";

    //Accounting Report
    public const string PagesAccountingReporting = "Pages.AccountingReport";
    public const string PagesAccountGroupReport = "Pages.AccountGroupReport";
    public const string PagesAccountLedgerReport = "Pages.AccountLedgerReport";
    public const string PagesBookReport = "Pages.BookReport";

    //Inventory Report
    public const string PagesInventoryReporting = "Pages.InventoryReport";
    public const string PagesStockReport = "Pages.StockReport";
    public const string PagesProductProfitReport = "Pages.ProductProfitReport";

    //Purchase Report
    public const string PagesPurchaseReporting = "Pages.PurchaseReport";
    public const string PagesPurchaseMasterReport = "Pages.PurchaseMasterReport";
    public const string PagesPurchaseReturnReport = "Pages.PurchaseReturnReport";
    public const string PagesProductWiseMonthlyReport = "Pages.ProductWiseMonthlyReport";

    //Sales Report
    public const string PagesSalesReporting = "Pages.SalesReport";
    public const string PagesSalesMasterReport = "Pages.SalesMasterReport";
    public const string PagesSalesReturnReport = "Pages.SalesReturnReport";
    public const string PagesMaterialSalesReport = "Pages.MaterialSalesReport";
    public const string PagesLedgerWiseSalesReport = "Pages.LedgerWiseSalesReport";
    public const string PagesProductWiseSalesReport = "Pages.ProductWiseSalesReport";

    //Tax Report
    public const string PagesTaxReporting = "Pages.TaxReporting";
    public const string PagesTaxSalesRegisterReport = "Pages.TaxSalesRegisterReport";
    public const string PagesTaxPurchaseRegisterReport = "Pages.TaxPurchaseRegisterReport";
    public const string PagesTdsReport = "Pages.TdsReport";
    public const string PagesVatSummaryReport = "Pages.VatSummaryReport";
    public const string PagesGetPayTaxReport = "Pages.GetPayTaxReport";
    public const string PagesTaxableCustomerReport = "Pages.TaxableCustomerReport";
    public const string PagesSalesAboveLakhsReport = "Pages.SalesAboveLakhsReport";
    public const string PagesPurchaseAboveLakhsReport = "Pages.PurchaseAboveLakhsReport";
    public const string PagesSalesTaxReport = "Pages.SalesTaxReport";
    public const string PagesPurchaseTaxReport = "Pages.PurchaseTaxReport";

}

