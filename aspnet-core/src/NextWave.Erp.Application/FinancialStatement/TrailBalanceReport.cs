using Abp.Authorization;
using Abp.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.FinancialStatement.Exporting;
using NextWave.Erp.FinancialStatement.Pdf;
using NextWave.Erp.FinancialStatement.Pdf.Dto;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Transaction;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.FinancialStatement;

[AbpAuthorize(AppPermissions.PagesTrialBalanceReport)]
public class TrailBalanceReport(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<Branch, Guid> branchRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<StockPosting, Guid> stockPostingRepository,
    ITrialBalanceExcelExporter excelExporter)
    : ErpAppServiceBase
{
    protected List<UniversalDropdownDto> AccountLedgerList = [];
    protected List<LedgerPostingForReportDto> PostingList = [];
    protected decimal OpeningStockAmt;


    public async Task<List<FinancialStatementDto>> GetReport([CanBeNull] string fromMiti, [CanBeNull] string toMiti)
    {


        var fromDate = FinancialYear.FromDate;
        var toDate = FinancialYear.ToDate;
        if (fromMiti != null && fromMiti.Trim() != string.Empty)
            fromDate = DateConverter.ConvertToEnglish(fromMiti);
        if (toMiti != null && toMiti.Trim() != string.Empty)
            toDate = DateConverter.ConvertToEnglish(toMiti);


        // Clear any previous data
        PostingList.Clear();

        // Get all ledger postings for the current tenant and financial year
        var postingQuery = ledgerPostingRepository.GetAll().AsNoTracking()
            .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId);


        // Execute the query with includes and projections
        PostingList = await postingQuery
            .Include(x => x.AccountLedgerFk)
            .Include(x => x.VoucherTypeFk)
            .AsSplitQuery()
            .Select(x => new LedgerPostingForReportDto
            {

                Debit = x.Debit,
                Credit = x.Credit,
                Date = x.Date,
                LedgerId = x.LedgerId,
                AccountGroupId = x.AccountLedgerFk.AccountGroupId,
                VoucherName = x.VoucherTypeFk.Name
            })
            .OrderBy(x => x.AccountGroupId)
            .Where(e => e.Date <= toDate.Date)
            .ToListAsync();

        // Get all account ledgers for the current tenant
        AccountLedgerList = await accountLedgerRepository.GetAll()
            .Where(e => e.TenantId == AbpSession.TenantId).AsNoTracking()
            .Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).ToListAsync();

        // Get the primary account group
        var primaryAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Primary");




        // Build the stock posting query
        var stockPostingQuery = stockPostingRepository.GetAll().AsNoTracking()
            .Include(x => x.ProductFk)
            .Include(x => x.VoucherTypeFk)
            .Include(x => x.UnitFk)
            .Where(x => x.InWardQty > 0
                        && x.Date <= toDate.Date

                        && x.FinancialYearId == FinancialYearId
                        && x.TenantId == AbpSession.TenantId
                        && !x.IsDeleted)
            .Select(x => new
            {
                VoucherType = x.VoucherTypeFk.Name,
                x.Date,
                x.ProductId,
                ProductName = x.ProductFk.Name,
                UnitName = x.UnitFk.Name,
                x.InWardQty,
                x.Rate,
                x.OutWardQty
            });


        var openingStock = await stockPostingQuery
            .Where(x => x.VoucherType == "OpeningStock")
            .SumAsync(e => e.InWardQty * e.Rate);

        var date = fromDate;
        var beforePeriodStock = await stockPostingQuery
            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < date.Date)
            .SumAsync(e => e.InWardQty * e.Rate);
        var beforePeriodMinStock = await stockPostingQuery
            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < date.Date)
            .SumAsync(e => e.OutWardQty * e.Rate);

        OpeningStockAmt = Math.Round(openingStock + beforePeriodStock - beforePeriodMinStock, 2);

        // Execute the stock posting query



        // Calculate opening difference
        var openingDifference = PostingList.Where(x => x.VoucherName == "OpeningBalance")
            .Sum(x => x.Debit - x.Credit);

        var beforePeriodPosting = PostingList
                .Where(x => x.VoucherName != "OpeningBalance" && x.Date < fromDate.Date)
                .Sum(x => x.Debit - x.Credit);

        var balance = openingDifference + beforePeriodPosting + OpeningStockAmt;



        // Parse fromMiti parameter if provided
        if (!string.IsNullOrEmpty(fromMiti))
            try
            {
                // Assuming you have a utility to convert Miti to DateTime
                fromDate = DateConverter.ConvertToEnglish(fromMiti);
            }
            catch
            {
                // Use default if parsing fails
                fromDate = FinancialYear.FromDate;
            }

        // Generate the recursive report
        var result = await FuncRecursiveOpBalanceGroupId(primaryAccountGroup.Id, fromDate);

        // Add opening difference
        var openingDifferent = new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Differences in the opening balances",
                OpeningDr = balance < 0 ? Math.Abs((int)balance) : 0,
                OpeningCr = balance > 0 ? Math.Abs((int)balance) : 0,
                ClosingDr = balance < 0 ? Math.Abs((int)balance) : 0,
                ClosingCr = balance > 0 ? Math.Abs((int)balance) : 0,
                GroupType = TrailBalanceGroupEnum.None
            }
        };
        result.Add(openingDifferent);

        // Add grand total
        result.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "GrandTotal",
                Debit = result.Sum(x => x.Data.Debit),
                Credit = result.Sum(x => x.Data.Credit),
                ClosingDr = result.Sum(x => x.Data.ClosingDr),
                ClosingCr = result.Sum(x => x.Data.ClosingCr),
                GroupType = TrailBalanceGroupEnum.None
            }
        });

        // Filter out empty rows
        return result.Where(x => x.Data.Debit > 0 || x.Data.Credit > 0 ||
                                 x.Data.ClosingDr != 0 || x.Data.ClosingCr != 0 ||
                                 x.Data.OpeningDr != 0 || x.Data.OpeningCr != 0
        ).ToList();
    }

    /// <summary>
    ///     Export Trial Balance to Excel
    /// </summary>
    public async Task<FileDto> TrialBalanceExportToExcel(string fromMiti, string toMiti)
    {
        var result = new List<FinancialStatementDetail>();
        var branchInfo = await branchRepository.GetAll()
           .Where(x => x.TenantId == AbpSession.TenantId && x.IsMain)
           .Select(x => new GetBranchForViewDto
           {
               Id = x.Id,
               Name = x.Name,
               Address = x.Address,




               Description = x.Description,
               BranchType = x.BranchType,

               CompanyName = x.CompanyName,
               CompanyContact = x.PhoneNo1,

               IsMain = x.IsMain,






           })
           .FirstOrDefaultAsync();

        var dataList = await GetReport(fromMiti, toMiti);
        var level = 0;
        foreach (var rep in dataList) result.AddRange(await GetRecursiveChild(rep, level));
        return excelExporter.ExportToFile(result, branchInfo);
    }

    /// <summary>
    ///     Generate PDF for Trial Balance
    /// </summary>
    public async Task<byte[]> GetPdfDownload(string fromMiti, string toMiti)
    {
        var result = new List<FinancialStatementDetail>();

        var dataList = await GetReport(fromMiti, toMiti);
        var level = 0;
        foreach (var rep in dataList) result.AddRange(await GetRecursiveChild(rep, level));

        // Get branch info - use the specified branch
        var branchInfo = await branchRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId && x.IsMain)
            .Select(x => new GetBranchForViewDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,




                Description = x.Description,
                BranchType = x.BranchType,

                CompanyName = x.CompanyName,
                CompanyContact = x.PhoneNo1,

                IsMain = x.IsMain,






            })
            .FirstOrDefaultAsync();



        var report = new TrialBalancePdfDto
        {
            FromMiti = fromMiti,
            ToMiti = toMiti,
            CompanyInfo = branchInfo,
            TrialBalanceDetails = result
        };
        var document = new TrialBalancePdf(report);
        return document.GeneratePdf();
    }

    /// <summary>
    ///     Recursive method to get child nodes with proper indentation
    /// </summary>
    private async Task<List<FinancialStatementDetail>> GetRecursiveChild(FinancialStatementDto child, int level)
    {
        var space = "  ";
        for (var i = 0; i < level; i++) space += " ->";
        level += 1;
        var result = new List<FinancialStatementDetail>();
        var data = new FinancialStatementDetail
        {
            Id = child.Data.Id,
            Name = space + child.Data.Name,
            OpeningDr = child.Data.OpeningDr,
            OpeningCr = child.Data.OpeningCr,
            Debit = child.Data.Debit,
            Credit = child.Data.Credit,
            ClosingDr = child.Data.ClosingDr,
            ClosingCr = child.Data.ClosingCr,
            GroupType = child.Data.GroupType
        };
        result.Add(data);
        if (child.Children != null)
            foreach (var rep in child.Children)
                result.AddRange(await GetRecursiveChild(rep, level));
        return result;
    }

    /// <summary>
    ///     Recursive method to build the trial balance report
    /// </summary>
    private async Task<List<FinancialStatementDto>> FuncRecursiveOpBalanceGroupId(Guid accountGroupId,
        DateTime startDate)
    {
        var result = new List<FinancialStatementDto>();
        var accountGroups = await accountGroupRepository.GetAll().AsNoTracking()
            .Where(x => x.GroupUnder == accountGroupId && x.Name != "Primary").ToListAsync();
        foreach (var accountGroup in accountGroups)
        {
            var ledgerPosting = PostingList.Where(x => x.AccountGroupId == accountGroup.Id).ToList();
            var openingPosting = ledgerPosting.Where(x => x.VoucherName == "OpeningBalance").ToList();
            var otherPosting = ledgerPosting.Where(x => x.VoucherName != "OpeningBalance" && x.Date < startDate)
                .ToList();
            var noOpeningPosting = ledgerPosting.Where(x => x.VoucherName != "OpeningBalance" && x.Date >= startDate)
                .ToList();

            var openingAmt = openingPosting.Sum(x => x.Debit - x.Credit) + otherPosting.Sum(x => x.Debit - x.Credit);
            var balanceAmt = noOpeningPosting.Sum(x => x.Debit - x.Credit);
            var data = new FinancialStatementDetail
            {
                Id = accountGroup.Id,
                Name = accountGroup.Name,
                OpeningDr = openingAmt > 0 ? openingAmt : 0,
                OpeningCr = openingAmt < 0 ? Math.Abs((int)openingAmt) : 0,
                Debit = balanceAmt > 0 ? balanceAmt : 0,
                Credit = balanceAmt < 0 ? Math.Abs((int)balanceAmt) : 0,
                GroupType = TrailBalanceGroupEnum.AccountGroup
            };

            var closingAmt = data.OpeningDr + data.Debit - (data.OpeningCr + data.Credit);
            data.ClosingDr = closingAmt > 0 ? closingAmt : 0;
            data.ClosingCr = closingAmt < 0 ? Math.Abs(closingAmt) : 0;

            var a = new FinancialStatementDto
            {
                Data = data,
                Id = accountGroup.Id
            };
            a.Children = a.Id != Guid.Empty ? await FuncRecursiveOpBalanceGroupId(accountGroup.Id, startDate) : null;

            var distinctLedgerIds = ledgerPosting.Select(x => x.LedgerId).Distinct().ToList();
            var ledgersList = new List<FinancialStatementDto>();
            foreach (var distinctLedgerId in distinctLedgerIds)
            {
                var m = new FinancialStatementDto();
                var openingDetailAmt = openingPosting.Where(x => x.LedgerId == distinctLedgerId)
                                           .Sum(x => x.Debit - x.Credit) +
                                       otherPosting.Where(x => x.LedgerId == distinctLedgerId)
                                           .Sum(x => x.Debit - x.Credit);

                var detailDebitAmt = noOpeningPosting.Where(x => x.LedgerId == distinctLedgerId)
                    .Sum(x => x.Debit);

                var detailCreditAmt = noOpeningPosting.Where(x => x.LedgerId == distinctLedgerId)
                    .Sum(x => x.Credit);

                var ledger = new FinancialStatementDetail
                {
                    Id = distinctLedgerId,
                    Name = AccountLedgerList.FirstOrDefault(x => x.Id == distinctLedgerId)?.DisplayName,
                    OpeningDr = openingDetailAmt > 0 ? openingDetailAmt : 0,
                    OpeningCr = openingDetailAmt < 0 ? Math.Abs((int)openingDetailAmt) : 0,
                    Debit = detailDebitAmt,
                    Credit = detailCreditAmt,
                    GroupType = TrailBalanceGroupEnum.AccountLedger
                };


                var closingDetailAmt = ledger.OpeningDr + ledger.Debit - (ledger.OpeningCr + ledger.Credit);
                ledger.ClosingDr = closingDetailAmt > 0 ? closingDetailAmt : 0;
                ledger.ClosingCr = closingDetailAmt < 0 ? Math.Abs(closingDetailAmt) : 0;

                m.Id = Guid.Empty;
                m.Data = ledger;
                ledgersList.Add(m);
            }

            if (a.Data.Name == "Current Assets")
            {
                var m = new FinancialStatementDto();
                var ledger = new FinancialStatementDetail
                {
                    Id = Guid.Empty,
                    Name = "Opening Stock",
                    OpeningDr = OpeningStockAmt,
                    OpeningCr = 0,
                    Debit = 0,
                    Credit = 0,
                    GroupType = TrailBalanceGroupEnum.OpeningStock,
                    ClosingDr = OpeningStockAmt,
                    ClosingCr = 0
                };
                data.OpeningDr += ledger.OpeningDr;
                data.OpeningCr += ledger.OpeningCr;
                data.Debit += ledger.Debit;
                data.Credit += ledger.Credit;
                data.ClosingDr += ledger.OpeningDr + ledger.Debit;
                data.ClosingCr += ledger.OpeningCr + ledger.Credit;
                m.Id = Guid.Empty;
                m.Data = ledger;
                m.Children = null;
                ledgersList.Add(m);
            }

            if (a.Children != null)
            {
                if (a.Children.Count > 0)
                {
                    var childOpening = a.Children.Sum(x => x.Data.OpeningDr - x.Data.OpeningCr);
                    var childClosing = a.Children.Sum(x => x.Data.ClosingDr - x.Data.ClosingCr);
                    var childDrAmt = a.Children.Sum(x => x.Data.Debit);
                    var childCrAmt = a.Children.Sum(x => x.Data.Credit);

                    data.OpeningDr += childOpening > 0 ? childOpening : 0;
                    data.OpeningCr += childOpening < 0 ? Math.Abs(childOpening) : 0;
                    data.Debit += childDrAmt;
                    data.Credit += childCrAmt;
                    data.ClosingDr += childClosing > 0 ? childClosing : 0;
                    data.ClosingCr += childClosing < 0 ? Math.Abs(childClosing) : 0;
                }

                a.Children.AddRange(ledgersList);
            }

            result.Add(a);
        }

        return result
            .Where(x => x.Data.Debit > 0 || x.Data.Credit > 0 || x.Data.ClosingDr != 0 || x.Data.ClosingCr != 0)
            .ToList();
    }
}