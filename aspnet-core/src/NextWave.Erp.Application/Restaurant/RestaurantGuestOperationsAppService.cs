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
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public class RestaurantGuestOperationsAppService(
        IRepository<RestaurantTable, Guid> tableRepository,
        IRepository<RestaurantStation, Guid> stationRepository,
        IRepository<RestaurantTableSession, Guid> sessionRepository,
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantReservation, Guid> reservationRepository,
        IRepository<RestaurantSmsOutbox, Guid> smsOutboxRepository,
        IRepository<RestaurantPrintJob, Guid> printJobRepository,
        IRepository<RestaurantTicket, Guid> ticketRepository,
        IRestaurantOrderAppService orderAppService,
        IAppConfigurationAccessor configurationAccessor)
        : ErpAppServiceBase, IRestaurantGuestOperationsAppService
    {
        [AbpAuthorize(AppPermissions.PagesRestaurantSetupEdit)]
        public async Task<RestaurantTableQrDto> GenerateTableQr(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId && !x.IsDeleted);
            if (table == null) throw new UserFriendlyException("Restaurant table not found");

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            table.QrTokenHash = HashToken(token);
            table.QrTokenUpdatedAt = DateTime.Now;
            await tableRepository.UpdateAsync(table);

            var rootAddress = configurationAccessor.Configuration["App:ClientRootAddress"]?.TrimEnd('/');
            var url = string.IsNullOrWhiteSpace(rootAddress)
                ? $"/guest/table/{token}"
                : $"{rootAddress}/guest/table/{token}";
            if (url.StartsWith("/guest/", StringComparison.Ordinal)) url = $"/guest/table/{token}";
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(8);
            return new RestaurantTableQrDto
            {
                TableId = table.Id,
                TableName = table.Name,
                Url = url,
                PngBase64 = Convert.ToBase64String(png),
                UpdatedAt = table.QrTokenUpdatedAt
            };
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantSetupEdit)]
        public async Task RevokeTableQr(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId && !x.IsDeleted);
            if (table == null) throw new UserFriendlyException("Restaurant table not found");
            table.QrTokenHash = null;
            table.QrTokenUpdatedAt = DateTime.UtcNow;
            await tableRepository.UpdateAsync(table);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
        public async Task<List<RestaurantGuestOrderQueueDto>> GetPendingGuestOrders()
        {
            var tenantId = AbpSession.GetTenantId();
            var orders = await orderRepository.GetAll()
                .Include(x => x.TableFk)
                .Where(x => x.TenantId == tenantId && x.Source == "TableQr" &&
                            x.GuestApprovalStatus == RestaurantGuestOrderApprovalStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
            var results = new List<RestaurantGuestOrderQueueDto>();
            foreach (var order in orders)
            {
                var items = await orderItemRepository.GetAll()
                    .Where(x => x.TenantId == tenantId && x.OrderId == order.Id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                results.Add(new RestaurantGuestOrderQueueDto
                {
                    OrderId = order.Id,
                    OrderNo = order.OrderNo,
                    TableId = order.TableId ?? Guid.Empty,
                    TableName = order.TableFk?.Name ?? "Table",
                    CreatedAt = order.CreatedAt,
                    GrandTotal = order.GrandTotal,
                    Lines = items.Select(x => new RestaurantCustomerQuoteLineDto
                    {
                        MenuItemId = x.MenuItemId ?? Guid.Empty,
                        VariantId = x.VariantId,
                        ItemName = x.ItemNameSnapshot,
                        VariantName = x.VariantNameSnapshot,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        ModifierTotal = x.ModifierTotal,
                        TaxAmount = x.TaxAmount,
                        Amount = x.Amount
                    }).ToList()
                });
            }
            return results;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
        public async Task ReviewGuestOrder(ReviewRestaurantGuestOrderDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var order = await orderRepository.FirstOrDefaultAsync(x => x.Id == input.OrderId && x.TenantId == tenantId);
            if (order == null || order.Source != "TableQr" ||
                order.GuestApprovalStatus != RestaurantGuestOrderApprovalStatus.Pending)
                throw new UserFriendlyException("Pending table QR order not found");

            if (input.Approve)
            {
                var sessionIsOpen = order.TableSessionId.HasValue && await sessionRepository.CountAsync(x =>
                    x.Id == order.TableSessionId.Value && x.TenantId == tenantId && x.ClosedAt == null &&
                    x.Status != RestaurantOrderStatus.Closed && x.Status != RestaurantOrderStatus.Cancelled) > 0;
                if (!sessionIsOpen)
                    throw new UserFriendlyException("The table session is closed. Reopen the table before approving this order.");
                order.GuestApprovalStatus = RestaurantGuestOrderApprovalStatus.Approved;
                await orderRepository.UpdateAsync(order);
                await CurrentUnitOfWork.SaveChangesAsync();
                await orderAppService.SendToKitchen(new EntityDto<Guid>(order.Id));
                return;
            }

            if (string.IsNullOrWhiteSpace(input.RejectionReason))
                throw new UserFriendlyException("A reason is required to reject a guest order");
            order.GuestApprovalStatus = RestaurantGuestOrderApprovalStatus.Rejected;
            order.GuestRejectionReason = input.RejectionReason.Trim()[..Math.Min(300, input.RejectionReason.Trim().Length)];
            order.Status = RestaurantOrderStatus.Cancelled;
            await orderRepository.UpdateAsync(order);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
        public async Task OpenTableSession(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId && x.IsActive);
            if (table == null) throw new UserFriendlyException("Restaurant table not found");
            if (await sessionRepository.CountAsync(x => x.TenantId == tenantId && x.TableId == table.Id && x.ClosedAt == null &&
                                                        x.Status != RestaurantOrderStatus.Closed && x.Status != RestaurantOrderStatus.Cancelled) > 0)
                return;
            await sessionRepository.InsertAsync(new RestaurantTableSession
            {
                TenantId = tenantId,
                TableId = table.Id,
                SessionNo = "TS-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..4],
                Status = RestaurantOrderStatus.Draft,
                OpenedAt = DateTime.Now
            });
            table.Status = RestaurantTableStatus.Occupied;
            await tableRepository.UpdateAsync(table);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
        public async Task CloseTableSession(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var session = await sessionRepository.GetAll()
                .Where(x => x.Id == input.Id && x.TenantId == tenantId && x.ClosedAt == null)
                .FirstOrDefaultAsync();
            if (session == null) throw new UserFriendlyException("Open table session not found");
            var hasUnsettledOrders = await orderRepository.CountAsync(x => x.TenantId == tenantId && x.TableSessionId == session.Id &&
                x.Status != RestaurantOrderStatus.Billed && x.Status != RestaurantOrderStatus.Closed && x.Status != RestaurantOrderStatus.Cancelled) > 0;
            if (hasUnsettledOrders)
                throw new UserFriendlyException("Settle or cancel all orders before closing this table session");
            session.Status = RestaurantOrderStatus.Closed;
            session.ClosedAt = DateTime.Now;
            await sessionRepository.UpdateAsync(session);
            if (session.TableId.HasValue)
            {
                var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == session.TableId.Value && x.TenantId == tenantId);
                if (table != null) { table.Status = RestaurantTableStatus.Available; await tableRepository.UpdateAsync(table); }
            }
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantReservations)]
        public async Task<List<RestaurantReservationDto>> GetReservations(DateTime? from, DateTime? to)
        {
            var tenantId = AbpSession.GetTenantId();
            var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-1);
            var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(7);
            var items = await reservationRepository.GetAll().Include(x => x.TableFk)
                .Where(x => x.TenantId == tenantId && x.StartsAtUtc >= fromUtc && x.StartsAtUtc <= toUtc)
                .OrderBy(x => x.StartsAtUtc).ToListAsync();
            var result = new List<RestaurantReservationDto>();
            foreach (var x in items)
            {
                var latestMessage = await smsOutboxRepository.GetAll()
                    .Where(m => m.TenantId == tenantId && m.ReservationId == x.Id)
                    .OrderByDescending(m => m.CreatedAtUtc).FirstOrDefaultAsync();
                result.Add(new RestaurantReservationDto
                {
                    Id = x.Id,
                    Status = x.Status,
                    IsWalkIn = x.IsWalkIn,
                    GuestName = x.GuestName,
                    PhoneNumber = x.PhoneNumber,
                    Notes = x.Notes,
                    PartySize = x.PartySize,
                    StartsAt = x.StartsAtUtc,
                    EndsAt = x.EndsAtUtc,
                    TableId = x.TableId,
                    TableName = x.TableFk?.Name,
                    CreatedAt = x.CreatedAtUtc,
                    SmsStatus = latestMessage == null ? "No message" : latestMessage.Status == RestaurantSmsOutboxStatus.Failed ? "Failed: " + latestMessage.LastError : latestMessage.Status == RestaurantSmsOutboxStatus.Sent ? "Queued for delivery" : "Queued for delivery"
                });
            }
            return result;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantReservations)]
        public async Task<Guid> AddWalkIn(CreateRestaurantWalkInDto input)
        {
            if (string.IsNullOrWhiteSpace(input.GuestName) || input.PartySize is < 1 or > 30)
                throw new UserFriendlyException("Guest name and a party size between 1 and 30 are required");
            var now = DateTime.UtcNow;
            var reservation = new RestaurantReservation
            {
                TenantId = AbpSession.GetTenantId(),
                IsWalkIn = true,
                Status = RestaurantReservationStatus.Waitlisted,
                GuestName = input.GuestName.Trim(),
                PhoneNumber = input.PhoneNumber?.Trim() ?? string.Empty,
                Notes = input.Notes?.Trim(),
                PartySize = input.PartySize,
                StartsAtUtc = now,
                EndsAtUtc = now.AddMinutes(90),
                CreatedAtUtc = now
            };
            return await reservationRepository.InsertAndGetIdAsync(reservation);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantReservations)]
        public async Task UpdateReservation(UpdateRestaurantReservationDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var reservation = await reservationRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
            if (reservation == null) throw new UserFriendlyException("Reservation not found");
            var nextEnd = input.EndsAt.HasValue ? SpecifyUtc(input.EndsAt.Value) : reservation.EndsAtUtc;
            if (nextEnd <= reservation.StartsAtUtc)
                throw new UserFriendlyException("Reservation end time must be after its start time");

            if (input.Status is RestaurantReservationStatus.Confirmed or RestaurantReservationStatus.Seated)
            {
                if (!input.TableId.HasValue) throw new UserFriendlyException("Assign a table before confirming or seating");
                var table = await tableRepository.FirstOrDefaultAsync(x => x.Id == input.TableId.Value && x.TenantId == tenantId && x.IsActive);
                if (table == null || table.Capacity < reservation.PartySize)
                    throw new UserFriendlyException("Choose an active table with enough seats");
                var overlaps = await reservationRepository.CountAsync(x => x.Id != reservation.Id && x.TenantId == tenantId &&
                    x.TableId == input.TableId.Value &&
                    (x.Status == RestaurantReservationStatus.Confirmed || x.Status == RestaurantReservationStatus.Seated) &&
                    x.StartsAtUtc < nextEnd && reservation.StartsAtUtc < x.EndsAtUtc) > 0;
                if (overlaps) throw new UserFriendlyException("The table already has an overlapping confirmed reservation");
                reservation.TableId = input.TableId;
            }

            reservation.EndsAtUtc = nextEnd;
            reservation.Status = input.Status;
            reservation.UpdatedAtUtc = DateTime.UtcNow;

            if (input.Status == RestaurantReservationStatus.Seated)
            {
                var tableId = reservation.TableId!.Value;
                var existing = await sessionRepository.CountAsync(x => x.TenantId == tenantId && x.TableId == tableId && x.ClosedAt == null &&
                    x.Status != RestaurantOrderStatus.Closed && x.Status != RestaurantOrderStatus.Cancelled) > 0;
                if (existing) throw new UserFriendlyException("The selected table already has an open session");
                await sessionRepository.InsertAsync(new RestaurantTableSession
                {
                    TenantId = tenantId,
                    TableId = tableId,
                    SessionNo = "TS-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..4],
                    Status = RestaurantOrderStatus.Draft,
                    OpenedAt = DateTime.Now,
                    GuestCount = reservation.PartySize,
                    CustomerName = reservation.GuestName,
                    CustomerPhoneNo = reservation.PhoneNumber
                });
                var table = await tableRepository.FirstOrDefaultAsync(tableId);
                table.Status = RestaurantTableStatus.Occupied;
                await tableRepository.UpdateAsync(table);
            }

            await reservationRepository.UpdateAsync(reservation);
            if (input.Status is RestaurantReservationStatus.Confirmed or RestaurantReservationStatus.Declined or RestaurantReservationStatus.Cancelled)
            {
                var displayTime = reservation.StartsAtUtc.ToLocalTime().ToString("g");
                var message = $"Reservation {reservation.Id.ToString()[..8]} is {input.Status.ToString().ToLowerInvariant()} for {displayTime}.";
                await smsOutboxRepository.InsertAsync(new RestaurantSmsOutbox
                {
                    TenantId = tenantId,
                    ReservationId = reservation.Id,
                    PhoneNumber = reservation.PhoneNumber,
                    Message = message,
                    Status = RestaurantSmsOutboxStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<RestaurantPrintJobDto> ClaimPrintJob(ClaimRestaurantPrintJobDto input)
        {
            if (string.IsNullOrWhiteSpace(input?.AgentId) || input.AgentId.Length > 120)
                throw new UserFriendlyException("Print station identity is required");
            var now = DateTime.UtcNow;
            var job = await printJobRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            (x.Status == RestaurantPrintJobStatus.Pending ||
                             (x.Status == RestaurantPrintJobStatus.Leased && x.LeaseUntilUtc < now)))
                .OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync();
            if (job == null) return null;
            job.Status = RestaurantPrintJobStatus.Leased;
            job.LeaseOwner = input.AgentId.Trim();
            job.LeaseUntilUtc = now.AddMinutes(5);
            job.Attempts++;
            await printJobRepository.UpdateAsync(job);
            return MapPrintJob(job);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<List<RestaurantPrintJobDto>> GetPrintJobs()
        {
            return await printJobRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(100)
                .Select(x => new RestaurantPrintJobDto
                {
                    Id = x.Id,
                    ExternalJobId = x.ExternalJobId,
                    Type = x.Type,
                    RouteName = x.RouteName,
                    PayloadBase64 = string.Empty,
                    AgentJobId = x.AgentJobId,
                    Status = x.Status,
                    LastError = x.LastError,
                    Attempts = x.Attempts,
                    ReprintReason = x.ReprintReason
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<RestaurantPrintJobDto> ReportPrintJob(ReportRestaurantPrintJobDto input)
        {
            var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == AbpSession.TenantId);
            if (job == null || job.Status != RestaurantPrintJobStatus.Leased || job.LeaseOwner != input.AgentId)
                throw new UserFriendlyException("Print job lease is no longer valid");
            if (input.Status is not RestaurantPrintJobStatus.Printed and not RestaurantPrintJobStatus.Failed)
                throw new UserFriendlyException("Print agent must report Printed or Failed");
            job.Status = input.Status;
            job.AgentJobId = input.AgentJobId;
            job.LastError = input.Error?.Length > 500 ? input.Error[..500] : input.Error;
            job.LeaseUntilUtc = null;
            if (job.Status == RestaurantPrintJobStatus.Printed)
            {
                job.PrintedAtUtc = DateTime.UtcNow;
                if (job.TicketId.HasValue)
                {
                    var ticket = await ticketRepository.FirstOrDefaultAsync(job.TicketId.Value);
                    if (ticket != null)
                    {
                        ticket.LastPrintConfirmedAt = DateTime.Now;
                        ticket.PrintedAt ??= DateTime.Now;
                        ticket.LastPrintedAt = DateTime.Now;
                        await ticketRepository.UpdateAsync(ticket);
                    }
                }
            }
            await printJobRepository.UpdateAsync(job);
            return MapPrintJob(job);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task RetryPrintJob(RetryRestaurantPrintJobDto input)
        {
            var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == input.JobId && x.TenantId == AbpSession.TenantId);
            if (job == null || job.Status != RestaurantPrintJobStatus.Failed)
                throw new UserFriendlyException("Failed print job not found");
            if (job.StationId.HasValue)
            {
                var station = await stationRepository.FirstOrDefaultAsync(x => x.Id == job.StationId.Value && x.TenantId == AbpSession.TenantId);
                if (!string.IsNullOrWhiteSpace(station?.PrintRouteName)) job.RouteName = station.PrintRouteName.Trim();
            }
            else if (job.Type == RestaurantPrintJobType.BillReceipt)
            {
                var receiptRoute = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantReceiptPrintRouteName, AbpSession.GetTenantId());
                if (!string.IsNullOrWhiteSpace(receiptRoute)) job.RouteName = receiptRoute.Trim();
            }
            if (string.IsNullOrWhiteSpace(job.RouteName) || job.RouteName == "unconfigured")
                throw new UserFriendlyException("Configure a printer route before retrying this job");
            job.Status = RestaurantPrintJobStatus.Pending;
            job.LastError = null;
            job.LeaseOwner = null;
            job.LeaseUntilUtc = null;
            await printJobRepository.UpdateAsync(job);
        }

        private static RestaurantPrintJobDto MapPrintJob(RestaurantPrintJob job) => new()
        {
            Id = job.Id,
            ExternalJobId = job.ExternalJobId,
            Type = job.Type,
            RouteName = job.RouteName,
            PayloadBase64 = Convert.ToBase64String(job.Payload ?? Array.Empty<byte>()),
            AgentJobId = job.AgentJobId,
            Status = job.Status,
            LastError = job.LastError,
            Attempts = job.Attempts,
            ReprintReason = job.ReprintReason
        };

        private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        private static DateTime SpecifyUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
