using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantReports)]
    public class RestaurantReportsAppService(
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        IRepository<RestaurantTicket, Guid> ticketRepository,
        IRepository<RestaurantTicketItem, Guid> ticketItemRepository,
        IRepository<RestaurantTableSession, Guid> tableSessionRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<RestaurantMenuCategory, Guid> categoryRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<RestaurantStockAdjustment, Guid> stockAdjustmentRepository,
        IRepository<RestaurantStockAdjustmentLine, Guid> stockAdjustmentLineRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<User, long> userRepository,
        IRepository<RestaurantPayrollEmployee, Guid> payrollEmployeeRepository,
        IRepository<RestaurantPayrollRun, Guid> payrollRunRepository,
        IRepository<RestaurantPayrollLine, Guid> payrollLineRepository,
        IRepository<RestaurantPayrollAttendance, Guid> payrollAttendanceRepository,
        RestaurantReorderService reorderService)
        : ErpAppServiceBase, IRestaurantReportsAppService
    {
        private static readonly string[] ReportCategoryPermissions =
        {
            AppPermissions.PagesRestaurantReportsSales,
            AppPermissions.PagesRestaurantReportsOperations,
            AppPermissions.PagesRestaurantReportsInventory,
            AppPermissions.PagesRestaurantReportsPayroll,
            AppPermissions.PagesRestaurantReportsAuditFinance
        };

        public async Task<RestaurantPosSalesSummaryDto> GetPosSalesSummary(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsSales);
            var sales = GetBilledSales(input);
            var orderCount = await sales.CountAsync();
            var grandTotal = await sales.Select(x => (decimal?)x.GrandTotal).SumAsync() ?? 0;

            return new RestaurantPosSalesSummaryDto
            {
                OrderCount = orderCount,
                GrossAmount = await sales.Select(x => (decimal?)x.GrossAmount).SumAsync() ?? 0,
                DiscountAmount = await sales.Select(x => (decimal?)x.BillDiscount).SumAsync() ?? 0,
                TaxAmount = await sales.Select(x => (decimal?)x.TaxAmount).SumAsync() ?? 0,
                NetAmount = await sales.Select(x => (decimal?)x.NetAmount).SumAsync() ?? 0,
                GrandTotal = grandTotal,
                AverageBill = orderCount == 0 ? 0 : grandTotal / orderCount
            };
        }

        public async Task<RestaurantPayrollReportBundleDto> GetPayrollReport(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsPayroll);
            var fromDate = input?.FromDate?.Date;
            var toDateExclusive = input?.ToDate?.Date.AddDays(1);

            var payrollRuns = payrollRunRepository.GetAll();
            if (fromDate.HasValue)
                payrollRuns = payrollRuns.Where(run => run.PeriodEnd >= fromDate.Value);
            if (toDateExclusive.HasValue)
                payrollRuns = payrollRuns.Where(run => run.PeriodStart < toDateExclusive.Value);

            var runs = await payrollRuns
                .OrderByDescending(run => run.PeriodEnd)
                .Select(run => new RestaurantPayrollRunDto
                {
                    Id = run.Id,
                    RunNumber = run.RunNumber,
                    PeriodStart = run.PeriodStart,
                    PeriodEnd = run.PeriodEnd,
                    Status = run.Status,
                    TipsPool = run.TipsPool,
                    ServiceChargePool = run.ServiceChargePool,
                    TotalGross = run.TotalGross,
                    TotalDeduction = run.TotalDeduction,
                    TotalNet = run.TotalNet,
                    Notes = run.Notes,
                    CreatedAt = run.CreatedAt,
                    EmployeeCount = payrollLineRepository.GetAll().Count(line => line.PayrollRunId == run.Id)
                })
                .ToListAsync();

            var runIds = runs.Select(run => run.Id).ToList();
            var employeeCosts = runIds.Count == 0
                ? new List<RestaurantPayrollEmployeeCostReportDto>()
                : await payrollLineRepository.GetAll()
                    .Where(line => runIds.Contains(line.PayrollRunId))
                    .GroupBy(line => new { line.EmployeeId, line.EmployeeName, line.StaffCode, line.JobRole })
                    .Select(group => new RestaurantPayrollEmployeeCostReportDto
                    {
                        EmployeeId = group.Key.EmployeeId,
                        EmployeeName = group.Key.EmployeeName,
                        StaffCode = group.Key.StaffCode,
                        JobRole = group.Key.JobRole,
                        WorkedHours = group.Sum(line => line.WorkedHours),
                        OvertimeHours = group.Sum(line => line.OvertimeHours),
                        BasicPay = group.Sum(line => line.BasicPay),
                        Allowance = group.Sum(line => line.Allowance),
                        TipsAndServiceCharge = group.Sum(line => line.TipsShare + line.ServiceChargeShare),
                        GrossPay = group.Sum(line => line.GrossPay),
                        TotalDeduction = group.Sum(line => line.TaxDeduction + line.OtherDeduction + line.AdvanceRecovery),
                        NetPay = group.Sum(line => line.NetPay)
                    })
                    .OrderByDescending(row => row.GrossPay)
                    .ToListAsync();

            var attendanceQuery = payrollAttendanceRepository.GetAll();
            if (fromDate.HasValue)
                attendanceQuery = attendanceQuery.Where(row => row.WorkDate >= fromDate.Value);
            if (toDateExclusive.HasValue)
                attendanceQuery = attendanceQuery.Where(row => row.WorkDate < toDateExclusive.Value);

            var attendance = await attendanceQuery
                .GroupBy(row => row.Status)
                .Select(group => new RestaurantPayrollAttendanceStatusReportDto
                {
                    Status = group.Key,
                    StatusName = group.Key.ToString(),
                    RecordCount = group.Count(),
                    RegularHours = group.Sum(row => row.RegularHours),
                    OvertimeHours = group.Sum(row => row.OvertimeHours)
                })
                .OrderBy(row => row.Status)
                .ToListAsync();

            return new RestaurantPayrollReportBundleDto
            {
                Summary = new RestaurantPayrollReportSummaryDto
                {
                    ActiveEmployeeCount = await payrollEmployeeRepository.GetAll().CountAsync(employee => employee.IsActive),
                    PayrollRunCount = runs.Count,
                    AttendanceRecordCount = attendance.Sum(row => row.RecordCount),
                    TotalGross = runs.Sum(run => run.TotalGross),
                    TotalDeduction = runs.Sum(run => run.TotalDeduction),
                    TotalNet = runs.Sum(run => run.TotalNet),
                    TotalOvertimeHours = employeeCosts.Sum(row => row.OvertimeHours)
                },
                Runs = runs,
                EmployeeCosts = employeeCosts,
                Attendance = attendance
            };
        }

        public async Task<List<RestaurantMaterialConsumptionReportDto>> GetMaterialConsumption(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsInventory);
            var rows = await (
                    from sales in GetBilledSales(input)
                    join stockPosting in stockPostingRepository.GetAll() on (Guid?)sales.Id equals stockPosting.MasterId
                    join product in productRepository.GetAll() on stockPosting.ProductId equals product.Id
                    join unit in unitRepository.GetAll() on stockPosting.UnitId equals unit.Id
                    where stockPosting.TenantId == AbpSession.TenantId &&
                          stockPosting.OutWardQty > 0
                    select new
                    {
                        stockPosting.ProductId,
                        ProductName = product.Name,
                        stockPosting.UnitId,
                        UnitName = unit.Name,
                        stockPosting.OutWardQty,
                        stockPosting.Amount
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.ProductId, x.ProductName, x.UnitId, x.UnitName })
                .Select(x => new RestaurantMaterialConsumptionReportDto
                {
                    ProductId = x.Key.ProductId,
                    ProductName = x.Key.ProductName,
                    UnitId = x.Key.UnitId,
                    UnitName = x.Key.UnitName,
                    Qty = x.Sum(y => y.OutWardQty),
                    Amount = x.Sum(y => y.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantItemSalesReportDto>> GetItemSales(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsSales);
            var rows = await (
                    from billLine in GetBilledBillLines(input)
                    join item in orderItemRepository.GetAll() on billLine.OrderItemId equals item.Id
                    join product in productRepository.GetAll() on item.ProductId equals product.Id
                    join menuItem in menuItemRepository.GetAll() on item.MenuItemId equals menuItem.Id into menuItems
                    from menuItem in menuItems.DefaultIfEmpty()
                    join category in categoryRepository.GetAll() on menuItem.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    where item.TenantId == AbpSession.TenantId &&
                          item.Status != RestaurantOrderItemStatus.Cancelled &&
                          (!input.CategoryId.HasValue || menuItem.CategoryId == input.CategoryId.Value)
                    select new
                    {
                        item.ProductId,
                        ProductName = product.Name,
                        CategoryId = menuItem == null ? (Guid?)null : menuItem.CategoryId,
                        CategoryName = category == null ? "" : category.Name,
                        billLine.Qty,
                        billLine.GrossAmount,
                        billLine.DiscountAmount,
                        billLine.TaxAmount,
                        billLine.NetAmount,
                        billLine.Amount
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryId, x.CategoryName })
                .Select(x => new RestaurantItemSalesReportDto
                {
                    ProductId = x.Key.ProductId,
                    ProductName = x.Key.ProductName,
                    CategoryId = x.Key.CategoryId,
                    CategoryName = x.Key.CategoryName,
                    Qty = x.Sum(y => y.Qty),
                    GrossAmount = x.Sum(y => y.GrossAmount),
                    DiscountAmount = x.Sum(y => y.DiscountAmount),
                    TaxAmount = x.Sum(y => y.TaxAmount),
                    NetAmount = x.Sum(y => y.NetAmount),
                    GrandTotal = x.Sum(y => y.Amount)
                })
                .OrderByDescending(x => x.GrandTotal)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantTableSalesReportDto>> GetTableSales(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsOperations);
            var rows = await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll().Include(x => x.TableFk) on sales.SourceDocumentId equals (Guid?)order.Id
                    select new
                    {
                        order.TableId,
                        TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                        sales.GrandTotal
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.TableId, x.TableName })
                .Select(x => new RestaurantTableSalesReportDto
                {
                    TableId = x.Key.TableId,
                    TableName = x.Key.TableName,
                    OrderCount = x.Count(),
                    GrandTotal = x.Sum(y => y.GrandTotal)
                })
                .OrderByDescending(x => x.GrandTotal)
                .ThenBy(x => x.TableName)
                .ToList();
        }

        public async Task<List<RestaurantWaiterSalesReportDto>> GetWaiterSales(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsOperations);
            var rows = await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll() on sales.SourceDocumentId equals (Guid?)order.Id
                    join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                    from waiter in waiters.DefaultIfEmpty()
                    select new
                    {
                        order.WaiterUserId,
                        WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                        sales.GrandTotal
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.WaiterUserId, x.WaiterName })
                .Select(x => new RestaurantWaiterSalesReportDto
                {
                    WaiterUserId = x.Key.WaiterUserId,
                    WaiterName = x.Key.WaiterName,
                    OrderCount = x.Count(),
                    GrandTotal = x.Sum(y => y.GrandTotal)
                })
                .OrderByDescending(x => x.GrandTotal)
                .ThenBy(x => x.WaiterName)
                .ToList();
        }

        public async Task<List<RestaurantDailySalesSummaryReportDto>> GetDailySalesSummary(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsSales);
            var rows = await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll()
                        .Include(x => x.TableFk)
                        .ThenInclude(x => x.AreaFk) on sales.SourceDocumentId equals (Guid?)order.Id
                    join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                    from waiter in waiters.DefaultIfEmpty()
                    select new
                    {
                        Date = sales.Date.Date,
                        OutletName = order.TableFk == null
                            ? (string.IsNullOrEmpty(order.Source) ? "Restaurant" : order.Source)
                            : order.TableFk.AreaFk == null ? "Restaurant" : order.TableFk.AreaFk.Name,
                        TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                        order.WaiterUserId,
                        UserName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                        sales.GrossAmount,
                        DiscountAmount = sales.BillDiscount,
                        sales.TaxAmount,
                        sales.NetAmount,
                        sales.GrandTotal
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.Date, x.OutletName, x.TableName, x.WaiterUserId, x.UserName })
                .Select(x =>
                {
                    var grandTotal = x.Sum(y => y.GrandTotal);
                    var orderCount = x.Count();

                    return new RestaurantDailySalesSummaryReportDto
                    {
                        Date = x.Key.Date,
                        OutletName = x.Key.OutletName,
                        TableName = x.Key.TableName,
                        UserId = x.Key.WaiterUserId,
                        UserName = x.Key.UserName,
                        OrderCount = orderCount,
                        GrossAmount = x.Sum(y => y.GrossAmount),
                        DiscountAmount = x.Sum(y => y.DiscountAmount),
                        TaxAmount = x.Sum(y => y.TaxAmount),
                        NetAmount = x.Sum(y => y.NetAmount),
                        GrandTotal = grandTotal,
                        AverageBill = orderCount == 0 ? 0 : grandTotal / orderCount
                    };
                })
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.OutletName)
                .ThenBy(x => x.TableName)
                .ThenBy(x => x.UserName)
                .ToList();
        }

        public async Task<List<RestaurantTicketStatusReportDto>> GetKotBotStatus(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsOperations);
            input ??= new RestaurantReportFilterDto();

            var query =
                from ticket in ticketRepository.GetAll()
                join order in orderRepository.GetAll().Include(x => x.TableFk) on ticket.OrderId equals order.Id
                join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                from waiter in waiters.DefaultIfEmpty()
                where ticket.TenantId == AbpSession.TenantId &&
                      (ticket.Status == RestaurantTicketStatus.Pending || ticket.Status == RestaurantTicketStatus.Cancelled)
                select new
                {
                    ticket.Id,
                    ticket.TicketNo,
                    order.OrderNo,
                    ticket.TicketType,
                    ticket.Status,
                    StationName = ticket.StationFk == null ? "" : ticket.StationFk.Name,
                    TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                    WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                    ticket.SentAt,
                    ticket.CancelledAt,
                    ticket.PrintCount,
                    ticket.CancelReason,
                    order.TableId,
                    order.WaiterUserId
                };

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                query = query.Where(x => x.SentAt >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.SentAt < toDate);
            }

            if (input.TableId.HasValue)
                query = query.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                query = query.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            if (input.CategoryId.HasValue)
            {
                query = query.Where(x => ticketItemRepository.GetAll().Any(y =>
                    y.TenantId == AbpSession.TenantId &&
                    y.TicketId == x.Id &&
                    y.OrderItemFk.MenuItemFk.CategoryId == input.CategoryId.Value));
            }

            var ticketRows = await query
                .AsNoTracking()
                .OrderByDescending(x => x.SentAt)
                .ToListAsync();

            var ticketIds = ticketRows.Select(x => x.Id).ToList();
            var itemAggregates = new Dictionary<Guid, (int ItemCount, decimal Qty)>();
            if (ticketIds.Count > 0)
            {
                var aggregateRows = await ticketItemRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId && ticketIds.Contains(x.TicketId))
                    .GroupBy(x => x.TicketId)
                    .Select(x => new
                    {
                        TicketId = x.Key,
                        ItemCount = x.Count(),
                        Qty = x.Sum(y => y.Qty)
                    })
                    .AsNoTracking()
                    .ToListAsync();

                itemAggregates = aggregateRows.ToDictionary(x => x.TicketId, x => (x.ItemCount, x.Qty));
            }

            return ticketRows.Select(x =>
                {
                    itemAggregates.TryGetValue(x.Id, out var aggregate);

                    return new RestaurantTicketStatusReportDto
                    {
                        TicketNo = x.TicketNo,
                        OrderNo = x.OrderNo,
                        TicketType = x.TicketType.ToString(),
                        Status = x.Status.ToString(),
                        StationName = x.StationName,
                        TableName = x.TableName,
                        WaiterName = x.WaiterName,
                        SentAt = x.SentAt,
                        CancelledAt = x.CancelledAt,
                        PrintCount = x.PrintCount,
                        ItemCount = aggregate.ItemCount,
                        Qty = aggregate.Qty,
                        CancelReason = x.CancelReason
                    };
                })
                .ToList();
        }

        public async Task<List<RestaurantItemMarginReportDto>> GetItemSalesWithMargin(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsSales);
            var salesRows = await (
                    from billLine in GetBilledBillLines(input)
                    join item in orderItemRepository.GetAll() on billLine.OrderItemId equals item.Id
                    join product in productRepository.GetAll() on item.ProductId equals product.Id
                    join menuItem in menuItemRepository.GetAll() on item.MenuItemId equals menuItem.Id into menuItems
                    from menuItem in menuItems.DefaultIfEmpty()
                    join category in categoryRepository.GetAll() on menuItem.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    where item.TenantId == AbpSession.TenantId &&
                          item.Status != RestaurantOrderItemStatus.Cancelled &&
                          (!input.CategoryId.HasValue || menuItem.CategoryId == input.CategoryId.Value)
                    select new
                    {
                        SalesMasterId = (Guid?)billLine.SalesMasterId,
                        item.ProductId,
                        ProductName = product.Name,
                        CategoryId = menuItem == null ? (Guid?)null : menuItem.CategoryId,
                        CategoryName = category == null ? "" : category.Name,
                        billLine.Qty,
                        billLine.GrossAmount,
                        billLine.DiscountAmount,
                        billLine.TaxAmount,
                        billLine.NetAmount,
                        billLine.Amount
                    })
                .AsNoTracking()
                .ToListAsync();

            if (salesRows.Count == 0)
                return new List<RestaurantItemMarginReportDto>();

            var salesMasterIds = salesRows
                .Where(x => x.SalesMasterId.HasValue)
                .Select(x => x.SalesMasterId.Value)
                .Distinct()
                .ToList();

            var costsByProduct = new Dictionary<Guid, decimal>();
            if (salesMasterIds.Count > 0)
            {
                var costRows = await stockPostingRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                x.MasterId.HasValue &&
                                salesMasterIds.Contains(x.MasterId.Value) &&
                                x.OutWardQty > 0)
                    .GroupBy(x => x.ProductId)
                    .Select(x => new
                    {
                        ProductId = x.Key,
                        CostAmount = x.Sum(y => y.Amount)
                    })
                    .AsNoTracking()
                    .ToListAsync();

                costsByProduct = costRows.ToDictionary(x => x.ProductId, x => x.CostAmount);
            }

            return salesRows
                .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryId, x.CategoryName })
                .Select(x =>
                {
                    costsByProduct.TryGetValue(x.Key.ProductId, out var costAmount);
                    var grandTotal = x.Sum(y => y.Amount);
                    var marginAmount = grandTotal - costAmount;

                    return new RestaurantItemMarginReportDto
                    {
                        ProductId = x.Key.ProductId,
                        ProductName = x.Key.ProductName,
                        CategoryId = x.Key.CategoryId,
                        CategoryName = x.Key.CategoryName,
                        Qty = x.Sum(y => y.Qty),
                        GrossAmount = x.Sum(y => y.GrossAmount),
                        DiscountAmount = x.Sum(y => y.DiscountAmount),
                        TaxAmount = x.Sum(y => y.TaxAmount),
                        NetAmount = x.Sum(y => y.NetAmount),
                        GrandTotal = grandTotal,
                        CostAmount = costAmount,
                        MarginAmount = marginAmount,
                        MarginPercent = grandTotal == 0 ? 0 : marginAmount * 100 / grandTotal
                    };
                })
                .OrderByDescending(x => x.MarginAmount)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantWaiterPerformanceReportDto>> GetWaiterPerformance(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsOperations);
            var orders = await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll()
                        .Include(x => x.TableSessionFk) on sales.SourceDocumentId equals (Guid?)order.Id
                    join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                    from waiter in waiters.DefaultIfEmpty()
                    select new
                    {
                        SalesMasterId = sales.Id,
                        order.WaiterUserId,
                        WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                        GuestCount = order.TableSessionFk == null ? 0 : order.TableSessionFk.GuestCount,
                        sales.GrossAmount,
                        DiscountAmount = sales.BillDiscount,
                        sales.GrandTotal
                    })
                .AsNoTracking()
                .ToListAsync();

            var salesMasterIds = orders.Select(x => x.SalesMasterId).ToList();
            var itemCounts = salesMasterIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await billLineRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                salesMasterIds.Contains(x.SalesMasterId))
                    .GroupBy(x => x.SalesMasterId)
                    .Select(x => new { SalesMasterId = x.Key, ItemCount = x.Count() })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.SalesMasterId, x => x.ItemCount);

            return orders
                .GroupBy(x => new { x.WaiterUserId, x.WaiterName })
                .Select(x =>
                {
                    var grandTotal = x.Sum(y => y.GrandTotal);
                    var orderCount = x.Count();

                    return new RestaurantWaiterPerformanceReportDto
                    {
                        WaiterUserId = x.Key.WaiterUserId,
                        WaiterName = x.Key.WaiterName,
                        OrderCount = orderCount,
                        ItemCount = x.Sum(y => itemCounts.TryGetValue(y.SalesMasterId, out var itemCount) ? itemCount : 0),
                        GuestCount = x.Sum(y => y.GuestCount),
                        GrossAmount = x.Sum(y => y.GrossAmount),
                        DiscountAmount = x.Sum(y => y.DiscountAmount),
                        GrandTotal = grandTotal,
                        AverageBill = orderCount == 0 ? 0 : grandTotal / orderCount
                    };
                })
                .OrderByDescending(x => x.GrandTotal)
                .ThenBy(x => x.WaiterName)
                .ToList();
        }

        public async Task<List<RestaurantTableTurnoverReportDto>> GetTableTurnover(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsOperations);
            input ??= new RestaurantReportFilterDto();

            var sessionQuery = tableSessionRepository.GetAll()
                .Include(x => x.TableFk)
                .Where(x => x.TenantId == AbpSession.TenantId);

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                sessionQuery = sessionQuery.Where(x => x.OpenedAt >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                sessionQuery = sessionQuery.Where(x => x.OpenedAt < toDate);
            }

            if (input.TableId.HasValue)
                sessionQuery = sessionQuery.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                sessionQuery = sessionQuery.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            var now = DateTime.Now;
            var sessions = await sessionQuery
                .Select(x => new
                {
                    x.Id,
                    x.TableId,
                    TableName = x.TableFk == null ? "Takeaway/Delivery" : x.TableFk.Name,
                    x.GuestCount,
                    x.OpenedAt,
                    x.ClosedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var sessionIds = sessions.Select(x => x.Id).ToList();
            var orderTotals = sessionIds.Count == 0
                ? new Dictionary<Guid, (int OrderCount, decimal GrandTotal)>()
                : (await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll() on sales.SourceDocumentId equals (Guid?)order.Id
                    where order.TableSessionId.HasValue && sessionIds.Contains(order.TableSessionId.Value)
                    group sales by order.TableSessionId.Value
                    into salesBySession
                    select new
                    {
                        TableSessionId = salesBySession.Key,
                        OrderCount = salesBySession.Count(),
                        GrandTotal = salesBySession.Sum(y => y.GrandTotal)
                    })
                    .AsNoTracking()
                    .ToListAsync())
                .ToDictionary(x => x.TableSessionId, x => (x.OrderCount, x.GrandTotal));

            return sessions
                .GroupBy(x => new { x.TableId, x.TableName })
                .Select(x =>
                {
                    var totalMinutes = x.Sum(y => Math.Max(0, (decimal)((y.ClosedAt ?? now) - y.OpenedAt).TotalMinutes));
                    var orderCount = x.Sum(y =>
                        orderTotals.TryGetValue(y.Id, out var orderTotal) ? orderTotal.OrderCount : 0);
                    var grandTotal = x.Sum(y =>
                        orderTotals.TryGetValue(y.Id, out var orderTotal) ? orderTotal.GrandTotal : 0);
                    var sessionCount = x.Count();

                    return new RestaurantTableTurnoverReportDto
                    {
                        TableId = x.Key.TableId,
                        TableName = x.Key.TableName,
                        SessionCount = sessionCount,
                        OrderCount = orderCount,
                        GuestCount = x.Sum(y => y.GuestCount),
                        TotalMinutes = totalMinutes,
                        AverageMinutes = sessionCount == 0 ? 0 : totalMinutes / sessionCount,
                        GrandTotal = grandTotal,
                        AverageBill = orderCount == 0 ? 0 : grandTotal / orderCount
                    };
                })
                .OrderByDescending(x => x.SessionCount)
                .ThenBy(x => x.TableName)
                .ToList();
        }

        public async Task<List<RestaurantVoidAuditReportDto>> GetVoidCancelledAudit(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsAuditFinance);
            input ??= new RestaurantReportFilterDto();
            var rows = new List<RestaurantVoidAuditReportDto>();

            var cancelledOrderQuery =
                from order in orderRepository.GetAll().Include(x => x.TableFk)
                join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                from waiter in waiters.DefaultIfEmpty()
                where order.TenantId == AbpSession.TenantId &&
                      order.Status == RestaurantOrderStatus.Cancelled
                select new
                {
                    Date = order.BilledAt ?? order.CreatedAt,
                    order.OrderNo,
                    TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                    WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                    Amount = order.GrandTotal,
                    Reason = order.Notes,
                    order.TableId,
                    order.WaiterUserId
                };

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                cancelledOrderQuery = cancelledOrderQuery.Where(x => x.Date >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                cancelledOrderQuery = cancelledOrderQuery.Where(x => x.Date < toDate);
            }

            if (input.TableId.HasValue)
                cancelledOrderQuery = cancelledOrderQuery.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                cancelledOrderQuery = cancelledOrderQuery.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            rows.AddRange((await cancelledOrderQuery.AsNoTracking().ToListAsync()).Select(x =>
                new RestaurantVoidAuditReportDto
                {
                    Date = x.Date,
                    OrderNo = x.OrderNo,
                    TicketNo = "",
                    TableName = x.TableName,
                    WaiterName = x.WaiterName,
                    ItemName = "Bill",
                    Qty = 0,
                    Amount = x.Amount,
                    Status = "Bill Cancelled",
                    Reason = x.Reason
                }));

            var cancelledItemQuery =
                from item in orderItemRepository.GetAll()
                join order in orderRepository.GetAll().Include(x => x.TableFk) on item.OrderId equals order.Id
                join product in productRepository.GetAll() on item.ProductId equals product.Id
                join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                from waiter in waiters.DefaultIfEmpty()
                where item.TenantId == AbpSession.TenantId &&
                      item.Status == RestaurantOrderItemStatus.Cancelled &&
                      (!input.CategoryId.HasValue || item.MenuItemFk.CategoryId == input.CategoryId.Value)
                select new
                {
                    item.CreatedAt,
                    order.OrderNo,
                    TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                    WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                    ItemName = string.IsNullOrEmpty(item.ItemNameSnapshot) ? product.Name : item.ItemNameSnapshot,
                    item.Qty,
                    item.Amount,
                    Reason = item.CancelReason,
                    order.TableId,
                    order.WaiterUserId
                };

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                cancelledItemQuery = cancelledItemQuery.Where(x => x.CreatedAt >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                cancelledItemQuery = cancelledItemQuery.Where(x => x.CreatedAt < toDate);
            }

            if (input.TableId.HasValue)
                cancelledItemQuery = cancelledItemQuery.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                cancelledItemQuery = cancelledItemQuery.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            rows.AddRange((await cancelledItemQuery.AsNoTracking().ToListAsync()).Select(x =>
                new RestaurantVoidAuditReportDto
                {
                    Date = x.CreatedAt,
                    OrderNo = x.OrderNo,
                    TicketNo = "",
                    TableName = x.TableName,
                    WaiterName = x.WaiterName,
                    ItemName = x.ItemName,
                    Qty = x.Qty,
                    Amount = x.Amount,
                    Status = "Item Cancelled",
                    Reason = x.Reason
                }));

            var cancelledTicketQuery =
                from ticket in ticketRepository.GetAll()
                join order in orderRepository.GetAll().Include(x => x.TableFk) on ticket.OrderId equals order.Id
                join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                from waiter in waiters.DefaultIfEmpty()
                where ticket.TenantId == AbpSession.TenantId &&
                      ticket.Status == RestaurantTicketStatus.Cancelled
                select new
                {
                    Date = ticket.CancelledAt ?? ticket.SentAt,
                    ticket.TicketNo,
                    order.OrderNo,
                    TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                    WaiterName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                    ticket.CancelReason,
                    order.TableId,
                    order.WaiterUserId,
                    ticket.Id
                };

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                cancelledTicketQuery = cancelledTicketQuery.Where(x => x.Date >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                cancelledTicketQuery = cancelledTicketQuery.Where(x => x.Date < toDate);
            }

            if (input.TableId.HasValue)
                cancelledTicketQuery = cancelledTicketQuery.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                cancelledTicketQuery = cancelledTicketQuery.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            if (input.CategoryId.HasValue)
            {
                cancelledTicketQuery = cancelledTicketQuery.Where(x => ticketItemRepository.GetAll().Any(y =>
                    y.TenantId == AbpSession.TenantId &&
                    y.TicketId == x.Id &&
                    y.OrderItemFk.MenuItemFk.CategoryId == input.CategoryId.Value));
            }

            var cancelledTickets = await cancelledTicketQuery.AsNoTracking().ToListAsync();
            var cancelledTicketIds = cancelledTickets.Select(x => x.Id).ToList();
            var cancelledTicketQty = cancelledTicketIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await ticketItemRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId && cancelledTicketIds.Contains(x.TicketId))
                    .GroupBy(x => x.TicketId)
                    .Select(x => new { TicketId = x.Key, Qty = x.Sum(y => y.Qty) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.TicketId, x => x.Qty);

            rows.AddRange(cancelledTickets.Select(x =>
            {
                cancelledTicketQty.TryGetValue(x.Id, out var qty);

                return new RestaurantVoidAuditReportDto
                {
                    Date = x.Date,
                    OrderNo = x.OrderNo,
                    TicketNo = x.TicketNo,
                    TableName = x.TableName,
                    WaiterName = x.WaiterName,
                    ItemName = "KOT/BOT",
                    Qty = qty,
                    Amount = 0,
                    Status = "Ticket Cancelled",
                    Reason = x.CancelReason
                };
            }));

            return rows
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.OrderNo)
                .ToList();
        }

        public async Task<List<RestaurantDiscountReportDto>> GetDiscountReport(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsAuditFinance);
            var orders = await (
                    from sales in GetBilledSales(input)
                    join order in orderRepository.GetAll().Include(x => x.TableFk) on sales.SourceDocumentId equals (Guid?)order.Id
                    join waiter in userRepository.GetAll() on order.WaiterUserId equals waiter.Id into waiters
                    from waiter in waiters.DefaultIfEmpty()
                    select new
                    {
                        SalesMasterId = sales.Id,
                        Date = sales.Date,
                        order.OrderNo,
                        UserName = waiter == null ? "Unassigned" : waiter.Name + " " + waiter.Surname,
                        TableName = order.TableFk == null ? "Takeaway/Delivery" : order.TableFk.Name,
                        order.CustomerName,
                        sales.GrossAmount,
                        DiscountAmount = sales.BillDiscount,
                        order.Notes
                    })
                .AsNoTracking()
                .ToListAsync();

            var salesMasterIds = orders.Select(x => x.SalesMasterId).ToList();
            var itemDiscounts = salesMasterIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await billLineRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                salesMasterIds.Contains(x.SalesMasterId) &&
                                (!input.CategoryId.HasValue || x.OrderItemFk.MenuItemFk.CategoryId == input.CategoryId.Value))
                    .GroupBy(x => x.SalesMasterId)
                    .Select(x => new { SalesMasterId = x.Key, DiscountAmount = x.Sum(y => y.DiscountAmount) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.SalesMasterId, x => x.DiscountAmount);

            return orders
                .Select(x =>
                {
                    itemDiscounts.TryGetValue(x.SalesMasterId, out var itemDiscountAmount);
                    var totalDiscountAmount = x.DiscountAmount + itemDiscountAmount;

                    return new RestaurantDiscountReportDto
                    {
                        Date = x.Date,
                        OrderNo = x.OrderNo,
                        UserName = x.UserName,
                        TableName = x.TableName,
                        CustomerName = x.CustomerName,
                        GrossAmount = x.GrossAmount,
                        OrderDiscountAmount = x.DiscountAmount,
                        ItemDiscountAmount = itemDiscountAmount,
                        TotalDiscountAmount = totalDiscountAmount,
                        Reason = string.IsNullOrWhiteSpace(x.Notes) ? "No reason stored" : x.Notes
                    };
                })
                .Where(x => x.TotalDiscountAmount > 0)
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.OrderNo)
                .ToList();
        }

        public async Task<List<RestaurantSettlementReportDto>> GetSettlementReport(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsAuditFinance);
            var rows = await GetBilledSales(input)
                .Select(sales => new
                    {
                        sales.PaymentMethod,
                        sales.GrossAmount,
                        DiscountAmount = sales.BillDiscount,
                        sales.TaxAmount,
                        sales.NetAmount,
                        sales.GrandTotal
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .GroupBy(x => x.PaymentMethod)
                .Select(x => new RestaurantSettlementReportDto
                {
                    PaymentMethod = (int)x.Key,
                    PaymentMethodName = GetPaymentMethodName(x.Key),
                    OrderCount = x.Count(),
                    GrossAmount = x.Sum(y => y.GrossAmount),
                    DiscountAmount = x.Sum(y => y.DiscountAmount),
                    TaxAmount = x.Sum(y => y.TaxAmount),
                    NetAmount = x.Sum(y => y.NetAmount),
                    GrandTotal = x.Sum(y => y.GrandTotal)
                })
                .OrderBy(x => x.PaymentMethodName)
                .ToList();
        }

        public async Task<List<RestaurantRecipeCostingReportDto>> GetRecipeCosting(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsInventory);
            input ??= new RestaurantReportFilterDto();

            var menuItems = await menuItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.CategoryFk)
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            !x.IsDeleted &&
                            x.IsActive &&
                            (!input.CategoryId.HasValue || x.CategoryId == input.CategoryId.Value))
                .OrderBy(x => x.CategoryFk.Name)
                .ThenBy(x => x.DisplayName)
                .AsNoTracking()
                .ToListAsync();

            var productIds = menuItems.Select(x => x.ProductId).Distinct().ToList();
            var recipeLines = productIds.Count == 0
                ? new List<Bom>()
                : await bomRepository.GetAll()
                    .Include(x => x.RawMaterialFk)
                    .Include(x => x.UnitFk)
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                productIds.Contains(x.ProductId) &&
                                !x.IsDeleted &&
                                x.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

            return menuItems.Select(item =>
                {
                    var lines = recipeLines
                        .Where(x => x.ProductId == item.ProductId)
                        .Select(x =>
                        {
                            var rate = x.CostRate > 0 ? x.CostRate : x.RawMaterialFk.PurchaseRate;
                            var costAmount = x.Quantity * (1 + x.WastagePercentage / 100) * rate;
                            return new RestaurantRecipeCostingLineDto
                            {
                                RawMaterialId = x.RawMaterialId,
                                RawMaterialName = x.RawMaterialFk.Name,
                                Quantity = x.Quantity,
                                UnitId = x.UnitId,
                                UnitName = x.UnitFk.Name,
                                WastagePercentage = x.WastagePercentage,
                                CostRate = rate,
                                CostAmount = costAmount
                            };
                        })
                        .ToList();
                    var recipeCost = lines.Sum(x => x.CostAmount);

                    return new RestaurantRecipeCostingReportDto
                    {
                        ProductId = item.ProductId,
                        ProductName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.ProductFk.Name : item.DisplayName,
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryFk == null ? "" : item.CategoryFk.Name,
                        MenuPrice = item.Price,
                        RecipeCost = recipeCost,
                        FoodCostPercent = item.Price == 0 ? 0 : recipeCost * 100 / item.Price,
                        MarginAmount = item.Price - recipeCost,
                        Lines = lines
                    };
                })
                .OrderByDescending(x => x.FoodCostPercent)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantFoodCostingReportDto>> GetFoodCosting(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsInventory);
            input ??= new RestaurantReportFilterDto();

            var salesRows = await (
                    from billLine in GetBilledBillLines(input)
                    join item in orderItemRepository.GetAll() on billLine.OrderItemId equals item.Id
                    join product in productRepository.GetAll() on item.ProductId equals product.Id
                    join menuItem in menuItemRepository.GetAll() on item.MenuItemId equals menuItem.Id into menuItems
                    from menuItem in menuItems.DefaultIfEmpty()
                    join category in categoryRepository.GetAll() on menuItem.CategoryId equals category.Id into categories
                    from category in categories.DefaultIfEmpty()
                    where item.TenantId == AbpSession.TenantId &&
                          item.Status != RestaurantOrderItemStatus.Cancelled &&
                          (!input.CategoryId.HasValue || menuItem.CategoryId == input.CategoryId.Value)
                    select new
                    {
                        billLine.SalesDetailId,
                        item.ProductId,
                        ProductName = string.IsNullOrEmpty(item.ItemNameSnapshot) ? product.Name : item.ItemNameSnapshot,
                        item.UnitId,
                        ProductUnitId = product.UnitId,
                        CategoryId = menuItem == null ? (Guid?)null : menuItem.CategoryId,
                        CategoryName = category == null ? "" : category.Name,
                        billLine.Qty,
                        SalesAmount = billLine.Amount
                    })
                .AsNoTracking()
                .ToListAsync();

            if (salesRows.Count == 0)
                return new List<RestaurantFoodCostingReportDto>();

            var productIds = salesRows.Select(x => x.ProductId).Distinct().ToList();
            var recipeLines = await bomRepository.GetAll()
                .Include(x => x.RawMaterialFk)
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            productIds.Contains(x.ProductId) &&
                            !x.IsDeleted &&
                            x.IsActive)
                .AsNoTracking()
                .ToListAsync();

            var salesDetailIds = salesRows
                .Where(x => x.SalesDetailId.HasValue)
                .Select(x => x.SalesDetailId.Value)
                .Distinct()
                .ToList();

            var actualCostBySalesDetail = salesDetailIds.Count == 0
                ? new Dictionary<Guid, decimal>()
                : await stockPostingRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                x.SourceDetailId.HasValue &&
                                salesDetailIds.Contains(x.SourceDetailId.Value) &&
                                x.OutWardQty > 0 &&
                                !x.IsDeleted)
                    .GroupBy(x => x.SourceDetailId.Value)
                    .Select(x => new { SalesDetailId = x.Key, Amount = x.Sum(y => y.Amount) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.SalesDetailId, x => x.Amount);

            var wastageByRawMaterial = await GetWastageByRawMaterial(input);

            var rows = new List<RestaurantFoodCostingReportDto>();
            foreach (var group in salesRows.GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryId, x.CategoryName }))
            {
                decimal theoreticalCost = 0;
                decimal actualCost = 0;
                foreach (var row in group)
                {
                    var productQty = await ConvertQtyAsync(row.ProductId, row.UnitId, row.ProductUnitId, row.Qty);
                    theoreticalCost += recipeLines
                        .Where(x => x.ProductId == row.ProductId)
                        .Sum(x =>
                        {
                            var rate = x.CostRate > 0 ? x.CostRate : x.RawMaterialFk.PurchaseRate;
                            return productQty * x.Quantity * (1 + x.WastagePercentage / 100) * rate;
                        });

                    if (row.SalesDetailId.HasValue && actualCostBySalesDetail.TryGetValue(row.SalesDetailId.Value, out var rowActualCost))
                        actualCost += rowActualCost;
                }

                var wastageCost = recipeLines
                    .Where(x => x.ProductId == group.Key.ProductId)
                    .Select(x => x.RawMaterialId)
                    .Distinct()
                    .Sum(rawMaterialId => wastageByRawMaterial.TryGetValue(rawMaterialId, out var amount) ? amount : 0);
                var salesAmount = group.Sum(x => x.SalesAmount);
                var costAmount = actualCost > 0 ? actualCost : theoreticalCost;

                rows.Add(new RestaurantFoodCostingReportDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    CategoryId = group.Key.CategoryId,
                    CategoryName = group.Key.CategoryName,
                    SoldQty = group.Sum(x => x.Qty),
                    SalesAmount = salesAmount,
                    TheoreticalCostAmount = theoreticalCost,
                    ActualCostAmount = actualCost,
                    WastageCostAmount = wastageCost,
                    MarginAmount = salesAmount - costAmount - wastageCost,
                    FoodCostPercent = salesAmount == 0 ? 0 : (costAmount + wastageCost) * 100 / salesAmount
                });
            }

            return rows
                .OrderByDescending(x => x.FoodCostPercent)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantWastageReportDto>> GetWastageReport(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsInventory);
            input ??= new RestaurantReportFilterDto();
            var adjustmentQuery = stockAdjustmentRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            !x.IsDeleted &&
                            x.AdjustmentType == RestaurantStockAdjustmentType.Wastage);

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                adjustmentQuery = adjustmentQuery.Where(x => x.Date >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                adjustmentQuery = adjustmentQuery.Where(x => x.Date < toDate);
            }

            var rows = await (
                    from adjustment in adjustmentQuery
                    join line in stockAdjustmentLineRepository.GetAll() on adjustment.Id equals line.StockAdjustmentId
                    join product in productRepository.GetAll() on line.ProductId equals product.Id
                    join unit in unitRepository.GetAll() on line.UnitId equals unit.Id
                    join user in userRepository.GetAll() on adjustment.CreateUserId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    where line.TenantId == AbpSession.TenantId && !line.IsDeleted
                    select new RestaurantWastageReportDto
                    {
                        Date = adjustment.Date,
                        DateMiti = adjustment.DateMiti,
                        VoucherNo = adjustment.VoucherNo,
                        ProductId = line.ProductId,
                        ProductName = product.Name,
                        UnitId = line.UnitId,
                        UnitName = unit.Name,
                        Qty = line.Qty,
                        Rate = line.Rate,
                        Amount = line.Amount,
                        Reason = line.Reason,
                        UserName = user == null ? "" : user.Name + " " + user.Surname
                    })
                .AsNoTracking()
                .ToListAsync();

            return rows
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<RestaurantLowStockSuggestionDto>> GetLowStockReport(RestaurantReportFilterDto input)
        {
            await EnsureReportCategoryGrantedAsync(AppPermissions.PagesRestaurantReportsInventory);
            return await reorderService.GetLowStockSuggestionsAsync();
        }

        private async Task EnsureReportCategoryGrantedAsync(string permission)
        {
            if (await PermissionChecker.IsGrantedAsync(permission))
                return;

            foreach (var categoryPermission in ReportCategoryPermissions)
            {
                if (await PermissionChecker.IsGrantedAsync(categoryPermission))
                    throw new AbpAuthorizationException(L("PermissionIsNotGranted", permission));
            }

            // A role with only the original Reports permission predates report
            // categories, so it retains full report access until explicitly split.
        }

        private async Task<Dictionary<Guid, decimal>> GetWastageByRawMaterial(RestaurantReportFilterDto input)
        {
            var adjustmentQuery = stockAdjustmentRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            !x.IsDeleted &&
                            x.AdjustmentType == RestaurantStockAdjustmentType.Wastage);

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                adjustmentQuery = adjustmentQuery.Where(x => x.Date >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                adjustmentQuery = adjustmentQuery.Where(x => x.Date < toDate);
            }

            return await (
                    from adjustment in adjustmentQuery
                    join line in stockAdjustmentLineRepository.GetAll() on adjustment.Id equals line.StockAdjustmentId
                    where line.TenantId == AbpSession.TenantId && !line.IsDeleted
                    group line by line.ProductId
                    into wastageByProduct
                    select new
                    {
                        ProductId = wastageByProduct.Key,
                        Amount = wastageByProduct.Sum(x => x.Amount)
                    })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ProductId, x => x.Amount);
        }

        private async Task<decimal> ConvertQtyAsync(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
        {
            if (qty == 0 || fromUnitId == toUnitId)
                return qty;

            var conversions = await unitConversionRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            var from = conversions.FirstOrDefault(x => x.UnitId == fromUnitId);
            var to = conversions.FirstOrDefault(x => x.UnitId == toUnitId);
            if (from == null || to == null || from.Qty == 0 || to.PrimaryQty == 0)
                return qty;

            var primaryQty = qty * from.PrimaryQty / from.Qty;
            return primaryQty * to.Qty / to.PrimaryQty;
        }

        private IQueryable<RestaurantOrder> GetBilledOrders(RestaurantReportFilterDto input)
        {
            var sourceOrderIds = GetBilledSales(input).Select(x => x.SourceDocumentId);

            return orderRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            sourceOrderIds.Contains((Guid?)x.Id));
        }

        private IQueryable<RestaurantBillLine> GetBilledBillLines(RestaurantReportFilterDto input)
        {
            var salesMasterIds = GetBilledSales(input).Select(x => x.Id);

            return billLineRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            salesMasterIds.Contains(x.SalesMasterId));
        }

        private IQueryable<SalesMaster> GetBilledSales(RestaurantReportFilterDto input)
        {
            input ??= new RestaurantReportFilterDto();

            var query = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            !x.IsDelete &&
                            x.SourceModule == "Restaurant" &&
                            x.SourceDocumentId.HasValue);

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;
                query = query.Where(x => x.Date >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.Date < toDate);
            }

            if (input.TableId.HasValue)
            {
                query =
                    from sales in query
                    join order in orderRepository.GetAll() on sales.SourceDocumentId equals (Guid?)order.Id
                    where order.TableId == input.TableId.Value
                    select sales;
            }

            if (input.WaiterUserId.HasValue)
            {
                query =
                    from sales in query
                    join order in orderRepository.GetAll() on sales.SourceDocumentId equals (Guid?)order.Id
                    where order.WaiterUserId == input.WaiterUserId.Value
                    select sales;
            }

            return query;
        }

        private static string GetPaymentMethodName(PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.Cash => "Cash",
                PaymentMethod.Cheque => "Cheque",
                PaymentMethod.Credit => "Credit",
                PaymentMethod.Card_Swipe => "Card",
                PaymentMethod.QR => "QR",
                _ => paymentMethod.ToString()
            };
        }
    }
}
