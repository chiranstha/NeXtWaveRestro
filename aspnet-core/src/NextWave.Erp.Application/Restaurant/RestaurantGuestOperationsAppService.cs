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
using NextWave.Erp.Restaurant.Dtos;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
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
        IRepository<RestaurantPrintDevice, Guid> printDeviceRepository,
        IRepository<RestaurantPrintDeviceRoute, Guid> printDeviceRouteRepository,
        IRepository<RestaurantPrintRoute, Guid> printRouteRepository,
        IRepository<RestaurantPrintDelivery, Guid> printDeliveryRepository,
        IRepository<RestaurantTicket, Guid> ticketRepository,
        IRestaurantOrderAppService orderAppService,
        IUnitOfWorkManager unitOfWorkManager,
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
                await orderAppService.SendToKitchen(new RestaurantOrderMutationDto
                {
                    OrderId = order.Id,
                    ClientRequestId = "guest-kitchen-" + order.Id.ToString("N"),
                    ExpectedOrderVersion = order.RowVersion == null ? null : Convert.ToBase64String(order.RowVersion)
                });
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
            var clientDeviceId = input?.AgentId?.Trim();
            if (string.IsNullOrWhiteSpace(clientDeviceId) || clientDeviceId.Length > 120)
                throw new UserFriendlyException("Print device identity is required");
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable,
                Scope = TransactionScopeOption.RequiresNew
            });
            var now = DateTime.UtcNow;
            var tenantId = AbpSession.GetTenantId();
            var device = await printDeviceRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ClientDeviceId == clientDeviceId);
            if (device == null || !device.IsEnabled)
            {
                await uow.CompleteAsync();
                return null;
            }
            device.LastSeenAtUtc = now;
            await printDeviceRepository.UpdateAsync(device);

            var delivery = await printDeliveryRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.DeviceId == device.Id &&
                    (x.Status == RestaurantPrintJobStatus.Pending ||
                     (x.Status == RestaurantPrintJobStatus.Leased && x.LeaseUntilUtc < now)))
                .OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync();
            if (delivery == null)
            {
                await uow.CompleteAsync();
                return null;
            }
            var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == delivery.PrintJobId && x.TenantId == tenantId);
            if (job == null)
            {
                delivery.Status = RestaurantPrintJobStatus.Failed;
                delivery.LastError = "The print job could not be found.";
                await printDeliveryRepository.UpdateAsync(delivery);
                await uow.CompleteAsync();
                return null;
            }

            delivery.Status = RestaurantPrintJobStatus.Leased;
            delivery.LeaseOwner = clientDeviceId;
            delivery.LeaseToken = Guid.NewGuid();
            delivery.LeaseUntilUtc = now.AddMinutes(5);
            delivery.Attempts++;
            job.Status = RestaurantPrintJobStatus.Leased;
            job.LeaseOwner = clientDeviceId;
            job.LeaseUntilUtc = delivery.LeaseUntilUtc;
            job.Attempts++;
            await printDeliveryRepository.UpdateAsync(delivery);
            await printJobRepository.UpdateAsync(job);
            await uow.CompleteAsync();
            var result = MapPrintJob(job);
            result.DeliveryId = delivery.Id;
            result.LeaseToken = delivery.LeaseToken;
            return result;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<RestaurantPrintDeviceDto> RegisterPrintDevice(RegisterRestaurantPrintDeviceDto input)
        {
            var clientDeviceId = input?.ClientDeviceId?.Trim();
            var name = input?.Name?.Trim();
            var platform = input?.Platform?.Trim();
            if (string.IsNullOrWhiteSpace(clientDeviceId) || clientDeviceId.Length > 120 ||
                string.IsNullOrWhiteSpace(name) || name.Length > 150 ||
                platform is not "Windows" and not "Android")
                throw new UserFriendlyException("A valid device ID, name, and Windows or Android platform are required.");

            var tenantId = AbpSession.GetTenantId();
            var routeNames = (input.RouteNames ?? new List<string>()).Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (routeNames.Any(x => x.Length > 128 || x.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_')))
                throw new UserFriendlyException("A printer route name is invalid.");
            var activeRoutes = await printRouteRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.IsActive && routeNames.Contains(x.Name))
                .Select(x => x.Name).ToListAsync();
            if (activeRoutes.Count != routeNames.Count)
                throw new UserFriendlyException("Every device route must exist and be active in restaurant printer setup.");

            var device = await printDeviceRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientDeviceId == clientDeviceId);
            if (device == null)
            {
                device = new RestaurantPrintDevice
                {
                    TenantId = tenantId,
                    ClientDeviceId = clientDeviceId,
                    Name = name,
                    Platform = platform,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await printDeviceRepository.InsertAsync(device);
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            else
            {
                device.Name = name;
                device.Platform = platform;
                device.LastSeenAtUtc = DateTime.UtcNow;
                await printDeviceRepository.UpdateAsync(device);
            }

            var existing = await printDeviceRouteRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.DeviceId == device.Id).ToListAsync();
            foreach (var mapping in existing.Where(x => !routeNames.Contains(x.RouteName, StringComparer.OrdinalIgnoreCase)))
            {
                await printDeviceRouteRepository.DeleteAsync(mapping);
                var staleDeliveries = await printDeliveryRepository.GetAll().Where(x => x.TenantId == tenantId &&
                    x.DeviceId == device.Id && x.RouteName == mapping.RouteName && x.Status == RestaurantPrintJobStatus.Pending).ToListAsync();
                var staleJobIds = staleDeliveries.Select(x => x.PrintJobId).Distinct().ToList();
                foreach (var delivery in staleDeliveries)
                {
                    delivery.Status = RestaurantPrintJobStatus.Cancelled;
                    delivery.LastError = "This route was removed from the print device.";
                    await printDeliveryRepository.UpdateAsync(delivery);
                }
                foreach (var staleJobId in staleJobIds)
                {
                    var staleJob = await printJobRepository.FirstOrDefaultAsync(x => x.Id == staleJobId && x.TenantId == tenantId);
                    if (staleJob != null) await UpdatePrintJobRollupAsync(staleJob, tenantId);
                }
            }
            foreach (var routeName in routeNames.Where(x => existing.All(e => !string.Equals(e.RouteName, x, StringComparison.OrdinalIgnoreCase))))
            {
                await printDeviceRouteRepository.InsertAsync(new RestaurantPrintDeviceRoute
                {
                    TenantId = tenantId,
                    DeviceId = device.Id,
                    RouteName = routeName
                });
            }
            await CurrentUnitOfWork.SaveChangesAsync();
            return new RestaurantPrintDeviceDto
            {
                Id = device.Id,
                ClientDeviceId = device.ClientDeviceId,
                Name = device.Name,
                Platform = device.Platform,
                IsEnabled = device.IsEnabled,
                LastSeenAtUtc = device.LastSeenAtUtc,
                RouteNames = routeNames
            };
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<List<RestaurantPrintDeviceDto>> GetPrintDevices()
        {
            var tenantId = AbpSession.GetTenantId();
            var devices = await printDeviceRepository.GetAll().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Name).ToListAsync();
            var deviceIds = devices.Select(x => x.Id).ToList();
            var routes = await printDeviceRouteRepository.GetAll()
                .Where(x => x.TenantId == tenantId && deviceIds.Contains(x.DeviceId)).ToListAsync();
            return devices.Select(device => new RestaurantPrintDeviceDto
            {
                Id = device.Id,
                ClientDeviceId = device.ClientDeviceId,
                Name = device.Name,
                Platform = device.Platform,
                IsEnabled = device.IsEnabled,
                LastSeenAtUtc = device.LastSeenAtUtc,
                RouteNames = routes.Where(route => route.DeviceId == device.Id).Select(route => route.RouteName).OrderBy(x => x).ToList()
            }).ToList();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task SetPrintDeviceEnabled(SetRestaurantPrintDeviceEnabledDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var device = await printDeviceRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
            if (device == null) throw new UserFriendlyException("Print device not found.");
            device.IsEnabled = input.IsEnabled;
            await printDeviceRepository.UpdateAsync(device);
            if (!device.IsEnabled)
            {
                var pending = await printDeliveryRepository.GetAll().Where(x => x.TenantId == tenantId &&
                    x.DeviceId == device.Id && x.Status == RestaurantPrintJobStatus.Pending).ToListAsync();
                var jobIds = pending.Select(x => x.PrintJobId).Distinct().ToList();
                foreach (var delivery in pending)
                {
                    delivery.Status = RestaurantPrintJobStatus.Cancelled;
                    delivery.LastError = "This print device was disabled.";
                    await printDeliveryRepository.UpdateAsync(delivery);
                }
                foreach (var jobId in jobIds)
                {
                    var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
                    if (job != null) await UpdatePrintJobRollupAsync(job, tenantId);
                }
            }
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<List<RestaurantPrintJobDto>> GetPrintJobs()
        {
            var tenantId = AbpSession.GetTenantId();
            var jobs = await printJobRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(100).ToListAsync();
            var jobIds = jobs.Select(x => x.Id).ToList();
            var deliveries = await printDeliveryRepository.GetAll()
                .Where(x => x.TenantId == tenantId && jobIds.Contains(x.PrintJobId)).ToListAsync();
            var devices = await printDeviceRepository.GetAll().Where(x => x.TenantId == tenantId).ToDictionaryAsync(x => x.Id);
            return jobs.Select(job =>
            {
                var result = MapPrintJob(job);
                result.Deliveries = deliveries.Where(x => x.PrintJobId == job.Id).Select(delivery => new RestaurantPrintDeliveryDto
                {
                    Id = delivery.Id,
                    DeviceId = delivery.DeviceId,
                    DeviceName = devices.TryGetValue(delivery.DeviceId, out var device) ? device.Name : "Unknown device",
                    Platform = devices.TryGetValue(delivery.DeviceId, out var platformDevice) ? platformDevice.Platform : "Unknown",
                    RouteName = delivery.RouteName,
                    Status = delivery.Status,
                    Attempts = delivery.Attempts,
                    LastError = delivery.LastError,
                    PrintedAtUtc = delivery.PrintedAtUtc
                }).ToList();
                return result;
            }).ToList();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task<RestaurantPrintJobDto> ReportPrintJob(ReportRestaurantPrintJobDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var clientDeviceId = input.AgentId?.Trim();
            var device = await printDeviceRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientDeviceId == clientDeviceId);
            var delivery = device == null ? null : input.DeliveryId.HasValue
                ? await printDeliveryRepository.FirstOrDefaultAsync(x => x.Id == input.DeliveryId && x.TenantId == tenantId)
                : await printDeliveryRepository.GetAll().FirstOrDefaultAsync(x => x.PrintJobId == input.Id && x.DeviceId == device.Id && x.TenantId == tenantId);
            var jobId = delivery?.PrintJobId ?? input.Id;
            var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
            if (device == null || delivery == null || job == null || delivery.DeviceId != device.Id ||
                delivery.Status != RestaurantPrintJobStatus.Leased || delivery.LeaseOwner != clientDeviceId ||
                !input.LeaseToken.HasValue || input.LeaseToken != delivery.LeaseToken)
                throw new UserFriendlyException("Print job lease is no longer valid");
            if (input.Status is not RestaurantPrintJobStatus.Printed and not RestaurantPrintJobStatus.Failed)
                throw new UserFriendlyException("Print agent must report Printed or Failed");
            delivery.Status = input.Status;
            delivery.AgentJobId = input.AgentJobId;
            delivery.LastError = input.Error?.Length > 500 ? input.Error[..500] : input.Error;
            delivery.LeaseOwner = null;
            delivery.LeaseUntilUtc = null;
            delivery.LeaseToken = null;
            if (delivery.Status == RestaurantPrintJobStatus.Printed)
            {
                delivery.PrintedAtUtc = DateTime.UtcNow;
            }
            await printDeliveryRepository.UpdateAsync(delivery);
            await UpdatePrintJobRollupAsync(job, tenantId);
            var result = MapPrintJob(job);
            result.DeliveryId = delivery.Id;
            return result;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantPrinterSetup)]
        public async Task RetryPrintJob(RetryRestaurantPrintJobDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var job = await printJobRepository.FirstOrDefaultAsync(x => x.Id == input.JobId && x.TenantId == tenantId);
            if (job == null)
                throw new UserFriendlyException("Failed print job not found");

            if (input.DeliveryId.HasValue)
            {
                var delivery = await printDeliveryRepository.FirstOrDefaultAsync(x => x.Id == input.DeliveryId &&
                    x.PrintJobId == job.Id && x.TenantId == tenantId);
                var device = delivery == null ? null : await printDeviceRepository.FirstOrDefaultAsync(x => x.Id == delivery.DeviceId && x.TenantId == tenantId);
                if (delivery == null || delivery.Status != RestaurantPrintJobStatus.Failed)
                    throw new UserFriendlyException("Failed device delivery not found");
                if (device == null || !device.IsEnabled)
                    throw new UserFriendlyException("Enable the print device before retrying its delivery.");
                delivery.Status = RestaurantPrintJobStatus.Pending;
                delivery.LastError = null;
                delivery.AgentJobId = null;
                delivery.LeaseOwner = null;
                delivery.LeaseToken = null;
                delivery.LeaseUntilUtc = null;
                await printDeliveryRepository.UpdateAsync(delivery);
                await UpdatePrintJobRollupAsync(job, tenantId);
            }
            else
            {
                if (job.Status != RestaurantPrintJobStatus.Failed)
                    throw new UserFriendlyException("Failed print job not found");
                var deviceIds = await (from mapping in printDeviceRouteRepository.GetAll()
                    join device in printDeviceRepository.GetAll() on mapping.DeviceId equals device.Id
                    join route in printRouteRepository.GetAll() on mapping.RouteName equals route.Name
                    where mapping.TenantId == tenantId && mapping.RouteName == job.RouteName &&
                        device.TenantId == tenantId && device.IsEnabled && route.TenantId == tenantId && route.IsActive
                    select device.Id).Distinct().ToListAsync();
                if (deviceIds.Count == 0) throw new UserFriendlyException("No enabled print device is configured for this route.");
                foreach (var deviceId in deviceIds)
                    await printDeliveryRepository.InsertAsync(new RestaurantPrintDelivery
                    {
                        TenantId = tenantId,
                        PrintJobId = job.Id,
                        DeviceId = deviceId,
                        RouteName = job.RouteName,
                        Status = RestaurantPrintJobStatus.Pending,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                job.Status = RestaurantPrintJobStatus.Pending;
                job.LastError = null;
            }
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

        private async Task UpdatePrintJobRollupAsync(RestaurantPrintJob job, int tenantId)
        {
            job.LeaseOwner = null;
            job.LeaseUntilUtc = null;
            var deliveries = await printDeliveryRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.PrintJobId == job.Id).ToListAsync();
            if (deliveries.Count == 0) return;

            if (deliveries.Any(x => x.Status is RestaurantPrintJobStatus.Pending or RestaurantPrintJobStatus.Leased))
            {
                job.Status = deliveries.Any(x => x.Status == RestaurantPrintJobStatus.Leased)
                    ? RestaurantPrintJobStatus.Leased : RestaurantPrintJobStatus.Pending;
                job.LastError = LimitPrintError(string.Join("; ", deliveries.Where(x => x.Status == RestaurantPrintJobStatus.Failed)
                    .Select(x => x.LastError).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(3)));
            }
            else if (deliveries.Any(x => x.Status == RestaurantPrintJobStatus.Failed))
            {
                job.Status = RestaurantPrintJobStatus.Failed;
                job.LastError = LimitPrintError(string.Join("; ", deliveries.Where(x => x.Status == RestaurantPrintJobStatus.Failed)
                    .Select(x => x.LastError).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(3)));
            }
            else if (deliveries.Any(x => x.Status == RestaurantPrintJobStatus.Printed))
            {
                job.Status = RestaurantPrintJobStatus.Printed;
                job.LastError = null;
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
            else job.Status = RestaurantPrintJobStatus.Cancelled;
            await printJobRepository.UpdateAsync(job);
        }

        private static string LimitPrintError(string value) => value?.Length > 500 ? value[..500] : value;

        private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        private static DateTime SpecifyUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
