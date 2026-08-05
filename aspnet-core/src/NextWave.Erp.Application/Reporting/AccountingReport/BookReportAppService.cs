using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Reporting.AccountingReport.Exporting;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport
{
    [AbpAuthorize(AppPermissions.PagesBookReport)]
    public class BookReportAppService(
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IBookReportExcelExporter excelExporter)
        : ErpAppServiceBase
    {
        private const string DayBook = "day-book";
        private const string CashBook = "cash-book";
        private const string BankBook = "bank-book";
        private const string LedgerStatement = "ledger-statement";
        private const string VoucherRegister = "voucher-register";
        private const string Journal = "journal";
        private const string Receipt = "receipt";
        private const string Payment = "payment";
        private const string Contra = "contra";
        private const string OpeningBalance = "opening-balance";
        private const string ReceivableAging = "receivable-aging";
        private const string PayableAging = "payable-aging";
        private const string PartyLedgerStatement = "party-ledger-statement";
        private const string CashBankReconciliation = "cash-bank-reconciliation";
        private const string VoucherAudit = "voucher-audit";
        private const string DailyCollectionPayment = "daily-collection-payment";
        private const string LedgerGroupSummary = "ledger-group-summary";
        private const string OpeningBalanceVoucher = "OpeningBalance";

        public async Task<BookReportLookupDto> GetLookups()
        {
            var ledgers = await accountLedgerRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .OrderBy(x => x.Name)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                })
                .ToListAsync();

            var voucherTypes = await voucherTypeRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new BookReportVoucherTypeDto
                {
                    Id = x.Id,
                    DisplayName = x.Name,
                    TypeOfVoucher = x.TypeOfVoucher
                })
                .ToListAsync();

            return new BookReportLookupDto
            {
                Ledgers = ledgers,
                VoucherTypes = voucherTypes,
                FinancialYear = new CurrentFinancialYearDto
                {
                    FromDate = FinancialYear.FromDate,
                    ToDate = FinancialYear.ToDate,
                    FromMiti = FinancialYear.FromMiti,
                    ToMiti = FinancialYear.ToMiti
                }
            };
        }

        public async Task<BookReportResultDto> GetReport(
            string reportType,
            string fromMiti,
            string toMiti,
            Guid? ledgerId,
            Guid? voucherTypeId)
        {
            var normalizedReportType = NormalizeReportType(reportType);
            var (fromDate, toDate) = ResolveDateRange(fromMiti, toMiti);

            return normalizedReportType switch
            {
                DayBook => await GetDayBookAsync(fromDate, toDate, ledgerId, voucherTypeId),
                CashBook => await GetCashOrBankBookAsync(CashBook, fromDate, toDate, ledgerId),
                BankBook => await GetCashOrBankBookAsync(BankBook, fromDate, toDate, ledgerId),
                LedgerStatement => await GetLedgerStatementAsync(fromDate, toDate, ledgerId, voucherTypeId),
                VoucherRegister => await GetVoucherRegisterAsync(fromDate, toDate, ledgerId, voucherTypeId),
                Journal => await GetVoucherBookAsync(Journal, fromDate, toDate, ledgerId, voucherTypeId),
                Receipt => await GetVoucherBookAsync(Receipt, fromDate, toDate, ledgerId, voucherTypeId),
                Payment => await GetVoucherBookAsync(Payment, fromDate, toDate, ledgerId, voucherTypeId),
                Contra => await GetVoucherBookAsync(Contra, fromDate, toDate, ledgerId, voucherTypeId),
                OpeningBalance => await GetOpeningBalanceAsync(fromDate, toDate, ledgerId, voucherTypeId),
                ReceivableAging => await GetAgingAsync(ReceivableAging, fromDate, toDate, ledgerId, false),
                PayableAging => await GetAgingAsync(PayableAging, fromDate, toDate, ledgerId, true),
                PartyLedgerStatement => await GetPartyLedgerStatementAsync(fromDate, toDate, ledgerId, voucherTypeId),
                CashBankReconciliation => await GetCashBankReconciliationAsync(fromDate, toDate, ledgerId),
                VoucherAudit => await GetVoucherAuditAsync(fromDate, toDate, ledgerId, voucherTypeId),
                DailyCollectionPayment => await GetDailyCollectionPaymentSummaryAsync(fromDate, toDate, ledgerId),
                LedgerGroupSummary => await GetLedgerGroupSummaryAsync(fromDate, toDate),
                _ => throw new UserFriendlyException("Invalid book report type.")
            };
        }

        public async Task<FileDto> CreateBookReportToExcel(
            string reportType,
            string fromMiti,
            string toMiti,
            Guid? ledgerId,
            Guid? voucherTypeId)
        {
            var report = await GetReport(reportType, fromMiti, toMiti, ledgerId, voucherTypeId);
            return excelExporter.ExportToFile(new BookReportExcelDto
            {
                ReportTitle = report.ReportTitle,
                FromMiti = fromMiti,
                ToMiti = toMiti,
                Rows = report.Rows,
                Summary = report.Summary
            });
        }

        private async Task<BookReportResultDto> GetDayBookAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            var postings = await GetPeriodPostings(fromDate, toDate, ledgerId, voucherTypeId).ToListAsync();
            var rows = BuildPostingRows(postings, false, 0);

            return CreateResult(DayBook, "Day Book", rows, 0);
        }

        private async Task<BookReportResultDto> GetLedgerStatementAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            if (!ledgerId.HasValue || ledgerId.Value == Guid.Empty)
            {
                throw new UserFriendlyException("Please select a ledger to view Ledger Statement.");
            }

            var openingBalance = await GetOpeningBalanceForLedgerAsync(ledgerId.Value, fromDate);
            var postings = (await GetPeriodPostings(fromDate, toDate, ledgerId, voucherTypeId)
                    .ToListAsync())
                .Where(x => !IsOpeningBalanceVoucher(x))
                .ToList();

            var rows = BuildPostingRows(postings, true, openingBalance);

            return CreateResult(LedgerStatement, "Ledger Statement", rows, openingBalance, true);
        }

        private async Task<BookReportResultDto> GetCashOrBankBookAsync(string reportType, DateTime fromDate, DateTime toDate, Guid? ledgerId)
        {
            var bookLedgerIds = await GetBookLedgerIdsAsync(reportType);

            if (ledgerId.HasValue && ledgerId.Value != Guid.Empty)
            {
                bookLedgerIds = bookLedgerIds.Contains(ledgerId.Value)
                    ? new HashSet<Guid> { ledgerId.Value }
                    : new HashSet<Guid>();
            }

            if (!bookLedgerIds.Any())
            {
                return CreateResult(reportType, GetReportTitle(reportType), new List<BookReportRowDto>(), 0);
            }

            var openingBalance = await GetOpeningBalanceForLedgersAsync(bookLedgerIds, fromDate);
            var postings = (await GetPeriodPostings(fromDate, toDate, null, null)
                    .Where(x => bookLedgerIds.Contains(x.LedgerId))
                    .ToListAsync())
                .Where(x => !IsOpeningBalanceVoucher(x))
                .ToList();

            var rows = BuildPostingRows(postings, true, openingBalance);

            return CreateResult(reportType, GetReportTitle(reportType), rows, openingBalance);
        }

        private async Task<BookReportResultDto> GetVoucherRegisterAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            var postings = await GetPeriodPostings(fromDate, toDate, ledgerId, voucherTypeId).ToListAsync();

            var rows = postings
                .GroupBy(x => new
                {
                    x.Date.Date,
                    x.DateMiti,
                    x.VoucherTypeId,
                    VoucherType = x.VoucherTypeFk == null ? string.Empty : x.VoucherTypeFk.Name,
                    x.VoucherNo
                })
                .OrderBy(x => x.Key.Date)
                .ThenBy(x => x.Key.VoucherType)
                .ThenBy(x => x.Key.VoucherNo)
                .Select((g, index) =>
                {
                    var debit = g.Sum(x => x.Debit);
                    var credit = g.Sum(x => x.Credit);
                    return new BookReportRowDto
                    {
                        Sn = index + 1,
                        Date = g.Key.Date,
                        DateMiti = g.Key.DateMiti,
                        VoucherType = g.Key.VoucherType,
                        VoucherNo = g.Key.VoucherNo,
                        LedgerName = "Lines: " + g.Count(),
                        LineCount = g.Count(),
                        Debit = debit,
                        Credit = credit,
                        InAmount = debit,
                        OutAmount = credit,
                        Difference = debit - credit,
                        ClosingBalance = debit - credit
                    };
                })
                .ToList();

            return CreateResult(VoucherRegister, "Voucher Register", rows, 0);
        }

        private async Task<BookReportResultDto> GetVoucherBookAsync(string reportType, DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            var postings = (await GetPeriodPostings(fromDate, toDate, ledgerId, voucherTypeId)
                    .ToListAsync())
                .Where(x => !IsOpeningBalanceVoucher(x))
                .ToList();

            postings = postings
                .Where(x => MatchesVoucherBook(x.VoucherTypeFk, reportType))
                .ToList();

            var rows = BuildPostingRows(postings, false, 0);
            return CreateResult(reportType, GetReportTitle(reportType), rows, 0);
        }

        private async Task<BookReportResultDto> GetOpeningBalanceAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            if (voucherTypeId.HasValue && !await IsOpeningVoucherTypeAsync(voucherTypeId.Value))
            {
                return CreateResult(OpeningBalance, "Opening Balance", new List<BookReportRowDto>(), 0);
            }

            var openingPostings = (await GetPeriodPostings(fromDate, toDate, ledgerId, null)
                    .ToListAsync())
                .Where(IsOpeningBalanceVoucher)
                .ToList();

            var rows = BuildPostingRows(openingPostings, false, 0);
            foreach (var row in rows)
            {
                row.Remarks = "Opening posting";
            }

            var ledgerIdsWithPostings = openingPostings.Select(x => x.LedgerId).ToHashSet();
            var ledgerQuery = accountLedgerRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.AccountGroupFk)
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete && x.OpeningBalance != 0);

            if (ledgerId.HasValue && ledgerId.Value != Guid.Empty)
            {
                ledgerQuery = ledgerQuery.Where(x => x.Id == ledgerId.Value);
            }

            var ledgerOpenings = await ledgerQuery
                .Where(x => !ledgerIdsWithPostings.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToListAsync();

            foreach (var ledger in ledgerOpenings)
            {
                rows.Add(new BookReportRowDto
                {
                    Sn = rows.Count + 1,
                    Date = ledger.OpeningDate ?? FinancialYear.FromDate,
                    DateMiti = string.Empty,
                    VoucherType = OpeningBalanceVoucher,
                    VoucherNo = OpeningBalanceVoucher,
                    LedgerId = ledger.Id,
                    LedgerName = ledger.Name,
                    GroupName = ledger.AccountGroupFk == null ? string.Empty : ledger.AccountGroupFk.Name,
                    LineCount = 1,
                    Debit = ledger.CrOrDr == DrOrCr.Dr ? ledger.OpeningBalance : 0,
                    Credit = ledger.CrOrDr == DrOrCr.Cr ? ledger.OpeningBalance : 0,
                    InAmount = ledger.CrOrDr == DrOrCr.Dr ? ledger.OpeningBalance : 0,
                    OutAmount = ledger.CrOrDr == DrOrCr.Cr ? ledger.OpeningBalance : 0,
                    Difference = ledger.CrOrDr == DrOrCr.Dr ? ledger.OpeningBalance : -ledger.OpeningBalance,
                    ClosingBalance = ledger.CrOrDr == DrOrCr.Dr ? ledger.OpeningBalance : -ledger.OpeningBalance,
                    Remarks = "Ledger opening value"
                });
            }

            rows = rows
                .OrderBy(x => x.LedgerName)
                .ThenBy(x => x.Date)
                .Select((x, index) =>
                {
                    x.Sn = index + 1;
                    return x;
                })
                .ToList();

            return CreateResult(OpeningBalance, "Opening Balance", rows, 0);
        }

        private async Task<BookReportResultDto> GetAgingAsync(string reportType, DateTime fromDate, DateTime toDate, Guid? ledgerId, bool payable)
        {
            var partyLedgerIds = await GetPartyLedgerIdsAsync(payable);

            if (ledgerId.HasValue && ledgerId.Value != Guid.Empty)
            {
                partyLedgerIds = partyLedgerIds.Contains(ledgerId.Value)
                    ? new HashSet<Guid> { ledgerId.Value }
                    : new HashSet<Guid>();
            }

            if (!partyLedgerIds.Any())
            {
                return CreateResult(reportType, GetReportTitle(reportType), new List<BookReportRowDto>(), 0);
            }

            var postings = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    partyLedgerIds.Contains(x.LedgerId) &&
                    x.Date.Date <= toDate.Date)
                .ToListAsync();

            var rows = postings
                .GroupBy(x => x.LedgerId)
                .Select(g =>
                {
                    var balance = g.Sum(x => x.Debit - x.Credit);
                    var amount = payable ? -balance : balance;
                    if (amount <= 0)
                    {
                        return null;
                    }

                    var latestPosting = g
                        .Where(x => !IsOpeningBalanceVoucher(x))
                        .OrderByDescending(x => x.Date)
                        .FirstOrDefault() ?? g.OrderByDescending(x => x.Date).First();
                    var ageDays = Math.Max(0, (toDate.Date - latestPosting.Date.Date).Days);

                    return new BookReportRowDto
                    {
                        Date = latestPosting.Date.Date,
                        DateMiti = latestPosting.DateMiti,
                        LedgerId = latestPosting.LedgerId,
                        LedgerName = latestPosting.AccountLedgerFk == null ? string.Empty : latestPosting.AccountLedgerFk.Name,
                        GroupName = latestPosting.AccountLedgerFk?.AccountGroupFk == null ? string.Empty : latestPosting.AccountLedgerFk.AccountGroupFk.Name,
                        Debit = payable ? 0 : amount,
                        Credit = payable ? amount : 0,
                        Difference = amount,
                        ClosingBalance = amount,
                        AgeDays = ageDays,
                        CurrentAmount = ageDays == 0 ? amount : 0,
                        Age1To30 = ageDays > 0 && ageDays <= 30 ? amount : 0,
                        Age31To60 = ageDays > 30 && ageDays <= 60 ? amount : 0,
                        Age61To90 = ageDays > 60 && ageDays <= 90 ? amount : 0,
                        AgeAbove90 = ageDays > 90 ? amount : 0,
                        Status = payable ? "Payable" : "Receivable",
                        Remarks = "Last transaction age"
                    };
                })
                .Where(x => x != null)
                .OrderByDescending(x => x.AgeDays)
                .ThenBy(x => x.LedgerName)
                .Select((x, index) =>
                {
                    x.Sn = index + 1;
                    return x;
                })
                .ToList();

            return CreateResult(reportType, GetReportTitle(reportType), rows, 0);
        }

        private async Task<BookReportResultDto> GetPartyLedgerStatementAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            if (!ledgerId.HasValue || ledgerId.Value == Guid.Empty)
            {
                throw new UserFriendlyException("Please select a party ledger to view Party Ledger Statement.");
            }

            var result = await GetLedgerStatementAsync(fromDate, toDate, ledgerId, voucherTypeId);
            result.ReportType = PartyLedgerStatement;
            result.ReportTitle = GetReportTitle(PartyLedgerStatement);
            return result;
        }

        private async Task<BookReportResultDto> GetCashBankReconciliationAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId)
        {
            var cashLedgerIds = await GetBookLedgerIdsAsync(CashBook);
            var bankLedgerIds = await GetBookLedgerIdsAsync(BankBook);
            cashLedgerIds.UnionWith(bankLedgerIds);

            if (ledgerId.HasValue && ledgerId.Value != Guid.Empty)
            {
                cashLedgerIds = cashLedgerIds.Contains(ledgerId.Value)
                    ? new HashSet<Guid> { ledgerId.Value }
                    : new HashSet<Guid>();
            }

            if (!cashLedgerIds.Any())
            {
                return CreateResult(CashBankReconciliation, GetReportTitle(CashBankReconciliation), new List<BookReportRowDto>(), 0);
            }

            var postings = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    cashLedgerIds.Contains(x.LedgerId) &&
                    x.Date.Date <= toDate.Date)
                .ToListAsync();

            var rows = postings
                .GroupBy(x => x.LedgerId)
                .OrderBy(x => x.First().AccountLedgerFk == null ? string.Empty : x.First().AccountLedgerFk.Name)
                .Select((g, index) =>
                {
                    var opening = g.Where(x => IsOpeningBalanceVoucher(x) || x.Date.Date < fromDate.Date).Sum(x => x.Debit - x.Credit);
                    var period = g.Where(x => !IsOpeningBalanceVoucher(x) && x.Date.Date >= fromDate.Date && x.Date.Date <= toDate.Date).ToList();
                    var debit = period.Sum(x => x.Debit);
                    var credit = period.Sum(x => x.Credit);
                    var latest = g.OrderByDescending(x => x.Date).First();

                    return new BookReportRowDto
                    {
                        Sn = index + 1,
                        Date = latest.Date.Date,
                        DateMiti = latest.DateMiti,
                        LedgerId = latest.LedgerId,
                        LedgerName = latest.AccountLedgerFk == null ? string.Empty : latest.AccountLedgerFk.Name,
                        GroupName = latest.AccountLedgerFk?.AccountGroupFk == null ? string.Empty : latest.AccountLedgerFk.AccountGroupFk.Name,
                        OpeningBalance = opening,
                        Debit = debit,
                        Credit = credit,
                        InAmount = debit,
                        OutAmount = credit,
                        Difference = debit - credit,
                        ClosingBalance = opening + debit - credit,
                        Status = "Book Balance",
                        Remarks = "Bank statement matching is not stored in ledger postings"
                    };
                })
                .ToList();

            return CreateResult(CashBankReconciliation, GetReportTitle(CashBankReconciliation), rows, rows.Sum(x => x.OpeningBalance));
        }

        private async Task<BookReportResultDto> GetVoucherAuditAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            var postings = await GetPeriodPostings(fromDate, toDate, ledgerId, voucherTypeId).ToListAsync();

            var rows = postings
                .GroupBy(x => new
                {
                    x.Date.Date,
                    x.DateMiti,
                    x.VoucherTypeId,
                    VoucherType = x.VoucherTypeFk == null ? string.Empty : x.VoucherTypeFk.Name,
                    x.VoucherNo
                })
                .OrderBy(x => x.Key.Date)
                .ThenBy(x => x.Key.VoucherType)
                .ThenBy(x => x.Key.VoucherNo)
                .Select((g, index) =>
                {
                    var debit = g.Sum(x => x.Debit);
                    var credit = g.Sum(x => x.Credit);
                    var difference = debit - credit;
                    return new BookReportRowDto
                    {
                        Sn = index + 1,
                        Date = g.Key.Date,
                        DateMiti = g.Key.DateMiti,
                        VoucherType = g.Key.VoucherType,
                        VoucherNo = g.Key.VoucherNo,
                        LedgerName = string.Join(", ", g.Select(x => x.AccountLedgerFk == null ? string.Empty : x.AccountLedgerFk.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(3)),
                        LineCount = g.Count(),
                        Debit = debit,
                        Credit = credit,
                        InAmount = debit,
                        OutAmount = credit,
                        Difference = difference,
                        ClosingBalance = difference,
                        Status = difference == 0 ? "Balanced" : "Out of balance",
                        Remarks = difference == 0 ? string.Empty : "Debit and credit totals do not match"
                    };
                })
                .ToList();

            return CreateResult(VoucherAudit, GetReportTitle(VoucherAudit), rows, 0);
        }

        private async Task<BookReportResultDto> GetDailyCollectionPaymentSummaryAsync(DateTime fromDate, DateTime toDate, Guid? ledgerId)
        {
            var postings = (await GetPeriodPostings(fromDate, toDate, ledgerId, null).ToListAsync())
                .Where(x => MatchesVoucherBook(x.VoucherTypeFk, Receipt) || MatchesVoucherBook(x.VoucherTypeFk, Payment))
                .ToList();

            var rows = postings
                .GroupBy(x => new
                {
                    x.Date.Date,
                    x.DateMiti,
                    Type = MatchesVoucherBook(x.VoucherTypeFk, Receipt) ? "Collection" : "Payment"
                })
                .OrderBy(x => x.Key.Date)
                .ThenBy(x => x.Key.Type)
                .Select((g, index) =>
                {
                    var debit = g.Sum(x => x.Debit);
                    var credit = g.Sum(x => x.Credit);
                    var collection = g.Key.Type == "Collection" ? debit : 0;
                    var payment = g.Key.Type == "Payment" ? credit : 0;

                    return new BookReportRowDto
                    {
                        Sn = index + 1,
                        Date = g.Key.Date,
                        DateMiti = g.Key.DateMiti,
                        VoucherType = g.Key.Type,
                        VoucherNo = g.Select(x => x.VoucherNo).Distinct().Count().ToString(),
                        LedgerName = g.Key.Type,
                        LineCount = g.Count(),
                        Debit = debit,
                        Credit = credit,
                        InAmount = collection,
                        OutAmount = payment,
                        Difference = collection - payment,
                        ClosingBalance = collection - payment,
                        Status = g.Key.Type,
                        Remarks = "Voucher count: " + g.Select(x => x.VoucherNo).Distinct().Count()
                    };
                })
                .ToList();

            return CreateResult(DailyCollectionPayment, GetReportTitle(DailyCollectionPayment), rows, 0);
        }

        private async Task<BookReportResultDto> GetLedgerGroupSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            var postings = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    x.Date.Date <= toDate.Date)
                .ToListAsync();

            var rows = postings
                .GroupBy(x => new
                {
                    AccountGroupId = x.AccountLedgerFk == null ? Guid.Empty : x.AccountLedgerFk.AccountGroupId,
                    GroupName = x.AccountLedgerFk?.AccountGroupFk == null ? "No Group" : x.AccountLedgerFk.AccountGroupFk.Name
                })
                .OrderBy(x => x.Key.GroupName)
                .Select((g, index) =>
                {
                    var opening = g.Where(x => IsOpeningBalanceVoucher(x) || x.Date.Date < fromDate.Date).Sum(x => x.Debit - x.Credit);
                    var period = g.Where(x => !IsOpeningBalanceVoucher(x) && x.Date.Date >= fromDate.Date && x.Date.Date <= toDate.Date).ToList();
                    var debit = period.Sum(x => x.Debit);
                    var credit = period.Sum(x => x.Credit);

                    return new BookReportRowDto
                    {
                        Sn = index + 1,
                        Date = toDate.Date,
                        GroupName = g.Key.GroupName,
                        LedgerName = g.Key.GroupName,
                        LineCount = g.Select(x => x.LedgerId).Distinct().Count(),
                        OpeningBalance = opening,
                        Debit = debit,
                        Credit = credit,
                        InAmount = debit,
                        OutAmount = credit,
                        Difference = debit - credit,
                        ClosingBalance = opening + debit - credit,
                        Status = "Group Summary",
                        Remarks = "Ledger count: " + g.Select(x => x.LedgerId).Distinct().Count()
                    };
                })
                .ToList();

            return CreateResult(LedgerGroupSummary, GetReportTitle(LedgerGroupSummary), rows, rows.Sum(x => x.OpeningBalance));
        }

        private IQueryable<LedgerPosting> GetPeriodPostings(DateTime fromDate, DateTime toDate, Guid? ledgerId, Guid? voucherTypeId)
        {
            var endDate = toDate.Date.AddDays(1);
            var query = ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    x.Date >= fromDate.Date &&
                    x.Date < endDate);

            if (ledgerId.HasValue && ledgerId.Value != Guid.Empty)
            {
                query = query.Where(x => x.LedgerId == ledgerId.Value);
            }

            if (voucherTypeId.HasValue && voucherTypeId.Value != Guid.Empty)
            {
                query = query.Where(x => x.VoucherTypeId == voucherTypeId.Value);
            }

            return query.OrderBy(x => x.Date).ThenBy(x => x.VoucherNo).ThenBy(x => x.PostingNumber);
        }

        private async Task<decimal> GetOpeningBalanceForLedgerAsync(Guid ledgerId, DateTime fromDate)
        {
            var postings = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    x.LedgerId == ledgerId)
                .ToListAsync();

            return postings
                .Where(x => IsOpeningBalanceVoucher(x) || x.Date.Date < fromDate.Date)
                .Sum(x => x.Debit - x.Credit);
        }

        private async Task<decimal> GetOpeningBalanceForLedgersAsync(IReadOnlyCollection<Guid> ledgerIds, DateTime fromDate)
        {
            var postings = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.VoucherTypeFk)
                .Where(x =>
                    x.TenantId == AbpSession.TenantId &&
                    x.FinancialYearId == FinancialYearId &&
                    ledgerIds.Contains(x.LedgerId))
                .ToListAsync();

            return postings
                .Where(x => IsOpeningBalanceVoucher(x) || x.Date.Date < fromDate.Date)
                .Sum(x => x.Debit - x.Credit);
        }

        private async Task<HashSet<Guid>> GetBookLedgerIdsAsync(string reportType)
        {
            var isBankBook = reportType == BankBook;
            var groups = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.GroupUnder
                })
                .ToListAsync();

            var matchingGroupIds = groups
                .Where(x => IsBookName(x.Name, isBankBook))
                .Select(x => x.Id)
                .ToHashSet();

            var added = true;
            while (added)
            {
                added = false;
                foreach (var childGroup in groups.Where(x => x.GroupUnder.HasValue && matchingGroupIds.Contains(x.GroupUnder.Value)))
                {
                    if (matchingGroupIds.Add(childGroup.Id))
                    {
                        added = true;
                    }
                }
            }

            var ledgers = await accountLedgerRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.AccountGroupId
                })
                .ToListAsync();

            return ledgers
                .Where(x => matchingGroupIds.Contains(x.AccountGroupId) || IsBookName(x.Name, isBankBook))
                .Select(x => x.Id)
                .ToHashSet();
        }

        private async Task<HashSet<Guid>> GetPartyLedgerIdsAsync(bool payable)
        {
            var groups = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.GroupUnder
                })
                .ToListAsync();

            var matchingGroupIds = groups
                .Where(x => IsPartyName(x.Name, payable))
                .Select(x => x.Id)
                .ToHashSet();

            var added = true;
            while (added)
            {
                added = false;
                foreach (var childGroup in groups.Where(x => x.GroupUnder.HasValue && matchingGroupIds.Contains(x.GroupUnder.Value)))
                {
                    if (matchingGroupIds.Add(childGroup.Id))
                    {
                        added = true;
                    }
                }
            }

            var ledgers = await accountLedgerRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.AccountGroupId
                })
                .ToListAsync();

            return ledgers
                .Where(x => matchingGroupIds.Contains(x.AccountGroupId) || IsPartyName(x.Name, payable))
                .Select(x => x.Id)
                .ToHashSet();
        }

        private static List<BookReportRowDto> BuildPostingRows(IEnumerable<LedgerPosting> postings, bool withRunningBalance, decimal openingBalance)
        {
            var rows = new List<BookReportRowDto>();
            var runningBalance = openingBalance;

            foreach (var posting in postings.OrderBy(x => x.Date).ThenBy(x => x.VoucherNo).ThenBy(x => x.PostingNumber))
            {
                var previousBalance = runningBalance;
                runningBalance += posting.Debit - posting.Credit;

                rows.Add(new BookReportRowDto
                {
                    Sn = rows.Count + 1,
                    Date = posting.Date.Date,
                    DateMiti = posting.DateMiti,
                    VoucherType = posting.VoucherTypeFk == null ? string.Empty : posting.VoucherTypeFk.Name,
                    VoucherNo = posting.VoucherNo,
                    LedgerId = posting.LedgerId,
                    LedgerName = posting.AccountLedgerFk == null ? string.Empty : posting.AccountLedgerFk.Name,
                    GroupName = posting.AccountLedgerFk?.AccountGroupFk == null ? string.Empty : posting.AccountLedgerFk.AccountGroupFk.Name,
                    LineCount = 1,
                    OpeningBalance = withRunningBalance ? previousBalance : 0,
                    Debit = posting.Debit,
                    Credit = posting.Credit,
                    InAmount = posting.Debit,
                    OutAmount = posting.Credit,
                    Difference = posting.Debit - posting.Credit,
                    ClosingBalance = withRunningBalance ? runningBalance : posting.Debit - posting.Credit,
                    Remarks = posting.InvoiceNo
                });
            }

            return rows;
        }

        private static BookReportResultDto CreateResult(string reportType, string title, List<BookReportRowDto> rows, decimal openingBalance, bool requiresLedger = false)
        {
            var totalDebit = rows.Sum(x => x.Debit);
            var totalCredit = rows.Sum(x => x.Credit);
            var closingBalance = openingBalance + totalDebit - totalCredit;

            return new BookReportResultDto
            {
                ReportType = reportType,
                ReportTitle = title,
                RequiresLedger = requiresLedger,
                Rows = rows,
                Summary = new BookReportSummaryDto
                {
                    TotalRows = rows.Count,
                    OpeningBalance = openingBalance,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    TotalIn = rows.Sum(x => x.InAmount),
                    TotalOut = rows.Sum(x => x.OutAmount),
                    Difference = totalDebit - totalCredit,
                    ClosingBalance = closingBalance
                }
            };
        }

        private async Task<bool> IsOpeningVoucherTypeAsync(Guid voucherTypeId)
        {
            var voucherType = await voucherTypeRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == voucherTypeId && x.TenantId == AbpSession.TenantId);

            return IsOpeningBalanceVoucher(voucherType);
        }

        private static bool MatchesVoucherBook(VoucherType voucherType, string reportType)
        {
            var value = Normalize((voucherType?.TypeOfVoucher ?? string.Empty) + " " + (voucherType?.Name ?? string.Empty));
            return reportType switch
            {
                Journal => value.Contains("journal"),
                Receipt => value.Contains("receipt"),
                Payment => value.Contains("payment"),
                Contra => value.Contains("contra"),
                _ => false
            };
        }

        private static bool IsOpeningBalanceVoucher(LedgerPosting posting)
        {
            return IsOpeningBalanceVoucher(posting.VoucherTypeFk);
        }

        private static bool IsOpeningBalanceVoucher(VoucherType voucherType)
        {
            var value = Normalize((voucherType?.TypeOfVoucher ?? string.Empty) + " " + (voucherType?.Name ?? string.Empty));
            return value.Contains(Normalize(OpeningBalanceVoucher));
        }

        private static bool IsBookName(string value, bool isBankBook)
        {
            var normalized = Normalize(value);
            return isBankBook
                ? normalized.Contains("bank")
                : normalized.Contains("cash") && !normalized.Contains("bank");
        }

        private static bool IsPartyName(string value, bool payable)
        {
            var normalized = Normalize(value);
            return payable
                ? normalized.Contains("payable") ||
                  normalized.Contains("creditor") ||
                  normalized.Contains("supplier") ||
                  normalized.Contains("sundrycreditor") ||
                  normalized.Contains("accountpayable")
                : normalized.Contains("receivable") ||
                  normalized.Contains("debtor") ||
                  normalized.Contains("customer") ||
                  normalized.Contains("sundrydebtor") ||
                  normalized.Contains("accountreceivable");
        }

        private static string NormalizeReportType(string reportType)
        {
            return string.IsNullOrWhiteSpace(reportType) ? DayBook : reportType.Trim().ToLowerInvariant();
        }

        private string GetReportTitle(string reportType)
        {
            return reportType switch
            {
                CashBook => "Cash Book",
                BankBook => "Bank Book",
                LedgerStatement => "Ledger Statement",
                VoucherRegister => "Voucher Register",
                Journal => "Journal Book",
                Receipt => "Receipt Book",
                Payment => "Payment Book",
                Contra => "Contra Book",
                OpeningBalance => "Opening Balance",
                ReceivableAging => "Receivable Aging",
                PayableAging => "Payable Aging",
                PartyLedgerStatement => "Party Ledger Statement",
                CashBankReconciliation => "Cash/Bank Reconciliation",
                VoucherAudit => "Voucher Audit Report",
                DailyCollectionPayment => "Daily Collection & Payment Summary",
                LedgerGroupSummary => "Ledger Group Summary",
                _ => "Day Book"
            };
        }

        private (DateTime FromDate, DateTime ToDate) ResolveDateRange(string fromMiti, string toMiti)
        {
            var fromDate = FinancialYear.FromDate.Date;
            var toDate = FinancialYear.ToDate.Date;

            if (!string.IsNullOrWhiteSpace(fromMiti))
            {
                fromDate = DateConverter.ConvertToEnglish(fromMiti).Date;
            }

            if (!string.IsNullOrWhiteSpace(toMiti))
            {
                toDate = DateConverter.ConvertToEnglish(toMiti).Date;
            }

            return (fromDate, toDate);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .ToLowerInvariant();
        }
    }
}
