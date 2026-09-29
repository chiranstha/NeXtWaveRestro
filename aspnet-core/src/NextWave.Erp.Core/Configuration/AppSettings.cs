namespace NextWave.Erp.Configuration;

/// <summary>
/// Defines string constants for setting names in the application.
/// See <see cref="AppSettingProvider"/> for setting definitions.
/// </summary>
public static class AppSettings
{
    public static class HostManagement
    {
        public const string BillingLegalName = "App.HostManagement.BillingLegalName";
        public const string BillingAddress = "App.HostManagement.BillingAddress";
    }

    public static class DashboardCustomization
    {
        public const string Configuration = "App.DashboardCustomization.Configuration";
    }

    public static class UiManagement
    {
        public const string LayoutType = "App.UiManagement.LayoutType";
        public const string DarkMode = "App.UiManagement.DarkMode";
        public const string FixedBody = "App.UiManagement.Layout.FixedBody";
        public const string MobileFixedBody = "App.UiManagement.Layout.MobileFixedBody";

        public const string Theme = "App.UiManagement.Theme";

        public const string SearchActive = "App.UiManagement.MenuSearch";

        public static class Header
        {
            public const string DesktopFixedHeader = "App.UiManagement.Header.DesktopFixedHeader";
            public const string MobileFixedHeader = "App.UiManagement.Header.MobileFixedHeader";
            public const string Skin = "App.UiManagement.Header.Skin";
            public const string MinimizeType = "App.UiManagement.Header.MinimizeType";
            public const string MenuArrows = "App.UiManagement.Header.MenuArrows";
        }

        public static class SubHeader
        {
            public const string Fixed = "App.UiManagement.SubHeader.Fixed";
            public const string Style = "App.UiManagement.SubHeader.Style";
        }

        public static class LeftAside
        {
            public const string Position = "App.UiManagement.Left.Position";
            public const string AsideSkin = "App.UiManagement.Left.AsideSkin";
            public const string FixedAside = "App.UiManagement.Left.FixedAside";
            public const string AllowAsideMinimizing = "App.UiManagement.Left.AllowAsideMinimizing";
            public const string DefaultMinimizedAside = "App.UiManagement.Left.DefaultMinimizedAside";
            public const string HoverableAside = "App.UiManagement.Left.HoverableAside";
            public const string SubmenuToggle = "App.UiManagement.Left.SubmenuToggle";
        }

        public static class Footer
        {
            public const string DesktopFixedFooter = "App.UiManagement.Footer.DesktopFixedFooter";
            public const string MobileFixedFooter = "App.UiManagement.Footer.MobileFixedFooter";
            public const string FooterWidthType = "App.UiManagement.Footer.FooterWidthType";
        }

        public static class Toolbar
        {
            public const string DesktopFixedToolbar = "App.UiManagement.Toolbar.DesktopFixedToolbar";
            public const string MobileFixedToolbar = "App.UiManagement.Toolbar.MobileFixedToolbar";
        }
    }

    public static class TenantManagement
    {
        public const string AllowSelfRegistration = "App.TenantManagement.AllowSelfRegistration";
        public const string IsNewRegisteredTenantActiveByDefault = "App.TenantManagement.IsNewRegisteredTenantActiveByDefault";
        public const string UseCaptchaOnRegistration = "App.TenantManagement.UseCaptchaOnRegistration";
        public const string UseCaptchaOnEmailActivation = "App.TenantManagement.UseCaptchaOnEmailActivation";
        public const string UseCaptchaOnResetPassword = "App.TenantManagement.UseCaptchaOnResetPassword";
        public const string DefaultEdition = "App.TenantManagement.DefaultEdition";
        public const string SubscriptionExpireNotifyDayCount = "App.TenantManagement.SubscriptionExpireNotifyDayCount";
        public const string BillingLegalName = "App.TenantManagement.BillingLegalName";
        public const string BillingAddress = "App.TenantManagement.BillingAddress";
        public const string BillingTaxVatNo = "App.TenantManagement.BillingTaxVatNo";
        public const string IsRestrictedEmailDomainEnabled = "App.TenantManagement.IsRestrictedEmailDomainEnabled";
    }

    public static class UserManagement
    {
        public static class TwoFactorLogin
        {
            public const string IsGoogleAuthenticatorEnabled = "App.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled";
        }

        public static class PasswordlessLogin
        {
            public const string IsEmailPasswordlessLoginEnabled = "App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled";
            public const string IsSmsPasswordlessLoginEnabled = "App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled";
        }

        public static class SessionTimeOut
        {
            public const string IsEnabled = "App.UserManagement.SessionTimeOut.IsEnabled";
            public const string TimeOutSecond = "App.UserManagement.SessionTimeOut.TimeOutSecond";
            public const string ShowTimeOutNotificationSecond = "App.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond";
            public const string ShowLockScreenWhenTimedOut = "App.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut";
        }

        public const string IsQrLoginEnabled = "App.UserManagement.IsQrLoginEnabled";
        public const string AllowSelfRegistration = "App.UserManagement.AllowSelfRegistration";
        public const string IsNewRegisteredUserActiveByDefault = "App.UserManagement.IsNewRegisteredUserActiveByDefault";
        public const string UseCaptchaOnRegistration = "App.UserManagement.UseCaptchaOnRegistration";
        public const string UseCaptchaOnLogin = "App.UserManagement.UseCaptchaOnLogin";
        public const string UseCaptchaOnEmailActivation = "App.UserManagement.UseCaptchaOnEmailActivation";
        public const string UseCaptchaOnResetPassword = "App.UserManagement.UseCaptchaOnResetPassword";
        public const string SmsVerificationEnabled = "App.UserManagement.SmsVerificationEnabled";
        public const string IsCookieConsentEnabled = "App.UserManagement.IsCookieConsentEnabled";
        public const string IsQuickThemeSelectEnabled = "App.UserManagement.IsQuickThemeSelectEnabled";
        public const string AllowOneConcurrentLoginPerUser = "App.UserManagement.AllowOneConcurrentLoginPerUser";
        public const string AllowUsingGravatarProfilePicture = "App.UserManagement.AllowUsingGravatarProfilePicture";
        public const string UseGravatarProfilePicture = "App.UserManagement.UseGravatarProfilePicture";
        public const string RestrictedEmailDomain = "App.UserManagement.RestrictedEmailDomain";
        public const string IsRestrictedEmailDomainEnabled = "App.UserManagement.IsRestrictedEmailDomainEnabled";
        public const string MaxProfilePictureSizeInMB = "App.UserManagement.MaxProfilePictureSizeInMB";
        public const string MaxProfilePictureWidth = "App.UserManagement.MaxProfilePictureWidth";
        public const string MaxProfilePictureHeight = "App.UserManagement.MaxProfilePictureHeight";
        public const string EmployeeDefaultPassword = "App.UserManagement.EmployeeDefaultPassword";

        public static class Password
        {
            public const string EnableCheckingLastXPasswordWhenPasswordChange = "App.UserManagement.EnableCheckingLastXPasswordWhenPasswordChange";
            public const string CheckingLastXPasswordCount = "App.UserManagement.CheckingLastXPasswordCount";
            public const string EnablePasswordExpiration = "App.UserManagement.EnablePasswordExpiration";
            public const string PasswordExpirationDayCount = "App.UserManagement.PasswordExpirationDayCount";
            public const string PasswordResetCodeExpirationHours = "App.UserManagement.PasswordResetCodeExpirationHours";
        }
    }

    public static class Email
    {
        public const string UseHostDefaultEmailSettings = "App.Email.UseHostDefaultEmailSettings";
    }

    public static class Recaptcha
    {
        public const string SiteKey = "Recaptcha.SiteKey";
    }

    public static class ExternalLoginProvider
    {
        public const string OpenIdConnectMappedClaims = "ExternalLoginProvider.OpenIdConnect.MappedClaims";
        public const string WsFederationMappedClaims = "ExternalLoginProvider.WsFederation.MappedClaims";

        public static class Host
        {
            public const string Facebook = "ExternalLoginProvider.Facebook";
            public const string Google = "ExternalLoginProvider.Google";
            public const string Twitter = "ExternalLoginProvider.Twitter";
            public const string Microsoft = "ExternalLoginProvider.Microsoft";
            public const string OpenIdConnect = "ExternalLoginProvider.OpenIdConnect";
            public const string WsFederation = "ExternalLoginProvider.WsFederation";
        }

        public static class Tenant
        {
            public const string Facebook = "ExternalLoginProvider.Facebook.Tenant";
            public const string Facebook_IsDeactivated = "ExternalLoginProvider.Facebook.IsDeactivated";
            public const string Google = "ExternalLoginProvider.Google.Tenant";
            public const string Google_IsDeactivated = "ExternalLoginProvider.Google.IsDeactivated";
            public const string Twitter = "ExternalLoginProvider.Twitter.Tenant";
            public const string Twitter_IsDeactivated = "ExternalLoginProvider.Twitter.IsDeactivated";
            public const string Microsoft = "ExternalLoginProvider.Microsoft.Tenant";
            public const string Microsoft_IsDeactivated = "ExternalLoginProvider.Microsoft.IsDeactivated";
            public const string OpenIdConnect = "ExternalLoginProvider.OpenIdConnect.Tenant";
            public const string OpenIdConnect_IsDeactivated = "ExternalLoginProvider.OpenIdConnect.IsDeactivated";
            public const string WsFederation = "ExternalLoginProvider.WsFederation.Tenant";
            public const string WsFederation_IsDeactivated = "ExternalLoginProvider.WsFederation.IsDeactivated";
        }
    }

    public static class ErpSettings
    {
        public const string IsTaxEnabled = "App.Nextwave.IsTaxEnabled";
        public const string AllowSalesRate = "App.Nextwave.AllowSalesRate";
        public const string Mrp = "App.Nextwave.Mrp";
        public const string NoOfPrint = "App.Nextwave.NoOfPrint";
        public const string IsIRDSoftware = "App.Nextwave.IsIRDSoftware";
     //   public const string IsIMEI = "App.Nextwave.IsIMEI";
        public const string DuplicateLedgerName = "App.Nextwave.DuplicateLedgerName";
        public const string DuplicatePAN = "App.Nextwave.DuplicatePAN";
        public const string IsCBMS = "App.Nextwave.IsCBMS";
        public const string CBMSUsername = "App.Nextwave.CBMSUsername";
        public const string CBMSPassword = "App.Nextwave.CBMSPassword";
        public const string SalesRate = "App.Nextwave.SalesRate";
        public const string PurchaseRate = "App.Nextwave.PurchaseRate";
        public const string Unit = "App.Nextwave.Unit";
        public const string DiscountAmount = "App.Nextwave.DiscountAmount";
        public const string ProductCode = "App.Nextwave.ProductCode";
        public const string ShowSalesAdditional = "App.Nextwave.ShowSalesAdditional";
        public const string IsAbt = "App.Nextwave.IsAbt";
        public const string DiscountPercent = "App.Nextwave.DiscountPercent";
        public static string StockCalcultaion = "App.Nextwave.StockCalcultaion";
        public static string NegativeStockStatus = "App.Nextwave.NegativeStockStatus";
        public const string RestaurantVatPercent = "App.Nextwave.Restaurant.VatPercent";
        public const string RestaurantServiceChargePercent = "App.Nextwave.Restaurant.ServiceChargePercent";
        public const string RestaurantTipLedgerId = "App.Nextwave.Restaurant.TipLedgerId";
        public const string RestaurantCardLedgerId = "App.Nextwave.Restaurant.CardLedgerId";
        public const string RestaurantQrLedgerId = "App.Nextwave.Restaurant.QrLedgerId";
        public const string RestaurantRequireManagerPinForSensitiveActions = "App.Nextwave.Restaurant.RequireManagerPinForSensitiveActions";
        public const string RestaurantManagerPin = "App.Nextwave.Restaurant.ManagerPin";
        public const string RestaurantTicketPrintingEnabled = "App.Nextwave.Restaurant.TicketPrintingEnabled";
        public const string RestaurantChannelAvailabilityEnabled = "App.Nextwave.Restaurant.ChannelAvailabilityEnabled";
        public const string RestaurantTableWorkflow = "App.Nextwave.Restaurant.TableWorkflow";
        public const string RestaurantQrOrderingEnabled = "App.Nextwave.Restaurant.QrOrderingEnabled";
        public const string RestaurantReservationBookingEnabled = "App.Nextwave.Restaurant.ReservationBookingEnabled";
        public const string RestaurantDefaultReservationDurationMinutes = "App.Nextwave.Restaurant.DefaultReservationDurationMinutes";
        public const string RestaurantReceiptPrintRouteName = "App.Nextwave.Restaurant.ReceiptPrintRouteName";
        public static string SalesBillFormat = "App.Nextwave.SalesBillFormat";
        public const string IsEmailSent = "App.Nextwave.IsEmailSent";
        public const string Transaction = "App.Nextwave.Transaction";
        public const string StockCalculation = "App.Nextwave.StockCalculation";
        public const string TDS = "App.Nextwave.TDS";
        public static string NegativeCashTranscation = "App.Nextwave.NegativeCashTranscation";

    }

    public static class PrintSettings
    {
        public const string PrintType = "App.PrintSettings.PrintType";
        public const string Layout = "App.PrintSettings.Layout";
        public const string Color = "App.PrintSettings.Color";
        public const string FontFamily = "App.PrintSettings.FontFamily";
        public const string FontSize = "App.PrintSettings.FontSize";
        public const string InvoiceText = "App.PrintSettings.InvoiceText";
        public const string InvoiceTextSize = "App.PrintSettings.InvoiceTextSize";
        public const string PaperSize = "App.PrintSettings.PaperSize";
        public const string Custom = "App.PrintSettings.Custom";
        public const string ServiceReceiptFormat = "App.PrintSettings.ServiceReceiptFormat";
        public const string SalesNoOfPrint = "App.PrintSettings.SalesNoOfPrint";
    }
}

