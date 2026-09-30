using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NepDate;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.Configuration;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.InventoryReport.Exporting;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.InventoryReport
{
    [AbpAuthorize(AppPermissions.PagesStockReport)]
    public class StockReport(
    IRepository<StockPosting, Guid> stockPostingRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<Product, Guid> productRepository,
    IRepository<ProductGroup, Guid> productGroupRepository,
    IRepository<UnitConversion, Guid> unitConversionRepository,
    IRepository<FinancialYear, Guid> financialYeaRepository,
    StockCalculationService stockCalculationService,
    IStockReportExcelExporter excelExporter)
    : ErpAppServiceBase
    {
        //public async Task<byte[]> GetPdfDownload(string? fromMiti, string? toMiti, Guid productGroupId, Guid ledgerId)
        //{
        //    var report = new StockReportPdfDto
        //    {
        //        CompanyInfo = await branchService.GetDefultBranch(),
        //        FromDate = fromMiti,
        //        ToDate = toMiti,
        //        StockDetails = await GetNewReport(fromMiti, toMiti, productGroupId, ledgerId,true)
        //    };
        //    var document = new StockReportPdf(report);
        //    report.FromDate = fromMiti;
        //    report.ToDate = toMiti;
        //    return document.GeneratePdf();
        //}

        public async Task<List<Guid>> GetAllChildProductGroups(Guid id)
        {
            var result = new List<Guid>();
            var childIds = await productGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId).Where(x => x.GroupUnder == id).Select(x => x.Id)
                .ToListAsync();
            result.Add(id);
            result.AddRange(childIds);
            foreach (var childId in childIds) result.AddRange(await GetAllChildProductGroups(childId));
            return result.Distinct().ToList();
        }

        public async Task<StockDetailReportDto> GetStockDetail(Guid productId, [CanBeNull] string fromMiti, [CanBeNull] string toMiti)
        {
            var fromDate = FinancialYear.FromDate;
            var toDate = FinancialYear.ToDate;
            if (!string.IsNullOrEmpty(fromMiti)) fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
            if (!string.IsNullOrEmpty(toMiti)) toDate = DateConverter.ConvertToEnglish(toMiti).Date;

            var postings = await stockPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId &&
                            !x.IsDeleted && x.ProductId == productId && x.Date.Date <= toDate.Date)
                .Include(x => x.VoucherTypeFk)
                .Include(x => x.AccountLedgerFk)
                .Include(x => x.UnitFk)
                .OrderBy(x => x.Date).ThenBy(x => x.VoucherNumbering).ThenBy(x => x.Id)
                .ToListAsync();

            var product = await productRepository.GetAll().AsNoTracking()
                .Where(x => x.Id == productId && x.TenantId == AbpSession.TenantId)
                .Select(x => new { x.Name })
                .FirstOrDefaultAsync();

            var openingQty = postings.Where(x => x.Date.Date < fromDate.Date)
                .Sum(x => x.InWardQty - x.OutWardQty);
            var openingAmount = postings.Where(x => x.Date.Date < fromDate.Date)
                .Sum(x => (x.InWardQty - x.OutWardQty) * x.Rate);
            var balanceQty = openingQty;
            var balanceAmount = openingAmount;
            var rows = new List<StockDetailReportRowDto>();

            foreach (var posting in postings.Where(x => x.Date.Date >= fromDate.Date))
            {
                balanceQty += posting.InWardQty - posting.OutWardQty;
                balanceAmount += (posting.InWardQty - posting.OutWardQty) * posting.Rate;
                rows.Add(new StockDetailReportRowDto
                {
                    Id = posting.Id,
                    Date = posting.Date,
                    DateMiti = posting.DateMiti,
                    VoucherNo = posting.VoucherNo,
                    VoucherType = posting.VoucherTypeFk?.Name,
                    LedgerName = posting.AccountLedgerFk?.Name,
                    UnitName = posting.UnitFk?.Name,
                    InwardQty = posting.InWardQty,
                    InwardRate = posting.Rate,
                    InwardAmount = posting.InWardQty * posting.Rate,
                    OutwardQty = posting.OutWardQty,
                    OutwardRate = posting.Rate,
                    OutwardAmount = posting.OutWardQty * posting.Rate,
                    BalanceQty = balanceQty,
                    BalanceAmount = balanceAmount
                });
            }

            return new StockDetailReportDto
            {
                ProductName = product?.Name ?? "Stock Detail",
                UnitName = rows.FirstOrDefault()?.UnitName ?? postings.FirstOrDefault()?.UnitFk?.Name,
                OpeningQty = openingQty,
                OpeningAmount = openingAmount,
                Rows = rows
            };
        }


        public async Task<List<UniversalDropdownDto>> GetAllLedgers()
        {
            var result = new List<UniversalDropdownDto>
        {
            new()
            {
                Id = Guid.Empty,
                DisplayName = "All"
            }
        };
            result.AddRange(await accountLedgerRepository.GetAll()
                .Include(x => x.AccountGroupFk).Where(x =>
                    x.AccountGroupFk.Name == "Sundry Creditors" || x.AccountGroupFk.Name == "Cash-in Hand" ||
                    x.AccountGroupFk.Name == "Sundry Debtors")
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToListAsync());
            return result;
        }

         

        public async Task<List<StockCalculationDto>> GetReport([CanBeNull] string fromMiti, [CanBeNull] string toMiti,
            Guid branchId, Guid productGroupId)
        {
            var fromDate = FinancialYear.FromDate;
            var toDate = FinancialYear.ToDate;

            if (!string.IsNullOrEmpty(fromMiti)) fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;

            if (!string.IsNullOrEmpty(toMiti)) toDate = DateConverter.ConvertToEnglish(toMiti).Date;

            var unitConversion = await unitConversionRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(x => x.UnitFk).Select(x => new
                {
                    x.ProductId,
                    x.UnitId,
                    x.PrimaryQty,
                    x.Qty,
                    UnitName = x.UnitFk.Name,
                    x.ConversionRate
                }).ToListAsync();

            var products = await productRepository.GetAll().AsNoTracking().Select(x => new
            {
                x.Id,
                x.Name,
            }).ToListAsync();

            var stockQuery = stockPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId && !x.IsDeleted)
                .Where(x => x.Date.Date >= fromDate.Date && x.Date.Date <= toDate.Date)
                .Include(x => x.VoucherTypeFk).Include(x => x.ProductFk)
                .AsSplitQuery().Select(x => new
                {
                    x.ProductId,
                    x.Date,
                    VoucherType = x.VoucherTypeFk.Name,
                    x.ProductFk.ProductGroupId,
                    x.Rate,
                    x.UnitId,
                    x.InWardQty,
                    x.OutWardQty
                });

            if (productGroupId != Guid.Empty)
            {
                var productGroupIds = await GetAllChildProductGroups(productGroupId);
                stockQuery = stockQuery.Where(x => productGroupIds.Contains(x.ProductGroupId));
            }


            var stockPostingList = await stockQuery.ToListAsync();

            var groupedByProductId = stockPostingList.GroupBy(x => x.ProductId);

            var result = new List<StockCalculationDto>();
            var sn = 1;
            foreach (var singleKey in groupedByProductId)
            {
                var productId = singleKey.Key;
                var thisProduct = products.FirstOrDefault(x => x.Id == productId);
                var minUnit = unitConversion.Where(a => a.ProductId == productId).MinBy(a => a.ConversionRate);

                if (minUnit == null) continue;
                {
                    var singleProductList = new List<StockReportForProductDto>();
                    foreach (var x in singleKey)
                    {
                        var product = new StockReportForProductDto();


                        product.UnitId = x.UnitId;
                        product.VoucherType = x.VoucherType;
                        product.Rate = x.Rate;
                        product.InWardQty = x.InWardQty;
                        product.OutWardQty = x.OutWardQty;
                        product.InWardValue = x.InWardQty * x.Rate;
                        product.OutWardValue = x.OutWardQty * x.Rate;
                        product.CurrentStock = product.InWardQty - product.OutWardQty;
                        singleProductList.Add(product);

                    }

                    var singleProduct = unitConversion.Where(a => a.ProductId == productId).ToList();
                    var orderedUnits = singleProduct.OrderByDescending(x => x.ConversionRate).ToList();

                    if (singleProductList.Count <= 0) continue;
                    {
                        var productName = thisProduct?.Name;
                        //var isMultipleUnit = thisProduct != null && thisProduct.IsMultipleUnit;
                        var stockCalculation = new StockCalculationDto();
                        var openingStock = singleProductList.Where(x => x.VoucherType == "OpeningStock").ToList();

                        var productStock = singleProductList.Where(x =>
                            x.VoucherType != "OpeningStock").ToList();

                        var stockRate = singleProductList.Where(x =>
                            x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
                                or "PurchaseReturn").ToList();

                        decimal purchaseRate = 0;

                        var inward = stockRate.Where(x => x.InWardQty > 0).ToList();
                        if (inward.Count > 0)
                            purchaseRate = inward.Sum(x => x.InWardValue) / inward.Sum(x => x.InWardQty);


                        stockCalculation.ProductId = productId;
                        stockCalculation.ProductName = productName;
                        stockCalculation.Rate = Math.Round(purchaseRate, 2);
                        var openingQty = openingStock.Sum(x => x.InWardQty);
                        var inWardQty = productStock.Sum(x => x.InWardQty);
                        var outWardQty = productStock.Sum(x => x.OutWardQty);
                        var closingQty1 = openingQty + productStock
                            .Where(x => x.VoucherType != "MaterialReceipt" && x.VoucherType != "RejectionOut")
                            .Sum(x => x.InWardQty - x.OutWardQty);
                        var closingQty = openingQty + inWardQty - outWardQty;
                        stockCalculation.OpeningStockValue = Math.Round(openingStock.Sum(x => x.InWardValue), 2);
                        stockCalculation.InWardValue = Math.Round(productStock.Sum(x => x.InWardValue), 2);
                        stockCalculation.OutWardValue = Math.Round(productStock.Sum(x => x.OutWardValue), 2);
                        stockCalculation.ClosingValue = Math.Round(closingQty1 * purchaseRate, 2);
                        stockCalculation.ClosingQty = closingQty;

                        stockCalculation.OpeningStockQtyString = $"{openingQty} {minUnit?.UnitName}";
                        stockCalculation.InWardQtyString = $"{inWardQty} {minUnit?.UnitName}";
                        stockCalculation.OutWardQtyString = $"{outWardQty} {minUnit?.UnitName}";
                        stockCalculation.ClosingQtyString = $"{closingQty} {minUnit?.UnitName}";

                        stockCalculation.SlNo = sn++;
                        result.Add(stockCalculation);
                    }
                }
            }

            return result /*.OrderBy(x => x.ProductName)*/.ToList();
        }

        //public async Task<List<StockMaintain>> GetReportForImport()
        //{
        //    var result = new List<StockMaintain>();
        //   // var branches = branchService.GetAll().Result.Items;
        //    foreach (var branch in branches)
        //    {
        //        var unitConversion = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
        //            .AsNoTracking()
        //            .Include(x => x.UnitFk).Select(x => new
        //            {
        //                x.ProductId,
        //                x.UnitId,
        //                x.PrimaryQty,
        //                x.Qty,
        //                UnitName = x.UnitFk.Name,
        //                x.ConversionRate
        //            }).ToListAsync();

        //        var stockQuery = stockPostingRepository.GetAll().AsNoTracking()
        //            .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId && !x.IsDeleted)
        //            .Include(x => x.VoucherTypeFk)
        //            .Include(x => x.ProductFk)
        //            .ThenInclude(x => x.UnitFk)
        //            .AsSplitQuery()
        //            .Select(x => new
        //            {
        //                x.ProductId,
        //                x.Date,
        //                UnitName = x.UnitFk.Name,
        //                VoucherType = x.VoucherTypeFk.Name,
        //                x.ProductFk.ProductGroupId,
        //                ProductName = x.ProductFk.Name,
        //                x.Rate,
        //                x.ProductFk.PurchaseRate,
        //                x.UnitId,
        //                x.VoucherNumbering,
        //                x.LedgerId,
        //                x.InWardQty,
        //                x.OutWardQty
        //            });

        //        var stockPostingList = await stockQuery
        //            .AsSplitQuery()
        //            .Select(x => new
        //            {
        //                x.ProductId,
        //                x.Date,
        //                x.VoucherType,
        //                x.ProductName,
        //                x.UnitName,
        //                x.Rate,
        //                x.VoucherNumbering,
        //                x.PurchaseRate,
        //                x.UnitId,
        //                x.InWardQty,
        //                x.OutWardQty
        //            }).ToListAsync();

        //        var posting = new List<NewProductReport>();
        //        foreach (var x in stockPostingList)
        //        {
        //            var product = new NewProductReport();


        //            product.ProductId = x.ProductId;
        //            product.ProductName = x.ProductName;
        //            product.UnitId = x.UnitId;
        //            product.VoucherType = x.VoucherType;
        //            product.Date = x.Date;
        //            product.Rate = x.Rate;
        //            product.InWardQty = x.InWardQty;
        //            product.OutWardQty = x.OutWardQty;
        //            product.InWardValue = x.InWardQty * x.Rate;
        //            product.OutWardValue = x.OutWardQty * x.Rate;
        //            product.CurrentStock = product.InWardQty - product.OutWardQty;
        //            posting.Add(product);

        //        }

        //        foreach (var product in stockPostingList.Select(a =>
        //                         new { a.ProductId, a.ProductName, a.UnitName, a.UnitId })
        //                     .DistinctBy(b => b.ProductId))
        //        {
        //            var stockPosting = posting.Where(x => x.ProductId == product.ProductId).ToList();
        //            if (stockPosting.Count == 0) continue;
        //            {
        //                var stockCalculationNew = new StockMaintain();
        //                var openingStock = stockPosting.Where(x => x.VoucherType == "OpeningStock").ToList();
        //                var productStock = stockPosting.Where(x => x.VoucherType != "OpeningStock").ToList();
        //                decimal openingRate = 0;
        //                decimal purchaseRate = 0;
        //                decimal outwardRate = 0;

        //                var inward = productStock.Where(x => x.InWardQty > 0).ToList();
        //                var outward = productStock.Where(x => x.OutWardQty > 0).ToList();
        //                if (inward.Count > 0)
        //                    purchaseRate = inward.Sum(x => x.InWardQty) > 0
        //                        ? inward.Sum(x => x.InWardValue) / inward.Sum(x => x.InWardQty)
        //                        : 0;
        //                if (openingStock.Count > 0)
        //                    openingRate = openingStock.Sum(x => x.InWardQty) > 0
        //                        ? openingStock.Sum(x => x.InWardValue) / openingStock.Sum(x => x.InWardQty)
        //                        : 0;
        //                if (outward.Count > 0)
        //                    outwardRate = outward.Sum(x => x.OutWardQty) > 0
        //                        ? outward.Sum(x => x.OutWardValue) / outward.Sum(x => x.OutWardQty)
        //                        : 0;
        //                stockCalculationNew.FinancialYearId = FinancialYearId;
        //                stockCalculationNew.ProductId = product.ProductId;
        //                stockCalculationNew.UnitId = product.UnitId;
        //                stockCalculationNew.OpeningQty = openingStock.Sum(x => x.InWardQty);
        //                stockCalculationNew.InwardQty = inward.Sum(x => x.InWardQty);
        //                stockCalculationNew.OutwardQty = outward.Sum(x => x.OutWardQty);
        //                stockCalculationNew.OpeningRate = openingRate;
        //                stockCalculationNew.InwardRate = purchaseRate;
        //                stockCalculationNew.OutwardRate = outwardRate;
        //                stockCalculationNew.OpeningAmt = openingStock.Sum(x => x.InWardValue);
        //                stockCalculationNew.InwardAmt = inward.Sum(x => x.InWardValue);
        //                stockCalculationNew.OutwardAmt = outward.Sum(x => x.OutWardValue);
        //                stockCalculationNew.LastModifiedDate = DateTime.Now;
        //                stockCalculationNew.TenantId = AbpSession.TenantId;
        //                result.Add(stockCalculationNew);
        //                var oldData = await stockMaintainRepository.FirstOrDefaultAsync(x =>
        //                    x.ProductId == stockCalculationNew.ProductId);
        //                if (oldData == null)
        //                    await stockMaintainRepository.InsertAsync(stockCalculationNew);
        //                var fifoData = await stockFifoTableRepository.FirstOrDefaultAsync(x =>
        //                    x.ProductId == stockCalculationNew.ProductId);
        //                if (fifoData == null)
        //                {
        //                    var fifo = new StockFifoTable
        //                    {
        //                        ProductId = product.ProductId,
        //                        Date = DateTime.Now,
        //                        DateMiti = DateTime.Now.ToNepaliDate().ToString(),
        //                        UnitId = product.UnitId,
        //                        Qty = stockCalculationNew.OpeningQty + stockCalculationNew.InwardQty -
        //                              stockCalculationNew.OutwardQty,
        //                        Rate = stockCalculationNew.InwardRate,
        //                        TenantId = AbpSession.TenantId
        //                    };
        //                    await stockFifoTableRepository.InsertAsync(fifo);
        //                }
        //            }
        //        }
        //    }

        //    return result;
        //}

        public async Task<List<StockCalculationDto>> GetNewReport1(string? fromMiti, string? toMiti, Guid productGroupId, Guid ledgerId, bool isSingleUnit, bool isZeroStock)
        {
            // Optimize date conversion only when necessary
            var fromDate = string.IsNullOrWhiteSpace(fromMiti)
                ? FinancialYear.FromDate.Date
                : DateConverter.ConvertToEnglish(fromMiti);
            var toDate = string.IsNullOrWhiteSpace(toMiti)
                ? FinancialYear.ToDate.Date
                : DateConverter.ConvertToEnglish(toMiti);

            // Prepare product groups filter if needed
            IEnumerable<Guid> productGroupIds = Array.Empty<Guid>();
            if (productGroupId != Guid.Empty) productGroupIds = await GetAllChildProductGroups(productGroupId);

            // Pre-load unit conversion data to avoid repeated queries
            var unitConversions = await unitConversionRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
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
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.FinancialYearId == FinancialYearId &&
                            !x.IsDeleted && x.ProductFk.ProductType != ProductTypeEnum.FixedAsset &&
                            x.Date.Date <= toDate.Date);


            if (ledgerId != Guid.Empty) stockQuery = stockQuery.Where(x => x.LedgerId == ledgerId);

            if (productGroupId != Guid.Empty)
                stockQuery = stockQuery.Where(x => productGroupIds.Contains(x.ProductFk.ProductGroupId));

            // Use projection to optimize data retrieval
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

            // Process the data in memory
            var productReports = stockCalculationService.ProcessStockPostings(stockPostingData, unitConversions);

            // Calculate final stock values
            var stockCalculations = CalculateStockValues(productReports, fromDate, unitConversions,  "fifo");

            // Apply zero stock filter if needed
            return !isZeroStock
                ? stockCalculations.Where(e => e.ClosingQty != 0).OrderBy(x => x.ProductName.Trim()).ToList()
                : stockCalculations.OrderBy(x => x.ProductName.Trim()).ToList();
        }


        public async Task<List<StockCalculationDto>> GetNewReport(string? fromMiti, string? toMiti, Guid productGroupId, Guid ledgerId, bool isZeroStock)
        {
            var fromDate = string.IsNullOrWhiteSpace(fromMiti)
                ? FinancialYear.FromDate.Date
                : DateConverter.ConvertToEnglish(fromMiti);
            var toDate = string.IsNullOrWhiteSpace(toMiti)
                ? FinancialYear.ToDate.Date
                : DateConverter.ConvertToEnglish(toMiti);


            IEnumerable<Guid> productGroupIds = Array.Empty<Guid>();
            if (productGroupId != Guid.Empty)
            {
                productGroupIds = await GetAllChildProductGroups(productGroupId);
            }

            // Pre-load unit conversion data to avoid repeated queries
            var unitConversions = await unitConversionRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
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
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.FinancialYearId == FinancialYearId &&
                            !x.IsDeleted &&
                            x.ProductFk.ProductType != ProductTypeEnum.FixedAsset &&
                            x.Date.Date >= fromDate.Date &&
                            x.Date.Date <= toDate.Date);


            if (ledgerId != Guid.Empty)
                stockQuery = stockQuery.Where(x => x.LedgerId == ledgerId);

            if (productGroupId != Guid.Empty)
                stockQuery = stockQuery.Where(x => productGroupIds.Contains(x.ProductFk.ProductGroupId));

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

            // Process the data in memory
            var productReports = stockCalculationService.ProcessStockPostings(stockPostingData, unitConversions);


            var stockCalculation = "fifo";
              //  await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId());
            // Calculate final stock values
            var stockCalculations = CalculateStockValues(productReports, fromDate, unitConversions, stockCalculation);

            // Apply zero stock filter if needed
            return !isZeroStock
                ? stockCalculations.Where(e => e.ClosingQty != 0).OrderBy(x => x.ProductName.Trim()).ToList()
                : stockCalculations.OrderBy(x => x.ProductName.Trim()).ToList();
        }






        private List<StockCalculationDto> CalculateStockValues(
                List<NewProductReport> postings,
                DateTime fromDate,
                List<UnitConversionDto> unitConversions,
                string type)
        {
            var results = new List<StockCalculationDto>();

            // Group by product to process each product once
            var productGroups = postings
                .GroupBy(p => new { p.ProductId, p.ProductName, p.ProductGroup });

            foreach (var group in productGroups)
            {
                var productPostings = group.ToList();
                if (productPostings.Count == 0) continue;

                var unitName = unitConversions
                    .FirstOrDefault(p => p.ProductId == group.Key.ProductId)?.UnitName ?? string.Empty;

                // Categorize stock movements
                var openingStock = productPostings
                    .Where(x => x.VoucherType == "OpeningStock")
                    .ToList();

                var beforePeriodStock = productPostings
                    .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < fromDate.Date)
                    .ToList();

                var periodStock = productPostings
                    .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date >= fromDate.Date)
                    .ToList();

                // Calculate quantities
                var openingQty = openingStock.Sum(x => x.InWardQty) +
                               beforePeriodStock.Sum(e => e.InWardQty - e.OutWardQty);
                var inWardQty = periodStock.Sum(x => x.InWardQty);
                var outWardQty = periodStock.Sum(x => x.OutWardQty);
                var closingQty = openingQty + inWardQty - outWardQty;

                // Calculate purchase rate and closing value based on valuation method
                var (purchaseRate, closingValue) = stockCalculationService.CalculateStockRateAndValue(
                    openingStock, beforePeriodStock, periodStock, closingQty, type);

                // Create the result
                var calculation = new StockCalculationDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    ProductGroup = group.Key.ProductGroup,
                    Rate = Math.Round(purchaseRate, 2),
                    OpeningStockValue = Math.Round(openingStock.Sum(x => x.InWardValue), 2),
                    InWardValue = Math.Round(periodStock.Sum(x => x.InWardValue), 2),
                    OutWardValue = Math.Round(periodStock.Sum(x => x.OutWardValue), 2),
                    ClosingValue = closingValue,
                    ClosingQty = closingQty
                };

                // Format quantity strings
                //if (!isSingleUnit)
                //{
                //    calculation = stockCalculationService.FormatMultipleUnitQuantities(
                //        calculation,
                //        unitConversions,
                //        group.Key.ProductId,
                //        openingQty,
                //        inWardQty,
                //        outWardQty,
                //        closingQty,
                //        false);
                //}
                //else
                //{
                    calculation.OpeningStockQtyString = $"{openingQty:F2} {unitName}";
                    calculation.InWardQtyString = $"{inWardQty:F2} {unitName}";
                    calculation.OutWardQtyString = $"{outWardQty:F2} {unitName}";
                    calculation.ClosingQtyString = $"{closingQty:F2} {unitName}";
             //   }

                results.Add(calculation);
            }

            return results;
        }






        ///// <returns></returns>
        //private List<StockCalculationDto> CalculateStockValues(
        //     List<NewProductReport> postings,
        //     DateTime fromDate,
        //     List<UnitConversionDto> unitConversions,
        //     bool isSingleUnit, string type)
        //{
        //    var results = new List<StockCalculationDto>();

        //    // Group by product to process each product once
        //    var productGroups = postings
        //        .GroupBy(p => new { p.ProductId, p.ProductName, p.ProductGroup });

        //    foreach (var group in productGroups)
        //    {
        //        var productPostings = group.ToList();
        //        if (productPostings.Count == 0) continue;

        //        var unitName = unitConversions
        //            .Where(p => p.ProductId == group.Key.ProductId)
        //            .Select(p => p.UnitName)
        //            .FirstOrDefault() ?? string.Empty;

        //        // Calculate stock quantities
        //        var openingStock = productPostings
        //            .Where(x => x.VoucherType == "OpeningStock")
        //            .ToList();

        //        var periodOpeningStock = productPostings
        //            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < fromDate.Date)
        //            .ToList();

        //        var periodStock = productPostings
        //            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date >= fromDate.Date)
        //            .ToList();

        //        var purchaseStockItems = productPostings
        //            .Where(x => x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
        //                or "PurchaseReturn")
        //            .ToList();

        //        // Calculate purchase rate
        //        decimal purchaseRate = 0;
        //        var inwardItems = purchaseStockItems.Where(x => x.InWardQty > 0).ToList();
        //        var outwardItems = productPostings.Where(x => x.OutWardQty > 0).ToList();
        //        if (inwardItems.Count > 0)
        //            purchaseRate = inwardItems.Sum(x => x.InWardValue) / inwardItems.Sum(x => x.InWardQty);
        //        else if (inwardItems.Count == 0 && outwardItems.Count > 0)
        //            purchaseRate = outwardItems.Sum(x => x.OutWardValue) / outwardItems.Sum(x => x.OutWardQty);

        //        // Calculate quantities
        //        var openingQty = openingStock.Sum(x => x.InWardQty) +
        //                         periodOpeningStock.Sum(e => e.InWardQty - e.OutWardQty);
        //        var inWardQty = periodStock.Sum(x => x.InWardQty);
        //        var outWardQty = periodStock.Sum(x => x.OutWardQty);
        //        var closingQty = openingQty + inWardQty - outWardQty;
        //        decimal closingValue = 0;

        //        // Calculate closing value based on valuation method
        //        if (type == "average")
        //        {
        //            closingValue = Math.Round(closingQty * purchaseRate, 2);
        //        }
        //        else if (type == "fifo")
        //        {
        //            var tempOutWardQty = outWardQty;
        //            var tempInWardItems = new List<NewProductReport>();

        //            // Add only inward movements
        //            tempInWardItems.AddRange(openingStock.Where(x => x.InWardQty > 0));
        //            tempInWardItems.AddRange(periodOpeningStock.Where(x => x.InWardQty > 0));
        //            tempInWardItems.AddRange(periodStock.Where(x => x.InWardQty > 0));

        //            // Sort by date ascending for FIFO (oldest first)
        //            tempInWardItems = tempInWardItems.OrderBy(x => x.Date).ToList();

        //            foreach (var item in tempInWardItems)
        //            {
        //                if (tempOutWardQty <= 0)
        //                {
        //                    // No more outward quantity to consume, add full value of remaining items
        //                    closingValue += item.InWardQty * item.Rate;
        //                }
        //                else if (item.InWardQty <= tempOutWardQty)
        //                {
        //                    // This entire batch is consumed
        //                    tempOutWardQty -= item.InWardQty;
        //                }
        //                else
        //                {
        //                    // This batch is partially consumed
        //                    var remainingQty = item.InWardQty - tempOutWardQty;
        //                    closingValue += remainingQty * item.Rate;
        //                    tempOutWardQty = 0;
        //                }
        //            }

        //            closingValue = Math.Round(closingValue, 2);
        //        }
        //        else if (type == "lifo")
        //        {
        //            var tempOutWardQty = outWardQty;
        //            var tempInWardItems = new List<NewProductReport>();

        //            // Add only inward movements
        //            tempInWardItems.AddRange(openingStock.Where(x => x.InWardQty > 0));
        //            tempInWardItems.AddRange(periodOpeningStock.Where(x => x.InWardQty > 0));
        //            tempInWardItems.AddRange(periodStock.Where(x => x.InWardQty > 0));

        //            // Sort by date descending for LIFO (newest first)
        //            tempInWardItems = tempInWardItems.OrderByDescending(x => x.Date).ToList();

        //            foreach (var item in tempInWardItems)
        //            {
        //                if (tempOutWardQty <= 0)
        //                {
        //                    // No more outward quantity to consume, add full value of remaining items
        //                    closingValue += item.InWardQty * item.Rate;
        //                }
        //                else if (item.InWardQty <= tempOutWardQty)
        //                {
        //                    // This entire batch is consumed
        //                    tempOutWardQty -= item.InWardQty;
        //                }
        //                else
        //                {
        //                    // This batch is partially consumed
        //                    var remainingQty = item.InWardQty - tempOutWardQty;
        //                    closingValue += remainingQty * item.Rate;
        //                    tempOutWardQty = 0;
        //                }
        //            }

        //            closingValue = Math.Round(closingValue, 2);
        //        }

        //        // Create the result
        //        var calculation = new StockCalculationDto
        //        {
        //            ProductId = group.Key.ProductId,
        //            ProductName = group.Key.ProductName,
        //            ProductGroup = group.Key.ProductGroup,
        //            Rate = Math.Round(purchaseRate, 2),
        //            OpeningStockValue = Math.Round(openingStock.Sum(x => x.InWardValue), 2),
        //            InWardValue = Math.Round(periodStock.Sum(x => x.InWardValue), 2),
        //            OutWardValue = Math.Round(periodStock.Sum(x => x.OutWardValue), 2),
        //            ClosingValue = closingValue,
        //            ClosingQty = closingQty
        //        };

        //        // Calculate quantities string representation
        //        if (!isSingleUnit)
        //        {
        //            calculation = FormatMultipleUnitQuantities(
        //                calculation,
        //                unitConversions,
        //                group.Key.ProductId,
        //                openingQty,
        //                inWardQty,
        //                outWardQty,
        //                closingQty,
        //                false);
        //        }
        //        else
        //        {
        //            calculation.OpeningStockQtyString = $"{openingQty} {unitName}";
        //            calculation.InWardQtyString = $"{inWardQty} {unitName}";
        //            calculation.OutWardQtyString = $"{outWardQty} {unitName}";
        //            calculation.ClosingQtyString = $"{closingQty} {unitName}";
        //        }

        //        results.Add(calculation);
        //    }

        //    return results;
        //}


        private List<StockCalculationDto> CalculateStockValues1(
            List<NewProductReport> postings,
            DateTime fromDate,
            List<UnitConversionDto> unitConversions,
            bool isSingleUnit)
        {
            var results = new List<StockCalculationDto>();


            // Group by product to process each product once
            var productGroups = postings
                .GroupBy(p => new { p.ProductId, p.ProductName, p.ProductGroup });

            foreach (var group in productGroups)
            {
                var productPostings = group.ToList();
                if (productPostings.Count == 0) continue;

                // Find product metadata from first item

                var unitName = unitConversions
                    .Where(p => p.ProductId == group.Key.ProductId)
                    .Select(p => p.UnitName)
                    .FirstOrDefault() ?? string.Empty;

                // Calculate stock quantities
                var openingStock = productPostings
                    .Where(x => x.VoucherType == "OpeningStock")
                    .ToList();

                var periodOpeningStock = productPostings
                    .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < fromDate.Date)
                    .ToList();

                var periodStock = productPostings
                    .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date >= fromDate.Date)
                    .ToList();

                var purchaseStockItems = productPostings
                    .Where(x => x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
                        or "PurchaseReturn")
                    .ToList();

                // Calculate purchase rate
                decimal purchaseRate = 0;
                var inwardItems = purchaseStockItems.Where(x => x.InWardQty > 0).ToList();
                var outwardItems = productPostings.Where(x => x.OutWardQty > 0).ToList();
                if (inwardItems.Count > 0)
                    purchaseRate = inwardItems.Sum(x => x.InWardValue) / inwardItems.Sum(x => x.InWardQty);
                else if (inwardItems.Count == 0 && outwardItems.Count > 0)
                    purchaseRate = outwardItems.Sum(x => x.OutWardValue) / outwardItems.Sum(x => x.OutWardQty);

                // Calculate quantities
                var openingQty = openingStock.Sum(x => x.InWardQty) +
                                 periodOpeningStock.Sum(e => e.InWardQty - e.OutWardQty);
                var inWardQty = periodStock.Sum(x => x.InWardQty);
                var outWardQty = periodStock.Sum(x => x.OutWardQty);
                var closingQty = openingQty + inWardQty - outWardQty;

                // Create the result
                var calculation = new StockCalculationDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    ProductGroup = group.Key.ProductGroup,
                    Rate = Math.Round(purchaseRate, 2),
                    OpeningStockValue = Math.Round(openingStock.Sum(x => x.InWardValue), 2),
                    InWardValue = Math.Round(periodStock.Sum(x => x.InWardValue), 2),
                    OutWardValue = Math.Round(periodStock.Sum(x => x.OutWardValue), 2),
                    ClosingValue = Math.Round(closingQty * purchaseRate, 2),
                    ClosingQty = closingQty
                };

                // Calculate quantities string representation
                if (!isSingleUnit)
                {
                    calculation = stockCalculationService.FormatMultipleUnitQuantities(
                        calculation,
                        unitConversions,
                        group.Key.ProductId,
                        openingQty,
                        inWardQty,
                        outWardQty,
                        closingQty,
                        false);
                }
                else
                {
                    calculation.OpeningStockQtyString = $"{openingQty} {unitName}";
                    calculation.InWardQtyString = $"{inWardQty} {unitName}";
                    calculation.OutWardQtyString = $"{outWardQty} {unitName}";
                    calculation.ClosingQtyString = $"{closingQty} {unitName}";
                }

                results.Add(calculation);
            }

            return results;
        }




        public async Task<FileDto> CreateStockReportToExcel([CanBeNull] string fromMiti, [CanBeNull] string toMiti,
            Guid branchId, Guid productGroupId)
        {
            var data = await GetReport(fromMiti, toMiti, branchId, productGroupId);
            return excelExporter.ExportToFile(data);
        }

        public async Task<FileDto> CreateStockReportToExcelNew([CanBeNull] string fromMiti, [CanBeNull] string toMiti,
            Guid productGroupId, Guid ledgerId)
        {
            var data = await GetNewReport(fromMiti, toMiti, productGroupId, ledgerId,
                false);
            return excelExporter.ExportToFile(data);
        }

        public async Task<List<UniversalDropdownDto>> GetAllProductGroupForTableDropdown()
        {
            return await productGroupRepository.GetAll().Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).ToListAsync();
        }

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
