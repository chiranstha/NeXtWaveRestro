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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantKds)]
    public class RestaurantKdsAppService(
        IRepository<RestaurantTicket, Guid> ticketRepository,
        IRepository<RestaurantTicketItem, Guid> ticketItemRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantOrderItemModifier, Guid> orderItemModifierRepository,
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantChangeLog, Guid> changeLogRepository,
        IRepository<RestaurantClientOperation, Guid> operationRepository)
        : ErpAppServiceBase, IRestaurantKdsAppService
    {
        public async Task<List<RestaurantTicketDto>> GetOpenTickets(Guid? stationId)
        {
            var tickets = await ticketRepository.GetAll()
                .Include(x => x.OrderFk)
                .ThenInclude(x => x.TableFk)
                .Include(x => x.StationFk)
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.Status != RestaurantTicketStatus.Served &&
                            x.Status != RestaurantTicketStatus.Cancelled)
                .Where(x => !stationId.HasValue || x.StationId == stationId)
                .OrderBy(x => x.SentAt)
                .ToListAsync();

            var result = new List<RestaurantTicketDto>();
            foreach (var ticket in tickets)
                result.Add(await MapTicket(ticket));

            return result;
        }

        public async Task UpdateTicketStatus(UpdateRestaurantTicketStatusDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var ticket = await ticketRepository.FirstOrDefaultAsync(x => x.Id == input.TicketId && x.TenantId == AbpSession.TenantId);
            if (ticket == null) throw new UserFriendlyException("Ticket not found");
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == ticket.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            RequireRequestIdWhenVersioned(input.ClientRequestId, versionChecksEnabled);
            var requestHash = HashOrderMutation(new { input.TicketId, input.Status, CancelReason = input.CancelReason ?? string.Empty, input.ExpectedOrderVersion });
            var priorOperation = await FindOperation(tenantId, "KdsTicketStatus", input.ClientRequestId);
            if (priorOperation != null)
            {
                EnsureOperationHash(priorOperation, requestHash);
                return;
            }
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
            ValidateTicketTransition(ticket.Status, input.Status);
            if (ticket.Purpose != RestaurantTicketPurpose.Cancellation &&
                input.Status == RestaurantTicketStatus.Cancelled &&
                string.IsNullOrWhiteSpace(input.CancelReason))
                throw new UserFriendlyException("Cancel reason is required");

            ApplyTicketStatus(ticket, input.Status, input.CancelReason);
            await ticketRepository.UpdateAsync(ticket);

            if (ticket.Purpose == RestaurantTicketPurpose.Cancellation)
            {
                await RecordOperation(tenantId, "KdsTicketStatus", input.ClientRequestId, requestHash, order.Id);
                return;
            }

            var itemStatus = input.Status switch
            {
                RestaurantTicketStatus.InProgress => RestaurantOrderItemStatus.Preparing,
                RestaurantTicketStatus.Ready => RestaurantOrderItemStatus.Ready,
                RestaurantTicketStatus.Served => RestaurantOrderItemStatus.Served,
                RestaurantTicketStatus.Cancelled => RestaurantOrderItemStatus.Cancelled,
                _ => RestaurantOrderItemStatus.Sent
            };

            var ticketItems = await ticketItemRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.TicketId == ticket.Id)
                .ToListAsync();

            foreach (var ticketItem in ticketItems)
            {
                ticketItem.Status = itemStatus;
                if (itemStatus == RestaurantOrderItemStatus.Cancelled)
                    ticketItem.CancelReason = input.CancelReason;
                await ticketItemRepository.UpdateAsync(ticketItem);

                var orderItem = await orderItemRepository.FirstOrDefaultAsync(ticketItem.OrderItemId);
                if (orderItem != null)
                {
                    orderItem.Status = itemStatus;
                    if (itemStatus == RestaurantOrderItemStatus.Cancelled)
                        orderItem.CancelReason = input.CancelReason;
                    await orderItemRepository.UpdateAsync(orderItem);
                }
            }

            await RefreshOrderStatus(ticket.OrderId);
            await RecordSyncChange(RestaurantSyncEntityType.Ticket, ticket.Id, new { ticket.OrderId, ticket.Status, ItemStatus = itemStatus });
            await RecordOperation(tenantId, "KdsTicketStatus", input.ClientRequestId, requestHash, order.Id);
        }

        public async Task UpdateTicketItemStatus(UpdateRestaurantTicketItemStatusDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var ticketItem = await ticketItemRepository.GetAll()
                .Include(x => x.TicketFk)
                .FirstOrDefaultAsync(x => x.Id == input.TicketItemId && x.TenantId == AbpSession.TenantId);
            if (ticketItem == null) throw new UserFriendlyException("Ticket item not found");
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == ticketItem.TicketFk.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            RequireRequestIdWhenVersioned(input.ClientRequestId, versionChecksEnabled);
            var requestHash = HashOrderMutation(new { input.TicketItemId, input.Status, CancelReason = input.CancelReason ?? string.Empty, input.ExpectedOrderVersion });
            var priorOperation = await FindOperation(tenantId, "KdsTicketItemStatus", input.ClientRequestId);
            if (priorOperation != null)
            {
                EnsureOperationHash(priorOperation, requestHash);
                return;
            }
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
            if (ticketItem.TicketFk.Purpose == RestaurantTicketPurpose.Cancellation)
                throw new UserFriendlyException("Cancellation ticket items cannot change item status");
            ValidateItemTransition(ticketItem.Status, input.Status);
            if (input.Status == RestaurantOrderItemStatus.Cancelled && string.IsNullOrWhiteSpace(input.CancelReason))
                throw new UserFriendlyException("Cancel reason is required");

            ticketItem.Status = input.Status;
            if (input.Status == RestaurantOrderItemStatus.Cancelled)
                ticketItem.CancelReason = input.CancelReason;
            await ticketItemRepository.UpdateAsync(ticketItem);

            var orderItem = await orderItemRepository.FirstOrDefaultAsync(ticketItem.OrderItemId);
            if (orderItem != null)
            {
                orderItem.Status = input.Status;
                if (input.Status == RestaurantOrderItemStatus.Cancelled)
                    orderItem.CancelReason = input.CancelReason;
                await orderItemRepository.UpdateAsync(orderItem);
            }

            await RefreshTicketStatus(ticketItem.TicketId);
            await RefreshOrderStatus(ticketItem.TicketFk.OrderId);
            await RecordSyncChanges(
                (RestaurantSyncEntityType.TicketItem, ticketItem.Id, new { ticketItem.TicketId, ticketItem.OrderItemId, ticketItem.Status }, RestaurantSyncOperation.Upsert),
                (RestaurantSyncEntityType.OrderItem, ticketItem.OrderItemId, new { ticketItem.TicketFk.OrderId, ticketItem.Status }, RestaurantSyncOperation.Upsert));
            await RecordOperation(tenantId, "KdsTicketItemStatus", input.ClientRequestId, requestHash, order.Id);
        }

        public async Task<BulkUpdateRestaurantTicketItemStatusResultDto> UpdateTicketItemStatuses(BulkUpdateRestaurantTicketItemStatusDto input)
        {
            if (input.Status is not (RestaurantOrderItemStatus.Preparing or RestaurantOrderItemStatus.Ready or RestaurantOrderItemStatus.Served))
                throw new UserFriendlyException("Only Start, Ready, and Served bulk updates are supported");

            var requestedIds = (input.TicketItemIds ?? new List<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
            if (requestedIds.Count == 0)
                return new BulkUpdateRestaurantTicketItemStatusResultDto();

            var tenantId = AbpSession.GetTenantId();
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            RequireRequestIdWhenVersioned(input.ClientRequestId, versionChecksEnabled);
            var requestHash = HashOrderMutation(new
            {
                Ids = requestedIds.OrderBy(x => x).ToArray(), input.Status,
                Versions = (input.ExpectedOrderVersions ?? new Dictionary<Guid, string>()).OrderBy(x => x.Key).ToArray()
            });
            var priorOperation = await FindOperation(tenantId, "KdsBulkTicketItems", input.ClientRequestId);
            if (priorOperation != null)
            {
                EnsureOperationHash(priorOperation, requestHash);
                return JsonSerializer.Deserialize<BulkUpdateRestaurantTicketItemStatusResultDto>(priorOperation.ResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new BulkUpdateRestaurantTicketItemStatusResultDto();
            }

            var ticketItems = await ticketItemRepository.GetAll()
                .Include(x => x.TicketFk)
                .Where(x => x.TenantId == AbpSession.TenantId && requestedIds.Contains(x.Id))
                .ToListAsync();

            var orderIdsToCheck = ticketItems.Select(x => x.TicketFk.OrderId).Distinct().ToList();
            var expectedOrderVersions = input.ExpectedOrderVersions ?? new Dictionary<Guid, string>();
            foreach (var orderId in orderIdsToCheck)
            {
                var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == orderId && x.TenantId == tenantId);
                if (order == null) throw new UserFriendlyException("Restaurant order not found");
                await EnsureOrderVersion(order, expectedOrderVersions.GetValueOrDefault(orderId), versionChecksEnabled);
            }

            var updatedItems = new List<RestaurantTicketItem>();
            var changes = new List<(RestaurantSyncEntityType EntityType, Guid EntityId, object Payload, RestaurantSyncOperation Operation)>();
            var orderIds = new HashSet<Guid>();
            var ticketIds = new HashSet<Guid>();

            foreach (var ticketItem in ticketItems)
            {
                if (ticketItem.TicketFk.Purpose == RestaurantTicketPurpose.Cancellation ||
                    !CanBulkTransition(ticketItem.Status, input.Status))
                    continue;

                ticketItem.Status = input.Status;
                await ticketItemRepository.UpdateAsync(ticketItem);

                var orderItem = await orderItemRepository.FirstOrDefaultAsync(ticketItem.OrderItemId);
                if (orderItem != null)
                {
                    orderItem.Status = input.Status;
                    await orderItemRepository.UpdateAsync(orderItem);
                }

                updatedItems.Add(ticketItem);
                ticketIds.Add(ticketItem.TicketId);
                orderIds.Add(ticketItem.TicketFk.OrderId);
                changes.Add((RestaurantSyncEntityType.TicketItem, ticketItem.Id,
                    new { ticketItem.TicketId, ticketItem.OrderItemId, ticketItem.Status }, RestaurantSyncOperation.Upsert));
                changes.Add((RestaurantSyncEntityType.OrderItem, ticketItem.OrderItemId,
                    new { ticketItem.TicketFk.OrderId, ticketItem.Status }, RestaurantSyncOperation.Upsert));
            }

            foreach (var ticketId in ticketIds)
                await RefreshTicketStatus(ticketId);
            foreach (var orderId in orderIds)
                await RefreshOrderStatus(orderId);

            if (changes.Count > 0)
                await RecordSyncChanges(changes.ToArray());

            var result = new BulkUpdateRestaurantTicketItemStatusResultDto
            {
                UpdatedCount = updatedItems.Count,
                SkippedCount = requestedIds.Count - updatedItems.Count
            };
            await RecordOperation(tenantId, "KdsBulkTicketItems", input.ClientRequestId, requestHash,
                orderIds.FirstOrDefault(), result);
            return result;
        }

        private async Task<bool> IsOrderVersionChecksEnabled(int tenantId) =>
            string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);

        private static void RequireRequestIdWhenVersioned(string requestId, bool enabled)
        {
            if (enabled && string.IsNullOrWhiteSpace(requestId))
                throw new UserFriendlyException("A unique request ID is required for this kitchen update");
            if (!string.IsNullOrWhiteSpace(requestId) && requestId.Trim().Length > 100)
                throw new UserFriendlyException("Kitchen request ID is too long");
        }

        private async Task EnsureOrderVersion(RestaurantOrder order, string expectedVersion, bool enabled)
        {
            if (!enabled) return;
            var currentVersion = order.RowVersion == null ? null : Convert.ToBase64String(order.RowVersion);
            if (string.IsNullOrWhiteSpace(expectedVersion) || !string.Equals(currentVersion, expectedVersion, StringComparison.Ordinal))
                throw new UserFriendlyException("This order changed on another device. Refresh the kitchen screen before retrying.",
                    JsonSerializer.Serialize(new
                    {
                        Code = "Restaurant.OrderConflict",
                        LatestOrder = new RestaurantOrderDto { Id = order.Id, RowVersion = currentVersion }
                    }));
        }

        private Task<RestaurantClientOperation> FindOperation(int tenantId, string operationType, string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return Task.FromResult<RestaurantClientOperation>(null);
            return operationRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                x.UserId == AbpSession.GetUserId() && x.OperationType == operationType && x.ClientRequestId == requestId.Trim());
        }

        private async Task RecordOperation(int tenantId, string operationType, string requestId, string requestHash, Guid entityId, object result = null)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;
            await operationRepository.InsertAsync(new RestaurantClientOperation
            {
                TenantId = tenantId,
                UserId = AbpSession.GetUserId(),
                ClientRequestId = requestId.Trim(),
                OperationType = operationType,
                RequestHash = requestHash,
                EntityId = entityId,
                ResultJson = JsonSerializer.Serialize(result ?? new { EntityId = entityId }),
                CreatedAt = DateTime.UtcNow
            });
        }

        private static void EnsureOperationHash(RestaurantClientOperation operation, string requestHash)
        {
            if (!string.Equals(operation.RequestHash, requestHash, StringComparison.Ordinal))
                throw new UserFriendlyException("This kitchen request ID was already used for different changes");
        }

        private static string HashOrderMutation<T>(T value)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static bool CanBulkTransition(RestaurantOrderItemStatus currentStatus, RestaurantOrderItemStatus nextStatus)
        {
            return nextStatus switch
            {
                RestaurantOrderItemStatus.Preparing => currentStatus == RestaurantOrderItemStatus.Sent,
                RestaurantOrderItemStatus.Ready => currentStatus is RestaurantOrderItemStatus.Sent or RestaurantOrderItemStatus.Preparing,
                RestaurantOrderItemStatus.Served => currentStatus == RestaurantOrderItemStatus.Ready,
                _ => false
            };
        }

        private async Task RefreshTicketStatus(Guid ticketId)
        {
            var ticket = await ticketRepository.FirstOrDefaultAsync(ticketId);
            if (ticket == null || ticket.Purpose == RestaurantTicketPurpose.Cancellation)
                return;

            var items = await ticketItemRepository.GetAll().Where(x => x.TicketId == ticketId).ToListAsync();

            if (items.All(x => x.Status == RestaurantOrderItemStatus.Cancelled))
            {
                ticket.Status = RestaurantTicketStatus.Cancelled;
                ticket.CancelledAt ??= DateTime.Now;
            }
            else if (items.All(x => x.Status == RestaurantOrderItemStatus.Served))
            {
                ticket.Status = RestaurantTicketStatus.Served;
                ticket.ServedAt ??= DateTime.Now;
            }
            else if (items.All(x => x.Status is RestaurantOrderItemStatus.Ready or RestaurantOrderItemStatus.Served))
            {
                ticket.Status = RestaurantTicketStatus.Ready;
                ticket.ReadyAt ??= DateTime.Now;
            }
            else if (items.Any(x => x.Status == RestaurantOrderItemStatus.Preparing))
            {
                ticket.Status = RestaurantTicketStatus.InProgress;
                ticket.StartedAt ??= DateTime.Now;
            }

            await ticketRepository.UpdateAsync(ticket);
        }

        private async Task RefreshOrderStatus(Guid orderId)
        {
            var order = await orderRepository.FirstOrDefaultAsync(orderId);
            if (order == null || order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed)
                return;

            var items = await orderItemRepository.GetAll()
                .Where(x => x.OrderId == orderId)
                .ToListAsync();

            var activeItems = items.Where(x => x.Status != RestaurantOrderItemStatus.Cancelled).ToList();
            if (activeItems.Count == 0)
                order.Status = RestaurantOrderStatus.Cancelled;
            else if (activeItems.All(x => x.Status == RestaurantOrderItemStatus.Draft))
                order.Status = RestaurantOrderStatus.Draft;
            else if (activeItems.All(x => x.Status == RestaurantOrderItemStatus.Served))
                order.Status = RestaurantOrderStatus.Served;
            else if (activeItems.All(x => x.Status is RestaurantOrderItemStatus.Ready or RestaurantOrderItemStatus.Served))
                order.Status = RestaurantOrderStatus.Ready;
            else if (activeItems.Any(x => x.Status == RestaurantOrderItemStatus.Preparing))
                order.Status = RestaurantOrderStatus.InProgress;
            else if (activeItems.Any(x => x.Status == RestaurantOrderItemStatus.Sent))
                order.Status = RestaurantOrderStatus.SentToKitchen;

            await orderRepository.UpdateAsync(order);
        }

        private static void ValidateTicketTransition(RestaurantTicketStatus currentStatus, RestaurantTicketStatus nextStatus)
        {
            if (currentStatus == nextStatus)
                return;

            var allowed = currentStatus switch
            {
                RestaurantTicketStatus.Pending => nextStatus is RestaurantTicketStatus.InProgress or RestaurantTicketStatus.Ready or RestaurantTicketStatus.Cancelled,
                RestaurantTicketStatus.InProgress => nextStatus is RestaurantTicketStatus.Ready or RestaurantTicketStatus.Cancelled,
                RestaurantTicketStatus.Ready => nextStatus is RestaurantTicketStatus.Served or RestaurantTicketStatus.Cancelled,
                _ => false
            };

            if (!allowed)
                throw new UserFriendlyException("Invalid ticket status transition");
        }

        private static void ValidateItemTransition(RestaurantOrderItemStatus currentStatus, RestaurantOrderItemStatus nextStatus)
        {
            if (currentStatus == nextStatus)
                return;

            var allowed = currentStatus switch
            {
                RestaurantOrderItemStatus.Sent => nextStatus is RestaurantOrderItemStatus.Preparing or RestaurantOrderItemStatus.Ready or RestaurantOrderItemStatus.Cancelled,
                RestaurantOrderItemStatus.Preparing => nextStatus is RestaurantOrderItemStatus.Ready or RestaurantOrderItemStatus.Cancelled,
                RestaurantOrderItemStatus.Ready => nextStatus is RestaurantOrderItemStatus.Served or RestaurantOrderItemStatus.Cancelled,
                _ => false
            };

            if (!allowed)
                throw new UserFriendlyException("Invalid ticket item status transition");
        }

        private static void ApplyTicketStatus(RestaurantTicket ticket, RestaurantTicketStatus status, string cancelReason)
        {
            ticket.Status = status;
            var now = DateTime.Now;
            switch (status)
            {
                case RestaurantTicketStatus.InProgress:
                    ticket.StartedAt ??= now;
                    break;
                case RestaurantTicketStatus.Ready:
                    ticket.ReadyAt ??= now;
                    break;
                case RestaurantTicketStatus.Served:
                    ticket.ServedAt ??= now;
                    break;
                case RestaurantTicketStatus.Cancelled:
                    ticket.CancelledAt ??= now;
                    ticket.CancelReason = cancelReason;
                    break;
            }
        }

        private async Task<RestaurantTicketDto> MapTicket(RestaurantTicket ticket)
        {
            var ticketItems = await ticketItemRepository.GetAll()
                .Include(x => x.OrderItemFk)
                .ThenInclude(x => x.ProductFk)
                .Include(x => x.OrderItemFk)
                .ThenInclude(x => x.UnitFk)
                .Where(x => x.TenantId == AbpSession.TenantId && x.TicketId == ticket.Id)
                .ToListAsync();

            var items = new List<RestaurantTicketItemDto>();
            foreach (var ticketItem in ticketItems)
            {
                var modifiers = await MapOrderItemModifiers(ticketItem.OrderItemId);
                items.Add(new RestaurantTicketItemDto
                {
                    Id = ticketItem.Id,
                    OrderItemId = ticketItem.OrderItemId,
                    ProductName = ticketItem.OrderItemFk.ProductFk.Name,
                    ItemNameSnapshot = string.IsNullOrWhiteSpace(ticketItem.OrderItemFk.ItemNameSnapshot)
                        ? ticketItem.OrderItemFk.ProductFk.Name
                        : ticketItem.OrderItemFk.ItemNameSnapshot,
                    VariantNameSnapshot = ticketItem.OrderItemFk.VariantNameSnapshot,
                    ModifierSummary = BuildModifierSummary(modifiers),
                    UnitName = ticketItem.OrderItemFk.UnitFk.Name,
                    Qty = ticketItem.Qty,
                    Status = ticketItem.Status,
                    Notes = ticketItem.OrderItemFk.Notes,
                    CancelReason = ticketItem.CancelReason ?? ticketItem.OrderItemFk.CancelReason
                });
            }

            var restaurantName = "Restaurant";
            if (AbpSession.TenantId.HasValue)
            {
                var tenant = await GetCurrentTenantAsync();
                restaurantName = tenant.Name;
            }

            var waiterName = "";
            if (ticket.OrderFk?.WaiterUserId.HasValue == true)
            {
                var waiter = await UserManager.FindByIdAsync(ticket.OrderFk.WaiterUserId.Value.ToString());
                waiterName = waiter == null ? ticket.OrderFk.WaiterUserId.Value.ToString() : waiter.Name;
            }

            return new RestaurantTicketDto
            {
                Id = ticket.Id,
                RestaurantName = restaurantName,
                TicketNo = ticket.TicketNo,
                OrderNo = ticket.OrderFk?.OrderNo,
                OrderId = ticket.OrderId,
                OrderType = ticket.OrderFk?.OrderType ?? RestaurantOrderType.DineIn,
                TableId = ticket.OrderFk?.TableId,
                TableName = ticket.OrderFk?.TableFk == null ? "" : ticket.OrderFk.TableFk.Name,
                WaiterName = waiterName,
                StationId = ticket.StationId,
                StationName = ticket.StationFk?.Name,
                TicketType = ticket.TicketType,
                Purpose = ticket.Purpose,
                Status = ticket.Status,
                SentAt = ticket.SentAt,
                StartedAt = ticket.StartedAt,
                ReadyAt = ticket.ReadyAt,
                ServedAt = ticket.ServedAt,
                CancelledAt = ticket.CancelledAt,
                PrintedAt = ticket.PrintedAt,
                LastPrintedAt = ticket.LastPrintedAt,
                PrintCount = ticket.PrintCount,
                OrderNotes = ticket.OrderFk?.Notes,
                CancelReason = ticket.CancelReason,
                Items = items
            };
        }

        private async Task<List<RestaurantOrderItemModifierDto>> MapOrderItemModifiers(Guid orderItemId)
        {
            return await orderItemModifierRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.OrderItemId == orderItemId)
                .OrderBy(x => x.ModifierNameSnapshot)
                .Select(x => new RestaurantOrderItemModifierDto
                {
                    Id = x.Id,
                    ModifierId = x.ModifierId,
                    ModifierNameSnapshot = x.ModifierNameSnapshot,
                    PriceDelta = x.PriceDelta,
                    Qty = x.Qty,
                    Amount = x.Amount
                }).ToListAsync();
        }

        private static string BuildModifierSummary(List<RestaurantOrderItemModifierDto> modifiers)
        {
            return modifiers == null || modifiers.Count == 0
                ? ""
                : string.Join(", ", modifiers.Select(x => x.Qty == 1
                    ? x.ModifierNameSnapshot
                    : $"{x.ModifierNameSnapshot} x{x.Qty:0.###}"));
        }

        private async Task RecordSyncChange(
            RestaurantSyncEntityType entityType,
            Guid entityId,
            object payload,
            RestaurantSyncOperation operation = RestaurantSyncOperation.Upsert)
        {
            await RecordSyncChanges((entityType, entityId, payload, operation));
        }

        private async Task RecordSyncChanges(
            params (RestaurantSyncEntityType EntityType, Guid EntityId, object Payload, RestaurantSyncOperation Operation)[] changes)
        {
            var tenantId = AbpSession.GetTenantId();
            var nextSeq = await changeLogRepository.GetAll()
                .Where(x => x.TenantId == tenantId)
                .Select(x => (long?)x.Seq)
                .MaxAsync() ?? 0;

            var now = DateTime.Now;
            foreach (var change in changes)
            {
                nextSeq++;
                await changeLogRepository.InsertAsync(new RestaurantChangeLog
                {
                    TenantId = tenantId,
                    Seq = nextSeq,
                    EntityType = change.EntityType,
                    Operation = change.Operation,
                    EntityId = change.EntityId.ToString(),
                    PayloadJson = JsonSerializer.Serialize(change.Payload),
                    ChangedAt = now
                });
            }
        }
    }
}
