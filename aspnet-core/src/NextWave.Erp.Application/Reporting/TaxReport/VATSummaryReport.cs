using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NepDate;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Purchase;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.TaxReport.Exporting;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.TaxReport
{
    public class VATSummaryReport(
    IRepository<SalesReturnDetail, Guid> salesReturnDetailRepository,
    IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository,
    IRepository<SalesDetail, Guid> salesDetailRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
    IVatSummaryReportExcelExporter excelExporter)
    : ErpAppServiceBase
    {
        public async Task<VatSummaryReportDto> GetSalesReport()
        {
            var salesDetails = await salesDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesMasterFk)
                .Where(x => x.SalesMasterFk.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var salesReturnDetail = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesReturnMasterFk).Where(x => x.SalesReturnMasterFk.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var taxableSales = new MonthListDto();
            var vAt = new MonthListDto();
            var exemptSales = new MonthListDto();
            var zeroVatSales = new MonthListDto();
            var totalSales = new MonthListDto();
            var creditNoteTotal = new MonthListDto();
            var creditNoteVat = new MonthListDto();
            var netSales = new MonthListDto();
            var netVatCollectionOnSales = new MonthListDto();
            var nepDate = new NepaliDate(FinancialYear.FromMiti);
            var financialYear = nepDate.FiscalYearStartDate();

            for (var i = 0; i < 12; i++)
            {
                var fromMiti = financialYear.AddMonths(i).ToString();
                var toMiti = financialYear.AddMonths(i + 1).ToString();
                var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                var details = salesDetails
                    .Where(x => x.SalesMasterFk.Date.Date >= fromDate && x.SalesMasterFk.Date.Date <= toDate)
                    .ToList();
                var salesReturns = salesReturnDetail
                    .Where(x => x.SalesReturnMasterFk.Date.Date >= fromDate && x.SalesReturnMasterFk.Date.Date <= toDate)
                    .ToList();
                if (i == 0)
                {
                    taxableSales.Shrawan = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Shrawan = details.Sum(x => x.TaxAmount);
                    exemptSales.Shrawan = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Shrawan = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Shrawan = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 1)
                {
                    taxableSales.Bhadra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Bhadra = details.Sum(x => x.TaxAmount);
                    exemptSales.Bhadra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Bhadra = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Bhadra = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 2)
                {
                    taxableSales.Asoj = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Asoj = details.Sum(x => x.TaxAmount);
                    exemptSales.Asoj = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Asoj = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Asoj = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 3)
                {
                    taxableSales.Kartik = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Kartik = details.Sum(x => x.TaxAmount);
                    exemptSales.Kartik = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Kartik = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Kartik = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 4)
                {
                    taxableSales.Mangsir = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Mangsir = details.Sum(x => x.TaxAmount);
                    exemptSales.Mangsir = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Mangsir = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Mangsir = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 5)
                {
                    taxableSales.Poush = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Poush = details.Sum(x => x.TaxAmount);
                    exemptSales.Poush = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Poush = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Poush = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 6)
                {
                    taxableSales.Magh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Magh = details.Sum(x => x.TaxAmount);
                    exemptSales.Magh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Magh = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Magh = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 7)
                {
                    taxableSales.Falgun = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Falgun = details.Sum(x => x.TaxAmount);
                    exemptSales.Falgun = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Falgun = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Falgun = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 8)
                {
                    taxableSales.Chaitra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Chaitra = details.Sum(x => x.TaxAmount);
                    exemptSales.Chaitra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Chaitra = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Chaitra = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 9)
                {
                    taxableSales.Baisakh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Baisakh = details.Sum(x => x.TaxAmount);
                    exemptSales.Baisakh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Baisakh = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Baisakh = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 10)
                {
                    taxableSales.Jestha = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Jestha = details.Sum(x => x.TaxAmount);
                    exemptSales.Jestha = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Jestha = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Jestha = salesReturns.Sum(x => x.TaxAmount);
                }

                if (i == 11)
                {
                    taxableSales.Asar = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Asar = details.Sum(x => x.TaxAmount);
                    exemptSales.Asar = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    creditNoteTotal.Asar = salesReturns.Sum(x => x.GrossAmount);
                    creditNoteVat.Asar = salesReturns.Sum(x => x.TaxAmount);
                }
            }

            totalSales.Baisakh = taxableSales.Baisakh + zeroVatSales.Baisakh + exemptSales.Baisakh;
            totalSales.Jestha = taxableSales.Jestha + zeroVatSales.Jestha + exemptSales.Jestha;
            totalSales.Asar = taxableSales.Asar + zeroVatSales.Asar + exemptSales.Asar;
            totalSales.Shrawan = taxableSales.Shrawan + zeroVatSales.Shrawan + exemptSales.Shrawan;
            totalSales.Bhadra = taxableSales.Bhadra + zeroVatSales.Bhadra + exemptSales.Bhadra;
            totalSales.Asoj = taxableSales.Asoj + zeroVatSales.Asoj + exemptSales.Asoj;
            totalSales.Kartik = taxableSales.Kartik + zeroVatSales.Kartik + exemptSales.Kartik;
            totalSales.Mangsir = taxableSales.Mangsir + zeroVatSales.Mangsir + exemptSales.Mangsir;
            totalSales.Poush = taxableSales.Poush + zeroVatSales.Poush + exemptSales.Poush;
            totalSales.Magh = taxableSales.Magh + zeroVatSales.Magh + exemptSales.Magh;
            totalSales.Falgun = taxableSales.Falgun + zeroVatSales.Falgun + exemptSales.Falgun;
            totalSales.Chaitra = taxableSales.Chaitra + zeroVatSales.Chaitra + exemptSales.Chaitra;

            netSales.Baisakh = totalSales.Baisakh - creditNoteTotal.Baisakh;
            netSales.Jestha = totalSales.Jestha - creditNoteTotal.Jestha;
            netSales.Asar = totalSales.Asar - creditNoteTotal.Asar;
            netSales.Shrawan = totalSales.Shrawan - creditNoteTotal.Shrawan;
            netSales.Bhadra = totalSales.Bhadra - creditNoteTotal.Bhadra;
            netSales.Asoj = totalSales.Asoj - creditNoteTotal.Asoj;
            netSales.Kartik = totalSales.Kartik - creditNoteTotal.Kartik;
            netSales.Mangsir = totalSales.Mangsir - creditNoteTotal.Mangsir;
            netSales.Poush = totalSales.Poush - creditNoteTotal.Poush;
            netSales.Magh = totalSales.Magh - creditNoteTotal.Magh;
            netSales.Falgun = totalSales.Falgun - creditNoteTotal.Falgun;
            netSales.Chaitra = totalSales.Chaitra - creditNoteTotal.Chaitra;

            netVatCollectionOnSales.Baisakh = vAt.Baisakh - creditNoteVat.Baisakh;
            netVatCollectionOnSales.Jestha = vAt.Jestha - creditNoteVat.Jestha;
            netVatCollectionOnSales.Asar = vAt.Asar - creditNoteVat.Asar;
            netVatCollectionOnSales.Shrawan = vAt.Shrawan - creditNoteVat.Shrawan;
            netVatCollectionOnSales.Bhadra = vAt.Bhadra - creditNoteVat.Bhadra;
            netVatCollectionOnSales.Asoj = vAt.Asoj - creditNoteVat.Asoj;
            netVatCollectionOnSales.Kartik = vAt.Kartik - creditNoteVat.Kartik;
            netVatCollectionOnSales.Mangsir = vAt.Mangsir - creditNoteVat.Mangsir;
            netVatCollectionOnSales.Poush = vAt.Poush - creditNoteVat.Poush;
            netVatCollectionOnSales.Magh = vAt.Magh - creditNoteVat.Magh;
            netVatCollectionOnSales.Falgun = vAt.Falgun - creditNoteVat.Falgun;
            netVatCollectionOnSales.Chaitra = vAt.Chaitra - creditNoteVat.Chaitra;

            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;


            var result = new VatSummaryReportDto
            {
                TaxableSales = taxableSales,
                Vat = vAt,
                ExemptSales = exemptSales,
                ZeroVatSales = zeroVatSales,
                TotalSales = totalSales,
                CreditNoteTotal = creditNoteTotal,
                CreditNoteVat = creditNoteVat,
                NetSales = netSales,
                NetVatCollectionOnSales = netVatCollectionOnSales
            };
            return result;
        }

        public async Task<PurchaseVatSummaryReportDto> PurchaseReport()
        {
            var financialYe = FinancialYear;
            var beginningDay = financialYe.FromMiti;

            var purchaseDetails = await purchaseDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseMasterFk)
                .Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .ToListAsync();

            var purchaseReturnDetails = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseReturnFk)
                .Where(x => x.PurchaseReturnFk.FinancialYearId == FinancialYearId)
                .ToListAsync();

            var taxablePurchase = new MonthListDto();
            var vAt = new MonthListDto();
            var capitalPurchase = new MonthListDto();
            var capitalPurchaseVat = new MonthListDto();
            var exemptPurchase = new MonthListDto();
            var totalPurchase = new MonthListDto();
            var debitNoteTotal = new MonthListDto();
            var debitNoteVat = new MonthListDto();
            var netPurchase = new MonthListDto();
            var netVatPaidOnPurchase = new MonthListDto();
            var netVatForTheMonth = new MonthListDto();

            var nepDate = new NepaliDate(FinancialYear.FromMiti);
            var financialYear = nepDate.FiscalYearStartDate();

            for (var i = 0; i < 12; i++)
            {
                var fromMiti = financialYear.AddMonths(i).ToString();
                var toMiti = financialYear.AddMonths(i + 1).ToString();
                var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                var details = purchaseDetails.Where(x =>
                    x.PurchaseMasterFk.Date.Date >= fromDate && x.PurchaseMasterFk.Date.Date <= toDate).ToList();
                var capilize = purchaseDetails.Where(x =>
                    x.PurchaseMasterFk.Date.Date >= fromDate && x.PurchaseMasterFk.Date.Date <= toDate).ToList();

                var purchaseReturns = purchaseReturnDetails
                    .Where(x => x.PurchaseReturnFk.Date.Date >= fromDate && x.PurchaseReturnFk.Date.Date <= toDate)
                    .ToList();
                if (i == 0)
                {
                    taxablePurchase.Shrawan = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Shrawan = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Shrawan = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Shrawan = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Shrawan = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Shrawan = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Shrawan = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 1)
                {
                    taxablePurchase.Bhadra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Bhadra = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Bhadra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Bhadra = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Bhadra = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Bhadra = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Bhadra = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 2)
                {
                    taxablePurchase.Asoj = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Asoj = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Asoj = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Asoj = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Asoj = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Asoj = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Asoj = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 3)
                {
                    taxablePurchase.Kartik = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Kartik = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Kartik = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Kartik = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Kartik = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Kartik = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Kartik = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 4)
                {
                    taxablePurchase.Mangsir = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Mangsir = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Mangsir = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Mangsir = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Mangsir = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Mangsir = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Mangsir = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 5)
                {
                    taxablePurchase.Poush = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Poush = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Poush = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Poush = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Poush = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Poush = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Poush = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 6)
                {
                    taxablePurchase.Magh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Magh = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Magh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Magh = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Magh = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Magh = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Magh = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 7)
                {
                    taxablePurchase.Falgun = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Falgun = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Falgun = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Falgun = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Falgun = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Falgun = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Falgun = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 8)
                {
                    taxablePurchase.Chaitra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Chaitra = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Chaitra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Chaitra = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Chaitra = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Chaitra = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Chaitra = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 9)
                {
                    taxablePurchase.Baisakh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Baisakh = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Baisakh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Baisakh = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Baisakh = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Baisakh = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Baisakh = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 10)
                {
                    taxablePurchase.Jestha = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Jestha = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Jestha = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Jestha = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Jestha = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Jestha = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Jestha = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 11)
                {
                    taxablePurchase.Asar = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    vAt.Asar = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Asar = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Asar = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Asar = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Asar = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Asar = capilize.Sum(x => x.TaxAmount);
                }
            }

            totalPurchase.Baisakh = taxablePurchase.Baisakh + capitalPurchase.Baisakh + exemptPurchase.Baisakh;
            totalPurchase.Jestha = taxablePurchase.Jestha + capitalPurchase.Jestha + exemptPurchase.Jestha;
            totalPurchase.Asar = taxablePurchase.Asar + capitalPurchase.Asar + exemptPurchase.Asar;
            totalPurchase.Shrawan = taxablePurchase.Shrawan + capitalPurchase.Shrawan + exemptPurchase.Shrawan;
            totalPurchase.Bhadra = taxablePurchase.Bhadra + capitalPurchase.Bhadra + exemptPurchase.Bhadra;
            totalPurchase.Asoj = taxablePurchase.Asoj + capitalPurchase.Asoj + exemptPurchase.Asoj;
            totalPurchase.Kartik = taxablePurchase.Kartik + capitalPurchase.Kartik + exemptPurchase.Kartik;
            totalPurchase.Mangsir = taxablePurchase.Mangsir + capitalPurchase.Mangsir + exemptPurchase.Mangsir;
            totalPurchase.Poush = taxablePurchase.Poush + capitalPurchase.Poush + exemptPurchase.Poush;
            totalPurchase.Magh = taxablePurchase.Magh + capitalPurchase.Magh + exemptPurchase.Magh;
            totalPurchase.Falgun = taxablePurchase.Falgun + capitalPurchase.Falgun + exemptPurchase.Falgun;
            totalPurchase.Chaitra = taxablePurchase.Chaitra + capitalPurchase.Chaitra + +exemptPurchase.Chaitra;

            netPurchase.Baisakh = totalPurchase.Baisakh - debitNoteTotal.Baisakh;
            netPurchase.Jestha = totalPurchase.Jestha - debitNoteTotal.Jestha;
            netPurchase.Asar = totalPurchase.Asar - debitNoteTotal.Asar;
            netPurchase.Shrawan = totalPurchase.Shrawan - debitNoteTotal.Shrawan;
            netPurchase.Bhadra = totalPurchase.Bhadra - debitNoteTotal.Bhadra;
            netPurchase.Asoj = totalPurchase.Asoj - debitNoteTotal.Asoj;
            netPurchase.Kartik = totalPurchase.Kartik - debitNoteTotal.Kartik;
            netPurchase.Mangsir = totalPurchase.Mangsir - debitNoteTotal.Mangsir;
            netPurchase.Poush = totalPurchase.Poush - debitNoteTotal.Poush;
            netPurchase.Magh = totalPurchase.Magh - debitNoteTotal.Magh;
            netPurchase.Falgun = totalPurchase.Falgun - debitNoteTotal.Falgun;
            netPurchase.Chaitra = totalPurchase.Chaitra - debitNoteTotal.Chaitra;

            netVatPaidOnPurchase.Baisakh = vAt.Baisakh - debitNoteVat.Baisakh + capitalPurchaseVat.Baisakh;
            netVatPaidOnPurchase.Jestha = vAt.Jestha - debitNoteVat.Jestha + capitalPurchaseVat.Jestha;
            netVatPaidOnPurchase.Asar = vAt.Asar - debitNoteVat.Asar + capitalPurchaseVat.Asar;
            netVatPaidOnPurchase.Shrawan = vAt.Shrawan - debitNoteVat.Shrawan + capitalPurchaseVat.Shrawan;
            netVatPaidOnPurchase.Bhadra = vAt.Bhadra - debitNoteVat.Bhadra + capitalPurchaseVat.Bhadra;
            netVatPaidOnPurchase.Asoj = vAt.Asoj - debitNoteVat.Asoj + capitalPurchaseVat.Asoj;
            netVatPaidOnPurchase.Kartik = vAt.Kartik - debitNoteVat.Kartik + capitalPurchaseVat.Kartik;
            netVatPaidOnPurchase.Mangsir = vAt.Mangsir - debitNoteVat.Mangsir + capitalPurchaseVat.Mangsir;
            netVatPaidOnPurchase.Poush = vAt.Poush - debitNoteVat.Poush + capitalPurchaseVat.Poush;
            netVatPaidOnPurchase.Magh = vAt.Magh - debitNoteVat.Magh + capitalPurchaseVat.Magh;
            netVatPaidOnPurchase.Falgun = vAt.Falgun - debitNoteVat.Falgun + capitalPurchaseVat.Falgun;
            netVatPaidOnPurchase.Chaitra = vAt.Chaitra - debitNoteVat.Chaitra + capitalPurchaseVat.Chaitra;

            var result = new PurchaseVatSummaryReportDto
            {
                TaxablePurchase = taxablePurchase,
                Vat = vAt,
                CapitalPurchase = capitalPurchase,
                CapitalPurchaseVat = capitalPurchaseVat,
                ExemptPurchase = exemptPurchase,
                TotalPurchase = totalPurchase,
                DebitNoteTotal = debitNoteTotal,
                DebitNoteVat = debitNoteVat,
                NetPurchase = netPurchase,
                NetVatPaidOnPurchase = netVatPaidOnPurchase,
                NetVatForTheMonth = netVatForTheMonth
            };
            return result;
        }

        public async Task<FinalVatSummaryReportDto> FinalReport()
        {
            var result = new FinalVatSummaryReportDto();
            var salesDetails = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesMasterFk).Where(x => x.SalesMasterFk.FinancialYearId == FinancialYearId).ToListAsync();
            var salesReturnDetail = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesReturnMasterFk).Where(x => x.SalesReturnMasterFk.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var taxableSales = new MonthListDto();
            var salesVat = new MonthListDto();
            var exemptSales = new MonthListDto();
            var zeroVatSales = new MonthListDto();
            var totalSales = new MonthListDto();
            var creditNoteTotal = new MonthListDto();
            var creditNoteVat = new MonthListDto();
            var netSales = new MonthListDto();
            var netVatCollectionOnSales = new MonthListDto();
            var nepaliDate = new NepaliDate(FinancialYear.FromMiti);
            var fYear = nepaliDate.FiscalYearStartDate();

            for (var i = 0; i < 12; i++)
            {
                var fromMiti = fYear.AddMonths(i).ToString();
                var toMiti = fYear.AddMonths(i + 1).ToString();
                var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                var details = salesDetails
                    .Where(x => x.SalesMasterFk.Date.Date >= fromDate && x.SalesMasterFk.Date.Date < toDate)
                    .ToList();
                var salesReturns = salesReturnDetail
                    .Where(x => x.SalesReturnMasterFk.Date.Date >= fromDate && x.SalesReturnMasterFk.Date.Date < toDate)
                    .ToList();
                switch (i)
                {
                    case 0:
                        taxableSales.Shrawan = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Shrawan = details.Sum(x => x.TaxAmount);
                        exemptSales.Shrawan = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Shrawan = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Shrawan = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 1:
                        taxableSales.Bhadra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Bhadra = details.Sum(x => x.TaxAmount);
                        exemptSales.Bhadra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Bhadra = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Bhadra = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 2:
                        taxableSales.Asoj = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Asoj = details.Sum(x => x.TaxAmount);
                        exemptSales.Asoj = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Asoj = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Asoj = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 3:
                        taxableSales.Kartik = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Kartik = details.Sum(x => x.TaxAmount);
                        exemptSales.Kartik = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Kartik = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Kartik = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 4:
                        taxableSales.Mangsir = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Mangsir = details.Sum(x => x.TaxAmount);
                        exemptSales.Mangsir = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Mangsir = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Mangsir = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 5:
                        taxableSales.Poush = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Poush = details.Sum(x => x.TaxAmount);
                        exemptSales.Poush = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Poush = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Poush = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 6:
                        taxableSales.Magh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Magh = details.Sum(x => x.TaxAmount);
                        exemptSales.Magh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Magh = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Magh = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 7:
                        taxableSales.Falgun = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Falgun = details.Sum(x => x.TaxAmount);
                        exemptSales.Falgun = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Falgun = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Falgun = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 8:
                        taxableSales.Chaitra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Chaitra = details.Sum(x => x.TaxAmount);
                        exemptSales.Chaitra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Chaitra = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Chaitra = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 9:
                        taxableSales.Baisakh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Baisakh = details.Sum(x => x.TaxAmount);
                        exemptSales.Baisakh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Baisakh = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Baisakh = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 10:
                        taxableSales.Jestha = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Jestha = details.Sum(x => x.TaxAmount);
                        exemptSales.Jestha = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Jestha = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Jestha = salesReturns.Sum(x => x.TaxAmount);
                        break;
                    case 11:
                        taxableSales.Asar = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                        salesVat.Asar = details.Sum(x => x.TaxAmount);
                        exemptSales.Asar = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                        creditNoteTotal.Asar = salesReturns.Sum(x => x.GrossAmount);
                        creditNoteVat.Asar = salesReturns.Sum(x => x.TaxAmount);
                        break;
                }
            }

            totalSales.Baisakh = taxableSales.Baisakh + zeroVatSales.Baisakh + exemptSales.Baisakh;
            totalSales.Jestha = taxableSales.Jestha + zeroVatSales.Jestha + exemptSales.Jestha;
            totalSales.Asar = taxableSales.Asar + zeroVatSales.Asar + exemptSales.Asar;
            totalSales.Shrawan = taxableSales.Shrawan + zeroVatSales.Shrawan + exemptSales.Shrawan;
            totalSales.Bhadra = taxableSales.Bhadra + zeroVatSales.Bhadra + exemptSales.Bhadra;
            totalSales.Asoj = taxableSales.Asoj + zeroVatSales.Asoj + exemptSales.Asoj;
            totalSales.Kartik = taxableSales.Kartik + zeroVatSales.Kartik + exemptSales.Kartik;
            totalSales.Mangsir = taxableSales.Mangsir + zeroVatSales.Mangsir + exemptSales.Mangsir;
            totalSales.Poush = taxableSales.Poush + zeroVatSales.Poush + exemptSales.Poush;
            totalSales.Magh = taxableSales.Magh + zeroVatSales.Magh + exemptSales.Magh;
            totalSales.Falgun = taxableSales.Falgun + zeroVatSales.Falgun + exemptSales.Falgun;
            totalSales.Chaitra = taxableSales.Chaitra + zeroVatSales.Chaitra + exemptSales.Chaitra;

            netSales.Baisakh = totalSales.Baisakh - creditNoteTotal.Baisakh;
            netSales.Jestha = totalSales.Jestha - creditNoteTotal.Jestha;
            netSales.Asar = totalSales.Asar - creditNoteTotal.Asar;
            netSales.Shrawan = totalSales.Shrawan - creditNoteTotal.Shrawan;
            netSales.Bhadra = totalSales.Bhadra - creditNoteTotal.Bhadra;
            netSales.Asoj = totalSales.Asoj - creditNoteTotal.Asoj;
            netSales.Kartik = totalSales.Kartik - creditNoteTotal.Kartik;
            netSales.Mangsir = totalSales.Mangsir - creditNoteTotal.Mangsir;
            netSales.Poush = totalSales.Poush - creditNoteTotal.Poush;
            netSales.Magh = totalSales.Magh - creditNoteTotal.Magh;
            netSales.Falgun = totalSales.Falgun - creditNoteTotal.Falgun;
            netSales.Chaitra = totalSales.Chaitra - creditNoteTotal.Chaitra;

            netVatCollectionOnSales.Baisakh = salesVat.Baisakh - creditNoteVat.Baisakh;
            netVatCollectionOnSales.Jestha = salesVat.Jestha - creditNoteVat.Jestha;
            netVatCollectionOnSales.Asar = salesVat.Asar - creditNoteVat.Asar;
            netVatCollectionOnSales.Shrawan = salesVat.Shrawan - creditNoteVat.Shrawan;
            netVatCollectionOnSales.Bhadra = salesVat.Bhadra - creditNoteVat.Bhadra;
            netVatCollectionOnSales.Asoj = salesVat.Asoj - creditNoteVat.Asoj;
            netVatCollectionOnSales.Kartik = salesVat.Kartik - creditNoteVat.Kartik;
            netVatCollectionOnSales.Mangsir = salesVat.Mangsir - creditNoteVat.Mangsir;
            netVatCollectionOnSales.Poush = salesVat.Poush - creditNoteVat.Poush;
            netVatCollectionOnSales.Magh = salesVat.Magh - creditNoteVat.Magh;
            netVatCollectionOnSales.Falgun = salesVat.Falgun - creditNoteVat.Falgun;
            netVatCollectionOnSales.Chaitra = salesVat.Chaitra - creditNoteVat.Chaitra;

            result.TaxableSales = taxableSales;
            result.SalesVat = salesVat;
            result.ExemptSales = exemptSales;
            result.ZeroVatSales = zeroVatSales;
            result.TotalSales = totalSales;
            result.CreditNoteTotal = creditNoteTotal;
            result.CreditNoteVat = creditNoteVat;
            result.NetSales = netSales;
            result.NetVatCollectionOnSales = netVatCollectionOnSales;
            // sales End
            // purchase Start

            var purchaseDetails = await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseMasterFk).Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var purchaseReturnDetails = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.PurchaseReturnFk)
                .Where(x => x.PurchaseReturnFk.FinancialYearId == FinancialYearId).ToListAsync();
            var taxablePurchase = new MonthListDto();
            var purchaseVat = new MonthListDto();
            var capitalPurchase = new MonthListDto();
            var capitalPurchaseVat = new MonthListDto();
            var exemptPurchase = new MonthListDto();
            var totalPurchase = new MonthListDto();
            var debitNoteTotal = new MonthListDto();
            var debitNoteVat = new MonthListDto();
            var netPurchase = new MonthListDto();
            var netVatPaidOnPurchase = new MonthListDto();
            var netOpeningForTheMonth = new MonthListDto();
            var netVatForTheMonth = new MonthListDto();
            var netClosingForTheMonth = new MonthListDto();

            var nepDate = new NepaliDate(FinancialYear.FromMiti);
            var financialYear = nepDate.FiscalYearStartDate();

            for (var i = 0; i < 12; i++)
            {
                var fromMiti = financialYear.AddMonths(i).ToString();
                var toMiti = financialYear.AddMonths(i + 1).ToString();
                var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                var details = purchaseDetails.Where(x =>
                    x.PurchaseMasterFk.Date.Date >= fromDate && x.PurchaseMasterFk.Date.Date <= toDate).ToList();
                var capilize = purchaseDetails.Where(x =>
                    x.PurchaseMasterFk.Date.Date >= fromDate && x.PurchaseMasterFk.Date.Date <= toDate).ToList();

                var purchaseReturns = purchaseReturnDetails
                    .Where(x => x.PurchaseReturnFk.Date.Date >= fromDate && x.PurchaseReturnFk.Date.Date <= toDate)
                    .ToList();
                if (i == 0)
                {
                    taxablePurchase.Shrawan = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Shrawan = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Shrawan = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Shrawan = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Shrawan = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Shrawan = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Shrawan = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 1)
                {
                    taxablePurchase.Bhadra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Bhadra = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Bhadra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Bhadra = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Bhadra = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Bhadra = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Bhadra = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 2)
                {
                    taxablePurchase.Asoj = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Asoj = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Asoj = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Asoj = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Asoj = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Asoj = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Asoj = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 3)
                {
                    taxablePurchase.Kartik = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Kartik = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Kartik = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Kartik = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Kartik = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Kartik = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Kartik = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 4)
                {
                    taxablePurchase.Mangsir = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Mangsir = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Mangsir = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Mangsir = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Mangsir = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Mangsir = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Mangsir = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 5)
                {
                    taxablePurchase.Poush = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Poush = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Poush = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Poush = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Poush = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Poush = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Poush = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 6)
                {
                    taxablePurchase.Magh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Magh = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Magh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Magh = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Magh = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Magh = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Magh = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 7)
                {
                    taxablePurchase.Falgun = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Falgun = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Falgun = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Falgun = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Falgun = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Falgun = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Falgun = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 8)
                {
                    taxablePurchase.Chaitra = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Chaitra = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Chaitra = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Chaitra = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Chaitra = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Chaitra = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Chaitra = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 9)
                {
                    taxablePurchase.Baisakh = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Baisakh = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Baisakh = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Baisakh = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Baisakh = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Baisakh = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Baisakh = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 10)
                {
                    taxablePurchase.Jestha = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Jestha = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Jestha = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Jestha = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Jestha = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Jestha = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Jestha = capilize.Sum(x => x.TaxAmount);
                }

                if (i == 11)
                {
                    taxablePurchase.Asar = details.Where(x => x.TaxAmount > 0).Sum(x => x.GrossAmount);
                    purchaseVat.Asar = details.Sum(x => x.TaxAmount);
                    exemptPurchase.Asar = details.Where(x => x.TaxAmount == 0).Sum(x => x.GrossAmount);
                    debitNoteTotal.Asar = purchaseReturns.Sum(x => x.GrossAmount);
                    debitNoteVat.Asar = purchaseReturns.Sum(x => x.TaxAmount);
                    capitalPurchase.Asar = capilize.Sum(x => x.GrossAmount);
                    capitalPurchaseVat.Asar = capilize.Sum(x => x.TaxAmount);
                }
            }

            totalPurchase.Baisakh = taxablePurchase.Baisakh + capitalPurchase.Baisakh + exemptPurchase.Baisakh;
            totalPurchase.Jestha = taxablePurchase.Jestha + capitalPurchase.Jestha + exemptPurchase.Jestha;
            totalPurchase.Asar = taxablePurchase.Asar + capitalPurchase.Asar + exemptPurchase.Asar;
            totalPurchase.Shrawan = taxablePurchase.Shrawan + capitalPurchase.Shrawan + exemptPurchase.Shrawan;
            totalPurchase.Bhadra = taxablePurchase.Bhadra + capitalPurchase.Bhadra + exemptPurchase.Bhadra;
            totalPurchase.Asoj = taxablePurchase.Asoj + capitalPurchase.Asoj + exemptPurchase.Asoj;
            totalPurchase.Kartik = taxablePurchase.Kartik + capitalPurchase.Kartik + exemptPurchase.Kartik;
            totalPurchase.Mangsir = taxablePurchase.Mangsir + capitalPurchase.Mangsir + exemptPurchase.Mangsir;
            totalPurchase.Poush = taxablePurchase.Poush + capitalPurchase.Poush + exemptPurchase.Poush;
            totalPurchase.Magh = taxablePurchase.Magh + capitalPurchase.Magh + exemptPurchase.Magh;
            totalPurchase.Falgun = taxablePurchase.Falgun + capitalPurchase.Falgun + exemptPurchase.Falgun;
            totalPurchase.Chaitra = taxablePurchase.Chaitra + capitalPurchase.Chaitra + +exemptPurchase.Chaitra;

            netPurchase.Baisakh = totalPurchase.Baisakh - debitNoteTotal.Baisakh;
            netPurchase.Jestha = totalPurchase.Jestha - debitNoteTotal.Jestha;
            netPurchase.Asar = totalPurchase.Asar - debitNoteTotal.Asar;
            netPurchase.Shrawan = totalPurchase.Shrawan - debitNoteTotal.Shrawan;
            netPurchase.Bhadra = totalPurchase.Bhadra - debitNoteTotal.Bhadra;
            netPurchase.Asoj = totalPurchase.Asoj - debitNoteTotal.Asoj;
            netPurchase.Kartik = totalPurchase.Kartik - debitNoteTotal.Kartik;
            netPurchase.Mangsir = totalPurchase.Mangsir - debitNoteTotal.Mangsir;
            netPurchase.Poush = totalPurchase.Poush - debitNoteTotal.Poush;
            netPurchase.Magh = totalPurchase.Magh - debitNoteTotal.Magh;
            netPurchase.Falgun = totalPurchase.Falgun - debitNoteTotal.Falgun;
            netPurchase.Chaitra = totalPurchase.Chaitra - debitNoteTotal.Chaitra;

            netVatPaidOnPurchase.Baisakh = purchaseVat.Baisakh - debitNoteVat.Baisakh + capitalPurchaseVat.Baisakh;
            netVatPaidOnPurchase.Jestha = purchaseVat.Jestha - debitNoteVat.Jestha + capitalPurchaseVat.Jestha;
            netVatPaidOnPurchase.Asar = purchaseVat.Asar - debitNoteVat.Asar + capitalPurchaseVat.Asar;
            netVatPaidOnPurchase.Shrawan = purchaseVat.Shrawan - debitNoteVat.Shrawan + capitalPurchaseVat.Shrawan;
            netVatPaidOnPurchase.Bhadra = purchaseVat.Bhadra - debitNoteVat.Bhadra + capitalPurchaseVat.Bhadra;
            netVatPaidOnPurchase.Asoj = purchaseVat.Asoj - debitNoteVat.Asoj + capitalPurchaseVat.Asoj;
            netVatPaidOnPurchase.Kartik = purchaseVat.Kartik - debitNoteVat.Kartik + capitalPurchaseVat.Kartik;
            netVatPaidOnPurchase.Mangsir = purchaseVat.Mangsir - debitNoteVat.Mangsir + capitalPurchaseVat.Mangsir;
            netVatPaidOnPurchase.Poush = purchaseVat.Poush - debitNoteVat.Poush + capitalPurchaseVat.Poush;
            netVatPaidOnPurchase.Magh = purchaseVat.Magh - debitNoteVat.Magh + capitalPurchaseVat.Magh;
            netVatPaidOnPurchase.Falgun = purchaseVat.Falgun - debitNoteVat.Falgun + capitalPurchaseVat.Falgun;
            netVatPaidOnPurchase.Chaitra = purchaseVat.Chaitra - debitNoteVat.Chaitra + capitalPurchaseVat.Chaitra;


            netVatForTheMonth.Baisakh = netVatCollectionOnSales.Baisakh - netVatPaidOnPurchase.Baisakh;
            netVatForTheMonth.Jestha = netVatCollectionOnSales.Jestha - netVatPaidOnPurchase.Jestha;
            netVatForTheMonth.Asar = netVatCollectionOnSales.Asar - netVatPaidOnPurchase.Asar;
            netVatForTheMonth.Shrawan = netVatCollectionOnSales.Shrawan - netVatPaidOnPurchase.Shrawan;
            netVatForTheMonth.Bhadra = netVatCollectionOnSales.Bhadra - netVatPaidOnPurchase.Bhadra;
            netVatForTheMonth.Asoj = netVatCollectionOnSales.Asoj - netVatPaidOnPurchase.Asoj;
            netVatForTheMonth.Kartik = netVatCollectionOnSales.Kartik - netVatPaidOnPurchase.Kartik;
            netVatForTheMonth.Mangsir = netVatCollectionOnSales.Mangsir - netVatPaidOnPurchase.Mangsir;
            netVatForTheMonth.Poush = netVatCollectionOnSales.Poush - netVatPaidOnPurchase.Poush;
            netVatForTheMonth.Magh = netVatCollectionOnSales.Magh - netVatPaidOnPurchase.Magh;
            netVatForTheMonth.Falgun = netVatCollectionOnSales.Falgun - netVatPaidOnPurchase.Falgun;
            netVatForTheMonth.Chaitra = netVatCollectionOnSales.Chaitra - netVatPaidOnPurchase.Chaitra;

            netOpeningForTheMonth.Shrawan = 0;
            netClosingForTheMonth.Shrawan = netVatForTheMonth.Shrawan;

            netOpeningForTheMonth.Bhadra = netClosingForTheMonth.Shrawan < 0 ? netClosingForTheMonth.Shrawan : 0;
            netClosingForTheMonth.Bhadra = netOpeningForTheMonth.Bhadra + netVatForTheMonth.Bhadra;

            netOpeningForTheMonth.Asoj = netClosingForTheMonth.Bhadra < 0 ? netClosingForTheMonth.Bhadra : 0;
            netClosingForTheMonth.Asoj = netOpeningForTheMonth.Asoj + netVatForTheMonth.Asoj;

            netOpeningForTheMonth.Kartik = netClosingForTheMonth.Asoj < 0 ? netClosingForTheMonth.Asoj : 0;
            netClosingForTheMonth.Kartik = netOpeningForTheMonth.Kartik + netVatForTheMonth.Kartik;

            netOpeningForTheMonth.Mangsir = netClosingForTheMonth.Kartik < 0 ? netClosingForTheMonth.Kartik : 0;
            netClosingForTheMonth.Mangsir = netOpeningForTheMonth.Mangsir + netVatForTheMonth.Mangsir;

            netOpeningForTheMonth.Poush = netClosingForTheMonth.Mangsir < 0 ? netClosingForTheMonth.Mangsir : 0;
            netClosingForTheMonth.Poush = netOpeningForTheMonth.Poush + netVatForTheMonth.Poush;


            netOpeningForTheMonth.Magh = netClosingForTheMonth.Poush < 0 ? netClosingForTheMonth.Poush : 0;
            netClosingForTheMonth.Magh = netOpeningForTheMonth.Magh + netVatForTheMonth.Magh;

            netOpeningForTheMonth.Falgun = netClosingForTheMonth.Magh < 0 ? netClosingForTheMonth.Magh : 0;
            netClosingForTheMonth.Falgun = netOpeningForTheMonth.Falgun + netVatForTheMonth.Falgun;

            netOpeningForTheMonth.Chaitra = netClosingForTheMonth.Falgun < 0 ? netClosingForTheMonth.Falgun : 0;
            netClosingForTheMonth.Chaitra = netOpeningForTheMonth.Chaitra + netVatForTheMonth.Chaitra;

            netOpeningForTheMonth.Baisakh = netClosingForTheMonth.Chaitra < 0 ? netClosingForTheMonth.Chaitra : 0;
            netClosingForTheMonth.Baisakh = netOpeningForTheMonth.Baisakh + netVatForTheMonth.Baisakh;

            netOpeningForTheMonth.Jestha = netClosingForTheMonth.Baisakh < 0 ? netClosingForTheMonth.Baisakh : 0;
            netClosingForTheMonth.Jestha = netOpeningForTheMonth.Jestha + netVatForTheMonth.Jestha;

            netOpeningForTheMonth.Asar = netClosingForTheMonth.Jestha < 0 ? netClosingForTheMonth.Jestha : 0;
            netClosingForTheMonth.Asar = netOpeningForTheMonth.Asar + netVatForTheMonth.Asar;

            taxableSales.Total = taxableSales.Baisakh + taxableSales.Jestha + taxableSales.Asar + taxableSales.Shrawan +
                                 taxableSales.Bhadra + taxableSales.Asoj + taxableSales.Kartik + taxableSales.Mangsir +
                                 taxableSales.Poush + taxableSales.Magh + taxableSales.Falgun + taxableSales.Chaitra;
            salesVat.Total = salesVat.Baisakh + salesVat.Jestha + salesVat.Asar + salesVat.Shrawan + salesVat.Bhadra +
                             salesVat.Asoj + salesVat.Kartik + salesVat.Mangsir + salesVat.Poush + salesVat.Magh +
                             salesVat.Falgun + salesVat.Chaitra;
            exemptSales.Total = exemptSales.Baisakh + exemptSales.Jestha + exemptSales.Asar + exemptSales.Shrawan +
                                exemptSales.Bhadra + exemptSales.Asoj + exemptSales.Kartik + exemptSales.Mangsir +
                                exemptSales.Poush + exemptSales.Magh + exemptSales.Falgun + exemptSales.Chaitra;
            zeroVatSales.Total = zeroVatSales.Baisakh + zeroVatSales.Jestha + zeroVatSales.Asar + zeroVatSales.Shrawan +
                                 zeroVatSales.Bhadra + zeroVatSales.Asoj + zeroVatSales.Kartik + zeroVatSales.Mangsir +
                                 zeroVatSales.Poush + zeroVatSales.Magh + zeroVatSales.Falgun + zeroVatSales.Chaitra;
            totalSales.Total = totalSales.Baisakh + totalSales.Jestha + totalSales.Asar + totalSales.Shrawan +
                               totalSales.Bhadra + totalSales.Asoj + totalSales.Kartik + totalSales.Mangsir +
                               totalSales.Poush + totalSales.Magh + totalSales.Falgun + totalSales.Chaitra;
            creditNoteTotal.Total = creditNoteTotal.Baisakh + creditNoteTotal.Jestha + creditNoteTotal.Asar +
                                    creditNoteTotal.Shrawan + creditNoteTotal.Bhadra + creditNoteTotal.Asoj +
                                    creditNoteTotal.Kartik + creditNoteTotal.Mangsir + creditNoteTotal.Poush +
                                    creditNoteTotal.Magh + creditNoteTotal.Falgun + creditNoteTotal.Chaitra;
            creditNoteVat.Total = creditNoteVat.Baisakh + creditNoteVat.Jestha + creditNoteVat.Asar +
                                  creditNoteVat.Shrawan + creditNoteVat.Bhadra + creditNoteVat.Asoj + creditNoteVat.Kartik +
                                  creditNoteVat.Mangsir + creditNoteVat.Poush + creditNoteVat.Magh + creditNoteVat.Falgun +
                                  creditNoteVat.Chaitra;
            netSales.Total = netSales.Baisakh + netSales.Jestha + netSales.Asar + netSales.Shrawan + netSales.Bhadra +
                             netSales.Asoj + netSales.Kartik + netSales.Mangsir + netSales.Poush + netSales.Magh +
                             netSales.Falgun + netSales.Chaitra;
            netVatCollectionOnSales.Total = netVatCollectionOnSales.Baisakh + netVatCollectionOnSales.Jestha +
                                            netVatCollectionOnSales.Asar + netVatCollectionOnSales.Shrawan +
                                            netVatCollectionOnSales.Bhadra + netVatCollectionOnSales.Asoj +
                                            netVatCollectionOnSales.Kartik + netVatCollectionOnSales.Mangsir +
                                            netVatCollectionOnSales.Poush + netVatCollectionOnSales.Magh +
                                            netVatCollectionOnSales.Falgun + netVatCollectionOnSales.Chaitra;
            taxablePurchase.Total = taxablePurchase.Baisakh + taxablePurchase.Jestha + taxablePurchase.Asar +
                                    taxablePurchase.Shrawan + taxablePurchase.Bhadra + taxablePurchase.Asoj +
                                    taxablePurchase.Kartik + taxablePurchase.Mangsir + taxablePurchase.Poush +
                                    taxablePurchase.Magh + taxablePurchase.Falgun + taxablePurchase.Chaitra;
            purchaseVat.Total = purchaseVat.Baisakh + purchaseVat.Jestha + purchaseVat.Asar + purchaseVat.Shrawan +
                                purchaseVat.Bhadra + purchaseVat.Asoj + purchaseVat.Kartik + purchaseVat.Mangsir +
                                purchaseVat.Poush + purchaseVat.Magh + purchaseVat.Falgun + purchaseVat.Chaitra;
            capitalPurchase.Total = capitalPurchase.Baisakh + capitalPurchase.Jestha + capitalPurchase.Asar +
                                    capitalPurchase.Shrawan + capitalPurchase.Bhadra + capitalPurchase.Asoj +
                                    capitalPurchase.Kartik + capitalPurchase.Mangsir + capitalPurchase.Poush +
                                    capitalPurchase.Magh + capitalPurchase.Falgun + capitalPurchase.Chaitra;
            capitalPurchaseVat.Total = capitalPurchaseVat.Baisakh + capitalPurchaseVat.Jestha + capitalPurchaseVat.Asar +
                                       capitalPurchaseVat.Shrawan + capitalPurchaseVat.Bhadra + capitalPurchaseVat.Asoj +
                                       capitalPurchaseVat.Kartik + capitalPurchaseVat.Mangsir + capitalPurchaseVat.Poush +
                                       capitalPurchaseVat.Magh + capitalPurchaseVat.Falgun + capitalPurchaseVat.Chaitra;
            exemptPurchase.Total = exemptPurchase.Baisakh + exemptPurchase.Jestha + exemptPurchase.Asar +
                                   exemptPurchase.Shrawan + exemptPurchase.Bhadra + exemptPurchase.Asoj +
                                   exemptPurchase.Kartik + exemptPurchase.Mangsir + exemptPurchase.Poush +
                                   exemptPurchase.Magh + exemptPurchase.Falgun + exemptPurchase.Chaitra;
            totalPurchase.Total = totalPurchase.Baisakh + totalPurchase.Jestha + totalPurchase.Asar +
                                  totalPurchase.Shrawan + totalPurchase.Bhadra + totalPurchase.Asoj + totalPurchase.Kartik +
                                  totalPurchase.Mangsir + totalPurchase.Poush + totalPurchase.Magh + totalPurchase.Falgun +
                                  totalPurchase.Chaitra;
            debitNoteTotal.Total = debitNoteTotal.Baisakh + debitNoteTotal.Jestha + debitNoteTotal.Asar +
                                   debitNoteTotal.Shrawan + debitNoteTotal.Bhadra + debitNoteTotal.Asoj +
                                   debitNoteTotal.Kartik + debitNoteTotal.Mangsir + debitNoteTotal.Poush +
                                   debitNoteTotal.Magh + debitNoteTotal.Falgun + debitNoteTotal.Chaitra;
            debitNoteVat.Total = debitNoteVat.Baisakh + debitNoteVat.Jestha + debitNoteVat.Asar + debitNoteVat.Shrawan +
                                 debitNoteVat.Bhadra + debitNoteVat.Asoj + debitNoteVat.Kartik + debitNoteVat.Mangsir +
                                 debitNoteVat.Poush + debitNoteVat.Magh + debitNoteVat.Falgun + debitNoteVat.Chaitra;
            netPurchase.Total = netPurchase.Baisakh + netPurchase.Jestha + netPurchase.Asar + netPurchase.Shrawan +
                                netPurchase.Bhadra + netPurchase.Asoj + netPurchase.Kartik + netPurchase.Mangsir +
                                netPurchase.Poush + netPurchase.Magh + netPurchase.Falgun + netPurchase.Chaitra;
            netVatPaidOnPurchase.Total = netVatPaidOnPurchase.Baisakh + netVatPaidOnPurchase.Jestha +
                                         netVatPaidOnPurchase.Asar + netVatPaidOnPurchase.Shrawan +
                                         netVatPaidOnPurchase.Bhadra + netVatPaidOnPurchase.Asoj +
                                         netVatPaidOnPurchase.Kartik + netVatPaidOnPurchase.Mangsir +
                                         netVatPaidOnPurchase.Poush + netVatPaidOnPurchase.Magh +
                                         netVatPaidOnPurchase.Falgun + netVatPaidOnPurchase.Chaitra;
            netOpeningForTheMonth.Total = 0;
            netVatForTheMonth.Total = netVatForTheMonth.Baisakh + netVatForTheMonth.Jestha + netVatForTheMonth.Asar +
                                      netVatForTheMonth.Shrawan + netVatForTheMonth.Bhadra + netVatForTheMonth.Asoj +
                                      netVatForTheMonth.Kartik + netVatForTheMonth.Mangsir + netVatForTheMonth.Poush +
                                      netVatForTheMonth.Magh + netVatForTheMonth.Falgun + netVatForTheMonth.Chaitra;
            netClosingForTheMonth.Total = 0;
            result.TaxablePurchase = taxablePurchase;
            result.PurchaseVat = purchaseVat;
            result.CapitalPurchase = capitalPurchase;
            result.CapitalPurchaseVat = capitalPurchaseVat;
            result.ExemptPurchase = exemptPurchase;
            result.TotalPurchase = totalPurchase;
            result.DebitNoteTotal = debitNoteTotal;
            result.DebitNoteVat = debitNoteVat;
            result.NetPurchase = netPurchase;
            result.NetVatPaidOnPurchase = netVatPaidOnPurchase;
            result.NetOpeningForTheMonth = netOpeningForTheMonth;
            result.NetVatForTheMonth = netVatForTheMonth;
            result.NetClosingForTheMonth = netClosingForTheMonth;

            // purchase End

            return result;
        }

        public async Task<FileDto> CreateVatSummeryReportToExcel()
        {
            var result = new List<FinalVatSummaryReportDto>();
            var data = await FinalReport();
            result.Add(data);
            return excelExporter.ExportToFile(result);
        }
    }
}
