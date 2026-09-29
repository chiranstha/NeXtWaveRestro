using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Accounting;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantSetup)]
    public class RestaurantReleaseAppService(
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantStation, Guid> stationRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<Role, int> roleRepository,
        IRepository<RolePermissionSetting, long> rolePermissionRepository,
        IRepository<RestaurantSetupAcknowledgement, Guid> acknowledgementRepository)
        : ErpAppServiceBase, IRestaurantReleaseAppService
    {
        [AbpAuthorize(AppPermissions.PagesRestaurantBilling)]
        public async Task<RestaurantReleaseCapabilitiesDto> GetCapabilities()
        {
            var tenantId = AbpSession.GetTenantId();
            var cash = await accountLedgerRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Cash");
            var cardId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantCardLedgerId, tenantId));
            var qrId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantQrLedgerId, tenantId));
            var clearingId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, tenantId));
            var card = await IsTenantLedger(cardId, tenantId) ? cardId : null;
            var qr = await IsTenantLedger(qrId, tenantId) ? qrId : null;
            var clearing = await IsTenantLedger(clearingId, tenantId) ? clearingId : null;
            var cashEnabled = cash != null;

            return new RestaurantReleaseCapabilitiesDto
            {
                MixedTenderEnabled = await IsEnabled(AppSettings.ErpSettings.RestaurantMixedTenderEnabled, tenantId),
                RefundsEnabled = await IsEnabled(AppSettings.ErpSettings.RestaurantRefundsEnabled, tenantId),
                OrderVersionChecksEnabled = await IsEnabled(AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, tenantId),
                AndroidDraftRecoveryEnabled = await IsEnabled(AppSettings.ErpSettings.RestaurantAndroidDraftRecoveryEnabled, tenantId),
                RefundPayableLedgerId = clearing,
                CardLedgerId = card,
                QrLedgerId = qr,
                PaymentMethods = new List<RestaurantEnabledPaymentMethodDto>
                {
                    new() { Method = PaymentMethod.Cash, Enabled = cashEnabled, LedgerId = cash?.Id },
                    new() { Method = PaymentMethod.Card_Swipe, Enabled = card.HasValue, LedgerId = card },
                    new() { Method = PaymentMethod.QR, Enabled = qr.HasValue, LedgerId = qr }
                }
            };
        }

        public async Task<RestaurantSetupReadinessDto> GetSetupReadiness()
        {
            var tenantId = AbpSession.GetTenantId();
            var acknowledgements = await acknowledgementRepository.GetAll()
                .Where(x => x.TenantId == tenantId)
                .ToDictionaryAsync(x => x.CheckKey);

            var activeMenu = await menuItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive)
                .Select(x => new { x.ProductId, x.DisplayName })
                .ToListAsync();
            var productIds = activeMenu.Select(x => x.ProductId).Distinct().ToList();
            var products = await productRepository.GetAll()
                .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
                .Select(x => new { x.Id, x.ProductType })
                .ToListAsync();
            var trackedProductIds = products.Where(x => x.ProductType != ProductTypeEnum.Services).Select(x => x.Id).ToList();
            var recipeIds = await bomRepository.GetAll()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive && trackedProductIds.Contains(x.ProductId))
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();
            var validProducts = products.Select(x => x.Id).ToHashSet();
            var missingProduct = activeMenu.Count(x => !validProducts.Contains(x.ProductId));
            var missingRecipe = trackedProductIds.Count(x => !recipeIds.Contains(x));

            var tables = await tableRepository.CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive);
            var stations = await stationRepository.GetAll()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive)
                .Select(x => x.PrintRouteName)
                .ToListAsync();
            var receiptRoute = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, tenantId);

            var cash = await accountLedgerRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Cash");
            var cardId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantCardLedgerId, tenantId));
            var qrId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantQrLedgerId, tenantId));
            var clearingId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, tenantId));
            var tipId = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantTipLedgerId, tenantId));
            var cardReady = await IsTenantLedger(cardId, tenantId);
            var qrReady = await IsTenantLedger(qrId, tenantId);
            var clearingReady = await IsTenantLedger(clearingId, tenantId);
            var tipReady = await IsTenantLedger(tipId, tenantId);
            var managerPin = await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantManagerPin, tenantId);

            var roles = await roleRepository.GetAll().Where(x => x.TenantId == tenantId)
                .Select(x => new { x.Id, x.Name }).ToListAsync();
            var managerRole = roles.FirstOrDefault(x => x.Name == StaticRoleNames.Tenants.RestaurantManager);
            var cashierRole = roles.FirstOrDefault(x => x.Name == StaticRoleNames.Tenants.RestaurantCashier);
            var roleIds = new[] { managerRole?.Id, cashierRole?.Id }.Where(x => x.HasValue).Select(x => x.Value).ToList();
            var grants = await rolePermissionRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.IsGranted && roleIds.Contains(x.RoleId))
                .Select(x => new { x.RoleId, x.Name })
                .ToListAsync();
            var staffReady = managerRole != null && cashierRole != null &&
                grants.Any(x => x.RoleId == managerRole.Id && x.Name == AppPermissions.PagesRestaurantRefundApprove) &&
                grants.Any(x => x.RoleId == cashierRole.Id && x.Name == AppPermissions.PagesRestaurantRefundSettle);

            var checks = new List<RestaurantSetupCheckDto>
            {
                Check("menu-recipes", "Menu and recipe mappings", activeMenu.Count > 0 && missingProduct == 0 && missingRecipe == 0,
                    $"{activeMenu.Count} active menu items; {missingProduct} missing products; {missingRecipe} missing recipes"),
                Check("tables", "Dining tables", tables > 0, $"{tables} active tables configured"),
                Check("stations", "Kitchen stations and routes", stations.Count > 0 && stations.All(x => !string.IsNullOrWhiteSpace(x)),
                    $"{stations.Count(x => !string.IsNullOrWhiteSpace(x))} of {stations.Count} active stations have print routes"),
                Check("payment-ledgers", "Cash, card, and QR ledgers", cash != null && cardReady && qrReady,
                    $"Cash: {cash != null}; card: {cardReady}; QR: {qrReady}"),
                Check("refund-ledgers", "Refund and tip ledgers", clearingReady && tipReady,
                    $"Refund clearing: {clearingReady}; tip: {tipReady}"),
                Check("manager-pin", "Manager approval PIN", !string.IsNullOrWhiteSpace(managerPin),
                    string.IsNullOrWhiteSpace(managerPin) ? "Configure a manager PIN" : "Configured"),
                Check("staff-permissions", "Manager and cashier permissions", staffReady,
                    staffReady ? "Manager approval and cashier settlement roles are ready" : "Seed or grant the required restaurant permissions"),
                Check("receipt-route", "Receipt printer route", !string.IsNullOrWhiteSpace(receiptRoute),
                    string.IsNullOrWhiteSpace(receiptRoute) ? "Configure a receipt print route" : receiptRoute),
                AcknowledgementCheck(RestaurantSetupCheckKey.TestReceipt, "Test receipt", acknowledgements),
                AcknowledgementCheck(RestaurantSetupCheckKey.TestKitchenTicket, "Test kitchen ticket", acknowledgements)
            };
            return new RestaurantSetupReadinessDto { Checks = checks, IsReady = checks.All(x => x.IsReady) };
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupReadiness)]
        public async Task AcknowledgeSetupCheck(AcknowledgeRestaurantSetupCheckDto input)
        {
            if (input == null || input.CheckKey is not RestaurantSetupCheckKey.TestReceipt and not RestaurantSetupCheckKey.TestKitchenTicket)
                throw new UserFriendlyException("Choose a supported physical setup check");
            if (string.IsNullOrWhiteSpace(input.Note) || input.Note.Trim().Length > 500)
                throw new UserFriendlyException("Confirm the test result in a short note");
            var tenantId = AbpSession.GetTenantId();
            var row = await acknowledgementRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CheckKey == input.CheckKey);
            if (row == null)
            {
                row = new RestaurantSetupAcknowledgement { TenantId = tenantId, CheckKey = input.CheckKey };
                await acknowledgementRepository.InsertAsync(row);
            }
            row.CompletedAt = GetNepalNow();
            row.CompletedByUserId = AbpSession.GetUserId();
            row.Note = input.Note.Trim();
            await acknowledgementRepository.UpdateAsync(row);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantReleaseFeatures)]
        public async Task UpdateReleaseFeatures(RestaurantOperationalSettingsDto input)
        {
            if (input == null) throw new UserFriendlyException("Feature settings are required");
            var tenantId = AbpSession.GetTenantId();
            var enablesAny = input.MixedTenderEnabled || input.RefundsEnabled || input.OrderVersionChecksEnabled || input.AndroidDraftRecoveryEnabled;
            if (enablesAny)
            {
                if (!input.ClientCompatibilityConfirmed)
                    throw new UserFriendlyException("Confirm that the browser and Android clients used by this restaurant are on a compatible release");
                var readiness = await GetSetupReadiness();
                if (!readiness.IsReady)
                    throw new UserFriendlyException("Complete every setup readiness check before enabling release features");
            }
            if (input.MixedTenderEnabled && (!await IsEnabledLedger(AppSettings.ErpSettings.RestaurantCardLedgerId, tenantId) ||
                                             !await IsEnabledLedger(AppSettings.ErpSettings.RestaurantQrLedgerId, tenantId)))
                throw new UserFriendlyException("Configure card and QR ledgers before enabling mixed tender");
            if (input.RefundsEnabled && !await IsEnabledLedger(AppSettings.ErpSettings.RestaurantRefundPayableLedgerId, tenantId))
                throw new UserFriendlyException("Configure the refund clearing ledger before enabling refunds");

            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantMixedTenderEnabled, input.MixedTenderEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantRefundsEnabled, input.RefundsEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, input.OrderVersionChecksEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantAndroidDraftRecoveryEnabled, input.AndroidDraftRecoveryEnabled.ToString().ToLowerInvariant());
        }

        private static RestaurantSetupCheckDto Check(string key, string label, bool ready, string details) =>
            new() { Key = key, Label = label, IsReady = ready, Details = details };

        private static RestaurantSetupCheckDto AcknowledgementCheck(
            RestaurantSetupCheckKey key,
            string label,
            IDictionary<RestaurantSetupCheckKey, RestaurantSetupAcknowledgement> acknowledgements)
        {
            acknowledgements.TryGetValue(key, out var value);
            return new RestaurantSetupCheckDto
            {
                Key = key.ToString(),
                Label = label,
                IsReady = value != null,
                Details = value?.Note ?? "Operator confirmation required after a physical test",
                CompletedByUserId = value?.CompletedByUserId,
                CompletedAt = value?.CompletedAt
            };
        }

        private async Task<bool> IsEnabledLedger(string setting, int tenantId)
        {
            var id = ParseGuid(await SettingManager.GetSettingValueForTenantAsync(setting, tenantId));
            return await IsTenantLedger(id, tenantId);
        }

        private async Task<bool> IsTenantLedger(Guid? id, int tenantId) => id.HasValue &&
            await accountLedgerRepository.CountAsync(x => x.TenantId == tenantId && x.Id == id.Value) > 0;

        private async Task<bool> IsEnabled(string name, int tenantId) =>
            string.Equals(await SettingManager.GetSettingValueForTenantAsync(name, tenantId), "true", StringComparison.OrdinalIgnoreCase);

        private static Guid? ParseGuid(string value) => Guid.TryParse(value, out var id) ? id : null;

        private static DateTime GetNepalNow()
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time"); }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        }
    }
}
