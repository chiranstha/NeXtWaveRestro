using System.Collections.Generic;
using System.Linq;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Json;
using Abp.Localization;
using Abp.Net.Mail;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap.Configuration;
using Microsoft.Extensions.Configuration;
using NextWave.Erp.Authentication;
using NextWave.Erp.ControlPanel.Enums;
using NextWave.Erp.DashboardCustomization;

namespace NextWave.Erp.Configuration;

/// <summary>
/// Defines settings for the application.
/// See <see cref="AppSettings"/> for setting names.
/// </summary>
public class AppSettingProvider : SettingProvider
{
    private readonly IConfigurationRoot _appConfiguration;
    VisibleSettingClientVisibilityProvider _visibleSettingClientVisibilityProvider;

    public AppSettingProvider(IAppConfigurationAccessor configurationAccessor)
    {
        _appConfiguration = configurationAccessor.Configuration;
        _visibleSettingClientVisibilityProvider = new VisibleSettingClientVisibilityProvider();
    }

    public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
    {
        // Disable TwoFactorLogin by default (can be enabled by UI)
        context.Manager.GetSettingDefinition(AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled)
            .DefaultValue = false.ToString().ToLowerInvariant();

        // Change scope of Email settings
        ChangeEmailSettingScopes(context);

        return GetHostSettings().Union(GetTenantSettings()).Union(GetSharedSettings())
            // theme settings
            .Union(GetDefaultThemeSettings())
            .Union(GetTheme8Settings())
            .Union(GetTheme2Settings())
            .Union(GetTheme3Settings())
            .Union(GetTheme11Settings())
            .Union(GetDashboardSettings())
            .Union(GetExternalLoginProviderSettings())
            .Union(GetAdditionalLdapSettings());
    }

    private IEnumerable<SettingDefinition> GetAdditionalLdapSettings()
    {
        return new[]
        {
                new SettingDefinition(LdapSettingNames.UseSsl, "false", L("UseSsl"), scopes: SettingScopes.Application | SettingScopes.Tenant, isInherited: false, isEncrypted:false)
            };
    }

    private void ChangeEmailSettingScopes(SettingDefinitionProviderContext context)
    {
        if (!ErpConsts.AllowTenantsToChangeEmailSettings)
        {
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Host).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Port).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.UserName).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Password).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Domain).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.EnableSsl).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.UseDefaultCredentials).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.DefaultFromAddress).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.DefaultFromDisplayName).Scopes =
                SettingScopes.Application;
        }
    }

    private IEnumerable<SettingDefinition> GetHostSettings()
    {
        return new[]
        {
                new SettingDefinition(AppSettings.TenantManagement.AllowSelfRegistration,
                    GetFromAppSettings(AppSettings.TenantManagement.AllowSelfRegistration, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.IsNewRegisteredTenantActiveByDefault,
                    GetFromAppSettings(AppSettings.TenantManagement.IsNewRegisteredTenantActiveByDefault, "false")),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnRegistration,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnRegistration, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnEmailActivation,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnEmailActivation, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnResetPassword,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnResetPassword, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.DefaultEdition,
                    GetFromAppSettings(AppSettings.TenantManagement.DefaultEdition, "")),
                new SettingDefinition(AppSettings.UserManagement.SmsVerificationEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SmsVerificationEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount,
                    GetFromAppSettings(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount, "7"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.HostManagement.BillingLegalName,
                    GetFromAppSettings(AppSettings.HostManagement.BillingLegalName, "")),
                new SettingDefinition(AppSettings.HostManagement.BillingAddress,
                    GetFromAppSettings(AppSettings.HostManagement.BillingAddress, "")),
                new SettingDefinition(AppSettings.Recaptcha.SiteKey, GetFromSettings("Recaptcha:SiteKey"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UiManagement.Theme,
                    GetFromAppSettings(AppSettings.UiManagement.Theme, "default"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.All),

                new SettingDefinition(AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange,
                    GetFromAppSettings(AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.CheckingLastXPasswordCount,
                    GetFromAppSettings(AppSettings.UserManagement.Password.CheckingLastXPasswordCount, "3"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.EnablePasswordExpiration,
                    GetFromAppSettings(AppSettings.UserManagement.Password.EnablePasswordExpiration, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.PasswordExpirationDayCount,
                    GetFromAppSettings(AppSettings.UserManagement.Password.PasswordExpirationDayCount, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours,
                    GetFromAppSettings(AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours, "24"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled,
                    GetFromAppSettings(AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
            };
    }

    private IEnumerable<SettingDefinition> GetTenantSettings()
    {
        return new[]
        {
                new SettingDefinition(AppSettings.UserManagement.AllowSelfRegistration,
                    GetFromAppSettings(AppSettings.UserManagement.AllowSelfRegistration, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault,
                    GetFromAppSettings(AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault, "false"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnRegistration,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnRegistration, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnEmailActivation,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnEmailActivation, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnResetPassword,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnResetPassword, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.BillingLegalName,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingLegalName, ""),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.TenantManagement.BillingAddress,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingAddress, ""), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.TenantManagement.BillingTaxVatNo,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingTaxVatNo, ""), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.Email.UseHostDefaultEmailSettings,
                    GetFromAppSettings(AppSettings.Email.UseHostDefaultEmailSettings,
                        ErpConsts.MultiTenancyEnabled ? "true" : "false"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.RestrictedEmailDomain,
                    GetFromAppSettings(AppSettings.UserManagement.RestrictedEmailDomain, ""),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsRestrictedEmailDomainEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsRestrictedEmailDomainEnabled, "false"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.EmployeeDefaultPassword,
                    GetFromAppSettings(AppSettings.UserManagement.EmployeeDefaultPassword,
                        ErpConsts.DefaultRestaurantEmployeePassword),
                    scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.SalesRate,
                    GetFromAppSettings(AppSettings.ErpSettings.SalesRate, "0"), scopes: SettingScopes.Tenant),



                //new SettingDefinition(AppSettings.ErpSettings.IsZeroValueEntryEnabled,
                //    GetFromAppSettings(AppSettings.ErpSettings.IsZeroValueEntryEnabled, "false"),
                //    scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.CurrencySymbolShow,
                //    GetFromAppSettings(AppSettings.ErpSettings.CurrencySymbolShow, "false"),
                //    scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.TickPrintAfterSave,
                //    GetFromAppSettings(AppSettings.ErpSettings.TickPrintAfterSave, "false"),
                //    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.IsAbt,
                    GetFromAppSettings(AppSettings.ErpSettings.IsAbt, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.IsLoyaltyPoint,
                //    GetFromAppSettings(AppSettings.ErpSettings.IsLoyaltyPoint, "false"), scopes: SettingScopes.Tenant),


                new SettingDefinition(AppSettings.ErpSettings.ShowSalesAdditional,
                    GetFromAppSettings(AppSettings.ErpSettings.ShowSalesAdditional, "false"),
                    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.ErpSettings.Barcode,
                //    GetFromAppSettings(AppSettings.ErpSettings.Barcode, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.AllowBatch,
                //    GetFromAppSettings(AppSettings.ErpSettings.AllowBatch, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.AllowSize,
                //    GetFromAppSettings(AppSettings.ErpSettings.AllowSize, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.AllowGodOwn,
                //    GetFromAppSettings(AppSettings.ErpSettings.AllowGodOwn, "true"), scopes: SettingScopes.Tenant),


                //new SettingDefinition(AppSettings.ErpSettings.AllowRack,
                //    GetFromAppSettings(AppSettings.ErpSettings.AllowRack, "true"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.AllowSalesRate,
                    GetFromAppSettings(AppSettings.ErpSettings.AllowSalesRate, "true"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.Mrp,
                    GetFromAppSettings(AppSettings.ErpSettings.Mrp, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.PurchaseRate,
                    GetFromAppSettings(AppSettings.ErpSettings.PurchaseRate, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.Unit,
                    GetFromAppSettings(AppSettings.ErpSettings.Unit, "true"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.TDS,
                    GetFromAppSettings(AppSettings.ErpSettings.TDS, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.NoOfPrint,
                    GetFromAppSettings(AppSettings.ErpSettings.NoOfPrint, "1"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.IsIRDSoftware,
                    GetFromAppSettings(AppSettings.ErpSettings.IsIRDSoftware, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.DuplicateLedgerName,
                    GetFromAppSettings(AppSettings.ErpSettings.DuplicateLedgerName, "false"),
                    scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.DuplicatePAN,
                    GetFromAppSettings(AppSettings.ErpSettings.DuplicatePAN, "false"),
                    scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.IsCBMS,
                    GetFromAppSettings(AppSettings.ErpSettings.IsCBMS, "false"), scopes: SettingScopes.Tenant,
                    clientVisibilityProvider: new VisibleSettingClientVisibilityProvider()),

                new SettingDefinition(AppSettings.ErpSettings.CBMSUsername,
                    GetFromAppSettings(AppSettings.ErpSettings.CBMSUsername, ""), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.CBMSPassword,
                    GetFromAppSettings(AppSettings.ErpSettings.CBMSPassword, ""), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.IsIMEI,
                //    GetFromAppSettings(AppSettings.ErpSettings.IsIMEI, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.SalesRate,
                    GetFromAppSettings(AppSettings.ErpSettings.SalesRate, "0"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.IsTaxEnabled,
                    GetFromAppSettings(AppSettings.ErpSettings.IsTaxEnabled, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.IsEmailSent,
                    GetFromAppSettings(AppSettings.ErpSettings.IsEmailSent, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.Transaction,
                    GetFromAppSettings(AppSettings.ErpSettings.Transaction, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.StockCalculation,
                    GetFromAppSettings(AppSettings.ErpSettings.StockCalculation, "Average"),
                    scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.AllowModelNo,
                //    GetFromAppSettings(AppSettings.ErpSettings.AllowModelNo, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.DiscountAmount,
                    GetFromAppSettings(AppSettings.ErpSettings.DiscountAmount, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.ProductCode,
                    GetFromAppSettings(AppSettings.ErpSettings.ProductCode, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.BrandCode,
                //    GetFromAppSettings(AppSettings.ErpSettings.BrandCode, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.NegativeCashTranscation,
                    GetFromAppSettings(AppSettings.ErpSettings.NegativeCashTranscation, "Block"),
                    scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.StockCalcultaion,
                    GetFromAppSettings(AppSettings.ErpSettings.StockCalcultaion, "Average"),
                    scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.NegativeStockStatus,
                    GetFromAppSettings(AppSettings.ErpSettings.NegativeStockStatus, "Warn"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantVatPercent,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantVatPercent, "13"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantServiceChargePercent,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantServiceChargePercent, "0"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantTipLedgerId, "", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantCardLedgerId, "", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantQrLedgerId, "", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, "", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantMixedTenderEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantRefundsEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantAndroidDraftRecoveryEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions, "false"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantManagerPin,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantManagerPin, ""),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantTicketPrintingEnabled,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantTicketPrintingEnabled, "true"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled, "true"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantTableWorkflow,
                    GetFromAppSettings(AppSettings.ErpSettings.RestaurantTableWorkflow, "TableSession"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantQrOrderingEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantReservationBookingEnabled, "false", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantDefaultReservationDurationMinutes, "90", scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, "", scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.ErpSettings.SalesBillFormat,
                    GetFromAppSettings(AppSettings.ErpSettings.SalesBillFormat, "Vat-2"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.Payroll,
                //    GetFromAppSettings(AppSettings.ErpSettings.Payroll, "false"), scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.ErpSettings.Payroll,
                //    GetFromAppSettings(AppSettings.ErpSettings.Payroll, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.Services,
                //    GetFromAppSettings(AppSettings.ErpSettings.Services, "false"), scopes: SettingScopes.Tenant),

                //new SettingDefinition(AppSettings.ErpSettings.Loan,
                //    GetFromAppSettings(AppSettings.ErpSettings.Loan, "false"), scopes: SettingScopes.Tenant),


                //new SettingDefinition(AppSettings.ErpSettings.Crm,
                //    GetFromAppSettings(AppSettings.ErpSettings.Crm, "false"), scopes: SettingScopes.Tenant),

                new SettingDefinition(AppSettings.PrintSettings.PrintType,
                    GetFromAppSettings(AppSettings.PrintSettings.PrintType, PrintType.Regular.ToString()),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.ServiceReceiptFormat,
                    GetFromAppSettings(AppSettings.PrintSettings.ServiceReceiptFormat, "Format-1"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.SalesNoOfPrint,
                    GetFromAppSettings(AppSettings.PrintSettings.SalesNoOfPrint, "1"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.Layout,
                    GetFromAppSettings(AppSettings.PrintSettings.Layout, LayoutType.LayoutOne.ToString()),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.Color,
                    GetFromAppSettings(AppSettings.PrintSettings.Color, "#000000"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.FontFamily,
                    GetFromAppSettings(AppSettings.PrintSettings.FontFamily, FontType.Helvetica.ToString()),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.FontSize,
                    GetFromAppSettings(AppSettings.PrintSettings.FontSize, "12"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.InvoiceText,
                    GetFromAppSettings(AppSettings.PrintSettings.InvoiceText, "Invoice"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.InvoiceTextSize,
                    GetFromAppSettings(AppSettings.PrintSettings.InvoiceTextSize, "16"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.PaperSize,
                    GetFromAppSettings(AppSettings.PrintSettings.PaperSize, "A4"), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.PrintSettings.Custom,
                    GetFromAppSettings(AppSettings.PrintSettings.Custom, "false"), scopes: SettingScopes.Tenant),


                new SettingDefinition(AppSettings.ErpSettings.DiscountPercent,
                    GetFromAppSettings(AppSettings.ErpSettings.DiscountPercent, "false"), scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.WhatAppSettings.WhatsAppUrl,
                //    GetFromAppSettings(AppSettings.WhatAppSettings.WhatsAppUrl, "https://api.chat-api.com/instance"),
                //    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.WhatAppSettings.IsEnableWhatsapp,
                //    GetFromAppSettings(AppSettings.WhatAppSettings.IsEnableWhatsapp, "false"),
                //    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.WhatAppSettings.WhatsAppInstanceId,
                //    GetFromAppSettings(AppSettings.WhatAppSettings.WhatsAppInstanceId, "369344"),
                //    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.WhatAppSettings.WhatsAppToken,
                //    GetFromAppSettings(AppSettings.WhatAppSettings.WhatsAppToken, "xy3chp3mt6vnc9uy"),
                //    scopes: SettingScopes.Tenant),


                //new SettingDefinition(AppSettings.SparrowSmsSettings.IsEnableSparrowSms,
                //    GetFromAppSettings(AppSettings.SparrowSmsSettings.IsEnableSparrowSms, "false"),
                //    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.SparrowSmsSettings.SparrowSmsUrl,
                //    GetFromAppSettings(AppSettings.SparrowSmsSettings.SparrowSmsUrl,
                //        "http://api.sparrowsms.com/v2/sms/"), scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.SparrowSmsSettings.SparrowSmsFrom,
                //    GetFromAppSettings(AppSettings.SparrowSmsSettings.SparrowSmsFrom, "NextwaveErp"),
                //    scopes: SettingScopes.Tenant),
                //new SettingDefinition(AppSettings.SparrowSmsSettings.SparrowSmsToken,
                //    GetFromAppSettings(AppSettings.SparrowSmsSettings.SparrowSmsToken, "ZzEsMrFwcmiyqBhSGwqL"),
                //    scopes: SettingScopes.Tenant)
            };
    }

    private IEnumerable<SettingDefinition> GetSharedSettings()
    {
        return new[]
        {
                new SettingDefinition(AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsCookieConsentEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsCookieConsentEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsQrLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsQrLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsQuickThemeSelectEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsQuickThemeSelectEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnLogin,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnLogin, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.IsEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.IsEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.TimeOutSecond,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.TimeOutSecond, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser,
                    GetFromAppSettings(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.AllowUsingGravatarProfilePicture,
                    GetFromAppSettings(AppSettings.UserManagement.AllowUsingGravatarProfilePicture, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.UseGravatarProfilePicture,
                    GetFromAppSettings(AppSettings.UserManagement.UseGravatarProfilePicture, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.User),
                new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureSizeInMB,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureSizeInMB, "5.0"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                 new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureWidth,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureWidth, "250"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                  new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureHeight,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureHeight, "250"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
            };
    }

    private string GetFromAppSettings(string name, string defaultValue = null)
    {
        return GetFromSettings("App:" + name, defaultValue);
    }

    private string GetFromSettings(string name, string defaultValue = null)
    {
        return _appConfiguration[name] ?? defaultValue;
    }

    private IEnumerable<SettingDefinition> GetDefaultThemeSettings()
    {
        var themeName = "default";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.Skin,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.Skin, "light"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Style,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Style, "solid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.AsideSkin,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.AsideSkin, "light"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.AllowAsideMinimizing,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.AllowAsideMinimizing,
                        "true"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.DefaultMinimizedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.DefaultMinimizedAside,
                        "false"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.SubmenuToggle,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.SubmenuToggle, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.HoverableAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.HoverableAside,
                        "true"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),


                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Toolbar.DesktopFixedToolbar,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Toolbar.DesktopFixedToolbar, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Toolbar.MobileFixedToolbar,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Toolbar.MobileFixedToolbar, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme8Settings()
    {
        var themeName = "theme8";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme11Settings()
    {
        var themeName = "theme11";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid-xxl"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetDashboardSettings()
    {
        var mvcDefaultHostView = GetDefaultMvcHostDashboardView();
        var mvcDefaultTenantView = GetDefaultMvcTenantDashboardView();

        var angularDefaultHostView = GetDefaultAngularHostDashboardView();
        var angularDefaultTenantView = GetDefaultAngularTenantDashboardView();

        string GetSettingName(string application, string dashboardName)
        {
            return AppSettings.DashboardCustomization.Configuration + "." + application + "." + dashboardName;
        }

        return new[]
        {
                new SettingDefinition(
                    GetSettingName(
                        ErpDashboardCustomizationConsts.Applications.Mvc,
                        mvcDefaultHostView.DashboardName
                    ),
                    mvcDefaultHostView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        ErpDashboardCustomizationConsts.Applications.Mvc,
                        mvcDefaultTenantView.DashboardName
                    ),
                    mvcDefaultTenantView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        ErpDashboardCustomizationConsts.Applications.Angular,
                        angularDefaultHostView.DashboardName
                    ),
                    angularDefaultHostView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        ErpDashboardCustomizationConsts.Applications.Angular,
                        angularDefaultTenantView.DashboardName
                    ),
                    angularDefaultTenantView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                )
            };
    }

    public Dashboard GetDefaultMvcHostDashboardView()
    {
        //It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = ErpDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{ErpDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard}_{ErpDashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = ErpDashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .TopStats, // Top Stats
                                Height = 6,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId =
                                    ErpDashboardCustomizationConsts.Widgets.Host
                                        .IncomeStatistics, // Income Statistics
                                Height = 11,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 6
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .RecentTenants, // Recent tenants
                                Height = 10,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 17
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .SubscriptionExpiringTenants, // Subscription expiring tenants
                                Height = 10,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 17
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .EditionStatistics, // Edition statistics
                                Height = 11,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 6
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultMvcTenantDashboardView()
    {
        // It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = ErpDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{ErpDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard}_{ErpDashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = ErpDashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .GeneralStats, // General Stats
                                Height = 9,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 19
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .ProfitShare, // Profit Share
                                Height = 13,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 28
                            },
                            new Widget
                            {
                                WidgetId =
                                    ErpDashboardCustomizationConsts.Widgets.Tenant
                                        .MemberActivity, // Memeber Activity
                                Height = 13,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 28
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .RegionalStats, // Regional Stats
                                Height = 14,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 5
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .DailySales, // Daily Sales
                                Height = 9,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 19
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .TopStats, // Top Stats
                                Height = 5,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Tenant
                                    .SalesSummary, // Sales Summary
                                Height = 14,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 5
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultAngularHostDashboardView()
    {
        // It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = ErpDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{ErpDashboardCustomizationConsts.DashboardNames.DefaultHostDashboard}_{ErpDashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = ErpDashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .TopStats, // Top Stats
                                Height = 5,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId =
                                    ErpDashboardCustomizationConsts.Widgets.Host
                                        .IncomeStatistics, // Income Statistics
                                Height = 8,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 5
                            },
                            new Widget
                            {
                                WidgetId =
                                    ErpDashboardCustomizationConsts.Widgets.Host
                                        .RecentTenants, // Recent tenants
                                Height = 9,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .SubscriptionExpiringTenants, // Subscription expiring tenants
                                Height = 9,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = ErpDashboardCustomizationConsts.Widgets.Host
                                    .EditionStatistics, // Edition statistics
                                Height = 8,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 5
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultAngularTenantDashboardView()
    {
        //It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = ErpDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{ErpDashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard}_{ErpDashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = ErpDashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>()
                    }
                }
        };
    }

    private IEnumerable<SettingDefinition> GetExternalLoginProviderSettings()
    {
        return GetFacebookExternalLoginProviderSettings()
            .Union(GetGoogleExternalLoginProviderSettings())
            .Union(GetTwitterExternalLoginProviderSettings())
            .Union(GetMicrosoftExternalLoginProviderSettings())
            .Union(GetOpenIdConnectExternalLoginProviderSettings())
            .Union(GetWsFederationExternalLoginProviderSettings());
    }

    private SettingDefinition[] GetFacebookExternalLoginProviderSettings()
    {
        string appId = GetFromSettings("Authentication:Facebook:AppId");
        string appSecret = GetFromSettings("Authentication:Facebook:AppSecret");

        var facebookExternalLoginProviderInfo = new FacebookExternalLoginProviderSettings()
        {
            AppId = appId,
            AppSecret = appSecret
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Facebook,
                    facebookExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Facebook_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Facebook,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                )
            };
    }

    private SettingDefinition[] GetGoogleExternalLoginProviderSettings()
    {
        string clientId = GetFromSettings("Authentication:Google:ClientId");
        string clientSecret = GetFromSettings("Authentication:Google:ClientSecret");
        string userInfoEndPoint = GetFromSettings("Authentication:Google:UserInfoEndpoint");

        var googleExternalLoginProviderInfo = new GoogleExternalLoginProviderSettings()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            UserInfoEndpoint = userInfoEndPoint
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Google,
                    googleExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Google_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Google,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetTwitterExternalLoginProviderSettings()
    {
        string consumerKey = GetFromSettings("Authentication:Twitter:ConsumerKey");
        string consumerSecret = GetFromSettings("Authentication:Twitter:ConsumerSecret");

        var twitterExternalLoginProviderInfo = new TwitterExternalLoginProviderSettings
        {
            ConsumerKey = consumerKey,
            ConsumerSecret = consumerSecret
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Twitter,
                    twitterExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Twitter_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Twitter,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetMicrosoftExternalLoginProviderSettings()
    {
        string consumerKey = GetFromSettings("Authentication:Microsoft:ConsumerKey");
        string consumerSecret = GetFromSettings("Authentication:Microsoft:ConsumerSecret");

        var microsoftExternalLoginProviderInfo = new MicrosoftExternalLoginProviderSettings()
        {
            ClientId = consumerKey,
            ClientSecret = consumerSecret
        };


        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Microsoft,
                    microsoftExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Microsoft_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Microsoft,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetOpenIdConnectExternalLoginProviderSettings()
    {
        var clientId = GetFromSettings("Authentication:OpenId:ClientId");
        var clientSecret = GetFromSettings("Authentication:OpenId:ClientSecret");
        var authority = GetFromSettings("Authentication:OpenId:Authority");
        var loginUrl = GetFromSettings("Authentication:OpenId:LoginUrl");
        var validateIssuerStr = GetFromSettings("Authentication:OpenId:ValidateIssuer");
        var responseType = GetFromSettings("Authentication:OpenId:ResponseType");

        bool.TryParse(validateIssuerStr, out bool validateIssuer);

        var openIdConnectExternalLoginProviderInfo = new OpenIdConnectExternalLoginProviderSettings
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Authority = authority,
            ValidateIssuer = validateIssuer,
            ResponseType = responseType
        };

        if (!loginUrl.IsNullOrEmpty())
        {
            openIdConnectExternalLoginProviderInfo.LoginUrl = loginUrl;
        }

        var jsonClaimMappings = new List<JsonClaimMapDto>();
        _appConfiguration.GetSection("Authentication:OpenId:ClaimsMapping").Bind(jsonClaimMappings);

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.OpenIdConnect,
                    openIdConnectExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.OpenIdConnectMappedClaims,
                    jsonClaimMappings.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application | SettingScopes.Tenant
                )
            };
    }

    private SettingDefinition[] GetWsFederationExternalLoginProviderSettings()
    {
        var clientId = GetFromSettings("Authentication:WsFederation:ClientId");
        var wtrealm = GetFromSettings("Authentication:WsFederation:Wtrealm");
        var authority = GetFromSettings("Authentication:WsFederation:Authority");
        var tenant = GetFromSettings("Authentication:WsFederation:Tenant");
        var metaDataAddress = GetFromSettings("Authentication:WsFederation:MetaDataAddress");

        var wsFederationExternalLoginProviderInfo = new WsFederationExternalLoginProviderSettings()
        {
            ClientId = clientId,
            Tenant = tenant,
            Authority = authority,
            Wtrealm = wtrealm,
            MetaDataAddress = metaDataAddress
        };

        var jsonClaimMappings = new List<JsonClaimMapDto>();
        _appConfiguration.GetSection("Authentication:WsFederation:ClaimsMapping").Bind(jsonClaimMappings);

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.WsFederation,
                    wsFederationExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.WsFederation,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.WsFederation_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.WsFederationMappedClaims,
                    jsonClaimMappings.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application | SettingScopes.Tenant
                )
            };
    }

    protected virtual ILocalizableString L(string name)
    {
        return new LocalizableString(name, ErpConsts.LocalizationSourceName);
    }
    private IEnumerable<SettingDefinition> GetTheme2Settings()
    {
        var themeName = "theme2";

        return
        [
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "true"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
        ];
    }


    private IEnumerable<SettingDefinition> GetTheme3Settings()
    {
        var themeName = "theme3";

        return
        [
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

            new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fluid"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
        ];
    }


}

