using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NepDate;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Reporting.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.PurchaseReport
{
    [AbpAuthorize(AppPermissions.PagesProductWiseMonthlyReport)]
    public class MonthlyPurchaseReport(
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<ProductGroup, Guid> productGroupRepository)
    : ErpAppServiceBase
    {
        private async Task<List<Guid>> FuncRecursive(Guid id)
        {
            var result = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.GroupUnder == id).Select(x => x.Id).ToListAsync();
            var groupList = new List<Guid>();
            foreach (var i in result)
            {
                var groupListnew = await FuncRecursive(i);
                groupList.AddRange(groupListnew);
            }

            groupList.Add(id);
            groupList.Sort();
            return groupList;
        }

        public async Task<List<LedgerwiseMonthlySalesDto>> GetAccountwiseReport(Guid accountGroupId)
        {
            var purchaseMasterQuery = purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Include(x => x.AccountLedgerFk).Select(x => new
                {
                    x.AccountLedgerFk.AccountGroupId,
                    x.Date,
                    x.LedgerId,
                    x.GrandTotal,
                    LedgerName = x.AccountLedgerFk.Name
                });
            if (accountGroupId != Guid.Empty)
            {
                var a = await FuncRecursive(accountGroupId);
                purchaseMasterQuery = purchaseMasterQuery.Where(x => a.Contains(x.AccountGroupId));
            }

            var salesData = await purchaseMasterQuery.ToListAsync();
            var financialYe = FinancialYear;
            var result = new List<LedgerwiseMonthlySalesDto>();
            var ledgers = salesData.Select(x => x.LedgerId).Distinct().ToList();
            var ledgerData = salesData.Select(x => new
            {
                x.LedgerId,
                x.LedgerName
            }).ToList();
            var yearStart = financialYe.FromMiti.Split('/')[0];
            var beginningDay = yearStart + "/" + 4 + "/" + 1;

            foreach (var ledgerId in ledgers)
            {
                var sales = salesData.Where(x => x.LedgerId == ledgerId).ToList();
                var data = new LedgerwiseMonthlySalesDto
                {
                    Id = ledgerId,
                    LedgerName = ledgerData.FirstOrDefault(x => x.LedgerId == ledgerId).LedgerName
                };
                var details = new MonthlySalesReportDto();
                var nepDate = new NepaliDate(FinancialYear.FromMiti);
                var financialYear = nepDate.FiscalYearStartDate();

                for (var i = 0; i < 12; i++)
                {
                    var fromMiti = financialYear.AddMonths(i).ToString();
                    var toMiti = financialYear.AddMonths(i + 1).ToString();
                    var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                    var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                    var getAmount = sales.Where(x => x.Date.Date >= fromDate && x.Date.Date < toDate)
                        .Sum(x => x.GrandTotal);
                    switch (i)
                    {
                        case 0:
                            details.Shrawan = getAmount;
                            break;
                        case 1:
                            details.Bhadra = getAmount;
                            break;
                        case 2:
                            details.Aswin = getAmount;
                            break;
                        case 3:
                            details.Kartik = getAmount;
                            break;
                        case 4:
                            details.Mangsir = getAmount;
                            break;
                        case 5:
                            details.Poush = getAmount;
                            break;
                        case 6:
                            details.Magh = getAmount;
                            break;
                        case 7:
                            details.Falgun = getAmount;
                            break;
                        case 8:
                            details.Chaitra = getAmount;
                            break;
                        case 9:
                            details.Baisakh = getAmount;
                            break;
                        case 10:
                            details.Jestha = getAmount;
                            break;
                        case 11:
                            details.Asar = getAmount;
                            break;
                    }

                    details.FirstQuarter = details.Shrawan + details.Bhadra + details.Aswin;
                    details.SecondQuarter = details.Kartik + details.Mangsir + details.Poush;
                    details.ThirdQuarter = details.Magh + details.Falgun + details.Chaitra;
                    details.Yearly = details.FirstQuarter + details.SecondQuarter + details.ThirdQuarter +
                                     details.Baisakh + details.Jestha + details.Asar;
                }

                data.Details = details;
                result.Add(data);
            }

            return result;
        }

        private async Task<List<Guid>> ProductGroupFuncRecursive(Guid id)
        {
            var result = await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.GroupUnder == id).Select(x => x.Id).ToListAsync();
            var groupList = new List<Guid>();
            foreach (var i in result)
            {
                var groupListnew = await FuncRecursive(i);
                groupList.AddRange(groupListnew);
            }

            groupList.Add(id);
            groupList.Sort();
            return groupList;
        }

        public async Task<List<LedgerwiseMonthlySalesDto>> GetProductwiseReport(Guid productGroupId)
        {
            var productGroupIds = await ProductGroupFuncRecursive(productGroupId);

            var purchaseDetailsQuery = purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId);
            if (productGroupId != Guid.Empty)
                purchaseDetailsQuery = purchaseDetailsQuery.Include(x => x.ProductFk)
                    .Where(x => productGroupIds.Contains(x.ProductFk.ProductGroupId));

            var salesData = await purchaseDetailsQuery.Include(x => x.PurchaseMasterFk).Include(x => x.ProductFk)
                .Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId).ToListAsync();
            var financialYe = FinancialYear;
            var result = new List<LedgerwiseMonthlySalesDto>();
            var products = salesData.Select(x => x.ProductId).Distinct().ToList();
            var productDate = salesData.Select(x => new
            {
                x.ProductId,
                ProductName = x.ProductFk.Name
            }).ToList();
            var year = financialYe.FromMiti.Split('/')[0];
            foreach (var productId in products)
            {
                var sales = salesData.Where(x => x.ProductId == productId).ToList();
                var data = new LedgerwiseMonthlySalesDto
                {
                    Id = productId,
                    LedgerName = productDate.FirstOrDefault(x => x.ProductId == productId)?.ProductName
                };
                var details = new MonthlySalesReportDto();

                var nepDate = new NepaliDate(FinancialYear.FromMiti);
                var financialYear = nepDate.FiscalYearStartDate();

                for (var i = 0; i < 12; i++)
                {
                    var fromMiti = financialYear.AddMonths(i).ToString();
                    var toMiti = financialYear.AddMonths(i + 1).ToString();
                    var fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
                    var toDate = DateConverter.ConvertToEnglish(toMiti).Date;
                    var monthSum = sales.Where(x =>
                            x.PurchaseMasterFk.Date.Date >= fromDate && x.PurchaseMasterFk.Date.Date < toDate)
                        .Sum(x => x.Amount);

                    switch (i)
                    {
                        case 0:
                            details.Shrawan = monthSum;
                            break;
                        case 1:
                            details.Bhadra = monthSum;
                            break;
                        case 2:
                            details.Aswin = monthSum;
                            break;
                        case 3:
                            details.Kartik = monthSum;
                            break;
                        case 4:
                            details.Mangsir = monthSum;
                            break;
                        case 5:
                            details.Poush = monthSum;
                            break;
                        case 6:
                            details.Magh = monthSum;
                            break;
                        case 7:
                            details.Falgun = monthSum;
                            break;
                        case 8:
                            details.Chaitra = monthSum;
                            break;
                        case 9:
                            details.Baisakh = monthSum;
                            break;
                        case 10:
                            details.Jestha = monthSum;
                            break;
                        case 11:
                            details.Asar = monthSum;
                            break;
                    }
                }

                details.FirstQuarter = details.Shrawan + details.Bhadra + details.Aswin;
                details.SecondQuarter = details.Kartik + details.Mangsir + details.Poush;
                details.ThirdQuarter = details.Magh + details.Falgun + details.Chaitra;
                details.Yearly = details.FirstQuarter + details.SecondQuarter + details.ThirdQuarter + details.Baisakh +
                                 details.Jestha + details.Asar;
                data.Details = details;
                result.Add(data);
            }

            return result;
        }
    }
}
