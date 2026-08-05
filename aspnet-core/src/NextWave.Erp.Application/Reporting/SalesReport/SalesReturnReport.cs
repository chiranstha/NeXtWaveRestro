using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.SalesReport
{
    [AbpAuthorize(AppPermissions.PagesSalesReturnReport)]

    public class SalesReturnReport(
    IRepository<SalesReturnMaster, Guid> salesReturnRepository,
    IRepository<SalesReturnDetail, Guid> salesReturnDetailRepository)
    : ErpAppServiceBase
    {
        public async Task<List<PurchaseOrderReportListDto>> GetSalesReturnReport(string? fromDate, string? toDate, Guid? ledgerId, Guid? productId, Guid? productGroupId, bool showDetails)
        {
            var query = salesReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking();
            if (fromDate != null && fromDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                query = query.Where(d => d.Date.Date.Date >= date.Date);
            }

            if (toDate != null && toDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                query = query.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != null && ledgerId != Guid.Empty) query = query.Where(x => x.LedgerId == ledgerId);
            var sn = 0;
            var data = (await query.Include(x => x.AccountLedgerFk).ToListAsync()).Select(x =>
                new PurchaseOrderReportListDto
                {
                    Id = x.Id,
                    Sn = sn++,
                    Date = x.Date,
                    DateMiti = x.DateMiti,
                    VoucherNo = x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    TotalAmount = x.GrandTotal
                }).ToList();
            if (showDetails)
                foreach (var master in data)
                {
                    var detailQuery = salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .AsNoTracking();
                    if (productId != null && productId != Guid.Empty)
                        detailQuery = detailQuery.Where(x => x.ProductId == productId);
                    if (productGroupId != null && productGroupId != Guid.Empty)
                        detailQuery = detailQuery.Include(x => x.ProductFk)
                            .Where(x => x.ProductFk.ProductGroupId == productGroupId);
                    var details = await detailQuery.Where(x => x.SalesReturnMasterId == master.Id)
                        .Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk).ToListAsync();
                    var detailsList = new List<PurchaseOrderReportDetailsListDto>();
                    foreach (var noteDetail in details)
                    {
                        var detail = new PurchaseOrderReportDetailsListDto
                        {
                            ProductName = noteDetail.ProductFk.Name,
                            Rate = noteDetail.Rate,
                            Qty = noteDetail.Qty,
                            Amount = noteDetail.Amount
                        };
                        detailsList.Add(detail);
                    }

                    master.Details = detailsList;
                }

            return data;
        }

        public async Task<List<PurchaseReportList>> GetSalesReportNew(string? fromDate, string? toDate,
            Guid? ledgerId, Guid? productId, Guid? productGroupId, bool showDetails)
        {
            var query = salesReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking();
            if (fromDate != null && fromDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                query = query.Where(d => d.Date.Date.Date >= date.Date);
            }

            if (toDate != null && toDate != string.Empty)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                query = query.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != null && ledgerId != Guid.Empty) query = query.Where(x => x.LedgerId == ledgerId);
            var sn = 0;
            var data = (await query.Include(x => x.AccountLedgerFk).ToListAsync()).Select(x =>
                new PurchaseReportList
                {
                    Date = x.DateMiti,
                    GrandTotal = x.GrandTotal,
                    Pan = x.AccountLedgerFk.Pan,
                    TotalTaxAmount = x.TaxAmount,
                    NonTaxable = x.NetAmount - x.TotalTaxableAmount,
                    Taxable = x.TotalTaxableAmount,
                    InvoiceNo = x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    TotalAmount = x.GrandTotal
                }).ToList();
            if (showDetails)
                foreach (var master in data)
                {
                    var detailQuery = salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .AsNoTracking();
                    if (productId != null && productId != Guid.Empty)
                        detailQuery = detailQuery.Where(x => x.ProductId == productId);
                    if (productGroupId != null && productGroupId != Guid.Empty)
                        detailQuery = detailQuery.Include(x => x.ProductFk)
                            .Where(x => x.ProductFk.ProductGroupId == productGroupId);
                    var details = await detailQuery.Where(x => x.SalesReturnMasterId == master.MasterId)
                        .Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk).ToListAsync();
                    var detailsList = new List<PurchaseReportDetailList>();
                    foreach (var noteDetail in details)
                    {
                        var detail = new PurchaseReportDetailList
                        {
                            ProductName = noteDetail.ProductFk.Name,
                            Rate = noteDetail.Rate,
                            Qty = noteDetail.Qty,
                            Amount = noteDetail.Amount
                        };
                        detailsList.Add(detail);
                    }

                    master.PurchaseDetail = detailsList;
                }

            return data;
        }

        //public async Task<byte[]> GetPdfDownload(string? fromDate, string? toDate, Guid? ledgerId,
        //    Guid? productId, Guid? productGroupId, bool showDetails)
        //{
        //    var branch = new Branch();
        //    branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
        //    var ledger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == ledgerId);
        //    var financial = await financialYearRepository.FirstOrDefaultAsync(x => x.Status);

        //    var data = await GetSalesReportNew(fromDate, toDate, ledgerId, productId, productGroupId,
        //        showDetails);
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
    }
}
