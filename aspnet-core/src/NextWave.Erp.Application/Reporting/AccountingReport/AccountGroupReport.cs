using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
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
    public class AccountGroupReport(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IAccountGroupExcelExporter excelExporter)
    : ErpAppServiceBase
    {
        public async Task<List<AccountGroupReportList>> GetReport(string? fromMiti, string? toMiti)
        {
            var accountGroup = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.Name
                }).ToListAsync();
            var ledgerPosting = ledgerPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId);

            if (fromMiti != null && fromMiti.Trim() != string.Empty)
                ledgerPosting = ledgerPosting.Where(x => x.Date.Date >= DateConverter.ConvertToEnglish(fromMiti).Date);
            if (toMiti != null && toMiti.Trim() != string.Empty)
                ledgerPosting = ledgerPosting.Where(x => x.Date.Date <= DateConverter.ConvertToEnglish(toMiti).Date);

            var ledgerQuery = await ledgerPosting.Include(x => x.AccountLedgerFk)
                .Include(x => x.VoucherTypeFk).AsSplitQuery()
                .Select(x => new
                {
                    VoucherType = x.VoucherTypeFk.Name,
                    x.Debit,
                    x.Credit,
                    x.AccountLedgerFk.AccountGroupId
                }).ToListAsync();

            var list = new List<AccountGroupReportList>();

            foreach (var ledgers in accountGroup)
            {
                var accounts = new AccountGroupReportList();
                var openingBalance = ledgerQuery
                    .Where(x => x.AccountGroupId == ledgers.Id && x.VoucherType == "OpeningBalance").ToList();
                var opening = openingBalance.Select(x => x.Debit).Sum() - openingBalance.Select(x => x.Credit).Sum();
                var opBalance = opening > 0 ? opening + " Dr" : Math.Abs(opening) + " Cr";
                var balance = ledgerQuery.Where(x => x.AccountGroupId == ledgers.Id && x.VoucherType != "OpeningBalance");
                {
                    var enumerable = balance.ToList();
                    decimal? debit = enumerable.Select(x => x.Debit).Sum();
                    decimal? credit = enumerable.Select(x => x.Credit).Sum();
                    var closing = opening + debit - credit;
                    var closingBalance = closing > 0 ? closing + " Dr" : Math.Abs((decimal)closing) + " Cr";
                    accounts.AccountGroupId = ledgers.Id;
                    accounts.AccountGroupName = ledgers.Name;
                    accounts.Opening = opBalance;
                    accounts.Debit = debit;
                    accounts.Credit = credit;
                    accounts.Balance = closingBalance;
                }
                list.Add(accounts);
            }

            var ordered = list.OrderBy(x => x.AccountGroupName).ToList();
            var sn = 1;
            foreach (var account in ordered)
                account.Sn = sn++;
            return ordered;
        }

        public async Task<FileDto> CreateAccountGroupReportToExcel(string? fromMiti, string? toMiti)
        {
            var data = await GetReport(fromMiti, toMiti);
            return excelExporter.ExportToFile(data);
        }
    }
}
