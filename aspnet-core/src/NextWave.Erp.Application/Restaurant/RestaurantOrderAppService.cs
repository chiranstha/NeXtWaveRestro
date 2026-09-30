using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
    public class RestaurantOrderAppService(
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantTableSession, Guid> tableSessionRepository,
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<RestaurantStation, Guid> stationRepository,
        IRepository<RestaurantTicket, Guid> ticketRepository,
        IRepository<RestaurantTicketItem, Guid> ticketItemRepository,
        IRepository<RestaurantChangeLog, Guid> changeLogRepository,
        IRepository<RestaurantMenuVariant, Guid> variantRepository,
        IRepository<RestaurantModifier, Guid> modifierRepository,
        IRepository<RestaurantOrderItemModifier, Guid> orderItemModifierRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        RestaurantPrintQueueService printQueueService,
        IRepository<RestaurantClientOperation, Guid> operationRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantOrderAppService
    {
        public async Task<RestaurantOrderDto> GetOrder(Guid id)
        {
            var order = await orderRepository.GetAll()
                .Include(x => x.TableFk)
                .Include(x => x.TableSessionFk)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == AbpSession.TenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");

            return await MapOrder(order);
        }

        public async Task<RestaurantOrderDto> GetOrderForPos(Guid id)
        {
            return await GetOrder(id);
        }

        public async Task<List<RestaurantOrderDto>> GetOpenOrders()
        {
            var orders = await orderRepository.GetAll()
                .Include(x => x.TableFk)
                .Include(x => x.TableSessionFk)
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.Status != RestaurantOrderStatus.Billed &&
                            x.Status != RestaurantOrderStatus.Closed &&
                            x.Status != RestaurantOrderStatus.Cancelled)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = new List<RestaurantOrderDto>();
            foreach (var order in orders)
                result.Add(await MapOrder(order));
            return result;
        }

        public async Task<List<RestaurantOrderDto>> GetOpenOrdersForPos()
        {
            return await GetOpenOrders();
        }

        public async Task<RestaurantOperationStatusDto> GetOperationStatus(string operationType, string clientRequestId)
        {
            if (string.IsNullOrWhiteSpace(operationType) || operationType.Trim().Length > 80 ||
                string.IsNullOrWhiteSpace(clientRequestId) || clientRequestId.Trim().Length > 100)
                throw new UserFriendlyException("A valid operation type and request ID are required");
            var tenantId = AbpSession.GetTenantId();
            var requestId = clientRequestId.Trim();
            var type = operationType.Trim();
            var operation = await FindOperation(tenantId, type, requestId);
            if (operation != null)
                return new RestaurantOperationStatusDto
                {
                    OperationType = type,
                    ClientRequestId = requestId,
                    Status = "Completed",
                    EntityId = operation.EntityId,
                    ResultJson = operation.ResultJson
                };

            if (string.Equals(type, "OrderUpsert", StringComparison.OrdinalIgnoreCase))
            {
                var createdOrder = await orderRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.PosClientRequestId == requestId &&
                    x.WaiterUserId == AbpSession.GetUserId());
                if (createdOrder != null)
                    return new RestaurantOperationStatusDto
                    {
                        OperationType = type,
                        ClientRequestId = requestId,
                        Status = "Completed",
                        EntityId = createdOrder.Id,
                        ResultJson = JsonSerializer.Serialize(new { EntityId = createdOrder.Id })
                    };
            }

            return new RestaurantOperationStatusDto
            {
                OperationType = type,
                ClientRequestId = requestId,
                Status = "NotFound"
            };
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
        public async Task<Guid> CreateOrEditOrder(CreateOrEditRestaurantOrderDto input)
        {
            if (input.Items == null || input.Items.All(x => x.Qty <= 0))
                throw new UserFriendlyException("At least one order item is required");

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var tenantId = AbpSession.GetTenantId();
            RestaurantOrder order;
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required for this order update");

            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                var clientRequestId = input.ClientRequestId?.Trim();
                var requestHash = string.IsNullOrWhiteSpace(clientRequestId) ? null : HashPosOrderRequest(input);
                if (!string.IsNullOrWhiteSpace(clientRequestId))
                {
                    if (clientRequestId.Length > 100)
                        throw new UserFriendlyException("Order request ID is too long");
                    var priorOrder = await orderRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == tenantId && x.PosClientRequestId == clientRequestId);
                    if (priorOrder != null)
                    {
                        if (!string.Equals(priorOrder.ClientPayloadHash, requestHash, StringComparison.Ordinal))
                            throw new UserFriendlyException("This order request ID was already used for different order details");
                        await uow.CompleteAsync();
                        return priorOrder.Id;
                    }
                }

                var session = await EnsureTableSession(input, tenantId);
                order = new RestaurantOrder
                {
                    TenantId = tenantId,
                    OrderNo = await GetNextOrderNo(),
                    PosClientRequestId = string.IsNullOrWhiteSpace(clientRequestId) ? null : clientRequestId,
                    ClientPayloadHash = requestHash,
                    CreatedAt = DateTime.Now,
                    TableSessionId = session?.Id
                };
                order.Id = await orderRepository.InsertAndGetIdAsync(order);
            }
            else
            {
                order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (order == null) throw new UserFriendlyException("Restaurant order not found");
                var existingOperation = await FindOperation(tenantId, "OrderUpsert", input.ClientRequestId);
                if (existingOperation != null)
                {
                    EnsureOperationHash(existingOperation, HashPosOrderRequest(input));
                    await uow.CompleteAsync();
                    return existingOperation.EntityId ?? order.Id;
                }
                await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
                if (order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                    throw new UserFriendlyException("Billed or closed orders cannot be edited");

                var oldItems = await orderItemRepository.GetAll()
                    .Where(x => x.TenantId == tenantId && x.OrderId == order.Id &&
                                x.Status == RestaurantOrderItemStatus.Draft)
                    .ToListAsync();
                var oldItemIds = oldItems.Select(x => x.Id).ToList();
                var oldModifiers = await orderItemModifierRepository.GetAll()
                    .Where(x => x.TenantId == tenantId && oldItemIds.Contains(x.OrderItemId))
                    .ToListAsync();
                foreach (var oldModifier in oldModifiers)
                    await orderItemModifierRepository.DeleteAsync(oldModifier);
                foreach (var oldItem in oldItems)
                    await orderItemRepository.DeleteAsync(oldItem);
            }

            order.OrderType = input.OrderType;
            order.TableId = input.TableId;
            order.DeviceId = input.DeviceId;
            order.Source = input.Source;
            order.ClientRequestId = input.ClientRequestId;
            order.WaiterUserId = input.WaiterUserId ?? AbpSession.UserId;
            order.CustomerName = input.CustomerName;
            order.CustomerPhoneNo = input.CustomerPhoneNo;
            order.Notes = input.Notes;

            var protectedItemIds = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.OrderId == order.Id &&
                            x.Status != RestaurantOrderItemStatus.Draft)
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var itemInput in input.Items.Where(x => x.Qty > 0))
            {
                if (itemInput.Id.HasValue &&
                    itemInput.Id.Value != Guid.Empty &&
                    protectedItemIds.Contains(itemInput.Id.Value))
                    continue;

                await CreateOrderItem(order.Id, itemInput, tenantId);
            }

            await RecalculateOrderTotals(order.Id);

            if (input.TableId.HasValue)
            {
                var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.TableId && x.TenantId == tenantId);
                if (table != null)
                {
                    table.Status = RestaurantTableStatus.Occupied;
                    await tableRepository.UpdateAsync(table);
                }
            }

            await RecordSensitiveAction(
                "OrderUpsert",
                RestaurantSyncEntityType.Order,
                order.Id,
                null,
                new { order.OrderNo, order.OrderType, order.Status, ItemCount = input.Items.Count });
            if (input.Id.HasValue && !string.IsNullOrWhiteSpace(input.ClientRequestId))
                await RecordOperation(tenantId, "OrderUpsert", input.ClientRequestId, HashPosOrderRequest(input), order.Id);
            await uow.CompleteAsync();
            return order.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPosDiscount)]
        public async Task ApplyOrderDiscount(ApplyRestaurantOrderDiscountDto input)
        {
            if (input.DiscountAmount < 0)
                throw new UserFriendlyException("Discount cannot be negative");

            var tenantId = AbpSession.GetTenantId();
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required for this discount update");
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null)
                throw new UserFriendlyException("Restaurant order not found");
            if (order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                throw new UserFriendlyException("Billed or closed orders cannot be discounted");
            var operationHash = HashOrderMutation(input);
            var priorOperation = await FindOperation(tenantId, "OrderDiscount", input.ClientRequestId);
            if (priorOperation != null)
            {
                EnsureOperationHash(priorOperation, operationHash);
                await uow.CompleteAsync();
                return;
            }
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);

            var items = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.OrderId == input.OrderId &&
                            x.Status != RestaurantOrderItemStatus.Cancelled)
                .ToListAsync();
            if (items.Count == 0)
                throw new UserFriendlyException("Cannot discount an empty order");

            var grossTotal = items.Sum(GetLineGross);
            if (input.DiscountAmount > grossTotal)
                throw new UserFriendlyException("Discount cannot exceed order gross amount");

            await ValidateSensitiveActionApproval("Discount", input.ApprovalPin);

            decimal assigned = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var lineGross = GetLineGross(item);
                var discount = i == items.Count - 1
                    ? input.DiscountAmount - assigned
                    : Math.Round(input.DiscountAmount * lineGross / grossTotal, 2);
                assigned += discount;

                var taxRate = item.TaxId == Guid.Empty ? 0 : ERPCommonManager.GetTaxRate(item.TaxId);
                var net = Math.Max(0, lineGross - discount);
                item.DiscountAmount = discount;
                item.NetAmount = net;
                item.TaxAmount = net * taxRate;
                item.Amount = item.NetAmount + item.TaxAmount;
                await orderItemRepository.UpdateAsync(item);
            }

            await RecalculateOrderTotals(order.Id);
            await RecordSensitiveAction(
                "Discount",
                RestaurantSyncEntityType.Order,
                order.Id,
                input.ApprovalNote,
                new { order.OrderNo, input.DiscountAmount });
            if (!string.IsNullOrWhiteSpace(input.ClientRequestId))
                await RecordOperation(tenantId, "OrderDiscount", input.ClientRequestId, operationHash, order.Id);
            await uow.CompleteAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantKotBot)]
        public async Task SendToKitchen(RestaurantOrderMutationDto input)
        {
            if (input == null || input.OrderId == Guid.Empty)
                throw new UserFriendlyException("Restaurant order not found");
            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required when sending an order to kitchen");
            if (!string.IsNullOrWhiteSpace(input.ClientRequestId) && input.ClientRequestId.Trim().Length > 100)
                throw new UserFriendlyException("Order request ID is too long");

            var requestHash = HashOrderMutation(new { input.OrderId, input.ExpectedOrderVersion });
            var priorOperation = await FindOperation(tenantId, "OrderKitchenSend", input.ClientRequestId);
            if (priorOperation != null)
            {
                EnsureOperationHash(priorOperation, requestHash);
                await uow.CompleteAsync();
                return;
            }

            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
            if (order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                throw new UserFriendlyException("This order cannot be sent to kitchen/bar");

            var items = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.OrderId == order.Id &&
                            x.Status == RestaurantOrderItemStatus.Draft &&
                            x.Qty > 0)
                .ToListAsync();

            if (items.Count == 0)
            {
                await RecordOperation(tenantId, "OrderKitchenSend", input.ClientRequestId, requestHash, order.Id);
                await uow.CompleteAsync();
                return;
            }

            var kitchenItems = items.Where(x => x.StationId.HasValue).ToList();
            if (kitchenItems.Count == 0)
            {
                await RecordOperation(tenantId, "OrderKitchenSend", input.ClientRequestId, requestHash, order.Id);
                await uow.CompleteAsync();
                return;
            }

            var hasPriorTickets = await ticketRepository.CountAsync(x =>
                x.TenantId == tenantId &&
                x.OrderId == order.Id &&
                x.Purpose != RestaurantTicketPurpose.Cancellation) > 0;
            var purpose = hasPriorTickets ? RestaurantTicketPurpose.AddOn : RestaurantTicketPurpose.NewOrder;
            var nextFireSequence = hasPriorTickets
                ? await ticketRepository.CountAsync(x => x.TenantId == tenantId && x.OrderId == order.Id) + 1
                : 1;

            var grouped = kitchenItems.GroupBy(x => x.StationId.Value).ToList();
            foreach (var group in grouped)
            {
                var station = await stationRepository.FirstOrDefaultAsync(x => x.Id == group.Key && x.TenantId == tenantId);
                if (station == null) throw new UserFriendlyException("Station not found");

                var ticketType = GetTicketType(station);

                var ticket = new RestaurantTicket
                {
                    TenantId = tenantId,
                    OrderId = order.Id,
                    StationId = station.Id,
                    TicketType = ticketType,
                    Purpose = purpose,
                    TicketNo = await GetNextTicketNo(ticketType),
                    Status = RestaurantTicketStatus.Pending,
                    SentAt = DateTime.Now
                };

                var ticketId = await ticketRepository.InsertAndGetIdAsync(ticket);
                foreach (var item in group)
                {
                    item.Status = RestaurantOrderItemStatus.Sent;
                    item.FireSequence = nextFireSequence;
                    await orderItemRepository.UpdateAsync(item);
                    await ticketItemRepository.InsertAsync(new RestaurantTicketItem
                    {
                        TenantId = tenantId,
                        TicketId = ticketId,
                        OrderItemId = item.Id,
                        Qty = item.Qty,
                        Status = RestaurantOrderItemStatus.Sent
                    });
                }

                var ticketItems = group.ToList();
                var routeName = string.IsNullOrWhiteSpace(station.PrintRouteName) ? "unconfigured" : station.PrintRouteName.Trim();
                var payload = BuildKitchenTicketPayload(order, ticket, station, ticketItems);
                await printQueueService.QueueAsync(new RestaurantPrintJob
                {
                    ExternalJobId = "KOT-" + ticketId.ToString("N"),
                    Type = RestaurantPrintJobType.KitchenTicket,
                    TicketId = ticketId,
                    OrderId = order.Id,
                    StationId = station.Id,
                    RouteName = routeName,
                    Payload = payload,
                    CreatedAtUtc = DateTime.UtcNow
                }, tenantId);
            }

            order.Status = RestaurantOrderStatus.SentToKitchen;
            order.SentAt = DateTime.Now;
            await orderRepository.UpdateAsync(order);
            await RecordSensitiveAction(
                "SendToKitchen",
                RestaurantSyncEntityType.Order,
                order.Id,
                null,
                new { order.OrderNo, order.Status, purpose, TicketCount = grouped.Count });
            await RecordOperation(tenantId, "OrderKitchenSend", input.ClientRequestId, requestHash, order.Id);
            await uow.CompleteAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantKotBot)]
        public async Task CancelItem(VoidRestaurantOrderItemDto input)
        {
            await VoidOrderItem(input);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantKotBot)]
        public async Task CancelTicket(CancelRestaurantTicketDto input)
        {
            if (input == null || input.TicketId == Guid.Empty)
                throw new UserFriendlyException("Ticket not found");

            var reason = (input.Reason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reason))
                throw new UserFriendlyException("Cancel reason is required");

            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var ticket = await ticketRepository.FirstOrDefaultAsync(x => x.Id == input.TicketId && x.TenantId == tenantId);
            if (ticket == null) throw new UserFriendlyException("Ticket not found");
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == ticket.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required when cancelling a kitchen ticket");
            var cancelHash = HashOrderMutation(new { input.TicketId, Reason = reason, input.ExpectedOrderVersion });
            var priorCancellation = await FindOperation(tenantId, "OrderCancelTicket", input.ClientRequestId);
            if (priorCancellation != null)
            {
                EnsureOperationHash(priorCancellation, cancelHash);
                await uow.CompleteAsync();
                return;
            }
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
            if (ticket.Purpose == RestaurantTicketPurpose.Cancellation)
                throw new UserFriendlyException("Cancellation tickets cannot be deleted");
            if (ticket.Status == RestaurantTicketStatus.Cancelled)
                throw new UserFriendlyException("Ticket is already cancelled");
            if (ticket.Status == RestaurantTicketStatus.Served)
                throw new UserFriendlyException("Served tickets cannot be deleted");

            var ticketItems = await ticketItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.TicketId == ticket.Id)
                .ToListAsync();

            var orderItemIds = ticketItems.Select(x => x.OrderItemId).ToList();
            var orderItems = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && orderItemIds.Contains(x.Id))
                .ToListAsync();

            if (orderItems.Any(x => x.Status == RestaurantOrderItemStatus.Served))
                throw new UserFriendlyException("Served ticket items cannot be deleted");

            await ValidateSensitiveActionApproval("CancelTicket", input.ApprovalPin);

            ticket.Status = RestaurantTicketStatus.Cancelled;
            ticket.CancelledAt = DateTime.Now;
            ticket.CancelReason = reason;
            await ticketRepository.UpdateAsync(ticket);

            foreach (var ticketItem in ticketItems)
            {
                ticketItem.Status = RestaurantOrderItemStatus.Cancelled;
                ticketItem.CancelReason = ticket.CancelReason;
                await ticketItemRepository.UpdateAsync(ticketItem);
            }

            foreach (var orderItem in orderItems)
            {
                orderItem.Status = RestaurantOrderItemStatus.Cancelled;
                orderItem.CancelReason = ticket.CancelReason;
                await orderItemRepository.UpdateAsync(orderItem);
            }

            await RecalculateOrderTotals(ticket.OrderId);
            await RefreshOrderStatus(ticket.OrderId);
            await RecordSensitiveAction(
                "CancelTicket",
                RestaurantSyncEntityType.Ticket,
                ticket.Id,
                input.ApprovalNote,
                new { ticket.TicketNo, ticket.OrderId, Reason = reason });
            await RecordOperation(tenantId, "OrderCancelTicket", input.ClientRequestId, cancelHash, order.Id);
            await uow.CompleteAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantKotBot)]
        public async Task VoidOrderItem(VoidRestaurantOrderItemDto input)
        {
            if (input == null || input.OrderItemId == Guid.Empty)
                throw new UserFriendlyException("Order item not found");
            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required when voiding an order item");
            var item = await orderItemRepository.GetAll()
                .Include(x => x.OrderFk)
                .FirstOrDefaultAsync(x => x.Id == input.OrderItemId && x.TenantId == tenantId);
            if (item == null) throw new UserFriendlyException("Order item not found");
            var requestHash = HashOrderMutation(new { input.OrderItemId, Reason = input.Reason ?? string.Empty, input.ExpectedOrderVersion });
            var priorVoid = await FindOperation(tenantId, "OrderVoidItem", input.ClientRequestId);
            if (priorVoid != null)
            {
                EnsureOperationHash(priorVoid, requestHash);
                await uow.CompleteAsync();
                return;
            }
            await EnsureOrderVersion(item.OrderFk, input.ExpectedOrderVersion, versionChecksEnabled);
            if (item.OrderFk.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed)
                throw new UserFriendlyException("Billed order items cannot be cancelled");
            if (item.Status == RestaurantOrderItemStatus.Cancelled)
            {
                await RecordOperation(tenantId, "OrderVoidItem", input.ClientRequestId, requestHash, item.OrderId);
                await uow.CompleteAsync();
                return;
            }
            if (item.Status == RestaurantOrderItemStatus.Served)
                throw new UserFriendlyException("Served order items cannot be voided from KOT/BOT");
            if (item.Status != RestaurantOrderItemStatus.Draft && string.IsNullOrWhiteSpace(input.Reason))
                throw new UserFriendlyException("Void reason is required for sent KOT/BOT items");

            await ValidateSensitiveActionApproval("VoidOrderItem", input.ApprovalPin);

            var previousStatus = item.Status;
            item.Status = RestaurantOrderItemStatus.Cancelled;
            item.CancelReason = input.Reason;
            await orderItemRepository.UpdateAsync(item);

            if (previousStatus != RestaurantOrderItemStatus.Draft)
                await CreateCancellationTicket(item, input.Reason);

            await RecalculateOrderTotals(item.OrderId);
            await RefreshOrderStatus(item.OrderId);
            await RecordSensitiveAction(
                "VoidOrderItem",
                RestaurantSyncEntityType.OrderItem,
                item.Id,
                input.ApprovalNote,
                new { item.OrderId, item.ItemNameSnapshot, item.Qty, Reason = input.Reason });
            await RecordOperation(tenantId, "OrderVoidItem", input.ClientRequestId, requestHash, item.OrderId);
            await uow.CompleteAsync();
        }

        public async Task<List<RestaurantTicketDto>> GetTicketsForOrder(EntityDto<Guid> input)
        {
            var tickets = await ticketRepository.GetAll()
                .Include(x => x.OrderFk)
                .ThenInclude(x => x.TableFk)
                .Include(x => x.StationFk)
                .Where(x => x.TenantId == AbpSession.TenantId && x.OrderId == input.Id)
                .OrderBy(x => x.SentAt)
                .ToListAsync();

            var result = new List<RestaurantTicketDto>();
            foreach (var ticket in tickets)
                result.Add(await MapTicket(ticket));
            return result;
        }

        public async Task<RestaurantTicketDto> GetTicketForPrint(EntityDto<Guid> input)
        {
            var ticket = await GetTicketEntity(input.Id);
            var isReprint = ticket.PrintCount > 0;
            await RegisterTicketPrint(ticket);
            return await MapTicket(ticket, isReprint);
        }

        public async Task MarkTicketPrinted(EntityDto<Guid> input)
        {
            var ticket = await GetTicketEntity(input.Id);
            var now = DateTime.Now;
            ticket.LastPrintConfirmedAt = now;
            ticket.PrintedAt ??= now;
            ticket.LastPrintedAt = now;
            await ticketRepository.UpdateAsync(ticket);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantKotBotReprint)]
        public async Task<RestaurantTicketDto> ReprintTicket(ReprintRestaurantTicketDto input)
        {
            if (string.IsNullOrWhiteSpace(input?.ApprovalNote))
                throw new UserFriendlyException("A reason is required to reprint a ticket");
            await ValidateSensitiveActionApproval("Reprint ticket", input.ApprovalPin);
            var ticket = await GetTicketEntity(input.TicketId);
            var isReprint = ticket.PrintCount > 0;
            await RegisterTicketPrint(ticket);
            await QueueAuditedTicketReprint(ticket, input.ApprovalNote.Trim());
            await RecordSensitiveAction(
                "ReprintTicket",
                RestaurantSyncEntityType.Ticket,
                ticket.Id,
                input.ApprovalNote,
                new { ticket.TicketNo, ticket.OrderId, ticket.PrintCount });
            return await MapTicket(ticket, isReprint);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPosTableTransfer)]
        public async Task TransferTable(TransferRestaurantTableDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required for this table transfer");
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null) throw new UserFriendlyException("Restaurant order not found");
            var transferHash = HashOrderMutation(input);
            var existingTransfer = await FindOperation(tenantId, "TransferTable", input.ClientRequestId);
            if (existingTransfer != null)
            {
                EnsureOperationHash(existingTransfer, transferHash);
                await uow.CompleteAsync();
                return;
            }
            await EnsureOrderVersion(order, input.ExpectedOrderVersion, versionChecksEnabled);
            if (order.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                throw new UserFriendlyException("Billed, closed, or cancelled orders cannot be transferred");
            if (order.TableId == input.NewTableId)
                return;

            if (await tableRepository.CountAsync(x => x.Id == input.NewTableId && x.TenantId == tenantId && x.IsActive) == 0)
                throw new UserFriendlyException("Target table not found");
            if (await HasOpenOrderOnTable(input.NewTableId, order.Id))
                throw new UserFriendlyException("Target table already has an open order. Use merge instead.");

            var oldTableId = order.TableId;
            order.TableId = input.NewTableId;
            await orderRepository.UpdateAsync(order);

            if (order.TableSessionId.HasValue)
            {
                var session = await tableSessionRepository.FirstOrDefaultAsync(x =>
                    x.Id == order.TableSessionId.Value &&
                    x.TenantId == tenantId);
                if (session != null)
                {
                    session.TableId = input.NewTableId;
                    await tableSessionRepository.UpdateAsync(session);
                }
            }

            if (oldTableId.HasValue && !await HasOpenOrderOnTable(oldTableId.Value, order.Id))
            {
                var oldTable = await tableRepository.FirstOrDefaultAsync(oldTableId.Value);
                if (oldTable != null)
                {
                    oldTable.Status = RestaurantTableStatus.Available;
                    await tableRepository.UpdateAsync(oldTable);
                }
            }

            var newTable = await tableRepository.FirstOrDefaultAsync(input.NewTableId);
            if (newTable != null)
            {
                newTable.Status = RestaurantTableStatus.Occupied;
                await tableRepository.UpdateAsync(newTable);
            }

            await RecordSensitiveAction(
                "TransferTable",
                RestaurantSyncEntityType.Order,
                order.Id,
                null,
                new { order.OrderNo, OldTableId = oldTableId, NewTableId = input.NewTableId });
            if (!string.IsNullOrWhiteSpace(input.ClientRequestId))
                await RecordOperation(tenantId, "TransferTable", input.ClientRequestId, transferHash, order.Id);
            await uow.CompleteAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPosSplitMerge)]
        public async Task<Guid> SplitOrder(SplitRestaurantOrderDto input)
        {
            if (input.OrderItemIds == null || input.OrderItemIds.Count == 0)
                throw new UserFriendlyException("Select items to split");

            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required for this split");
            var source = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.SourceOrderId && x.TenantId == tenantId);
            if (source == null) throw new UserFriendlyException("Source order not found");
            var splitHash = HashOrderMutation(input);
            var priorSplit = await FindOperation(tenantId, "SplitOrder", input.ClientRequestId);
            if (priorSplit != null)
            {
                EnsureOperationHash(priorSplit, splitHash);
                await uow.CompleteAsync();
                return priorSplit.EntityId ?? Guid.Empty;
            }
            await EnsureOrderVersion(source, input.ExpectedOrderVersion, versionChecksEnabled);
            if (source.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                throw new UserFriendlyException("Billed, closed, or cancelled orders cannot be split");
            if (input.NewTableId.HasValue &&
                await tableRepository.CountAsync(x => x.Id == input.NewTableId.Value && x.TenantId == tenantId && x.IsActive) == 0)
                throw new UserFriendlyException("Target table not found");
            if (input.NewTableId.HasValue && await HasOpenOrderOnTable(input.NewTableId.Value, source.Id))
                throw new UserFriendlyException("Target table already has an open order. Use merge instead.");

            var newOrder = new RestaurantOrder
            {
                TenantId = tenantId,
                OrderNo = await GetNextOrderNo(),
                OrderType = source.OrderType,
                Status = source.Status,
                TableId = input.NewTableId ?? source.TableId,
                TableSessionId = input.NewTableId.HasValue && input.NewTableId != source.TableId
                    ? null
                    : source.TableSessionId,
                WaiterUserId = source.WaiterUserId,
                CustomerName = source.CustomerName,
                CustomerPhoneNo = source.CustomerPhoneNo,
                CreatedAt = DateTime.Now,
                Notes = "Split from " + source.OrderNo
            };
            var newOrderId = await orderRepository.InsertAndGetIdAsync(newOrder);

            var items = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.OrderId == source.Id &&
                            input.OrderItemIds.Contains(x.Id))
                .ToListAsync();
            if (items.Count != input.OrderItemIds.Distinct().Count())
                throw new UserFriendlyException("One or more split items were not found");
            if (items.Any(x => x.Status == RestaurantOrderItemStatus.Cancelled))
                throw new UserFriendlyException("Cancelled items cannot be split");

            foreach (var item in items)
            {
                item.OrderId = newOrderId;
                await orderItemRepository.UpdateAsync(item);
            }

            await RecalculateOrderTotals(source.Id);
            await RecalculateOrderTotals(newOrderId);

            if (input.NewTableId.HasValue)
            {
                var table = await tableRepository.FirstOrDefaultAsync(input.NewTableId.Value);
                if (table != null)
                {
                    table.Status = RestaurantTableStatus.Occupied;
                    await tableRepository.UpdateAsync(table);
                }
            }

            await RefreshOrderStatus(source.Id);
            await RefreshOrderStatus(newOrderId);
            await RecordSensitiveAction(
                "SplitOrder",
                RestaurantSyncEntityType.Order,
                source.Id,
                null,
                new { source.OrderNo, NewOrderId = newOrderId, ItemIds = input.OrderItemIds });
            await RecordSensitiveAction(
                "SplitOrderCreated",
                RestaurantSyncEntityType.Order,
                newOrderId,
                null,
                new { SourceOrderId = source.Id, ItemIds = input.OrderItemIds });
            if (!string.IsNullOrWhiteSpace(input.ClientRequestId))
                await RecordOperation(tenantId, "SplitOrder", input.ClientRequestId, splitHash, newOrderId);
            await uow.CompleteAsync();
            return newOrderId;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPosSplitMerge)]
        public async Task MergeOrders(MergeRestaurantOrdersDto input)
        {
            if (input.SourceOrderIds == null || input.SourceOrderIds.Count == 0)
                return;

            var tenantId = AbpSession.GetTenantId();
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var versionChecksEnabled = await IsOrderVersionChecksEnabled(tenantId);
            if (versionChecksEnabled && string.IsNullOrWhiteSpace(input.ClientRequestId))
                throw new UserFriendlyException("A unique request ID is required for this merge");
            var mergeHash = HashOrderMutation(input);
            var priorMerge = await FindOperation(tenantId, "MergeOrders", input.ClientRequestId);
            if (priorMerge != null)
            {
                EnsureOperationHash(priorMerge, mergeHash);
                await uow.CompleteAsync();
                return;
            }
            var sourceOrderIds = input.SourceOrderIds.Where(x => x != Guid.Empty && x != input.TargetOrderId).Distinct().ToList();
            if (sourceOrderIds.Count == 0)
                return;

            var target = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.TargetOrderId && x.TenantId == tenantId);
            if (target == null) throw new UserFriendlyException("Target order not found");
            await EnsureOrderVersion(target, input.ExpectedOrderVersions?.GetValueOrDefault(target.Id), versionChecksEnabled);
            if (target.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled)
                throw new UserFriendlyException("Billed, closed, or cancelled orders cannot be merged");

            var sourceItems = await orderItemRepository.GetAll()
                .Where(x => x.TenantId == tenantId && sourceOrderIds.Contains(x.OrderId))
                .ToListAsync();

            foreach (var item in sourceItems)
            {
                item.OrderId = target.Id;
                await orderItemRepository.UpdateAsync(item);
            }

            var sources = await orderRepository.GetAll()
                .Where(x => x.TenantId == tenantId && sourceOrderIds.Contains(x.Id))
                .ToListAsync();
            if (sources.Count != sourceOrderIds.Count)
                throw new UserFriendlyException("One or more source orders were not found");
            if (sources.Any(x => x.Status is RestaurantOrderStatus.Billed or RestaurantOrderStatus.Closed or RestaurantOrderStatus.Cancelled))
                throw new UserFriendlyException("Billed, closed, or cancelled source orders cannot be merged");
            foreach (var source in sources)
                await EnsureOrderVersion(source, input.ExpectedOrderVersions?.GetValueOrDefault(source.Id), versionChecksEnabled);

            foreach (var source in sources)
            {
                source.Status = RestaurantOrderStatus.Cancelled;
                await orderRepository.UpdateAsync(source);

                if (source.TableId.HasValue && source.TableId != target.TableId && !await HasOpenOrderOnTable(source.TableId.Value, source.Id))
                {
                    var sourceTable = await tableRepository.FirstOrDefaultAsync(source.TableId.Value);
                    if (sourceTable != null)
                    {
                        sourceTable.Status = RestaurantTableStatus.Available;
                        await tableRepository.UpdateAsync(sourceTable);
                    }
                }
            }

            await RecalculateOrderTotals(target.Id);
            await RefreshOrderStatus(target.Id);
            await RecordSensitiveAction(
                "MergeOrders",
                RestaurantSyncEntityType.Order,
                target.Id,
                null,
                new { target.OrderNo, SourceOrderIds = sourceOrderIds });
            if (!string.IsNullOrWhiteSpace(input.ClientRequestId))
                await RecordOperation(tenantId, "MergeOrders", input.ClientRequestId, mergeHash, target.Id);
            await uow.CompleteAsync();
        }

        private async Task<RestaurantTableSession> EnsureTableSession(CreateOrEditRestaurantOrderDto input, int tenantId)
        {
            if (!input.TableId.HasValue || input.OrderType != RestaurantOrderType.DineIn)
                return null;

            var session = await tableSessionRepository.GetAll()
                .Where(x => x.TenantId == tenantId &&
                            x.TableId == input.TableId &&
                            x.ClosedAt == null &&
                            x.Status != RestaurantOrderStatus.Closed &&
                            x.Status != RestaurantOrderStatus.Cancelled)
                .OrderByDescending(x => x.OpenedAt)
                .FirstOrDefaultAsync();

            if (session != null) return session;

            session = new RestaurantTableSession
            {
                TenantId = tenantId,
                TableId = input.TableId,
                SessionNo = "TS-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Status = RestaurantOrderStatus.Draft,
                OpenedAt = DateTime.Now,
                GuestCount = input.GuestCount,
                WaiterUserId = input.WaiterUserId ?? AbpSession.UserId,
                CustomerName = input.CustomerName,
                CustomerPhoneNo = input.CustomerPhoneNo
            };
            session.Id = await tableSessionRepository.InsertAndGetIdAsync(session);
            return session;
        }

        private async Task CreateOrderItem(Guid orderId, CreateOrEditRestaurantOrderItemDto input, int tenantId)
        {
            Product product;
            Guid? stationId = input.StationId;
            Guid productId = input.ProductId;
            Guid unitId = input.UnitId;
            Guid taxId = input.TaxId;
            decimal rate = input.Rate;
            string itemNameSnapshot;
            string variantNameSnapshot = null;
            RestaurantStationType? stationTypeSnapshot = null;

            if (input.MenuItemId.HasValue && input.MenuItemId != Guid.Empty)
            {
                var menuItem = await menuItemRepository.GetAll()
                    .Include(x => x.ProductFk)
                    .Include(x => x.StationFk)
                    .FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
                if (menuItem == null) throw new UserFriendlyException("Menu item not found");
                if (!menuItem.IsActive)
                    throw new UserFriendlyException("Menu item is inactive");
                if (!menuItem.IsAvailable ||
                    (menuItem.UnavailableUntil.HasValue && menuItem.UnavailableUntil.Value > DateTime.Now))
                    throw new UserFriendlyException("Menu item is currently sold out", menuItem.DisplayName);

                product = menuItem.ProductFk;
                productId = menuItem.ProductId;
                unitId = product.UnitId;
                taxId = product.TaxId;
                stationId = menuItem.StationId;
                stationTypeSnapshot = menuItem.StationFk?.StationType;
                rate = input.Rate > 0 ? input.Rate : menuItem.Price;
                itemNameSnapshot = string.IsNullOrWhiteSpace(menuItem.DisplayName) ? product.Name : menuItem.DisplayName;

                if (input.VariantId.HasValue && input.VariantId != Guid.Empty)
                {
                    var variant = await variantRepository.FirstOrDefaultAsync(x =>
                        x.Id == input.VariantId &&
                        x.TenantId == tenantId &&
                        x.MenuItemId == menuItem.Id &&
                        x.IsActive &&
                        !x.IsDeleted);
                    if (variant == null)
                        throw new UserFriendlyException("Menu variant not found");

                    variantNameSnapshot = variant.Name;
                    if (input.Rate <= 0)
                        rate = variant.IsAbsolutePrice ? variant.PriceDelta : menuItem.Price + variant.PriceDelta;
                }
            }
            else
            {
                product = await productRepository.FirstOrDefaultAsync(x => x.Id == input.ProductId && x.TenantId == tenantId);
                if (product == null) throw new UserFriendlyException("Product not found");
                unitId = input.UnitId == Guid.Empty ? product.UnitId : input.UnitId;
                taxId = input.TaxId == Guid.Empty ? product.TaxId : input.TaxId;
                rate = input.Rate > 0 ? input.Rate : product.SalesRate;
                itemNameSnapshot = product.Name;
            }

            if (await unitRepository.CountAsync(x => x.Id == unitId && x.TenantId == tenantId) == 0)
                throw new UserFriendlyException("Unit not found");

            var modifierRows = await BuildModifierRows(input.Modifiers, tenantId);
            var modifierTotal = input.Qty * modifierRows.Sum(x => x.PriceDelta * x.Qty);
            var taxRate = taxId == Guid.Empty ? 0 : ERPCommonManager.GetTaxRate(taxId);
            var gross = input.Qty * rate + modifierTotal;
            var net = Math.Max(0, gross - input.DiscountAmount);
            var tax = net * taxRate;

            var orderItem = new RestaurantOrderItem
            {
                TenantId = tenantId,
                OrderId = orderId,
                MenuItemId = input.MenuItemId == Guid.Empty ? null : input.MenuItemId,
                VariantId = input.VariantId == Guid.Empty ? null : input.VariantId,
                ProductId = productId,
                UnitId = unitId,
                StationId = stationId,
                TaxId = taxId,
                Qty = input.Qty,
                Rate = rate,
                ItemNameSnapshot = itemNameSnapshot,
                VariantNameSnapshot = variantNameSnapshot,
                StationTypeSnapshot = stationTypeSnapshot,
                UnitPriceSnapshot = rate,
                ModifierTotal = modifierTotal,
                DiscountAmount = input.DiscountAmount,
                NetAmount = net,
                TaxAmount = tax,
                Amount = net + tax,
                Notes = input.Notes,
                Status = RestaurantOrderItemStatus.Draft,
                CreatedAt = DateTime.Now
            };

            var orderItemId = await orderItemRepository.InsertAndGetIdAsync(orderItem);
            foreach (var modifierRow in modifierRows)
            {
                modifierRow.OrderItemId = orderItemId;
                await orderItemModifierRepository.InsertAsync(modifierRow);
            }
        }

        private async Task RecalculateOrderTotals(Guid orderId)
        {
            await CurrentUnitOfWork.SaveChangesAsync();

            var order = await orderRepository.FirstOrDefaultAsync(orderId);
            if (order == null)
                throw new UserFriendlyException("Restaurant order not found");

            var items = await orderItemRepository.GetAll()
                .Where(x => x.OrderId == orderId && x.Status != RestaurantOrderItemStatus.Cancelled)
                .ToListAsync();

            order.GrossAmount = items.Sum(GetLineGross);
            order.DiscountAmount = items.Sum(x => x.DiscountAmount);
            order.TaxAmount = items.Sum(x => x.TaxAmount);
            order.NetAmount = items.Sum(x => x.NetAmount);
            order.GrandTotal = items.Sum(x => x.Amount);
            await orderRepository.UpdateAsync(order);
        }

        private async Task<RestaurantOrderDto> MapOrder(RestaurantOrder order)
        {
            var orderItems = await orderItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.UnitFk)
                .Include(x => x.StationFk)
                .Where(x => x.TenantId == AbpSession.TenantId && x.OrderId == order.Id)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            var orderItemIds = orderItems.Select(x => x.Id).ToList();
            var billedQtyByItem = orderItemIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await billLineRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId && orderItemIds.Contains(x.OrderItemId))
                    .GroupBy(x => x.OrderItemId)
                    .Select(x => new { OrderItemId = x.Key, Qty = x.Sum(y => y.Qty) })
                    .ToDictionaryAsync(x => x.OrderItemId, x => x.Qty);

            var billLines = await billLineRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.OrderId == order.Id)
                .ToListAsync();

            var items = new List<RestaurantOrderItemDto>();
            foreach (var item in orderItems)
            {
                var modifiers = await MapOrderItemModifiers(item.Id);
                billedQtyByItem.TryGetValue(item.Id, out var billedQty);
                var unbilledQty = item.Status == RestaurantOrderItemStatus.Cancelled || billedQty >= item.Qty
                    ? 0
                    : item.Qty - billedQty;

                items.Add(new RestaurantOrderItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductFk.Name,
                    MenuItemId = item.MenuItemId,
                    VariantId = item.VariantId,
                    ItemNameSnapshot = string.IsNullOrWhiteSpace(item.ItemNameSnapshot) ? item.ProductFk.Name : item.ItemNameSnapshot,
                    VariantNameSnapshot = item.VariantNameSnapshot,
                    ModifierSummary = BuildModifierSummary(modifiers),
                    UnitId = item.UnitId,
                    UnitName = item.UnitFk.Name,
                    StationId = item.StationId,
                    StationName = item.StationFk == null ? "" : item.StationFk.Name,
                    TaxId = item.TaxId,
                    Qty = item.Qty,
                    BilledQty = billedQty,
                    UnbilledQty = unbilledQty,
                    Rate = item.Rate,
                    ModifierTotal = item.ModifierTotal,
                    DiscountAmount = item.DiscountAmount,
                    TaxAmount = item.TaxAmount,
                    NetAmount = item.NetAmount,
                    Amount = item.Amount,
                    Status = item.Status,
                    Notes = item.Notes,
                    CancelReason = item.CancelReason,
                    Modifiers = modifiers
                });
            }

            var billedGross = billLines.Sum(x => x.GrossAmount);
            var billedDiscount = billLines.Sum(x => x.DiscountAmount);
            var billedTax = billLines.Sum(x => x.TaxAmount);
            var billedNet = billLines.Sum(x => x.NetAmount);
            var billedGrand = billLines.Sum(x => x.Amount);

            return new RestaurantOrderDto
            {
                Id = order.Id,
                RowVersion = order.RowVersion == null ? null : Convert.ToBase64String(order.RowVersion),
                OrderNo = order.OrderNo,
                OrderType = order.OrderType,
                Status = order.Status,
                TableId = order.TableId,
                TableName = order.TableFk == null ? "" : order.TableFk.Name,
                CustomerName = order.CustomerName,
                CustomerPhoneNo = order.CustomerPhoneNo,
                CreatedAt = order.CreatedAt,
                SentAt = order.SentAt,
                TableSessionOpenedAt = order.TableSessionFk?.OpenedAt ?? order.CreatedAt,
                GrossAmount = order.GrossAmount,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                NetAmount = order.NetAmount,
                GrandTotal = order.GrandTotal,
                BilledGrossAmount = billedGross,
                BilledDiscountAmount = billedDiscount,
                BilledTaxAmount = billedTax,
                BilledNetAmount = billedNet,
                BilledGrandTotal = billedGrand,
                RemainingGrossAmount = Math.Max(0, order.GrossAmount - billedGross),
                RemainingDiscountAmount = Math.Max(0, order.DiscountAmount - billedDiscount),
                RemainingTaxAmount = Math.Max(0, order.TaxAmount - billedTax),
                RemainingNetAmount = Math.Max(0, order.NetAmount - billedNet),
                RemainingGrandTotal = Math.Max(0, order.GrandTotal - billedGrand),
                SalesMasterId = order.SalesMasterId,
                Items = items
            };
        }

        private async Task<List<RestaurantOrderItemModifier>> BuildModifierRows(
            List<CreateOrEditRestaurantOrderItemModifierDto> inputModifiers,
            int tenantId)
        {
            var rows = new List<RestaurantOrderItemModifier>();
            if (inputModifiers == null)
                return rows;

            foreach (var input in inputModifiers.Where(x => x.ModifierId != Guid.Empty && x.Qty > 0))
            {
                var modifier = await modifierRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.ModifierId &&
                    x.TenantId == tenantId &&
                    x.IsActive &&
                    !x.IsDeleted);
                if (modifier == null)
                    throw new UserFriendlyException("Modifier not found");

                rows.Add(new RestaurantOrderItemModifier
                {
                    TenantId = tenantId,
                    ModifierId = modifier.Id,
                    ModifierNameSnapshot = modifier.Name,
                    PriceDelta = modifier.PriceDelta,
                    Qty = input.Qty,
                    Amount = modifier.PriceDelta * input.Qty
                });
            }

            return rows;
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

        private static decimal GetLineGross(RestaurantOrderItem item)
        {
            return item.Qty * item.Rate + item.ModifierTotal;
        }

        private static string HashPosOrderRequest(CreateOrEditRestaurantOrderDto input)
        {
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input))));
        }

        private Task<string> GetNextOrderNo()
        {
            return Task.FromResult("RO-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant());
        }

        private Task<string> GetNextTicketNo(RestaurantTicketType ticketType)
        {
            var prefix = ticketType + "-" + DateTime.Now.ToString("yyyyMMdd") + "-";
            return Task.FromResult(prefix + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant());
        }

        private async Task<bool> HasOpenOrderOnTable(Guid tableId, Guid exceptOrderId)
        {
            return await orderRepository.CountAsync(x =>
                x.TenantId == AbpSession.TenantId &&
                x.Id != exceptOrderId &&
                x.TableId == tableId &&
                x.Status != RestaurantOrderStatus.Billed &&
                x.Status != RestaurantOrderStatus.Closed &&
                x.Status != RestaurantOrderStatus.Cancelled) > 0;
        }

        private async Task<bool> IsOrderVersionChecksEnabled(int tenantId) =>
            string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantOrderVersionChecksEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);

        private async Task EnsureOrderVersion(RestaurantOrder order, string expectedVersion, bool enabled)
        {
            if (!enabled) return;
            var latest = await MapOrder(order);
            var current = order.RowVersion == null ? null : Convert.ToBase64String(order.RowVersion);
            if (string.IsNullOrWhiteSpace(expectedVersion) || !string.Equals(current, expectedVersion, StringComparison.Ordinal))
            {
                var error = JsonSerializer.Serialize(new
                {
                    Code = "Restaurant.OrderConflict",
                    LatestOrder = latest
                });
                throw new UserFriendlyException(
                    "This order changed on another device. Reload the latest order or reapply your changes.", error);
            }
        }

        private Task<RestaurantClientOperation> FindOperation(int tenantId, string type, string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return Task.FromResult<RestaurantClientOperation>(null);
            return operationRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                x.UserId == AbpSession.GetUserId() && x.ClientRequestId == requestId.Trim() && x.OperationType == type);
        }

        private async Task RecordOperation(int tenantId, string type, string requestId, string hash, Guid entityId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;
            if (requestId.Trim().Length > 100)
                throw new UserFriendlyException("Order request ID is too long");
            await operationRepository.InsertAsync(new RestaurantClientOperation
            {
                TenantId = tenantId,
                UserId = AbpSession.GetUserId(),
                ClientRequestId = requestId.Trim(),
                OperationType = type,
                RequestHash = hash,
                EntityId = entityId,
                ResultJson = JsonSerializer.Serialize(new { EntityId = entityId }),
                CreatedAt = DateTime.UtcNow
            });
        }

        private static void EnsureOperationHash(RestaurantClientOperation previous, string hash)
        {
            if (!string.Equals(previous.RequestHash, hash, StringComparison.Ordinal))
                throw new UserFriendlyException("This order request ID was already used for different changes");
        }

        private static string HashOrderMutation<T>(T input)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(input)));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static byte[] BuildKitchenTicketPayload(RestaurantOrder order, RestaurantTicket ticket, RestaurantStation station, List<RestaurantOrderItem> items)
        {
            var text = new StringBuilder("\u001b@\u001ba\u0001");
            text.AppendLine("NEXTWAVE ERP");
            text.AppendLine(station.Name ?? station.StationType.ToString());
            text.AppendLine(new string('-', 32));
            text.AppendLine($"{ticket.TicketNo}  {ticket.Purpose}");
            text.AppendLine($"Order: {order.OrderNo}");
            text.AppendLine($"Time: {ticket.SentAt:yyyy-MM-dd HH:mm}");
            text.AppendLine(new string('-', 32));
            foreach (var item in items)
            {
                text.AppendLine($"{item.Qty:0.##} x {item.ItemNameSnapshot}");
                if (!string.IsNullOrWhiteSpace(item.VariantNameSnapshot)) text.AppendLine("  " + item.VariantNameSnapshot);
                if (!string.IsNullOrWhiteSpace(item.Notes)) text.AppendLine("  NOTE: " + item.Notes);
            }
            text.AppendLine();
            text.Append("\u001dV\u0000");
            return Encoding.UTF8.GetBytes(text.ToString());
        }

        private async Task QueueAuditedTicketReprint(RestaurantTicket ticket, string reason)
        {
            var station = await stationRepository.FirstOrDefaultAsync(x => x.Id == ticket.StationId && x.TenantId == AbpSession.TenantId);
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == ticket.OrderId && x.TenantId == AbpSession.TenantId);
            if (station == null || order == null) throw new UserFriendlyException("Ticket printer route could not be resolved");
            var ticketLines = await ticketItemRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.TicketId == ticket.Id)
                .Join(orderItemRepository.GetAll(), line => line.OrderItemId, item => item.Id,
                    (line, item) => new RestaurantOrderItem
                    {
                        Qty = line.Qty,
                        ItemNameSnapshot = item.ItemNameSnapshot,
                        VariantNameSnapshot = item.VariantNameSnapshot,
                        Notes = item.Notes
                    })
                .ToListAsync();
            var routeName = string.IsNullOrWhiteSpace(station.PrintRouteName) ? "unconfigured" : station.PrintRouteName.Trim();
            await printQueueService.QueueAsync(new RestaurantPrintJob
            {
                ExternalJobId = $"REPRINT-{ticket.Id:N}-{ticket.PrintCount}",
                Type = RestaurantPrintJobType.KitchenTicket,
                TicketId = ticket.Id,
                OrderId = order.Id,
                StationId = station.Id,
                RouteName = routeName,
                Payload = BuildKitchenTicketPayload(order, ticket, station, ticketLines),
                IsDeliberateReprint = true,
                ReprintReason = reason.Length > 500 ? reason[..500] : reason,
                CreatedAtUtc = DateTime.UtcNow
            }, AbpSession.GetTenantId());
        }

        private static RestaurantTicketType GetTicketType(RestaurantStation station)
        {
            return station.StationType == RestaurantStationType.Bar
                ? RestaurantTicketType.BOT
                : RestaurantTicketType.KOT;
        }

        private async Task CreateCancellationTicket(RestaurantOrderItem item, string reason)
        {
            if (!item.StationId.HasValue)
                throw new UserFriendlyException("Cancelled KOT/BOT item has no station");

            var station = await stationRepository.FirstOrDefaultAsync(x =>
                x.Id == item.StationId.Value &&
                x.TenantId == AbpSession.TenantId);
            if (station == null)
                throw new UserFriendlyException("Station not found");

            var relatedTicketItems = await ticketItemRepository.GetAll()
                .Include(x => x.TicketFk)
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.OrderItemId == item.Id &&
                            x.TicketFk.Purpose != RestaurantTicketPurpose.Cancellation)
                .ToListAsync();

            foreach (var relatedTicketItem in relatedTicketItems)
            {
                relatedTicketItem.Status = RestaurantOrderItemStatus.Cancelled;
                relatedTicketItem.CancelReason = reason;
                await ticketItemRepository.UpdateAsync(relatedTicketItem);
                await RefreshTicketStatus(relatedTicketItem.TicketId);
            }

            var ticketType = GetTicketType(station);
            var ticket = new RestaurantTicket
            {
                TenantId = AbpSession.GetTenantId(),
                OrderId = item.OrderId,
                StationId = station.Id,
                TicketType = ticketType,
                Purpose = RestaurantTicketPurpose.Cancellation,
                TicketNo = await GetNextTicketNo(ticketType),
                Status = RestaurantTicketStatus.Pending,
                SentAt = DateTime.Now,
                CancelReason = reason
            };

            var ticketId = await ticketRepository.InsertAndGetIdAsync(ticket);
            await ticketItemRepository.InsertAsync(new RestaurantTicketItem
            {
                TenantId = AbpSession.TenantId,
                TicketId = ticketId,
                OrderItemId = item.Id,
                Qty = item.Qty,
                Status = RestaurantOrderItemStatus.Cancelled,
                CancelReason = reason
            });
        }

        private async Task<RestaurantTicket> GetTicketEntity(Guid ticketId)
        {
            var ticket = await ticketRepository.GetAll()
                .Include(x => x.OrderFk)
                .ThenInclude(x => x.TableFk)
                .Include(x => x.StationFk)
                .FirstOrDefaultAsync(x => x.Id == ticketId && x.TenantId == AbpSession.TenantId);
            if (ticket == null)
                throw new UserFriendlyException("Ticket not found");

            return ticket;
        }

        private async Task<RestaurantTicketDto> MapTicket(RestaurantTicket ticket, bool isReprint = false)
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
                OrderRowVersion = ticket.OrderFk?.RowVersion == null ? null : Convert.ToBase64String(ticket.OrderFk.RowVersion),
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
                PrintStatus = ticket.PrintRequestedAt.HasValue &&
                    (!ticket.LastPrintConfirmedAt.HasValue || ticket.PrintRequestedAt > ticket.LastPrintConfirmedAt)
                    ? "Print requested"
                    : ticket.LastPrintConfirmedAt.HasValue ? "Printed" : "Not printed",
                LastPrintConfirmedAt = ticket.LastPrintConfirmedAt,
                PrintCount = ticket.PrintCount,
                IsReprint = isReprint,
                OrderNotes = ticket.OrderFk?.Notes,
                CancelReason = ticket.CancelReason,
                Items = items
            };
        }

        private async Task RegisterTicketPrint(RestaurantTicket ticket)
        {
            var now = DateTime.Now;
            ticket.PrintCount += 1;
            ticket.PrintRequestedAt = now;
            await ticketRepository.UpdateAsync(ticket);
        }

        private async Task ValidateSensitiveActionApproval(string actionName, string approvalPin)
        {
            var tenantId = AbpSession.GetTenantId();
            var requiresPin = string.Equals(
                await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions,
                    tenantId),
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (!requiresPin)
                return;

            var expectedPin = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantManagerPin,
                tenantId);
            if (string.IsNullOrWhiteSpace(expectedPin))
                throw new UserFriendlyException("Manager approval PIN is not configured");

            if (!RestaurantPinHasher.Verify(expectedPin, approvalPin, out var legacy))
                throw new UserFriendlyException($"{actionName} requires manager approval");
            if (legacy)
                await SettingManager.ChangeSettingForTenantAsync(tenantId,
                    AppSettings.ErpSettings.RestaurantManagerPin, RestaurantPinHasher.Hash(expectedPin.Trim()));
        }

        private async Task RecordSensitiveAction(
            string actionName,
            RestaurantSyncEntityType entityType,
            Guid entityId,
            string approvalNote,
            object details)
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
                Operation = RestaurantSyncOperation.Upsert,
                EntityId = entityId.ToString(),
                PayloadJson = JsonSerializer.Serialize(new
                {
                    Action = actionName,
                    UserId = AbpSession.UserId,
                    ApprovalNote = approvalNote,
                    Details = details
                }),
                ChangedAt = DateTime.Now
            });
        }

        private async Task RefreshTicketStatus(Guid ticketId)
        {
            var ticket = await ticketRepository.FirstOrDefaultAsync(ticketId);
            if (ticket == null || ticket.Purpose == RestaurantTicketPurpose.Cancellation)
                return;

            var items = await ticketItemRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.TicketId == ticketId)
                .ToListAsync();

            if (items.Count == 0)
                return;

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
                .Where(x => x.TenantId == AbpSession.TenantId && x.OrderId == orderId)
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
    }
}
