using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.AccountingReport.Exporting;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport
{
    [AbpAuthorize(AppPermissions.PagesAccountLedgerReport)]
    public class AccountLedgerReport(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IAccountLedgerExcelExporter excelExporter
        )
    : ErpAppServiceBase
    {
        // Cache for recursive group results to avoid repeated DB queries
        private readonly Dictionary<Guid, HashSet<Guid>> _groupCache = new();

        public async Task<List<AccountLedgerReportList>> GetReport(Guid groupId, bool isZeroBalance, string? fromMiti, string? toMiti)
        {
            var reportFinancialYear = FinancialYear;
            var fromDate = reportFinancialYear.FromDate;
            var toDate = reportFinancialYear.ToDate;

            if (!string.IsNullOrWhiteSpace(fromMiti))
                fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;

            if (!string.IsNullOrWhiteSpace(toMiti))
                toDate = DateConverter.ConvertToEnglish(toMiti).Date;

            // Build base query
            var baseQuery = ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Include(e => e.VoucherTypeFk)
                .Select(x => new
                {
                    x.TenantId,
                    x.FinancialYearId,
                    x.AccountLedgerFk.AccountGroupId,
                    x.LedgerId,
                    x.Date,
                    x.Debit,
                    x.Credit,
                    VoucherName = x.VoucherTypeFk.Name
                })
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == reportFinancialYear.Id);

            HashSet<Guid>? applicableGroupIds = null;

            // Apply filters
            if (groupId != Guid.Empty)
            {
                applicableGroupIds = await GetGroupIdsRecursivelyAsync(groupId);
                baseQuery = baseQuery.Where(x => applicableGroupIds.Contains(x.AccountGroupId));
            }

            // Use raw SQL for complex aggregations if EF Core allows
            var result = await baseQuery
                .GroupBy(x => new
                {
                    x.LedgerId
                })
                .Select(g => new
                {
                    AccountLedgerId = g.Key.LedgerId,
                    // Simplified aggregations - calculate derived fields in memory
                    OpeningBalance = g.Where(x => x.VoucherName == "OpeningBalance").Sum(x => x.Debit - x.Credit),
                    PriorBalance = g.Where(x => x.VoucherName != "OpeningBalance" && x.Date.Date < fromDate)
                        .Sum(x => x.Debit - x.Credit),
                    Debit = g.Where(x => x.VoucherName != "OpeningBalance" &&
                                         x.Date.Date >= fromDate && x.Date.Date <= toDate)
                        .Sum(x => x.Debit),
                    Credit = g.Where(x => x.VoucherName != "OpeningBalance" &&
                                          x.Date.Date >= fromDate && x.Date.Date <= toDate)
                        .Sum(x => x.Credit)
                })
                .ToListAsync();

            var returnList = new List<AccountLedgerReportList>();

            var accountLedgerIdsWithPostings = result.Select(x => x.AccountLedgerId).ToList();
            var accountNameQuery = accountLedgerRepository.GetAll()
                .AsNoTracking()
                .Include(e => e.AccountGroupFk)
                .Where(e => e.TenantId == AbpSession.TenantId);

            if (applicableGroupIds != null)
                accountNameQuery = accountNameQuery.Where(e => applicableGroupIds.Contains(e.AccountGroupId));

            if (!isZeroBalance)
                accountNameQuery = accountNameQuery.Where(e => accountLedgerIdsWithPostings.Contains(e.Id));

            var accountNameList = await accountNameQuery
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    AccountGroupName = x.AccountGroupFk.Name,
                    x.Pan,
                    x.Address,
                    x.Phone
                })
                .ToListAsync();

            var postingByLedgerId = result.ToDictionary(x => x.AccountLedgerId);

            // Calculate derived fields in memory
            foreach (var getLedger in accountNameList)
            {
                postingByLedgerId.TryGetValue(getLedger.Id, out var item);
                var opening = (item?.OpeningBalance ?? 0) + (item?.PriorBalance ?? 0);
                var debit = item?.Debit ?? 0;
                var credit = item?.Credit ?? 0;
                var closing = opening + debit - credit;
                var model = new AccountLedgerReportList
                {
                    AccountLedgerId = getLedger.Id,
                    LedgerName = getLedger.Name,
                    OpeningDr = opening > 0 ? opening : 0,
                    OpeningCr = opening < 0 ? Math.Abs(opening) : 0,
                    Debit = debit,
                    Credit = credit,
                    BalanceDr = closing > 0 ? Math.Abs(closing) : 0,
                    BalanceCr = closing < 0 ? Math.Abs(closing) : 0,
                    Address = getLedger.Address,
                    Pan = getLedger.Pan,
                    Phone = getLedger.Phone,
                    GroupName = getLedger.AccountGroupName
                };
                returnList.Add(model);
            }

            // Apply filters and sort
            return returnList
                .Where(x => isZeroBalance || x.BalanceDr != x.BalanceCr)
                .OrderBy(x => x.LedgerName)
                .ToList();
        }


        public async Task<FileDto> CreateAccountLedgerReportToExcel(Guid groupId, bool isZeroBalance, string? fromMiti, string? toMiti)
        {
            var data = await GetReport(groupId, isZeroBalance, fromMiti, toMiti);


            // Create result with branch data
            var result = new AccountLedgerReportExcelDto
            {
                PhoneNo = "",
                Pan = "",
                Address = "",
                FromMiti = fromMiti,
                ToMiti = toMiti,
                Details = data
            };

            return excelExporter.ExportToFile(result);
        }

        // Optimized recursive function with memoization
        private async Task<HashSet<Guid>> GetGroupIdsRecursivelyAsync(Guid groupId)
        {
            // Return from cache if available
            if (_groupCache.TryGetValue(groupId, out var cachedResult)) return cachedResult;

            // Create result set including the current ID
            var resultSet = new HashSet<Guid> { groupId };

            // Get all direct children in a single query
            var childGroups = await accountGroupRepository
                .GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.GroupUnder == groupId)
                .Select(x => x.Id)
                .ToListAsync();

            // Process each child recursively
            foreach (var childId in childGroups)
            {
                var childResults = await GetGroupIdsRecursivelyAsync(childId);
                resultSet.UnionWith(childResults);
            }

            // Cache the result
            _groupCache[groupId] = resultSet;

            return resultSet;
        }
        public async Task<List<UniversalDropdownDto>> getAllAccountGroupForTableDropdown()
        {
            return await accountGroupRepository.GetAll().Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).ToListAsync();
        }

        public async Task<CurrentFinancialYearDto> GetFinancialYears()
        {
            var data = FinancialYear;
            return new CurrentFinancialYearDto
            {
                FromDate = data.FromDate,
                ToDate = data.ToDate,
                FromMiti = data.FromMiti,
                ToMiti = data.ToMiti
            };
        }

        //public async Task<byte[]> GetPdfDownload(Guid groupId, bool isShowZero, string? fromMiti, string? toMiti)
        //{
        //    // Execute both data fetching operations in parallel for better performance
        //    var reportDataTask = GetReport(groupId, isShowZero, fromMiti, toMiti);
        //    //   var companyInfoTask = branchService.GetDefultBranch();
        //    var branch = branchRepository.FirstOrDefaultAsync(x => x.IsMain);
        //    // Wait for both tasks to complete
        //    await Task.WhenAll(reportDataTask, branch);

        //    var report = new AccountLedgerPdfDto
        //    {
        //        //   CompanyInfo = await branch,
        //        LedgerDetails = await reportDataTask,
        //        FromDate = fromMiti,
        //        ToDate = toMiti
        //    };

        //    var document = new AccountLedgerReportPdf(report);
        //    return document.GeneratePdf();
        //}
    }
}
