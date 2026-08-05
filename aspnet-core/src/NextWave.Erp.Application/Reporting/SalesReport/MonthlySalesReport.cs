using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NepDate;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.SalesReport
{
  //  [AbpAuthorize(AppPermissions.PagesLedgerWiseSalesReport, AppPermissions.PagesProductWiseSalesReport)]
    public class MonthlySalesReport(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<SalesDetail, Guid> salesDetailRepository,
    IRepository<ProductGroup, Guid> productGroupRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository)
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
            var salesMasterQuery = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
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
                salesMasterQuery = salesMasterQuery.Where(x => a.Contains(x.AccountGroupId));
            }

            var salesData = await salesMasterQuery.ToListAsync();
            var financialYe = FinancialYear;
            var result = new List<LedgerwiseMonthlySalesDto>();
            var ledgers = salesData.Select(x => x.LedgerId).Distinct().ToList();
            var ledgerData = salesData.Select(x => new
            {
                x.LedgerId,
                x.LedgerName
            }).ToList();


            foreach (var ledgerId in ledgers)
            {
                var sales = salesData.Where(x => x.LedgerId == ledgerId).ToList();
                var data = new LedgerwiseMonthlySalesDto
                {
                    Id = (Guid)ledgerId,
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
                    var getAmount = sales.Where(x => x.Date.Date >= fromDate.Date && x.Date.Date < toDate.Date)
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
                    details.Yearly = details.FirstQuarter + details.SecondQuarter + details.ThirdQuarter + details.Baisakh +
                                     details.Jestha + details.Asar;
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

            var salesQuery = salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId);
            if (productGroupId != Guid.Empty)
                salesQuery = salesQuery.Include(x => x.ProductFk)
                    .Where(x => productGroupIds.Contains(x.ProductFk.ProductGroupId));

            var salesData = await salesQuery.Include(x => x.SalesMasterFk).Include(x => x.ProductFk)
                .Where(x => x.SalesMasterFk.FinancialYearId == FinancialYearId).ToListAsync();
            var financialYe = FinancialYear;
            var result = new List<LedgerwiseMonthlySalesDto>();
            var products = salesData.Select(x => x.ProductId).Distinct().ToList();
            var productDate = salesData.Select(x => new
            {
                x.ProductId,
                ProductName = x.ProductFk.Name
            }).ToList();
            foreach (var productId in products)
            {
                var sales = salesData.Where(x => x.ProductId == productId).ToList();
                var data = new LedgerwiseMonthlySalesDto
                {
                    Id = productId,
                    LedgerName = productDate.FirstOrDefault(x => x.ProductId == productId).ProductName
                };
                var details = new MonthlySalesReportDto();
                for (var i = financialYe.FromDate; i <= financialYe.ToDate; i = i.AddMonths(1))
                {
                    if (i.Month == 7)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Shrawan = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 8)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Bhadra = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 9)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Aswin = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 10)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Kartik = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 11)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Mangsir = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 12)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Poush = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 1)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Magh = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 2)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Falgun = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 3)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Chaitra = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 4)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Baisakh = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 5)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Jestha = aaa.Sum(x => x.Amount);
                    }

                    if (i.Month == 6)
                    {
                        var aaa = sales.Where(x =>
                                x.SalesMasterFk.Date.Date > i && x.SalesMasterFk.Date.Date <= i.AddMonths(1))
                            .ToList();
                        if (aaa.Count > 0) details.Asar = aaa.Sum(x => x.Amount);
                    }

                    details.FirstQuarter = details.Shrawan + details.Bhadra + details.Aswin;
                    details.SecondQuarter = details.Kartik + details.Mangsir + details.Poush;
                    details.ThirdQuarter = details.Magh + details.Falgun + details.Chaitra;
                    details.Yearly = details.FirstQuarter + details.SecondQuarter + details.ThirdQuarter + details.Baisakh +
                                     details.Jestha + details.Asar;
                }

                data.Details = details;
                result.Add(data);
            }

            return result;
        }
    }
}
