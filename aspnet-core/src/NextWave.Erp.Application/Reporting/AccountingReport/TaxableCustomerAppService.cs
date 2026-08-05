using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Purchase;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport
{
    [AbpAuthorize(AppPermissions.PagesTaxableCustomerReport)]

    public class TaxableCustomerAppService(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<SalesDetail, Guid> salesDetailRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository)
    : ErpAppServiceBase
    {
        public async Task<List<TaxableCustomerDto>> GetTaxableCustomers(string? fromMiti, string? toMiti, decimal maxTaxableAmount)
        {
            var result = new List<TaxableCustomerDto>();
            var salesMastersLedgerIds =
                (await salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                    .AsNoTracking().Where(x => x.LedgerId != null && !x.IsDelete).ToListAsync()).Select(x => x.LedgerId)
                .Distinct().ToList();
            var sn = 1;
            foreach (var salesMastersLedgerId in salesMastersLedgerIds)
            {
                var query = salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                    .AsNoTracking()
                    .Where(x => x.LedgerId == salesMastersLedgerId && x.FinancialYearId == FinancialYearId &&
                                x.LedgerId != null && !x.IsDelete);

                if (fromMiti != null)
                {
                    var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                    query = query.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (toMiti != null)
                {
                    var date = DateConverter.ConvertToEnglish(toMiti).Date;
                    query = query.Where(x => x.Date.Date.Date <= date.Date);
                }

                var salesMasters = await query.ToListAsync();
                decimal taxableAmount = 0;
                foreach (var salesMaster in salesMasters)
                {
                    var salesDetails = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .AsNoTracking()
                        .Where(x => x.SalesMasterId == salesMaster.Id).ToListAsync();
                    foreach (var detail in salesDetails)
                        if (detail.TaxAmount > 0)
                            taxableAmount = taxableAmount + detail.GrossAmount;
                }

                if (taxableAmount >= maxTaxableAmount)
                {
                    var accountLedger =
                        await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == salesMastersLedgerId);
                    var data = new TaxableCustomerDto
                    {
                        Sn = sn++,
                        Name = accountLedger.Name,
                        PanNo = accountLedger.Pan,
                        LedgerId = salesMastersLedgerId ?? Guid.Empty,
                        Amount = taxableAmount
                    };
                    result.Add(data);
                }
            }

            return result;
        }

        public async Task<List<TaxableCustomerDto>> GetTaxableSuppliers(string? fromMiti, string? toMiti,
            decimal maxTaxableAmount)
        {
            var result = new List<TaxableCustomerDto>();
            var purchaseMastersLedgerIds = (await purchaseMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Select(x => x.LedgerId).ToListAsync()).Distinct();
            var sn = 1;
            foreach (var purchaseMastersLedgerId in purchaseMastersLedgerIds)
            {
                var query = purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                    .Where(x => x.LedgerId == purchaseMastersLedgerId && x.FinancialYearId == FinancialYearId);

                if (fromMiti != null)
                {
                    var date = DateConverter.ConvertToEnglish(fromMiti).Date;
                    query = query.Where(x => x.Date.Date.Date >= date.Date);
                }

                if (toMiti != null)
                {
                    var date = DateConverter.ConvertToEnglish(toMiti).Date;
                    query = query.Where(x => x.Date.Date.Date <= date.Date);
                }

                var purchaseMasters = await query.ToListAsync();
                decimal taxableAmount = 0;
                decimal taxAmount = 0;
                decimal totalAmount = 0;
                foreach (var purchaseMaster in purchaseMasters)
                {
                    var purchaseDetails = await purchaseDetailRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                        .Where(x => x.PurchaseMasterId == purchaseMaster.Id).ToListAsync();
                    foreach (var detail in purchaseDetails)
                        if (detail.TaxAmount > 0)
                        {
                            taxableAmount = taxableAmount + detail.GrossAmount;
                            taxAmount = taxAmount + detail.TaxAmount;
                            totalAmount = totalAmount + detail.Amount;
                        }
                }

                var purchaseReturn = await purchaseReturnDetailRepository.GetAll().Include(x => x.PurchaseReturnFk)
                    .Where(x => x.PurchaseReturnFk.LedgerId == purchaseMastersLedgerId && x.TaxAmount > 0)
                    .Select(x => new
                    {
                        NetAmount = x.PurchaseReturnFk.DebitOrCreditNote ? -x.NetAmount : x.NetAmount,
                        TaxAmount = x.PurchaseReturnFk.DebitOrCreditNote ? -x.TaxAmount : x.TaxAmount,
                        Amount = x.PurchaseReturnFk.DebitOrCreditNote ? -x.Amount : x.Amount
                    }).AsNoTracking().ToListAsync();
                taxableAmount = taxableAmount - purchaseReturn.Sum(x => x.NetAmount);
                taxAmount = taxAmount - purchaseReturn.Sum(x => x.TaxAmount);
                totalAmount = totalAmount - purchaseReturn.Sum(x => x.Amount);
                if (taxableAmount >= maxTaxableAmount)
                {
                    var accountLedger =
                        await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseMastersLedgerId);
                    var data = new TaxableCustomerDto
                    {
                        Sn = sn++,
                        Name = accountLedger.Name,
                        PanNo = accountLedger.Pan,
                        LedgerId = purchaseMastersLedgerId,
                        Amount = taxableAmount,
                        TaxAmount = taxAmount,
                        GrandTotal = totalAmount
                    };
                    result.Add(data);
                }
            }

            return result;
        }
    }
}
