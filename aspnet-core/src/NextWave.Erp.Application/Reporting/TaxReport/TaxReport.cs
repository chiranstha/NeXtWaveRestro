using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Purchase;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.TaxReport.Exporting;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.TaxReport
{
    public class TaxReport(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<SalesDetail, Guid> salesDetailRepository,
    IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
    IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository,
    IRepository<SalesReturnDetail, Guid> salesReturnDetailRepository,
    IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
    IRepository<AccountLedger, Guid> accountLegerRepository,
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    ISalesTaxExcelExporter excelExporter,
    IPurchaseTaxExcelExporter purchaseTaxExcelExporter)
    : ErpAppServiceBase
    {
        public async Task<List<TaxReportDto>> GetReportNew(string? fromDate, string? toDate, PurchaseTaxDataType type)
        {
            var result = new List<TaxReportDto>();
            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.PurchaseOnly)
            {
                var purchaseQuery = purchaseMasterRepository.GetAll();
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
                }

                var purchaseDatas = await purchaseQuery.Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.AccountLedgerFk).AsNoTracking().ToListAsync();

                var purchaseDetails = await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk).AsNoTracking().ToListAsync();

                foreach (var purchaseData in purchaseDatas)
                {
                    var data = new TaxReportDto
                    {
                        DateMiti = purchaseData.DateMiti,
                        VoucherNo = purchaseData.VoucherNo,
                        LedgerName = purchaseData.AccountLedgerFk.Name,
                        Pan = purchaseData.AccountLedgerFk.Pan,
                        Quantity = purchaseDetails.Where(x => x.PurchaseMasterId == purchaseData.Id).Sum(x => x.Qty),
                        ProductName = purchaseDetails.Where(x => x.PurchaseMasterId == purchaseData.Id)
                            .Select(x => x.ProductFk.ProductGroupFk.Name).Distinct().Aggregate((x, y) => x + ", " + y),
                        Amount = purchaseData.GrandTotal,
                        LocalTaxableAmount = purchaseData.TotalTaxableAmount,
                        LocalTaxAmount = purchaseData.TotalTax
                    };
                    result.Add(data);
                }
            }

            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.ReturnsOnly)
            {
                var purchaseQuery = purchaseReturnRepository.GetAll();
                if (fromDate != null && fromDate != string.Empty)
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (toDate != null && toDate != string.Empty)
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
                }

                var purchaseReturns = await purchaseQuery.Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.AccountLedgerFk)
                    .AsNoTracking().ToListAsync();
                var purchaseReturnDetails = await purchaseReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .AsNoTracking().ToListAsync();

                foreach (var purchasereturn in purchaseReturns)
                {
                    var data = new TaxReportDto
                    {
                        DateMiti = purchasereturn.DateMiti,
                        VoucherNo = purchasereturn.VoucherNo,
                        LedgerName = purchasereturn.AccountLedgerFk.Name,
                        Pan = purchasereturn.AccountLedgerFk.Pan,
                        Quantity = -purchaseReturnDetails.Where(x => x.PurchaseReturnId == purchasereturn.Id)
                            .Sum(x => x.Qty),
                        ProductName = purchaseReturnDetails.Where(x => x.PurchaseReturnId == purchasereturn.Id)
                            .Select(x => x.ProductFk.ProductGroupFk.Name).Distinct().Aggregate((x, y) => x + ", " + y),
                        Amount = -purchasereturn.GrandTotal,
                        LocalTaxableAmount = purchasereturn.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -purchasereturn.TotalTaxableAmount
                            : 0,
                        LocalTaxAmount = purchasereturn.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -purchasereturn.TotalTax
                            : 0
                    };
                    result.Add(data);
                }
            }

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesTaxSalesRegisterReport)]
        public async Task<List<TaxReportDto>> GetSalesReportNew(string? fromDate, string? toDate, PurchaseTaxDataType type)
        {
            var result = new List<TaxReportDto>();
            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.PurchaseOnly)
            {
                var salesQuery = salesMasterRepository.GetAll()
                    .Include(e => e.AccountLedgerFk)
                    .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId)
                    .Select(e => new
                    {
                        e.Id,
                        LedgerName = e.AccountLedgerFk != null && !e.IsDelete ? e.AccountLedgerFk.Name : "<<CANCEL BILL>>",
                        Pan = e.AccountLedgerFk != null && !e.IsDelete ? e.AccountLedgerFk.Pan : "",

                        e.Date,
                        e.DateMiti,
                        e.VoucherNumbering,
                        e.VoucherNo,
                        e.IsDelete,
                        Amount = e.GrandTotal,
                        e.GrossAmount,
                        Discount = e.BillDiscount,
                        e.NetAmount,
                        NonTaxableAmount = e.NetAmount - e.TaxableAmount,
                        InvoiceTypeEnum = e.InvoiceType,
                        LocalTaxableAmount = e.InvoiceType == InvoiceTypeEnum.LocalInvoice ? e.TaxableAmount : 0,
                        LocalTaxAmount = e.InvoiceType == InvoiceTypeEnum.LocalInvoice ? e.TaxAmount : 0,
                        ImportTaxableAmount = e.InvoiceType == InvoiceTypeEnum.ImportInvoice ? e.TaxableAmount : 0,
                        ImportTaxAmount = e.InvoiceType == InvoiceTypeEnum.ImportInvoice ? e.TaxAmount : 0,
                        TaxableCapitalizeAmount = e.InvoiceType == InvoiceTypeEnum.TaxableCapitalize ? e.TaxableAmount : 0,
                        TaxableCapitalizeTax = e.InvoiceType == InvoiceTypeEnum.TaxableCapitalize ? e.TaxAmount : 0
                    });
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    salesQuery = salesQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    salesQuery = salesQuery.Where(x => x.Date.Date.Date <= date.Date);
                }

                var salesList = await salesQuery.OrderBy(x => x.Date.Date)
                    .ThenBy(e => e.VoucherNumbering).ThenBy(e => e.VoucherNo).ToListAsync();


                var salesDetails = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.ProductFk)
                    .ThenInclude(x => x.ProductGroupFk)
                    .Include(x => x.UnitFk).ToListAsync();
                foreach (var salesData in salesList)
                {
                    var sale = salesDetails.Where(x => x.SalesMasterId == salesData.Id).ToList();
                    var data = new TaxReportDto
                    {
                        DateMiti = salesData.DateMiti,
                        Date = salesData.Date,
                        VoucherNo = salesData.VoucherNo,
                        LedgerName = salesData.IsDelete ? "<<CANCEL BILL>>" : salesData.LedgerName,
                        Pan = salesData.IsDelete ? "" : salesData.Pan,
                        Quantity = salesData.IsDelete ? 0 : sale.Sum(x => x.Qty),
                        ProductName = salesData.IsDelete
                            ? ""
                            : string.Join(',',
                                sale.Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name).Distinct()),
                        ProductCategoryName = salesData.IsDelete
                            ? ""
                            : string.Join(',', sale.Select(x => x.ProductFk.ProductGroupFk.Name).Distinct()),
                        Amount = salesData.IsDelete ? 0 : salesData.Amount,
                        UnitsName = salesData.IsDelete ? "" : string.Join(',', sale.Select(x => x.UnitFk.Name).Distinct()),
                        GrossAmount = salesData.IsDelete ? 0 : salesData.GrossAmount,
                        Discount = salesData.IsDelete ? 0 : salesData.Discount,
                        NetAmount = salesData.IsDelete ? 0 : salesData.NetAmount,
                        NonTaxableAmount = salesData.IsDelete ? 0 : salesData.NonTaxableAmount,
                        LocalTaxableAmount = salesData.IsDelete ? 0 : salesData.LocalTaxableAmount,
                        LocalTaxAmount = salesData.IsDelete ? 0 : salesData.LocalTaxAmount
                    };
                    const decimal precision = (decimal)0.3;
                    if (data.NonTaxableAmount is > -precision and < precision)
                        data.NonTaxableAmount = 0;
                    result.Add(data);
                }
            }

            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.ReturnsOnly)
            {
                var salesQuery = salesReturnMasterRepository.GetAll();
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    salesQuery = salesQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    salesQuery = salesQuery.Where(x => x.Date.Date.Date <= date.Date);
                }

                var salesReturns = await salesQuery.Where(x => x.TenantId == AbpSession.TenantId
                                                               && x.FinancialYearId == FinancialYearId)
                    .Include(x => x.AccountLedgerFk)
                    .OrderBy(x => x.Date.Date).ThenBy(e => e.VoucherNumbering)
                    .ToListAsync();
                var salesReturnDetail = await salesReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(e => e.SalesReturnMasterFk)
                    .Include(x => x.ProductFk)
                    .ThenInclude(x => x.ProductGroupFk)
                    .Include(x => x.UnitFk)
                    .Where(e => e.SalesReturnMasterFk.FinancialYearId == FinancialYearId)
                    .ToListAsync();

                foreach (var salesReturnData in salesReturns)
                {
                    var productNames = "";
                    var productCategoryName = "";
                    var returnDetailQuery =
                        salesReturnDetail.Where(x => x.SalesReturnMasterId == salesReturnData.Id).ToList();

                    if (returnDetailQuery.Any())
                    {
                        productNames = string.Join(',',
                            returnDetailQuery.Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name).Distinct());
                        productCategoryName = string.Join(',',
                            returnDetailQuery.Select(x => x.ProductFk.ProductGroupFk.Name).Distinct());
                    }

                    var data = new TaxReportDto
                    {
                        Date = salesReturnData.Date,
                        DateMiti = salesReturnData.DateMiti,
                        VoucherNo = salesReturnData.VoucherNo,
                        LedgerName = salesReturnData.AccountLedgerFk.Name,
                        Pan = salesReturnData.AccountLedgerFk.Pan,
                        Quantity = -salesReturnDetail.Where(x => x.SalesReturnMasterId == salesReturnData.Id)
                            .Sum(x => x.Qty),
                        ProductName = productNames,
                        ProductCategoryName = productCategoryName,
                        UnitsName = string.Join(',', returnDetailQuery.Select(x => x.UnitFk.Name).Distinct()),
                        GrossAmount = -salesReturnData.TotalAmount,
                        Discount = -salesReturnData.BillDiscount,
                        NetAmount = -salesReturnData.NetAmount,
                        NonTaxableAmount = -(salesReturnData.NetAmount - salesReturnData.TotalTaxableAmount),
                        Amount = -salesReturnData.GrandTotal,
                        LocalTaxableAmount = salesReturnData.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -salesReturnData.TotalTaxableAmount
                            : 0,
                        LocalTaxAmount = salesReturnData.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -salesReturnData.TaxAmount
                            : 0,
                        //TaxableCapitalizeAmount = salesReturnData.InvoiceType == Purchase.Dtos.InvoiceTypeEnum.TaxableCapitalize ? salesReturnData.TaxableAmount : 0,
                        //TaxableCapitalizeTax = salesReturnData.InvoiceType == Purchase.Dtos.InvoiceTypeEnum.TaxableCapitalize ? salesReturnData.TaxAmount : 0,
                    };
                    const decimal precision = (decimal)0.3;
                    if (data.NonTaxableAmount is > -precision and < precision)
                        data.NonTaxableAmount = 0;
                    result.Add(data);
                }
            }

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesTaxSalesRegisterReport)]
        public async Task<FileDto> GetSalesTaxToExcel(string? fromDate, string? toDate, PurchaseTaxDataType type)
        {
            var data = await GetSalesReportNew(fromDate, toDate, type);
            return excelExporter.ExportToFile(data);
        }

        public async Task<List<Above1LakhMasterDto>> GetAbove1LakhParty(string? fromDate, string? toDate)
        {
            var result = new List<Above1LakhMasterDto>();
            var salesQuery = salesMasterRepository.GetAll()
                .Where(e => e.FinancialYearId == FinancialYearId && !e.IsDelete && e.TenantId == AbpSession.TenantId);
            if (!string.IsNullOrEmpty(fromDate))
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                salesQuery = salesQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (!string.IsNullOrEmpty(toDate))
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                salesQuery = salesQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            var sales = await salesQuery.ToListAsync();
            var salesMasterIds = sales.Select(x => x.Id).ToList();
            var salesReturn = await salesReturnMasterRepository.GetAll()
                .Where(x => salesMasterIds.Contains((Guid)x.SalesMasterId))
                .ToListAsync();
            var ledgerIds = sales.Select(x => x.LedgerId).Distinct().ToList();
            var accountLedgers = await accountLegerRepository.GetAll().Where(x => ledgerIds.Contains(x.Id)).ToListAsync();
            foreach (var ledger in accountLedgers)
            {
                var individualSales = sales.Where(x => x.LedgerId == ledger.Id).ToList();
                var individualSalesReturn = salesReturn.Where(x => x.LedgerId == ledger.Id).ToList();
                if (sales.Sum(x => x.GrandTotal) - salesReturn.Sum(x => x.GrandTotal) >= 100000)
                {
                    var data = new Above1LakhMasterDto
                    {
                        PartyName = ledger.Name,
                        Address = ledger.Address,
                        Pan = ledger.Pan,
                        GrossAmount = individualSales.Sum(x => x.GrossAmount) -
                                      individualSalesReturn.Sum(x => x.TotalAmount),
                        Discount =
                            individualSales.Sum(x => x.BillDiscount) - individualSalesReturn.Sum(x => x.BillDiscount),
                        NetAmount = individualSales.Sum(x => x.NetAmount) - individualSalesReturn.Sum(x => x.NetAmount),
                        TaxableAmount = individualSales.Sum(x => x.TaxableAmount) -
                                        individualSalesReturn.Sum(x => x.TotalTaxableAmount),
                        NonTaxableAmount = individualSales.Sum(x => x.NetAmount - x.TaxableAmount) -
                                           individualSalesReturn.Sum(x => x.NetAmount - x.TotalTaxableAmount),
                        TaxAmount = individualSales.Sum(x => x.TaxAmount) - individualSalesReturn.Sum(x => x.TaxAmount),
                        GrandTotal = individualSales.Sum(x => x.GrandTotal) - individualSalesReturn.Sum(x => x.GrandTotal)
                    };
                    if (data.GrandTotal >= 100000)
                        result.Add(data);
                }
            }

            return result;
        }

        public async Task<FileDto> GetAbove1LakhPartyExcel(string? fromDate, string? toDate)
        {
            var data = await GetAbove1LakhParty(fromDate, toDate);
            return excelExporter.ExportToFileDetail(data);
        }

        public async Task<List<Above1LakhMasterDto>> GetAbove1LakhDetail(string? fromDate, string? toDate)
        {
            var result = new List<Above1LakhMasterDto>();
            var salesQuery = salesMasterRepository.GetAll()
                .Where(e => e.FinancialYearId == FinancialYearId && !e.IsDelete && e.TenantId == AbpSession.TenantId);
            if (fromDate != null && fromDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                salesQuery = salesQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (toDate != null && toDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                salesQuery = salesQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            var sales = await salesQuery.Include(x => x.AccountLedgerFk).ToListAsync();
            var salesMasterIds = sales.Select(x => x.Id).ToList();
            var salesReturn = await salesReturnMasterRepository.GetAll()
                .Where(x => salesMasterIds.Contains((Guid)x.SalesMasterId)).ToListAsync();
            var ledgerIds = sales.Select(x => x.LedgerId).Distinct().ToList();
            var accountLedgers = await accountLegerRepository.GetAll().Where(x => ledgerIds.Contains(x.Id)).ToListAsync();
            foreach (var ledger in accountLedgers)
            {
                var individualSales = sales.Where(x => x.LedgerId == ledger.Id).ToList();
                var salesDetails = await salesDetailRepository.GetAll()
                    .Where(e => individualSales.Select(x => x.Id).Contains(e.SalesMasterId))
                    .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync();
                var individualSalesReturn = salesReturn.Where(x => x.LedgerId == ledger.Id).ToList();
                var totalSales = individualSales.Sum(x => x.GrandTotal) - individualSalesReturn.Sum(x => x.GrandTotal);
                if (totalSales >= 100000)
                    foreach (var individual in individualSales)
                    {
                        var data = new Above1LakhMasterDto
                        {
                            Date = individual.DateMiti,
                            BillNo = individual.VoucherNo,
                            PartyName = individual.AccountLedgerFk.Name,
                            GrossAmount = individual.GrossAmount,
                            Discount = individual.BillDiscount,
                            NetAmount = individual.NetAmount,
                            NonTaxableAmount = individual.NetAmount - individual.TaxableAmount,
                            TaxableAmount = individual.TaxableAmount,
                            TaxAmount = individual.TaxAmount,
                            GrandTotal = individual.GrandTotal,
                            ProductName = salesDetails.Where(x => x.SalesMasterId == individual.Id)
                                .Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name).Distinct()
                                .Aggregate((x, y) => x + ", " + y)
                        };
                        result.Add(data);
                    }
            }

            return result;
        }

        public async Task<FileDto> GetAbove1LakhDetailExcel(string? fromDate, string? toDate)
        {
            var data = await GetAbove1LakhDetail(fromDate, toDate);
            return excelExporter.ExportToFileDetail(data);
        }

        [AbpAuthorize(AppPermissions.PagesTaxPurchaseRegisterReport)]
        public async Task<List<TaxReportDto>> GetPurchaseReportNew(string? fromDate, string? toDate,
            PurchaseTaxDataType type, Guid branchId)
        {
            var result = new List<TaxReportDto>();
            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.PurchaseOnly)
            {
                var purchaseQuery = purchaseMasterRepository.GetAll();
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
                }


                var purchaseDatas = await purchaseQuery
                    .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId)
                    .Include(x => x.AccountLedgerFk).ToListAsync();
                var purchaseDetails = await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk).Include(x => x.UnitFk).ToListAsync();
                foreach (var purchaseData in purchaseDatas)
                {
                    var sale = purchaseDetails.Where(x => x.PurchaseMasterId == purchaseData.Id).ToList();
                    var data = new TaxReportDto
                    {
                        DateMiti = purchaseData.DateMiti,
                        VoucherNo = purchaseData.VoucherNo,
                        VendorInvoiceNo = purchaseData.VendorInvoiceNo,
                        LedgerName = purchaseData.AccountLedgerFk.Name,
                        Pan = purchaseData.AccountLedgerFk.Pan,
                        Quantity = sale.Sum(x => x.Qty),
                        ProductName = string.Join(',',
                            sale.Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name).Distinct()),
                        ProductCategoryName =
                            string.Join(',', sale.Select(x => x.ProductFk.ProductGroupFk.Name).Distinct()),
                        Amount = purchaseData.GrandTotal,
                        UnitsName = string.Join(',', sale.Select(x => x.UnitFk.Name).Distinct()),
                        GrossAmount = purchaseData.TotalAmount,
                        Discount = purchaseData.BillDiscount,
                        NetAmount = purchaseData.TotalAmount - purchaseData.BillDiscount,
                        NonTaxableAmount = purchaseData.TotalAmount - purchaseData.BillDiscount -
                                           purchaseData.TotalTaxableAmount,
                        LocalTaxableAmount = purchaseData.TotalTaxableAmount,
                        LocalTaxAmount = purchaseData.TotalTax
                    };
                    const decimal precision = (decimal)0.3;
                    if (data.NonTaxableAmount > -precision && data.NonTaxableAmount < precision)
                        data.NonTaxableAmount = 0;
                    result.Add(data);
                }
            }

            if (type is PurchaseTaxDataType.PurchaseAndReturns or PurchaseTaxDataType.ReturnsOnly)
            {
                var purchaseQuery = purchaseReturnRepository.GetAll();
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var date = DateConverter.ConvertToEnglish(fromDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    var date = DateConverter.ConvertToEnglish(toDate);
                    purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
                }

                var purchaseReturns = await purchaseQuery.Where(x => x.TenantId == AbpSession.TenantId)
                    .Include(x => x.AccountLedgerFk).ToListAsync();
                var purchaseReturnDetail = await purchaseReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.ProductFk)
                    .ThenInclude(x => x.ProductGroupFk).Include(x => x.UnitFk).ToListAsync();

                foreach (var purchaseReturnData in purchaseReturns)
                {
                    var invoiceTypeEnum = InvoiceTypeEnum.LocalInvoice;
                    var purchaseMasterData =
                        await purchaseMasterRepository.FirstOrDefaultAsync(x =>
                            x.Id == purchaseReturnData.PurchaseMasterId);
                    var productNames = "";
                    var productCategoryName = "";
                    var returndetailQuery = purchaseReturnDetail.Where(x => x.PurchaseReturnId == purchaseReturnData.Id)
                        .ToList();
                    if (returndetailQuery.Any())
                    {
                        productNames = returndetailQuery.Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name)
                            .Distinct().Aggregate((x, y) => x + ", " + y);
                        productCategoryName = returndetailQuery.Select(x => x.ProductFk.ProductGroupFk.Name).Distinct()
                            .Aggregate((x, y) => x + ", " + y);
                    }

                    var data = new TaxReportDto
                    {
                        DateMiti = purchaseReturnData.DateMiti,
                        VoucherNo = purchaseReturnData.VoucherNo,
                        LedgerName = purchaseReturnData.AccountLedgerFk.Name,
                        Pan = purchaseReturnData.AccountLedgerFk.Pan,
                        Quantity = -purchaseReturnDetail.Where(x => x.PurchaseReturnId == purchaseReturnData.Id)
                            .Sum(x => x.Qty),
                        ProductName = productNames,
                        ProductCategoryName = productCategoryName,
                        UnitsName = string.Join(',', returndetailQuery.Select(x => x.UnitFk.Name).Distinct()),
                        GrossAmount = -purchaseReturnData.TotalAmount,
                        Discount = -purchaseReturnData.TotalDiscount,
                        NetAmount = -purchaseReturnData.NetAmount,
                        NonTaxableAmount = -(purchaseReturnData.NetAmount - purchaseReturnData.TotalTaxableAmount),
                        Amount = -purchaseReturnData.GrandTotal,
                        LocalTaxableAmount = purchaseReturnData.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -purchaseReturnData.TotalTaxableAmount
                            : 0,
                        LocalTaxAmount = purchaseReturnData.InvoiceType == InvoiceTypeEnum.LocalInvoice
                            ? -purchaseReturnData.TotalTax
                            : 0,
                    };
                    const decimal precision = (decimal)0.3;
                    if (data.NonTaxableAmount > -precision && data.NonTaxableAmount < precision)
                        data.NonTaxableAmount = 0;
                    result.Add(data);
                }
            }

            return result;
        }

        public async Task<FileDto> GetPurchaseTaxToExcel(string? fromDate, string? toDate, PurchaseTaxDataType type,
            Guid branchId)
        {
            var data = await GetPurchaseReportNew(fromDate, toDate, type, branchId);
            return purchaseTaxExcelExporter.ExportToFile(data);
        }

        public async Task<List<Above1LakhMasterDto>> GetAbove1LakhPurchaseParty(string? fromDate, string? toDate)
        {
            var result = new List<Above1LakhMasterDto>();
            var purchaseQuery = purchaseMasterRepository.GetAll();
            if (fromDate != null && fromDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (toDate != null && toDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            var purchases = await purchaseQuery.ToListAsync();
            var purchaseMasterIds = purchases.Select(x => x.Id).ToList();
            var purchaseReturn = await purchaseReturnRepository.GetAll()
                .Where(x => purchaseMasterIds.Contains((Guid)x.PurchaseMasterId)).ToListAsync();
            var ledgerIds = purchases.Select(x => x.LedgerId).Distinct().ToList();
            var accountLedgers = await accountLegerRepository.GetAll().Where(x => ledgerIds.Contains(x.Id)).ToListAsync();
            foreach (var ledger in accountLedgers)
            {
                var individualpurchases = purchases.Where(x => x.LedgerId == ledger.Id).ToList();
                var individualPurchaseReturn = purchaseReturn.Where(x => x.LedgerId == ledger.Id).ToList();
                if (purchases.Sum(x => x.GrandTotal) - purchaseReturn.Sum(x => x.GrandTotal) >= 100000)
                {
                    var data = new Above1LakhMasterDto
                    {
                        PartyName = ledger.Name,
                        Address = ledger.Address,
                        Pan = ledger.Pan,
                        GrossAmount = individualpurchases.Sum(x => x.TotalAmount) -
                                      individualPurchaseReturn.Sum(x => x.TotalAmount),
                        Discount = individualpurchases.Sum(x => x.BillDiscount) -
                                   individualPurchaseReturn.Sum(x => x.TotalDiscount),
                        NetAmount = individualpurchases.Sum(x => x.TotalAmount - x.BillDiscount) -
                                    individualPurchaseReturn.Sum(x => x.NetAmount),
                        TaxableAmount = individualpurchases.Sum(x => x.TotalTaxableAmount) -
                                        individualPurchaseReturn.Sum(x => x.TotalTaxableAmount),
                        NonTaxableAmount =
                            individualpurchases.Sum(x => x.TotalAmount - x.BillDiscount - x.TotalTaxableAmount) -
                            individualPurchaseReturn.Sum(x => x.NetAmount - x.TotalTaxableAmount),
                        TaxAmount =
                            individualpurchases.Sum(x => x.TotalTax) - individualPurchaseReturn.Sum(x => x.TotalTax),
                        GrandTotal = individualpurchases.Sum(x => x.GrandTotal) -
                                     individualPurchaseReturn.Sum(x => x.GrandTotal)
                    };
                    if (data.GrandTotal >= 100000)
                        result.Add(data);
                }
            }

            return result;
        }

        public async Task<FileDto> GetAbove1LakhPurchasePartyExcel(string? fromDate, string? toDate)
        {
            var data = await GetAbove1LakhPurchaseParty(fromDate, toDate);
            return excelExporter.ExportToFileDetail(data);
        }

        public async Task<List<Above1LakhMasterDto>> GetAbove1LakhPurchaseDetail(string? fromDate, string? toDate,
            Guid branchId)
        {
            var result = new List<Above1LakhMasterDto>();
            var purchaseQuery = purchaseMasterRepository.GetAll();
            if (fromDate != null && fromDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (toDate != null && toDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                purchaseQuery = purchaseQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            var purchases = await purchaseQuery.Include(x => x.AccountLedgerFk).ToListAsync();
            var purchaseMasterIds = purchases.Select(x => x.Id).ToList();
            var purchaseReturn = await purchaseReturnRepository.GetAll()
                .Where(x => purchaseMasterIds.Contains((Guid)x.PurchaseMasterId)).ToListAsync();
            var ledgerIds = purchases.Select(x => x.LedgerId).Distinct().ToList();
            var accountLedgers = await accountLegerRepository.GetAll().Where(x => ledgerIds.Contains(x.Id)).ToListAsync();
            foreach (var ledger in accountLedgers)
            {
                var individualPurchases = purchases.Where(x => x.LedgerId == ledger.Id).ToList();
                var purchaseDetails = await purchaseDetailRepository.GetAll()
                    .Where(e => individualPurchases.Select(x => x.Id).Contains(e.PurchaseMasterId))
                    .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync();
                var individualPurchaseReturn = purchaseReturn.Where(x => x.LedgerId == ledger.Id).ToList();
                var totalSales = individualPurchases.Sum(x => x.GrandTotal) -
                                 individualPurchaseReturn.Sum(x => x.GrandTotal);
                if (totalSales >= 100000)
                    foreach (var individual in individualPurchases)
                    {
                        var data = new Above1LakhMasterDto
                        {
                            Date = individual.DateMiti,
                            BillNo = individual.VoucherNo,
                            PartyName = individual.AccountLedgerFk.Name,
                            GrossAmount = individual.TotalAmount,
                            Discount = individual.BillDiscount,
                            NetAmount = individual.TotalAmount - individual.BillDiscount,
                            NonTaxableAmount = individual.TotalAmount - individual.BillDiscount -
                                               individual.TotalTaxableAmount,
                            TaxableAmount = individual.TotalTaxableAmount,
                            TaxAmount = individual.TotalTax,
                            GrandTotal = individual.GrandTotal,
                            ProductName = purchaseDetails.Where(x => x.PurchaseMasterId == individual.Id)
                                .Select(x => x.ProductFk.Name + " " + x.Qty + " " + x.UnitFk.Name).Distinct()
                                .Aggregate((x, y) => x + ", " + y)
                        };
                        result.Add(data);
                    }
            }

            return result;
        }

        public async Task<FileDto> GetAbove1LakhPurchaseDetailExcel(string? fromDate, string? toDate, Guid branchId)
        {
            var data = await GetAbove1LakhPurchaseDetail(fromDate, toDate, branchId);
            return excelExporter.ExportToFileDetail(data);
        }
    }
}
