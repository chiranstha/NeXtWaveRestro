using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.SalesReport.Exporting;
using NextWave.Erp.Sales;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.SharedDtos;
using NextWave.Erp.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.SalesReport
{
    [AbpAuthorize(AppPermissions.PagesSalesMasterReport)]

    public class SalesMasterReport(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<Branch, Guid> branchRepository,
    ISalesReportExport salesReportExport,
    IRepository<UnitConversion, Guid> unitConversionRepository,
    IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
    IRepository<SalesDetail, Guid> salesDetailRepository)
    : ErpAppServiceBase
    {
        public async Task<List<UniversalDropdownDto>> GetAllAccountLegers()
        {
            var result = new List<UniversalDropdownDto>();
            result.Add(new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            });
            result.AddRange(await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Debtors" || x.AccountGroupFk.Name == "Sundry Creditors" ||
                            x.Name == "Cash").Select(x => new UniversalDropdownDto
                            {
                                Id = x.Id,
                                DisplayName = x.Name
                            }).ToListAsync());
            return result;
        }

        public async Task<List<PurchaseReportList>> GetNewSalesReport(
            string? fromDate,
            string? toDate,
            Guid? ledgerId,
            Guid? productId,
            Guid? productGroupId,
            bool showDetails,
            PaymentMethod paymentMethod,
            long userId)
        {
            // Convert dates once, outside the query
            DateTime? fromDateParsed = !string.IsNullOrEmpty(fromDate)
                ? DateConverter.ConvertToEnglish(fromDate).Date
                : null;

            DateTime? toDateParsed = !string.IsNullOrEmpty(toDate)
                ? DateConverter.ConvertToEnglish(toDate).Date
                : null;

            // Build the base query with all filters
            var query = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking();

            if (fromDateParsed.HasValue)
                query = query.Where(d => d.Date.Date >= fromDateParsed.Value);

            if (toDateParsed.HasValue)
                query = query.Where(x => x.Date.Date <= toDateParsed.Value);


            if (ledgerId.HasValue && ledgerId != Guid.Empty)
                query = query.Where(x => x.LedgerId == ledgerId);

            if (paymentMethod != PaymentMethod.NA)
                query = query.Where(x => x.PaymentMethod == paymentMethod);

            if (userId != 0)
                query = query.Where(x => x.CreateUserId == userId);

            // Select only what we need
            var masterQuery = query
                .OrderBy(x => x.VoucherNumbering)
                .Select(x => new PurchaseReportList
                {
                    MasterId = x.Id,
                    Date = x.DateMiti,
                    InvoiceNo = x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    Pan = x.AccountLedgerFk.Pan,
                    TotalAmount = x.NetAmount,
                    NonTaxable = x.NetAmount - x.TaxableAmount,
                    Taxable = x.TaxableAmount,
                    TotalTaxAmount = x.TaxAmount,
                    GrandTotal = x.GrandTotal
                });

            // If we don't need details, just return the masters
            if (!showDetails) return await masterQuery.ToListAsync();

            // For detailed reporting, optimize by using a single join query
            var masterIds = await masterQuery.Select(m => m.MasterId).ToListAsync();

            if (masterIds.Count == 0) return new List<PurchaseReportList>();

            // Build details query with all filters
            var detailsQuery = salesDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && masterIds.Contains(x.SalesMasterId))
                .AsNoTracking();

            if (productId.HasValue && productId != Guid.Empty)
                detailsQuery = detailsQuery.Where(x => x.ProductId == productId);

            if (productGroupId.HasValue && productGroupId != Guid.Empty)
                detailsQuery = detailsQuery.Where(x => x.ProductFk.ProductGroupId == productGroupId);

            // Fetch all details in a single query
            var allDetails = await detailsQuery
                .Select(d => new
                {
                    d.SalesMasterId,
                    Detail = new PurchaseReportDetailList
                    {
                        ProductName = d.ProductFk.Name,
                        Rate = d.Rate,
                        Qty = d.Qty,
                        Amount = d.Amount,
                        TaxAmount = d.TaxAmount,
                        GrossAmount = d.GrossAmount
                    }
                })
                .ToListAsync();

            // Group details by master ID
            var detailsByMasterId = allDetails
                .GroupBy(d => d.SalesMasterId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Detail).ToList()
                );

            // Fetch masters again but only those with details
            var mastersWithDetails = await masterQuery
                .Where(m => detailsByMasterId.Keys.Contains(m.MasterId))
                .ToListAsync();

            // Add details to masters and calculate totals
            foreach (var master in mastersWithDetails)
                if (detailsByMasterId.TryGetValue(master.MasterId, out var details))
                {
                    master.PurchaseDetail = details;
                    master.TotalAmount = details.Sum(x => x.GrossAmount);
                    master.TotalTaxAmount = details.Sum(x => x.TaxAmount);
                    master.GrandTotal = details.Sum(x => x.Amount);
                }

            return mastersWithDetails;
        }

        public async Task<FileDto> GetNewSalesReportExcel1(string? fromDate, string? toDate, Guid? ledgerId,
            Guid? productId, Guid? productGroupId, bool showDetails, PaymentMethod paymentMethod, long userId)
        {
            var data = await GetNewSalesReport(fromDate, toDate, ledgerId, productId, productGroupId, showDetails,
                paymentMethod, userId);

            return salesReportExport.ExportToFile(data);
        }

        public async Task<FileDto> GetNewSalesReportExcel(string? fromDate, string? toDate, Guid? ledgerId,
            Guid? productId, Guid? productGroupId, bool showDetails, PaymentMethod paymentMethod, long userId)
        {
            var filteredSalesMasters = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId);

            if (paymentMethod != PaymentMethod.NA)
                filteredSalesMasters = filteredSalesMasters.Where(x => x.PaymentMethod == paymentMethod);

            if (fromDate != null)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (toDate != null)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != Guid.Empty) filteredSalesMasters = filteredSalesMasters.Where(x => x.LedgerId == ledgerId);

            var accountLedger = await accountLedgerRepository.GetAll().Where(x => !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Pan
                }).AsNoTracking().ToListAsync();
            var list = new List<GetSalesMasterExportDto>();
            var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);

            var productdetail = showDetails
                ? await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .AsNoTracking()
                    .Include(x => x.SalesMasterFk).Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk)
                    .Include(x => x.UnitFk)
                    .Where(x => x.SalesMasterFk.IsDelete == false).ToListAsync()
                : new List<SalesDetail>();

            foreach (var item in filteredSalesMasters.OrderBy(x => x.VoucherNumbering))
            {
                var productname = string.Empty;
                decimal qty = 0;
                foreach (var salesitem in productdetail.Where(x => x.SalesMasterId == item.Id).ToList())
                {
                    productname = productname + salesitem.ProductFk.ProductGroupFk.Name + ",";
                    qty += salesitem.Qty;
                }

                var details = showDetails
                    ? productdetail.Where(x => x.SalesMasterId == item.Id).Select(x => new ProductDetailsForExcelExport
                    {
                        Name = x.ProductFk.Name,
                        Unit = x.UnitFk.Name,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Amount = x.Amount
                    }).ToList()
                    : new List<ProductDetailsForExcelExport>();
                productname = productname.Length > 0 ? productname.Remove(productname.Length - 1) : "";
                var sales = new GetSalesMasterExportDto
                {
                    DateMiti = item.DateMiti.Replace('/', '.'),
                    VoucherNo = item.VoucherNo,
                    LedgerName = item.IsDelete
                        ? "CANCELLED"
                        : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Name,
                    PanNo = item.IsDelete ? "" : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Pan,
                    ProductName = item.IsDelete ? "" : productname,
                    Quantity = item.IsDelete ? 0 : qty,
                    GrossAmount = item.IsDelete ? 0 : item.GrossAmount,
                    NetAmount = item.IsDelete ? 0 : item.NetAmount,
                    TotalAmount = item.IsDelete ? 0 : item.GrandTotal,
                    BillDiscount = item.IsDelete ? 0 : item.BillDiscount,
                    TaxAmount = item.IsDelete ? 0 : item.TaxAmount,
                    TaxableAmount = item.IsDelete ? 0 : item.TaxableAmount,
                    Details = details
                };
                list.Add(sales);
            }

            var data = new GetSalesMasterExportMasterDto
            {
                Company = branch.CompanyName,
                Address = branch.Address,
                Phone = branch.PhoneNo1,
                Details = list
            };

            return salesReportExport.ExportToFileDetails(data);
        }

        public async Task<List<SalesReportDtoByDate>> GetSalesReportByDate(string? fromMiti, string? toMiti)
        {
            var result = new List<SalesReportDtoByDate>();
            var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
            var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
            var salesData = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .Where(x => x.Date.Date.Date >= fromDate.Date && x.Date.Date.Date <= toDate.Date)
                .Include(a => a.AccountLedgerFk).ToListAsync();
            foreach (var x in salesData)
            {
                var master = new SalesReportDtoByDate
                {
                    BillNo = x.VoucherNo,
                    DateMiti = x.DateMiti,
                    Date = x.Date,
                    LedgerName = x.AccountLedgerFk.Name,
                    PaNumber = x.AccountLedgerFk.Pan,
                    Total = x.NetAmount,
                    TaxAmount = x.TaxAmount,
                    GrandTotal = x.GrandTotal
                };
                var detailsResult = new List<SalesReportDtoByDateDetail>();
                var details = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(m => m.SalesMasterId == x.Id).Include(a => a.ProductFk).ToListAsync();
                foreach (var y in details)
                {
                    var data = new SalesReportDtoByDateDetail
                    {
                        Product = y.ProductFk.Name,
                        Qty = y.Qty,
                        Amount = y.Amount,
                        TaxAmount = y.TaxAmount
                    };
                    detailsResult.Add(data);
                }

                master.Details = detailsResult;
                result.Add(master);
            }

            return result;
        }

        public async Task<SalesReportDtoMaster> GetSalesReportNew(string? fromMiti, string? toMiti, Guid? branchId,
            long? userId, Guid? ledgerId, PaymentMethod paymentMethod)
        {
            var result = new SalesReportDtoMaster();
            var query = salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .Include(x => x.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && !x.IsDelete)
                .Select(x => new
                {
                    x.Date,
                    x.DateMiti,
                    x.LedgerId,
                    x.PaymentMethod,
                    x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    x.AccountLedgerFk.Pan,
                    MasterId = x.Id,
                    x.GrandTotal,
                    x.GrossAmount,
                    x.TaxableAmount,
                    x.TaxAmount,
                    Discount = x.BillDiscount,
                    x.NetAmount,
                    x.CreateUserId
                });

            if (!string.IsNullOrEmpty(fromMiti))
                if (ValidationHelper.IsValidNepaliDate(fromMiti))
                {
                    var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                    query = query.Where(x => x.Date.Date.Date >= date.Date);
                }

            if (!string.IsNullOrEmpty(toMiti))
                if (ValidationHelper.IsValidNepaliDate(toMiti))
                {
                    var date = DateConverter.ConvertToEnglish(toMiti).Date;
                    query = query.Where(x => x.Date.Date.Date <= date.Date);
                }

            if (ledgerId != Guid.Empty && ledgerId != null) query = query.Where(x => x.LedgerId == ledgerId);
            if (paymentMethod != PaymentMethod.NA) query = query.Where(x => x.PaymentMethod == paymentMethod);

            if (userId != 0) query = query.Where(x => x.CreateUserId == userId);


            var salesData = await query.OrderBy(x => x.MasterId).Select(x => new SalesReportDtoNew
            {
                MasterId = x.MasterId,
                Date = x.Date,
                DateMiti = x.DateMiti,
                VoucherNo = x.VoucherNo,
                PartyName = x.LedgerName,
                PaymentMethod = x.PaymentMethod,
                Pan = x.Pan,
                NonTaxableAmount = x.NetAmount - x.TaxableAmount,
                GrandTotal = x.GrandTotal,
                TaxableAmount = x.TaxableAmount,
                TaxAmount = x.TaxAmount,
                Discount = x.Discount,
                TotalAmount = x.NetAmount
            }).ToListAsync();


            var returnQuery = salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.Date,
                    x.DateMiti,
                    x.LedgerId,
                    x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    x.AccountLedgerFk.Pan,
                    MasterId = x.Id,
                    x.GrandTotal,
                    x.TotalTaxableAmount,
                    x.TaxAmount,
                    Discount = x.BillDiscount,
                    x.TotalAmount
                });

            if (!string.IsNullOrEmpty(fromMiti))
            {
                var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                returnQuery = returnQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (!string.IsNullOrEmpty(toMiti))
            {
                var date = DateConverter.ConvertToEnglish(toMiti).Date;
                returnQuery = returnQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != Guid.Empty && ledgerId != null) returnQuery = returnQuery.Where(x => x.LedgerId == ledgerId);

            var salesReturnData = await returnQuery.OrderBy(x => x.MasterId).Select(x => new SalesReportDtoNew
            {
                MasterId = x.MasterId,
                Date = x.Date,
                DateMiti = x.DateMiti,
                VoucherNo = x.VoucherNo,
                PartyName = x.LedgerName,
                Pan = x.Pan,
                NonTaxableAmount = -x.TotalAmount + x.TotalTaxableAmount,
                GrandTotal = -x.GrandTotal,
                TaxableAmount = -x.TotalTaxableAmount,
                TaxAmount = -x.TaxAmount,
                Discount = -x.Discount,
                TotalAmount = -x.TotalAmount
            }).ToListAsync();

            salesData.AddRange(salesReturnData);
            result.GrandTotal = salesData.Select(x => x.GrandTotal).Sum();
            result.TaxableAmount = salesData.Select(x => x.TaxableAmount).Sum();
            result.NonTaxableAmount = salesData.Sum(x => x.NonTaxableAmount);
            result.TaxAmount = salesData.Sum(x => x.TaxAmount);
            result.Discount = salesData.Select(x => x.Discount).Sum();
            result.TotalAmount = salesData.Select(x => x.TotalAmount).Sum();
            result.Details = salesData;
            return result;
        }

        public async Task<FileDto> GetSalesReportExcel(string? fromDate, string? toDate, Guid branchId, long? userId,
            Guid? ledgerId, PaymentMethod paymentMethod)
        {
            var data = await GetSalesReportNew(fromDate, toDate, branchId, userId, ledgerId, paymentMethod);

            return salesReportExport.NewExportToFile(data.Details);
        }

        //public async Task<byte[]> GetPdfDownload(string? fromDate, string? toDate, Guid? branchId, Guid? ledgerId,
        //    Guid? productId, Guid? productGroupId, bool showDetails, PaymentMethod paymentMethod, long userId)
        //{
        //    var branch = new Branch();
        //    if (branchId != Guid.Empty)
        //        branch = await branchRepository.FirstOrDefaultAsync(x => x.Id == branchId);
        //    else
        //        branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
        //    var ledger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == ledgerId);
        //    var financial = await financialYearRepository.FirstOrDefaultAsync(x => x.Status);

        //    var data = await GetNewSalesReport(fromDate, toDate, ledgerId, productId, productGroupId, showDetails,
        //        paymentMethod, userId);
        //    var master = new PurchaseReportMasterDto
        //    {
        //        FromMiti = fromDate,
        //        ToMiti = toDate,
        //        PhoneNo = branch.PhoneNo1,
        //        Pan = branch.PANumber,
        //        Address = branch.Address,
        //        FinancialYear = financial.Name,
        //        TotalAmount = data.Sum(x => x.TotalAmount),
        //        NonTaxable = data.Sum(x => x.NonTaxable),
        //        Taxable = data.Sum(x => x.Taxable),
        //        TaxAmount = data.Sum(x => x.TotalTaxAmount),
        //        GrandTotal = data.Sum(x => x.GrandTotal),
        //        Details = data
        //    };
        //    var document = new SalesReportPdf(master);
        //    return document.GeneratePdf();
        //}

        public async Task<List<TableLongDto>> GetAllUsers()
        {
            var data = new List<TableLongDto>
        {
            new()
            {
                Id = 0,
                Name = "All"
            }
        };
            var users = (await UserManager.GetAllUserAsync()).Where(x => x.TenantId == AbpSession.TenantId).Select(x =>
                new TableLongDto
                {
                    Id = x.Id,
                    Name = x.UserName
                }).ToList();
            data.AddRange(users);
            return data;
        }


        public async Task<List<GetSalesReportByNumberDto>> GetSalesReportByNumber(int from, int to)
        {
            var unitConversion = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
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


            var stockQuery = await salesDetailRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.SalesMasterFk.FinancialYearId == FinancialYearId &&
                            !x.SalesMasterFk.IsDelete && x.SalesMasterFk.VoucherNumbering >= from &&
                            x.SalesMasterFk.VoucherNumbering <= to)
                .Include(x => x.ProductFk)
                .ThenInclude(x => x.UnitFk)
                .AsSplitQuery().Select(x => new
                {
                    x.ProductId,
                    x.SalesMasterFk.VoucherNumbering,
                    x.SalesMasterFk.Date,
                    UnitName = x.UnitFk.Name,
                    x.ProductFk.ProductGroupId,
                    ProductName = x.ProductFk.Name,
                    x.Rate,
                    x.Amount,
                    x.TaxAmount,
                    x.Discount,
                    x.GrossAmount,
                    x.UnitId,
                    x.Qty
                }).ToListAsync();


            var result = new List<GetSalesReportByNumberDto>();
            var sn = 1;

            foreach (var query in stockQuery.DistinctBy(e => e.ProductId))
            {
                var minUnit = unitConversion.Where(a => a.ProductId == query.ProductId).MinBy(a => a.ConversionRate);

                var orderedUnits = unitConversion.Where(e => e.ProductId == query.ProductId)
                    .OrderByDescending(arg => arg.ConversionRate).ToList();


                var inwardTemporaryUnits = new List<TemporaryUnitsDto>();
                var singleQuery = stockQuery.Where(e => e.ProductId == query.ProductId).ToList();
                decimal tempInWardQty = 0;
                foreach (var data in singleQuery)
                {
                    var unit = unitConversion.FirstOrDefault(x => x.UnitId == data.UnitId && x.ProductId == data.ProductId);
                    if (minUnit == null || unit == null) continue;
                    {
                        tempInWardQty += data.Qty * unit.PrimaryQty * minUnit.Qty /
                                         (unit.Qty * minUnit.PrimaryQty);
                    }
                }


                foreach (var orderedUnit in orderedUnits)
                {
                    if (minUnit == null) continue;


                    var multiplicationFactor =
                        orderedUnit.PrimaryQty * minUnit.Qty / (orderedUnit.Qty * minUnit.PrimaryQty);


                    var temporaryInwardUnit = new TemporaryUnitsDto
                    {
                        Qty = (int)(tempInWardQty / multiplicationFactor),
                        UnitId = orderedUnit.UnitId,
                        UnitName = orderedUnit.UnitName
                    };


                    inwardTemporaryUnits.Add(temporaryInwardUnit);

                    tempInWardQty %= multiplicationFactor;
                }


                var model = new GetSalesReportByNumberDto
                {
                    Sn = sn++,
                    Name = query.ProductName,
                    QtyString = string.Join(",",
                        inwardTemporaryUnits.Where(a => a.Qty != 0).Select(x => x.Qty + " " + x.UnitName)),
                    NetAmount = singleQuery.Sum(e => e.Amount),
                    DiscountAmount = singleQuery.Sum(e => e.Discount),
                    TaxAmount = singleQuery.Sum(e => e.TaxAmount),
                    TotalAmount = singleQuery.Sum(e => e.Amount)
                };
                result.Add(model);
            }

            return result;
        }


        public async Task<List<GetSalesReportByNumberDto>> GetPurchaseReportByNumber(int from, int to)
        {
            var unitConversion = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
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


            var stockQuery = await purchaseDetailRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.PurchaseMasterFk.FinancialYearId == FinancialYearId &&
                            x.PurchaseMasterFk.VoucherNumbering >= from && x.PurchaseMasterFk.VoucherNumbering <= to)
                .Include(x => x.ProductFk)
                .ThenInclude(x => x.UnitFk)
                .AsSplitQuery().Select(x => new
                {
                    x.ProductId,
                    x.PurchaseMasterFk.VoucherNumbering,
                    UnitName = x.UnitFk.Name,
                    x.ProductFk.ProductGroupId,
                    ProductName = x.ProductFk.Name,
                    x.Rate,
                    x.Amount,
                    x.TaxAmount,
                    x.Discount,
                    x.GrossAmount,
                    x.UnitId,
                    x.Qty
                }).ToListAsync();


            var result = new List<GetSalesReportByNumberDto>();
            var sn = 1;

            foreach (var query in stockQuery.DistinctBy(e => e.ProductId))
            {
                var minUnit = unitConversion.Where(a => a.ProductId == query.ProductId).MinBy(a => a.ConversionRate);
                var unitConversionDetail =
                    unitConversion.FirstOrDefault(a => a.ProductId == query.ProductId && a.UnitId == query.UnitId);
                var orderedUnits = unitConversion.Where(e => e.ProductId == query.ProductId)
                    .OrderByDescending(arg => arg.ConversionRate).ToList();


                var inwardTemporaryUnits = new List<TemporaryUnitsDto>();
                var singleQuery = stockQuery.Where(e => e.ProductId == query.ProductId).ToList();
                decimal tempInWardQty = 0;
                foreach (var data in singleQuery)
                {
                    if (unitConversionDetail == null || minUnit == null) continue;
                    {
                        tempInWardQty += data.Qty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                         (unitConversionDetail.Qty * minUnit.PrimaryQty);
                    }
                }


                foreach (var orderedUnit in orderedUnits)
                {
                    if (minUnit == null) continue;
                    var multiplicationFactor = orderedUnit.PrimaryQty * minUnit.Qty /
                                               (orderedUnit.Qty * minUnit.PrimaryQty);


                    var temporaryInwardUnit = new TemporaryUnitsDto
                    {
                        Qty = (int)(tempInWardQty / multiplicationFactor),
                        UnitId = orderedUnit.UnitId,
                        UnitName = orderedUnit.UnitName
                    };
                    inwardTemporaryUnits.Add(temporaryInwardUnit);

                    tempInWardQty %= multiplicationFactor;
                }


                var model = new GetSalesReportByNumberDto
                {
                    Sn = sn++,
                    Name = query.ProductName,
                    QtyString = string.Join(",",
                        inwardTemporaryUnits.Where(a => a.Qty != 0).Select(x => x.Qty + " " + x.UnitName)),
                    NetAmount = singleQuery.Sum(e => e.Amount),
                    DiscountAmount = singleQuery.Sum(e => e.Discount),
                    TaxAmount = singleQuery.Sum(e => e.TaxAmount),
                    TotalAmount = singleQuery.Sum(e => e.Amount)
                };
                result.Add(model);
            }

            return result;
        }
    }
}
