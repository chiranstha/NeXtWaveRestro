using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.PurchaseReport.Exporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.PurchaseReport
{
    [AbpAuthorize(AppPermissions.PagesPurchaseMasterReport)]

    public class PurchaseMasterReport(
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
    IRepository<PurchaseMaster, Guid> purchaseRepository,
    IRepository<FinancialYear, Guid> financialYeaRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<Product, Guid> productRepository,
    IRepository<Branch, Guid> branchRepository,
    IPurchaseReportExport purchaseReportExport)
    : ErpAppServiceBase
    {
        public async Task<List<PurchaseReportList>> GetPurchaseReport(string? fromDate, string? toDate, Guid ledgerId,
            Guid productId, bool isShowDetail)
        {
            var list = new List<PurchaseReportList>();

            var query = purchaseRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.Id,
                    x.Date,
                    x.DateMiti,
                    x.LedgerId,
                    x.VoucherNo,
                    x.VendorInvoiceNo,
                    LedegerName = x.AccountLedgerFk.Name,
                    x.TotalAmount,
                    x.TotalTax,
                    x.GrandTotal
                });

            var detailsQuery = purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseMasterFk)
                .Include(x => x.PurchaseMasterFk).ThenInclude(x => x.AccountLedgerFk)
                .Include(x => x.ProductFk)
                .Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .Select(x => new PurchaseReportDetailList
                {
                    PurchaseMasterId = x.PurchaseMasterId,
                    Date = x.PurchaseMasterFk.DateMiti,
                    ProductName = x.ProductFk.Name,
                    LedgerName = x.PurchaseMasterFk.AccountLedgerFk.Name,
                    TaxAmount = x.TaxAmount,
                    InvoiceNo = x.PurchaseMasterFk.VoucherNo,
                    Rate = x.Rate,
                    GrossAmount = x.GrossAmount,
                    ProductId = x.ProductId,
                    Qty = x.Qty,
                    Amount = x.Amount
                });

            if (productId != Guid.Empty)
                detailsQuery = detailsQuery.Where(x => x.ProductId == productId);
            var details = await detailsQuery.ToListAsync();

            if (!string.IsNullOrEmpty(fromDate))
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                query = query.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (!string.IsNullOrEmpty(toDate))
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                query = query.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != Guid.Empty) query = query.Where(x => x.LedgerId == ledgerId);


            if (productId != Guid.Empty)
            {
                foreach (var detail in details)
                {
                    var model = new PurchaseReportList
                    {
                        MasterId = detail.PurchaseMasterId,
                        Date = detail.Date,
                        InvoiceNo = detail.InvoiceNo,
                        LedgerName = detail.LedgerName,
                        TotalAmount = detail.GrossAmount,
                        TotalTaxAmount = detail.TaxAmount,
                        GrandTotal = detail.Amount,
                        PurchaseDetail = isShowDetail
                            ? details.Where(a => a.PurchaseMasterId == detail.PurchaseMasterId).ToList()
                            : null
                    };
                    list.Add(model);
                }

                return list;
            }

            foreach (var purchase in await query.OrderByDescending(x => x.Date).ToListAsync())
            {
                var model = new PurchaseReportList
                {
                    MasterId = purchase.Id,
                    Date = purchase.DateMiti,
                    InvoiceNo = purchase.VoucherNo + "-->" + purchase.VendorInvoiceNo,
                    LedgerName = purchase.LedegerName,
                    TotalAmount = purchase.TotalAmount,
                    TotalTaxAmount = purchase.TotalTax,
                    GrandTotal = purchase.GrandTotal,
                    PurchaseDetail = isShowDetail ? details.Where(a => a.PurchaseMasterId == purchase.Id).ToList() : null
                };
                list.Add(model);
            }

            return list;
        }

        //public async Task<byte[]> GetPurchaseReportPdf(string? fromDate, string? toDate, Guid ledgerId,
        //    Guid productId, bool isShowDetail)
        //{
        //    var data = await GetPurchaseReport(fromDate, toDate, ledgerId, productId, isShowDetail);
        //    var company = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
        //    var aaa = new PurchaseReportMasterList
        //    {
        //        Logo = company.Image1,
        //        Address = company.Address,
        //        BranchName = company.Name,
        //        CompanyName = company.CompanyName,
        //        Contact = company.PhoneNo1,
        //        FromDate = fromDate,
        //        ToDate = toDate,
        //        Details = data
        //    };

        //    var document = new PurchaseReportPdf(aaa);
        //    return document.GeneratePdf();
        //}

        public async Task<FileDto> GetPurchaseReportExcel1(string? fromDate, string? toDate, Guid ledgerId,
            Guid productId, bool isShowDetail)
        {
            var data = await GetPurchaseReport(fromDate, toDate, ledgerId, productId, isShowDetail);

            return purchaseReportExport.ExportToFile(data);
        }

        public async Task<FileDto> GetPurchaseReportExcel(string? fromDate, string? toDate, Guid ledgerId, Guid productId, bool isShowDetail)
        {
            var filteredPurchaseMasters = purchaseRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk).Where(x => x.FinancialYearId == FinancialYearId);

            if (fromDate != null)
            {
                var date = DateConverter.ConvertToEnglish(fromDate);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (toDate != null)
            {
                var date = DateConverter.ConvertToEnglish(toDate);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != Guid.Empty)
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.LedgerId == ledgerId);

            var detailsQuery = purchaseDetailRepository.GetAll()
                .Include(x => x.PurchaseMasterFk).Include(x => x.UnitFk).Include(x => x.ProductFk)
                .Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.ProductFk.Name,
                    UnitName = x.UnitFk.Name,
                    x.ProductId,
                    x.Qty,
                    x.Rate,
                    x.Amount,
                    x.PurchaseMasterId
                });
            if (productId != Guid.Empty)
                detailsQuery = detailsQuery.Where(x => x.ProductId == productId);

            var purchaseDetails = await detailsQuery.AsNoTracking().ToListAsync();

            var query = from o in filteredPurchaseMasters.OrderBy(x => x.Date.Date).ThenBy(x => x.VoucherNumbering)
                        select new GetPurchaseMasterExcelExportDetailDto
                        {
                            Date = o.Date,
                            VoucherNo = o.VoucherNo,
                            DateMiti = o.DateMiti,
                            VendorInvoiceNo = o.VendorInvoiceNo,
                            TotalAmount = o.TotalAmount,
                            LedgerPan = o.AccountLedgerFk.Pan,
                            TotalTax = o.TotalTax,
                            GrandTotal = o.GrandTotal,
                            NonTaxable = Math.Round(o.TotalAmount - o.BillDiscount - o.TotalTaxableAmount, 1),
                            TotalTaxableAmount = o.TotalTaxableAmount,
                            BillDiscount = o.BillDiscount,
                            Narration = o.Narration,
                            Id = o.Id,
                            LedgerName = o.AccountLedgerFk.Name,
                        };

            var purchaseMasterListDtos = await query.ToListAsync();
            var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);

            if (isShowDetail)
                foreach (var data in purchaseMasterListDtos)
                    data.Details = purchaseDetails.Where(x => x.PurchaseMasterId == data.Id)
                        .Select(x => new ProductDetailsForExcelExport
                        {
                            Name = x.Name,
                            Unit = x.UnitName,
                            Qty = x.Qty,
                            Rate = x.Rate,
                            Amount = x.Amount
                        }).ToList();
            var data1 = new GetPurchaseMasterExcelExportMasterDto
            {
                Branch = branch.Name,
                PhoneNo = branch.PhoneNo1,
                Address = branch.Address,
                Pan = branch.PANumber,
                FromDate = fromDate,
                ToDate = toDate,
                Details = purchaseMasterListDtos
            };
            return purchaseReportExport.ExportToFileNepali(data1);
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

        public async Task<List<UniversalDropdownDto>> GetAllProducts()
        {
            var result = new List<UniversalDropdownDto>
              {
                  new()
                  {
                      Id = Guid.Empty,
                      DisplayName = "All"
                  }
              };
            result.AddRange(await productRepository.GetAll()                
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToListAsync());
            return result;
        }
    }
}
