using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.InventoryReport
{
    [AbpAuthorize(AppPermissions.PagesProductProfitReport)]

    public class ProductProfitReport(
    IRepository<StockPosting, Guid> stockPostingRepository,
    IRepository<UnitConversion, Guid> unitConversionRepository,
    IRepository<FinancialYear, Guid> financialYeaRepository,
    StockCalculationService stockCalculationService) : ErpAppServiceBase
    {
        /// <summary>
        /// Calculate closing value based on stock valuation method
        /// </summary>
        private static decimal CalculateClosingValue(
            ProductProfitReportDto profitReport,
            string stockCalculationMethod,
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock)
        {
            if (profitReport.ClosingQty <= 0) return 0;

            return stockCalculationMethod?.ToUpperInvariant() switch
            {
                "AVERAGE" => CalculateAverageClosingValue(profitReport),
                "FIFO" => CalculateFifoClosingValue(openingStock, purchaseStock, salesStock, profitReport.ClosingQty),
                "LIFO" => CalculateLifoClosingValue(openingStock, purchaseStock, salesStock, profitReport.ClosingQty),
                _ => CalculateAverageClosingValue(profitReport)
            };
        }

        /// <summary>
        /// Calculate closing value using average cost method
        /// </summary>
        private static decimal CalculateAverageClosingValue(ProductProfitReportDto profitReport)
        {
            var totalCostValue = profitReport.OpeningStockValue + profitReport.InwardValue;
            var totalCostQty = profitReport.OpeningStockQty + profitReport.InwardQty;

            var averageCostRate = totalCostQty > 0 ? totalCostValue / totalCostQty : 0;
            return Math.Round(averageCostRate * profitReport.ClosingQty, 2);
        }

        /// <summary>
        /// Calculate closing value using FIFO method
        /// </summary>
        private static decimal CalculateFifoClosingValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock,
            decimal closingQty)
        {
            if (closingQty <= 0) return 0;

            // Create incoming products queue (FIFO order)
            var incomingQueue = new Queue<(decimal Rate, decimal Qty)>();

            // Add opening stock first (FIFO)
            foreach (var stock in openingStock.OrderBy(x => x.Date))
            {
                if (stock.InWardQty > 0)
                    incomingQueue.Enqueue((stock.Rate, stock.InWardQty));
            }

            // Add purchases in chronological order
            foreach (var purchase in purchaseStock.OrderBy(x => x.Date))
            {
                if (purchase.InWardQty > 0)
                    incomingQueue.Enqueue((purchase.Rate, purchase.InWardQty));
            }

            // Process sales to consume inventory (FIFO)
            var totalOutwardQty = salesStock.Sum(x => x.OutWardQty);
            var remainingOutward = totalOutwardQty;

            while (remainingOutward > 0 && incomingQueue.Count > 0)
            {
                var (rate, availableQty) = incomingQueue.Dequeue();

                if (availableQty <= remainingOutward)
                {
                    // Fully consumed
                    remainingOutward -= availableQty;
                }
                else
                {
                    // Partially consumed - put back the remainder
                    incomingQueue.Enqueue((rate, availableQty - remainingOutward));
                    remainingOutward = 0;
                }
            }

            // Calculate value of remaining inventory
            decimal closingValue = 0;
            var remainingClosingQty = closingQty;

            while (remainingClosingQty > 0 && incomingQueue.Count > 0)
            {
                var (rate, availableQty) = incomingQueue.Dequeue();

                if (availableQty <= remainingClosingQty)
                {
                    closingValue += availableQty * rate;
                    remainingClosingQty -= availableQty;
                }
                else
                {
                    closingValue += remainingClosingQty * rate;
                    remainingClosingQty = 0;
                }
            }

            return Math.Round(closingValue, 2);
        }

        /// <summary>
        /// Calculate closing value using LIFO method
        /// </summary>
        private static decimal CalculateLifoClosingValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock,
            decimal closingQty)
        {
            if (closingQty <= 0) return 0;

            // Create incoming products stack (LIFO order)
            var incomingStack = new Stack<(decimal Rate, decimal Qty)>();

            // Add opening stock first (will be at bottom of stack)
            foreach (var stock in openingStock.OrderBy(x => x.Date))
            {
                if (stock.InWardQty > 0)
                    incomingStack.Push((stock.Rate, stock.InWardQty));
            }

            // Add purchases in chronological order (latest will be at top)
            foreach (var purchase in purchaseStock.OrderBy(x => x.Date))
            {
                if (purchase.InWardQty > 0)
                    incomingStack.Push((purchase.Rate, purchase.InWardQty));
            }

            // Process sales to consume inventory (LIFO)
            var totalOutwardQty = salesStock.Sum(x => x.OutWardQty);
            var remainingOutward = totalOutwardQty;

            var tempStack = new Stack<(decimal Rate, decimal Qty)>();

            while (remainingOutward > 0 && incomingStack.Count > 0)
            {
                var (rate, availableQty) = incomingStack.Pop();

                if (availableQty <= remainingOutward)
                {
                    // Fully consumed
                    remainingOutward -= availableQty;
                }
                else
                {
                    // Partially consumed - put back the remainder
                    tempStack.Push((rate, availableQty - remainingOutward));
                    remainingOutward = 0;
                }
            }

            // Restore remaining inventory to stack
            while (tempStack.Count > 0)
            {
                incomingStack.Push(tempStack.Pop());
            }

            // Calculate value of remaining inventory (from oldest first for LIFO closing value)
            decimal closingValue = 0;
            var remainingClosingQty = closingQty;
            var finalStack = new Stack<(decimal Rate, decimal Qty)>();

            // Reverse the stack to get oldest items first for closing value
            while (incomingStack.Count > 0)
            {
                finalStack.Push(incomingStack.Pop());
            }

            while (remainingClosingQty > 0 && finalStack.Count > 0)
            {
                var (rate, availableQty) = finalStack.Pop();

                if (availableQty <= remainingClosingQty)
                {
                    closingValue += availableQty * rate;
                    remainingClosingQty -= availableQty;
                }
                else
                {
                    closingValue += remainingClosingQty * rate;
                    remainingClosingQty = 0;
                }
            }

            return Math.Round(closingValue, 2);
        }

        /// <summary>
        /// Optimized profit report generation with batch processing and efficient algorithms
        /// </summary>
        public async Task<List<ProductProfitReportDto>> GetReport(
            [CanBeNull] string fromMiti,
            [CanBeNull] string toMiti)
        {
            var financialYearId = FinancialYearId;
            var tenantId = AbpSession.TenantId;

            // Convert dates once
            var fromDate = string.IsNullOrWhiteSpace(fromMiti)
                ? FinancialYear.FromDate.Date
                : DateConverter.ConvertToEnglish(fromMiti);
            var toDate = string.IsNullOrWhiteSpace(toMiti)
                ? FinancialYear.ToDate.Date
                : DateConverter.ConvertToEnglish(toMiti);

            // Pre-load unit conversion data to avoid repeated queries
            var unitConversions = await unitConversionRepository.GetAll()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsNoTracking()
                .Include(x => x.UnitFk)
                .Select(x => new UnitConversionDto
                {
                    ProductId = x.ProductId,
                    UnitId = x.UnitId,
                    PrimaryQty = x.PrimaryQty,
                    Qty = x.Qty,
                    UnitName = x.UnitFk.Name,
                    ConversionRate = x.ConversionRate
                })
                .ToListAsync();

            // Build optimized query with all necessary filters applied at once
            var stockQuery = stockPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.FinancialYearId == financialYearId &&
                            !x.IsDeleted &&
                            x.ProductFk.ProductType != ProductTypeEnum.FixedAsset &&
                            x.Date.Date >= fromDate.Date &&
                            x.Date.Date <= toDate.Date);
                      

            // Execute query with optimized projection
            var stockPostingData = await stockQuery
                .AsSplitQuery()
                .Select(x => new StockPostingDto
                {
                    ProductId = x.ProductId,
                    ProductType = x.ProductFk.ProductType,
                    Date = x.Date,
                    VoucherType = x.VoucherTypeFk.Name,
                    ProductGroup = x.ProductFk.ProductGroupFk.Name,
                    ProductName = x.ProductFk.Name,
                    Rate = x.Rate,
                    UnitId = x.UnitId,
                    InWardQty = x.InWardQty,
                    OutWardQty = x.OutWardQty
                })
                .ToListAsync();

            if (!stockPostingData.Any()) return new List<ProductProfitReportDto>();

            // Process the data using the optimized stock calculation service
            var productReports = stockCalculationService.ProcessStockPostings(stockPostingData, unitConversions);

            // Get stock calculation method from settings
            var stockCalculationMethod = "fifo";
                
                //await SettingManager.GetSettingValueForTenantAsync(
                //AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId());

            // Calculate profit reports efficiently
            var profitReports = CalculateProfitReports(productReports, stockCalculationMethod, unitConversions);

            return profitReports.OrderBy(x => x.ProductName.Trim()).ToList();
        }

        /// <summary>
        /// Efficient profit calculation with batch processing and optimized algorithms
        /// </summary>
        private List<ProductProfitReportDto> CalculateProfitReports(
            List<NewProductReport> productReports,
            string stockCalculationMethod,
            List<UnitConversionDto> unitConversions)
        {
            var results = new List<ProductProfitReportDto>();

            // Group by product for efficient processing
            var productGroups = productReports
                .GroupBy(p => new { p.ProductId, p.ProductName, p.ProductGroup });

            foreach (var group in productGroups)
            {
                var productPostings = group.ToList();
                if (!productPostings.Any()) continue;

                var unitName = unitConversions
                    .FirstOrDefault(p => p.ProductId == group.Key.ProductId)?.UnitName ?? string.Empty;

                // Categorize stock movements efficiently
                var openingStock = productPostings.Where(x => x.VoucherType == "OpeningStock").ToList();
                var purchaseStock = productPostings.Where(x => IsPurchaseVoucherType(x.VoucherType)).ToList();
                var salesStock = productPostings.Where(x => IsSalesVoucherType(x.VoucherType)).ToList();

                // Calculate basic quantities
                var profitReport = new ProductProfitReportDto
                {
                    InwardRate = 0,
                    OutwardRate = 0,
                    ClosingQty = 0,
                    ClosingValue = 0,
                    ProfitAmount = 0,

                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    ProductGroup = group.Key.ProductGroup,
                    UnitName = unitName,
                    OpeningStockQty = openingStock.Sum(x => x.InWardQty),
                    OpeningStockValue = openingStock.Sum(x => x.InWardValue),
                    InwardQty = purchaseStock.Sum(x => x.InWardQty),
                    InwardValue = purchaseStock.Sum(x => x.InWardValue),
                    OutwardQty = salesStock.Sum(x => x.OutWardQty),
                    OutwardValue = salesStock.Sum(x => x.OutWardValue)
                };

                profitReport.ClosingQty = profitReport.OpeningStockQty + profitReport.InwardQty - profitReport.OutwardQty;

                // Calculate rates and profit based on method
                CalculateRatesAndProfit(profitReport, stockCalculationMethod, openingStock, purchaseStock, salesStock);

                // Calculate closing value using proper stock valuation method
                profitReport.ClosingValue = CalculateClosingValue(profitReport, stockCalculationMethod, openingStock, purchaseStock, salesStock);

                results.Add(profitReport);
            }

            return results;
        }

        /// <summary>
        /// Optimized rate and profit calculation with strategy pattern
        /// </summary>
        private static void CalculateRatesAndProfit(
            ProductProfitReportDto profitReport,
            string stockCalculationMethod,
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock)
        {
            switch (stockCalculationMethod?.ToUpperInvariant())
            {
                case "AVERAGE":
                    CalculateAverageMethod(profitReport, salesStock);
                    break;
                case "FIFO":
                    CalculateFifoMethod(profitReport, openingStock, purchaseStock, salesStock);
                    break;
                case "LIFO":
                    CalculateLifoMethod(profitReport, openingStock, purchaseStock, salesStock);
                    break;
                default:
                    CalculateAverageMethod(profitReport, salesStock);
                    break;
            }

            // Round profit amount
            profitReport.ProfitAmount = Math.Round(profitReport.ProfitAmount, 2);
        }

        /// <summary>
        /// Optimized average method calculation
        /// </summary>
        private static void CalculateAverageMethod(
            ProductProfitReportDto profitReport,
            List<NewProductReport> salesStock)
        {
            // Calculate weighted average inward rate
            var totalInwardValue = profitReport.OpeningStockValue + profitReport.InwardValue;
            var totalInwardQty = profitReport.OpeningStockQty + profitReport.InwardQty;

            profitReport.InwardRate = totalInwardQty > 0 ? totalInwardValue / totalInwardQty : 0;

            // Calculate outward rate
            profitReport.OutwardRate = profitReport.OutwardQty > 0 ? profitReport.OutwardValue / profitReport.OutwardQty : 0;

            // Calculate profit (outward value minus cost of goods sold)
            var actualSalesQty = profitReport.OutwardQty - salesStock.Sum(x => x.InWardQty); // Net sales
            profitReport.ProfitAmount = actualSalesQty > 0 ? (profitReport.OutwardRate - profitReport.InwardRate) * actualSalesQty : 0;
        }

        /// <summary>
        /// Optimized FIFO method calculation
        /// </summary>
        private static void CalculateFifoMethod(
            ProductProfitReportDto profitReport,
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock)
        {
            // Calculate rates (similar to average for display purposes)
            CalculateAverageMethod(profitReport, salesStock);

            // Calculate FIFO profit
            profitReport.ProfitAmount = CalculateFifoProfit(openingStock, purchaseStock, salesStock);
        }

        /// <summary>
        /// Optimized LIFO method calculation
        /// </summary>
        private static void CalculateLifoMethod(
            ProductProfitReportDto profitReport,
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock)
        {
            // Get latest rates for LIFO
            var latestPurchase = purchaseStock.OrderByDescending(x => x.Date).FirstOrDefault();
            var latestOpening = openingStock.OrderByDescending(x => x.Date).FirstOrDefault();

            // Determine inward rate based on latest entry
            if (latestPurchase != null && latestOpening != null)
            {
                profitReport.InwardRate = latestPurchase.Date >= latestOpening.Date
                    ? latestPurchase.Rate
                    : latestOpening.Rate;
            }
            else if (latestPurchase != null)
            {
                profitReport.InwardRate = latestPurchase.Rate;
            }
            else if (latestOpening != null)
            {
                profitReport.InwardRate = latestOpening.Rate;
            }

            // Calculate outward rate
            var latestSale = salesStock.OrderByDescending(x => x.Date).FirstOrDefault();
            profitReport.OutwardRate = latestSale?.Rate ?? 0;

            // Calculate LIFO profit
            var actualSalesQty = profitReport.OutwardQty - salesStock.Sum(x => x.InWardQty);
            profitReport.ProfitAmount = actualSalesQty > 0 ? (profitReport.OutwardRate - profitReport.InwardRate) * actualSalesQty : 0;
        }

        /// <summary>
        /// Optimized FIFO profit calculation using efficient algorithm
        /// </summary>
        private static decimal CalculateFifoProfit(
            List<NewProductReport> openingStock,
            List<NewProductReport> purchaseStock,
            List<NewProductReport> salesStock)
        {
            // Create incoming products queue (FIFO order)
            var incomingQueue = new Queue<(decimal Rate, decimal Qty)>();

            // Add opening stock first (FIFO)
            foreach (var stock in openingStock.OrderBy(x => x.Date))
            {
                if (stock.InWardQty > 0)
                    incomingQueue.Enqueue((stock.Rate, stock.InWardQty));
            }

            // Add purchases in chronological order
            foreach (var purchase in purchaseStock.OrderBy(x => x.Date))
            {
                if (purchase.InWardQty > 0)
                    incomingQueue.Enqueue((purchase.Rate, purchase.InWardQty));
            }

            decimal totalProfit = 0;

            // Process sales in chronological order
            foreach (var sale in salesStock.OrderBy(x => x.Date))
            {
                var remainingSaleQty = sale.OutWardQty;
                var saleRate = sale.Rate;

                while (remainingSaleQty > 0 && incomingQueue.Count > 0)
                {
                    var (purchaseRate, availableQty) = incomingQueue.Peek();

                    if (availableQty <= remainingSaleQty)
                    {
                        // Consume entire batch
                        totalProfit += availableQty * (saleRate - purchaseRate);
                        remainingSaleQty -= availableQty;
                        incomingQueue.Dequeue();
                    }
                    else
                    {
                        // Partial consumption
                        totalProfit += remainingSaleQty * (saleRate - purchaseRate);
                        incomingQueue.Dequeue();
                        incomingQueue.Enqueue((purchaseRate, availableQty - remainingSaleQty));
                        remainingSaleQty = 0;
                    }
                }
            }

            return totalProfit;
        }

        /// <summary>
        /// Optimized voucher type checks with HashSet lookup
        /// </summary>
        private static readonly HashSet<string> PurchaseVoucherTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "StockJournal", "StockReceipt", "PurchaseInvoice", "SalesReturn"
    };

        private static readonly HashSet<string> SalesVoucherTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SalesInvoice", "PurchaseReturn"
    };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsPurchaseVoucherType(string voucherType) => PurchaseVoucherTypes.Contains(voucherType);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsSalesVoucherType(string voucherType) => SalesVoucherTypes.Contains(voucherType);

        public async Task<CurrentFinancialYearDto> GetFinancialYears()
        {
            var data = await financialYeaRepository.FirstOrDefaultAsync(x => x.Id == FinancialYearId);
            return new CurrentFinancialYearDto
            {
                FromDate = data.FromDate,
                ToDate = data.ToDate,
                FromMiti = data.FromMiti,
                ToMiti = data.ToMiti
            };
        }
    }
}