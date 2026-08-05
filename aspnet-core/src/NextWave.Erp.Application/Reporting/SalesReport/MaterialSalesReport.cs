using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Reporting.SalesReport.Exporting;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.SalesReport
{
    [AbpAuthorize(AppPermissions.PagesMaterialSalesReport)]

    public class MaterialSalesReport(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IMaterialSalesReportExcelExporter excelExporter)
    : ErpAppServiceBase
    {
        public async Task<List<MaterialSalesReportDto>> GetReport(string? fromMiti, string? toMiti)
        {
            var salesMasterQuery = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .Include(x => x.FinancialYearFk)
                .AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId);
            if (!string.IsNullOrWhiteSpace(fromMiti))
            {
                var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                salesMasterQuery = salesMasterQuery.Where(x => x.Date.Date.Date >= date.Date);
            }

            if (!string.IsNullOrWhiteSpace(toMiti))
            {
                var date = DateConverter.ConvertToEnglish(toMiti).Date;
                salesMasterQuery = salesMasterQuery.Where(x => x.Date.Date.Date <= date.Date);
            }

            return await salesMasterQuery
                .AsNoTracking().OrderByDescending(x => x.Date.Date).ThenByDescending(e => e.VoucherNumbering)
                .Select(x => new MaterialSalesReportDto
                {
                    FiscalYear = x.FinancialYearFk.Name,
                    PaymentMethod = x.IsDelete ? "" : x.PaymentMethod.ToString(),
                    CustomerName = x.IsDelete ? "BillCancel" : x.AccountLedgerFk.Name,
                    CustomerPan = x.IsDelete ? "" : x.AccountLedgerFk.Pan,
                    BillDate = x.DateMiti,
                    BillNo = x.VoucherNo,
                    TaxAmount = x.IsDelete ? 0 : x.TaxAmount,
                    Discount = x.IsDelete ? 0 : x.BillDiscount,
                    Amount = x.IsDelete ? 0 : x.GrossAmount,
                    TaxableAmount = x.IsDelete ? 0 : x.TaxableAmount,
                    TotalAmount = x.IsDelete ? 0 : x.GrandTotal,
                    PrintedTime = x.PrintedTime,
                    EnteredBy = x.CreateUserFk.Name,
                    PrintedBy = x.CreateUserFk.Name,
                    VatRefundAmount = x.VatRefundAmount ?? 0,
                    TransactionId = 0,
                    SyncIrd = x.SyncwithIrd,
                    BillPrint = x.IsPrint,
                    RealTime = x.IsRealTime,
                    Active = !x.IsDelete
                }).ToListAsync();
        }

        public async Task<FileDto> CreateMaterialSalesReportToExcel(string? fromMiti, string? toMiti)
        {
            var data = await GetReport(fromMiti, toMiti);
            return excelExporter.ExportToFile(data);
        }
    }
}
