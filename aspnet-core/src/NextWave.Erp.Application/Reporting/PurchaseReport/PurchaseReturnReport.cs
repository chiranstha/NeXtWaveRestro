using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Purchase;
using NextWave.Erp.Reporting.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.PurchaseReport
{
    [AbpAuthorize(AppPermissions.PagesPurchaseReturnReport)]

    public class PurchaseReturnReport(
    IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
    IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository)
    : ErpAppServiceBase
    {
        public async Task<List<PurchaseReturnReportListDto>> GetPurchaseReturnReport(string? fromMiti, string? toMiti, Guid ledgerId, Guid productId, Guid productGroupId, bool showDetails)
        {
            var query = purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking();
            if (!string.IsNullOrEmpty(fromMiti))
            {
                var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                query = query.Where(d => d.Date.Date.Date >= date.Date);
            }

            if (!string.IsNullOrEmpty(toMiti))
            {
                var date = DateConverter.ConvertToEnglish(toMiti).Date;
                query = query.Where(x => x.Date.Date.Date <= date.Date);
            }

            if (ledgerId != Guid.Empty) query = query.Where(x => x.LedgerId == ledgerId);
            var sn = 0;
            var data = (await query.Include(x => x.AccountLedgerFk).ToListAsync())
                .Select(x => new PurchaseReturnReportListDto
                {
                    Id = x.Id,
                    Sn = sn++,
                    Date = x.Date,
                    DateMiti = x.DateMiti,
                    VoucherNo = x.VoucherNo,
                    LedgerName = x.AccountLedgerFk.Name,
                    TotalAmount = x.TotalAmount
                }).ToList();
            if (showDetails)
                foreach (var master in data)
                {
                    var detailQuery = purchaseReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .AsNoTracking();
                    if (productId != Guid.Empty) detailQuery = detailQuery.Where(x => x.ProductId == productId);
                    if (productGroupId != Guid.Empty)
                        detailQuery = detailQuery.Include(x => x.ProductFk)
                            .Where(x => x.ProductFk.ProductGroupId == productGroupId);
                    var details = await detailQuery.Where(x => x.PurchaseReturnId == master.Id)
                        .Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk).ToListAsync();
                    var detailsList = new List<PurchaseReturnReportDetailsListDto>();
                    foreach (var noteDetail in details)
                    {
                        var detail = new PurchaseReturnReportDetailsListDto
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
    }
}
