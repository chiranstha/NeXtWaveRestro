using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantSync)]
    public class RestaurantSyncAppService(
        IRepository<RestaurantDevice, Guid> deviceRepository,
        IRepository<RestaurantSyncUpload, Guid> syncUploadRepository,
        IRepository<RestaurantPushToken, Guid> pushTokenRepository,
        IRepository<RestaurantChangeLog, Guid> changeLogRepository,
        IRepository<RestaurantSyncUploadBatch, Guid> uploadBatchRepository,
        IRestaurantOrderAppService orderAppService)
        : ErpAppServiceBase, IRestaurantSyncAppService
    {
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

        public async Task RegisterPushToken(RegisterRestaurantPushTokenDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Token))
                throw new UserFriendlyException("Push token is required");

            var tenantId = AbpSession.GetTenantId();
            var device = await EnsureActiveDevice(input.DeviceId, tenantId);
            var tokenValue = input.Token.Trim();

            var pushToken = await pushTokenRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.DeviceId == input.DeviceId &&
                x.Token == tokenValue);

            if (pushToken == null)
            {
                pushToken = new RestaurantPushToken
                {
                    TenantId = tenantId,
                    DeviceId = input.DeviceId,
                    Token = tokenValue,
                    RegisteredAt = DateTime.Now
                };
                await pushTokenRepository.InsertAsync(pushToken);
            }

            pushToken.Platform = input.Platform?.Trim();
            pushToken.IsActive = true;
            pushToken.LastSeenAt = DateTime.Now;
            device.LastSeenAt = DateTime.Now;

            await CurrentUnitOfWork.SaveChangesAsync();
        }

        public async Task<PullRestaurantChangesResultDto> PullChanges(PullRestaurantChangesDto input)
        {
            input ??= new PullRestaurantChangesDto();
            var tenantId = AbpSession.GetTenantId();
            RestaurantDevice pullingDevice = null;
            if (input.DeviceId.HasValue)
            {
                pullingDevice = await EnsureActiveDevice(input.DeviceId.Value, tenantId);
                pullingDevice.LastSeenAt = DateTime.Now;
            }

            var maxResultCount = input.MaxResultCount <= 0 ? 500 : Math.Min(input.MaxResultCount, 1000);
            var query = changeLogRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.Seq > input.SinceSeq)
                .Where(x => !input.DeviceId.HasValue ||
                            !x.ChangedByDeviceId.HasValue ||
                            x.ChangedByDeviceId.Value != input.DeviceId.Value);

            if (input.EntityTypes is { Count: > 0 })
                query = query.Where(x => input.EntityTypes.Contains(x.EntityType));

            var changes = await query
                .OrderBy(x => x.Seq)
                .Take(maxResultCount)
                .Select(x => new RestaurantChangeDto
                {
                    Seq = x.Seq,
                    EntityType = x.EntityType,
                    Operation = x.Operation,
                    EntityId = x.EntityId,
                    PayloadJson = x.PayloadJson,
                    ChangedByDeviceId = x.ChangedByDeviceId,
                    ChangedAt = x.ChangedAt
                })
                .ToListAsync();

            if (pullingDevice != null)
            {
                pullingDevice.LastPulledSeq = changes.Count == 0 ? Math.Max(pullingDevice.LastPulledSeq, input.SinceSeq) : changes.Max(x => x.Seq);
                pullingDevice.LastSyncAt = DateTime.Now;
                pullingDevice.LastSyncError = null;
                pullingDevice.HasConflict = false;
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return new PullRestaurantChangesResultDto
            {
                LastSeq = changes.Count == 0 ? input.SinceSeq : changes.Max(x => x.Seq),
                Changes = changes
            };
        }

        public async Task<Guid> UploadOrder(UploadRestaurantOrderDto input)
        {
            if (string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("Client request id is required");

            var tenantId = AbpSession.GetTenantId();
            if (input.DeviceId.HasValue)
            {
                var device = await deviceRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.DeviceId &&
                    x.TenantId == tenantId &&
                    x.Status == RestaurantDeviceStatus.Active);
                if (device == null) throw new UserFriendlyException("Restaurant device is not active");

                device.LastSeenAt = DateTime.Now;
                device.LastSyncAt = DateTime.Now;
                device.LastSyncError = null;
                device.HasConflict = false;
                await deviceRepository.UpdateAsync(device);
            }

            var existing = await syncUploadRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.ClientRequestId == input.ClientRequestId);

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.PayloadHash) &&
                    !string.IsNullOrWhiteSpace(input.PayloadHash) &&
                    existing.PayloadHash != input.PayloadHash)
                    throw new UserFriendlyException("Duplicate mobile upload has a different payload");

                return existing.ServerReferenceId ?? Guid.Empty;
            }

            input.Order.ClientRequestId = input.ClientRequestId;
            input.Order.DeviceId = input.DeviceId;
            input.Order.Source = string.IsNullOrWhiteSpace(input.Order.Source) ? "Mobile" : input.Order.Source;

            var orderId = await orderAppService.CreateOrEditOrder(input.Order);
            await syncUploadRepository.InsertAsync(new RestaurantSyncUpload
            {
                TenantId = tenantId,
                DeviceId = input.DeviceId,
                ClientRequestId = input.ClientRequestId,
                PayloadHash = input.PayloadHash,
                ServerReferenceId = orderId,
                CreatedAt = DateTime.Now
            });

            return orderId;
        }

        public async Task<UploadRestaurantSyncBatchResultDto> UploadBatch(UploadRestaurantSyncBatchDto input)
        {
            if (input == null)
                throw new UserFriendlyException("Sync batch is required");
            if (string.IsNullOrWhiteSpace(input.BatchGuid))
                throw new UserFriendlyException("Batch guid is required");

            var tenantId = AbpSession.GetTenantId();
            if (input.DeviceId.HasValue)
                await EnsureActiveDevice(input.DeviceId.Value, tenantId);

            var batchGuid = input.BatchGuid.Trim();
            var existingBatch = await uploadBatchRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.BatchGuid == batchGuid);

            if (existingBatch != null && existingBatch.Status != RestaurantSyncUploadStatus.Pending)
                return await BuildBatchResult(existingBatch, input.Orders);

            var batch = existingBatch ?? new RestaurantSyncUploadBatch
            {
                TenantId = tenantId,
                BatchGuid = batchGuid,
                DeviceId = input.DeviceId,
                ReceivedAt = DateTime.Now
            };

            if (existingBatch == null)
                await uploadBatchRepository.InsertAsync(batch);

            batch.Status = RestaurantSyncUploadStatus.Pending;
            batch.ItemCount = input.Orders?.Count ?? 0;
            batch.ErrorMessage = null;

            var serverOrderIds = new List<Guid>();
            try
            {
                foreach (var orderUpload in input.Orders ?? new List<UploadRestaurantOrderDto>())
                {
                    orderUpload.DeviceId ??= input.DeviceId;
                    serverOrderIds.Add(await UploadOrder(orderUpload));
                }

                batch.Status = RestaurantSyncUploadStatus.Applied;
                batch.CompletedAt = DateTime.Now;
                if (input.DeviceId.HasValue)
                {
                    var device = await deviceRepository.FirstOrDefaultAsync(x =>
                        x.Id == input.DeviceId.Value &&
                        x.TenantId == tenantId);
                    if (device != null)
                    {
                        device.LastSeenAt = DateTime.Now;
                        device.LastSyncAt = DateTime.Now;
                        device.LastSyncError = null;
                        device.HasConflict = false;
                    }
                }
                await CurrentUnitOfWork.SaveChangesAsync();

                return new UploadRestaurantSyncBatchResultDto
                {
                    BatchGuid = batch.BatchGuid,
                    Status = batch.Status,
                    ServerOrderIds = serverOrderIds
                };
            }
            catch (Exception ex)
            {
                batch.Status = RestaurantSyncUploadStatus.Failed;
                batch.ErrorMessage = ex.Message;
                batch.CompletedAt = DateTime.Now;
                if (input.DeviceId.HasValue)
                {
                    var device = await deviceRepository.FirstOrDefaultAsync(x =>
                        x.Id == input.DeviceId.Value &&
                        x.TenantId == tenantId);
                    if (device != null)
                    {
                        device.LastSyncAt = DateTime.Now;
                        device.LastSyncError = ex.Message;
                        device.HasConflict = true;
                    }
                }
                await CurrentUnitOfWork.SaveChangesAsync();

                return new UploadRestaurantSyncBatchResultDto
                {
                    BatchGuid = batch.BatchGuid,
                    Status = batch.Status,
                    ServerOrderIds = serverOrderIds,
                    ErrorMessage = batch.ErrorMessage
                };
            }
        }

        public async Task AcknowledgeChanges(AcknowledgeRestaurantChangesDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var device = await EnsureActiveDevice(input.DeviceId, tenantId);
            device.LastSeenAt = DateTime.Now;
            device.LastAcknowledgedSeq = Math.Max(device.LastAcknowledgedSeq, input.LastSeq);
            device.LastSyncAt = DateTime.Now;
            device.LastSyncError = null;
            device.HasConflict = false;
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        private async Task<RestaurantDevice> EnsureActiveDevice(Guid deviceId, int tenantId)
        {
            var device = await deviceRepository.FirstOrDefaultAsync(x =>
                x.Id == deviceId &&
                x.TenantId == tenantId &&
                x.Status == RestaurantDeviceStatus.Active);
            if (device == null)
                throw new UserFriendlyException("Restaurant device is not active");

            return device;
        }

        private async Task<UploadRestaurantSyncBatchResultDto> BuildBatchResult(
            RestaurantSyncUploadBatch batch,
            List<UploadRestaurantOrderDto> orderUploads)
        {
            var clientRequestIds = (orderUploads ?? new List<UploadRestaurantOrderDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.ClientRequestId))
                .Select(x => x.ClientRequestId)
                .ToList();

            var serverOrderIds = clientRequestIds.Count == 0
                ? new List<Guid>()
                : await syncUploadRepository.GetAll()
                    .Where(x => x.TenantId == batch.TenantId && clientRequestIds.Contains(x.ClientRequestId))
                    .Where(x => x.ServerReferenceId.HasValue)
                    .Select(x => x.ServerReferenceId.Value)
                    .ToListAsync();

            return new UploadRestaurantSyncBatchResultDto
            {
                BatchGuid = batch.BatchGuid,
                Status = batch.Status,
                ServerOrderIds = serverOrderIds,
                ErrorMessage = batch.ErrorMessage
            };
        }
    }
}
