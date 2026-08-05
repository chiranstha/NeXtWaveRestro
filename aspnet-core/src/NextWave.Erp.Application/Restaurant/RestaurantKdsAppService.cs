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
        IRepository<RestaurantChangeLog, Guid> changeLogRepository)
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
            var ticket = await ticketRepository.FirstOrDefaultAsync(x => x.Id == input.TicketId && x.TenantId == AbpSession.TenantId);
            if (ticket == null) throw new UserFriendlyException("Ticket not found");
            ValidateTicketTransition(ticket.Status, input.Status);
            if (ticket.Purpose != RestaurantTicketPurpose.Cancellation &&
                input.Status == RestaurantTicketStatus.Cancelled &&
                string.IsNullOrWhiteSpace(input.CancelReason))
                throw new UserFriendlyException("Cancel reason is required");

            ApplyTicketStatus(ticket, input.Status, input.CancelReason);
            await ticketRepository.UpdateAsync(ticket);

            if (ticket.Purpose == RestaurantTicketPurpose.Cancellation)
                return;

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
        }

        public async Task UpdateTicketItemStatus(UpdateRestaurantTicketItemStatusDto input)
        {
            var ticketItem = await ticketItemRepository.GetAll()
                .Include(x => x.TicketFk)
                .FirstOrDefaultAsync(x => x.Id == input.TicketItemId && x.TenantId == AbpSession.TenantId);
            if (ticketItem == null) throw new UserFriendlyException("Ticket item not found");
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
