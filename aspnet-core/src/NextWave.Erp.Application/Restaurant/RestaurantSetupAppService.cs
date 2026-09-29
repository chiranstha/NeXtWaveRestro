using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantSetup)]
    public class RestaurantSetupAppService(
        IRepository<RestaurantArea, Guid> areaRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantStation, Guid> stationRepository,
        IRepository<RestaurantDevice, Guid> deviceRepository,
        IRepository<RestaurantChangeLog, Guid> changeLogRepository,
        IRepository<NextWave.Erp.Accounting.AccountLedger, Guid> accountLedgerRepository)
        : ErpAppServiceBase, IRestaurantSetupAppService
    {
        public async Task<List<RestaurantAreaDto>> GetAreas()
        {
            return await areaRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new RestaurantAreaDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupCreate, AppPermissions.PagesRestaurantSetupEdit)]
        public async Task<Guid> CreateOrEditArea(CreateOrEditRestaurantAreaDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Area name is required");

            var tenantId = AbpSession.GetTenantId();
            RestaurantArea area;

            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                area = new RestaurantArea { TenantId = tenantId };
                await areaRepository.InsertAsync(area);
            }
            else
            {
                area = await areaRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (area == null) throw new UserFriendlyException("Restaurant area not found");
            }

            area.Name = input.Name.Trim();
            area.Description = input.Description;
            area.SortOrder = input.SortOrder;
            area.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.DiningTable, area.Id, new { Entity = "Area", area.Name, area.IsActive });
            return area.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupDelete)]
        public async Task DeleteArea(EntityDto<Guid> input)
        {
            var area = await areaRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == AbpSession.TenantId);
            if (area == null) throw new UserFriendlyException("Restaurant area not found");

            area.IsDeleted = true;
            area.IsActive = false;
            await areaRepository.UpdateAsync(area);
            await RecordSyncChange(RestaurantSyncEntityType.DiningTable, area.Id, new { Entity = "Area", area.Name }, RestaurantSyncOperation.Delete);
        }

        public async Task<List<RestaurantTableDto>> GetTables(Guid? areaId)
        {
            return await tableRepository.GetAll()
                .Include(x => x.AreaFk)
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .Where(x => !areaId.HasValue || x.AreaId == areaId)
                .OrderBy(x => x.AreaFk.Name).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new RestaurantTableDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Capacity = x.Capacity,
                    SortOrder = x.SortOrder,
                    Status = x.Status,
                    IsActive = x.IsActive,
                    AreaId = x.AreaId,
                    AreaName = x.AreaFk.Name,
                    HasQrCode = x.QrTokenHash != null,
                    QrTokenUpdatedAt = x.QrTokenUpdatedAt
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupCreate, AppPermissions.PagesRestaurantSetupEdit)]
        public async Task<Guid> CreateOrEditTable(CreateOrEditRestaurantTableDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Table name is required");

            var tenantId = AbpSession.GetTenantId();
            if (await areaRepository.CountAsync(x => x.Id == input.AreaId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Restaurant area not found");

            RestaurantTable table;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                table = new RestaurantTable { TenantId = tenantId };
                await tableRepository.InsertAsync(table);
            }
            else
            {
                table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (table == null) throw new UserFriendlyException("Restaurant table not found");
            }

            table.Name = input.Name.Trim();
            table.Code = input.Code;
            table.Capacity = input.Capacity;
            table.SortOrder = input.SortOrder;
            table.Status = input.IsActive ? input.Status : RestaurantTableStatus.Inactive;
            table.IsActive = input.IsActive;
            table.AreaId = input.AreaId;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.DiningTable, table.Id, new { Entity = "Table", table.Name, table.Code, table.Status, table.IsActive });
            return table.Id;
        }

        public async Task<List<RestaurantStationDto>> GetStations()
        {
            return await stationRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .OrderBy(x => x.StationType).ThenBy(x => x.Name)
                .Select(x => new RestaurantStationDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    StationType = x.StationType,
                    IsActive = x.IsActive,
                    PrintRouteName = x.PrintRouteName
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupCreate, AppPermissions.PagesRestaurantSetupEdit)]
        public async Task<Guid> CreateOrEditStation(CreateOrEditRestaurantStationDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Station name is required");

            var tenantId = AbpSession.GetTenantId();
            RestaurantStation station;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                station = new RestaurantStation { TenantId = tenantId };
                await stationRepository.InsertAsync(station);
            }
            else
            {
                station = await stationRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (station == null) throw new UserFriendlyException("Restaurant station not found");
            }

            station.Name = input.Name.Trim();
            station.StationType = input.StationType;
            station.IsActive = input.IsActive;
            station.PrintRouteName = string.IsNullOrWhiteSpace(input.PrintRouteName) ? null : input.PrintRouteName.Trim();

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.DiningTable, station.Id, new { Entity = "Station", station.Name, station.StationType, station.IsActive });
            return station.Id;
        }

        public async Task<List<RestaurantDeviceDto>> GetDevices()
        {
            return await deviceRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .OrderBy(x => x.Name)
                .Select(x => new RestaurantDeviceDto
                {
                    Id = x.Id,
                    DeviceCode = x.DeviceCode,
                    Name = x.Name,
                    UserId = x.UserId,
                    Status = x.Status,
                    RegisteredAt = x.RegisteredAt,
                    LastSeenAt = x.LastSeenAt,
                    LastPulledSeq = x.LastPulledSeq,
                    LastAcknowledgedSeq = x.LastAcknowledgedSeq,
                    LastSyncAt = x.LastSyncAt,
                    LastSyncError = x.LastSyncError,
                    HasConflict = x.HasConflict
                }).ToListAsync();
        }

        public async Task<RestaurantDeviceDto> RegisterDevice(RegisterRestaurantDeviceDto input)
        {
            if (string.IsNullOrWhiteSpace(input.DeviceCode))
                throw new UserFriendlyException("Device code is required");

            var tenantId = AbpSession.GetTenantId();
            var device = await deviceRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.DeviceCode == input.DeviceCode);

            if (device == null)
            {
                device = new RestaurantDevice
                {
                    TenantId = tenantId,
                    DeviceCode = input.DeviceCode,
                    RegisteredAt = DateTime.Now,
                    Status = RestaurantDeviceStatus.Active
                };
                await deviceRepository.InsertAsync(device);
            }

            device.Name = input.Name;
            device.UserId = input.UserId;
            device.LastSeenAt = DateTime.Now;
            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.Device, device.Id, new { Entity = "Device", device.DeviceCode, device.Name, device.Status });

            return new RestaurantDeviceDto
            {
                Id = device.Id,
                DeviceCode = device.DeviceCode,
                Name = device.Name,
                UserId = device.UserId,
                Status = device.Status,
                RegisteredAt = device.RegisteredAt,
                LastSeenAt = device.LastSeenAt,
                LastPulledSeq = device.LastPulledSeq,
                LastAcknowledgedSeq = device.LastAcknowledgedSeq,
                LastSyncAt = device.LastSyncAt,
                LastSyncError = device.LastSyncError,
                HasConflict = device.HasConflict
            };
        }

        public async Task<RestaurantOperationalSettingsDto> GetOperationalSettings()
        {
            var tenantId = AbpSession.GetTenantId();
            return new RestaurantOperationalSettingsDto
            {
                VatPercent = await GetDecimalSetting(
                    AppSettings.ErpSettings.RestaurantVatPercent,
                    tenantId),
                ServiceChargePercent = await GetDecimalSetting(
                    AppSettings.ErpSettings.RestaurantServiceChargePercent,
                    tenantId),
                TipLedgerId = Guid.TryParse(await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantTipLedgerId, tenantId), out var tipLedgerId) ? tipLedgerId : null,
                RequireManagerPinForSensitiveActions = await GetBoolSetting(
                    AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions,
                    tenantId),
                ManagerPin = string.Empty,
                NegativeStockStatus = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.NegativeStockStatus,
                    tenantId),
                TicketPrintingEnabled = await GetBoolSetting(
                    AppSettings.ErpSettings.RestaurantTicketPrintingEnabled,
                    tenantId),
                ChannelAvailabilityEnabled = await GetBoolSetting(
                    AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled,
                    tenantId),
                TableWorkflow = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantTableWorkflow,
                    tenantId),
                QrOrderingEnabled = await GetBoolSetting(AppSettings.ErpSettings.RestaurantQrOrderingEnabled, tenantId),
                ReservationBookingEnabled = await GetBoolSetting(AppSettings.ErpSettings.RestaurantReservationBookingEnabled, tenantId),
                DefaultReservationDurationMinutes = int.TryParse(await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantDefaultReservationDurationMinutes, tenantId), out var bookingDuration) ? bookingDuration : 90,
                ReceiptPrintRouteName = await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, tenantId)
            };
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupEdit)]
        public async Task UpdateOperationalSettings(RestaurantOperationalSettingsDto input)
        {
            if (input == null)
                throw new UserFriendlyException("Restaurant settings are required");

            var tenantId = AbpSession.GetTenantId();
            var negativeStockStatus = NormalizeOption(input.NegativeStockStatus, new[] { "Allow", "Warn", "Block" }, "Warn");
            var tableWorkflow = NormalizeOption(input.TableWorkflow, new[] { "TableSession", "PerOrder" }, "TableSession");

            if (input.TipLedgerId.HasValue && await accountLedgerRepository.CountAsync(x =>
                    x.Id == input.TipLedgerId.Value && x.TenantId == tenantId) == 0)
                throw new UserFriendlyException("Select an active restaurant tip ledger");

            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantVatPercent,
                ClampPercent(input.VatPercent).ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantServiceChargePercent,
                ClampPercent(input.ServiceChargePercent).ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantTipLedgerId,
                input.TipLedgerId?.ToString() ?? string.Empty);
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions,
                input.RequireManagerPinForSensitiveActions.ToString().ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(input.ManagerPin))
            {
                var pin = input.ManagerPin.Trim();
                if (pin.Length is < 4 or > 12 || pin.Any(character => !char.IsDigit(character)))
                    throw new UserFriendlyException("Manager PIN must contain 4 to 12 digits");

                await SettingManager.ChangeSettingForTenantAsync(
                    tenantId,
                    AppSettings.ErpSettings.RestaurantManagerPin,
                    RestaurantPinHasher.Hash(pin));
            }
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.NegativeStockStatus,
                negativeStockStatus);
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantTicketPrintingEnabled,
                input.TicketPrintingEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled,
                input.ChannelAvailabilityEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AppSettings.ErpSettings.RestaurantTableWorkflow,
                tableWorkflow);
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantQrOrderingEnabled, input.QrOrderingEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantReservationBookingEnabled, input.ReservationBookingEnabled.ToString().ToLowerInvariant());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantDefaultReservationDurationMinutes, Math.Clamp(input.DefaultReservationDurationMinutes, 30, 240).ToString(System.Globalization.CultureInfo.InvariantCulture));
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, (input.ReceiptPrintRouteName ?? string.Empty).Trim());
        }

        private async Task RecordSyncChange(
            RestaurantSyncEntityType entityType,
            Guid entityId,
            object payload,
            RestaurantSyncOperation operation = RestaurantSyncOperation.Upsert)
        {
            var tenantId = AbpSession.GetTenantId();
            var lastSeq = await changeLogRepository.GetAll()
                .Where(x => x.TenantId == tenantId)
                .Select(x => (long?)x.Seq)
                .MaxAsync() ?? 0;

            await changeLogRepository.InsertAsync(new RestaurantChangeLog
            {
                TenantId = tenantId,
                Seq = lastSeq + 1,
                EntityType = entityType,
                Operation = operation,
                EntityId = entityId.ToString(),
                PayloadJson = JsonSerializer.Serialize(payload),
                ChangedAt = DateTime.Now
            });
        }

        private static decimal ClampPercent(decimal value)
        {
            return Math.Min(100, Math.Max(0, value));
        }

        private static string NormalizeOption(string value, string[] allowedValues, string fallback)
        {
            var match = allowedValues.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            return match ?? fallback;
        }

        private async Task<decimal> GetDecimalSetting(string name, int tenantId)
        {
            var rawValue = await SettingManager.GetSettingValueForTenantAsync(name, tenantId);
            return decimal.TryParse(
                rawValue,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
                ? value
                : 0;
        }

        private async Task<bool> GetBoolSetting(string name, int tenantId)
        {
            var rawValue = await SettingManager.GetSettingValueForTenantAsync(name, tenantId);
            return string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
