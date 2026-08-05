using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.FinancialStatement.Exporting;
using NextWave.Erp.FinancialStatement.Pdf;
using NextWave.Erp.Transaction;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.Enums;
using Suktas.Erp.FinancialStatement;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.FinancialStatement;

[AbpAuthorize(AppPermissions.PagesCashFlowReport)]
public class CashFlowReportAppService(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<Branch, Guid> branchRepository,
	IRepository<AccountLedger, Guid> accountLedgerRepository,
    Exporting.ICashFlowExcelExporter excelExporter)
    : ErpAppServiceBase
{
    protected List<AccountLedgerDto> AccountLedgerList = [];
    protected List<LedgerPostingForReportDto> CurrentPeriodPostings = [];
    protected List<LedgerPostingForReportDto> PreviousPeriodPostings = [];

    public async Task<List<CashFlowStatementDto>> GetCashFlowReport(string fromDate, string toDate,
        string comparisonPeriod)
    {
        var result = new List<CashFlowStatementDto>();

        var startDate = FinancialYear.FromDate;
        var endDate = FinancialYear.ToDate;

        if (!string.IsNullOrEmpty(fromDate)) startDate = DateConverter.ConvertToEnglish(fromDate);
        if (!string.IsNullOrEmpty(toDate)) endDate = DateConverter.ConvertToEnglish(toDate);

        // normalize in case user swaps dates
        if (startDate > endDate)
            (startDate, endDate) = (endDate, startDate);

        // Load account ledgers
        await LoadAccountLedgers();

        // Load postings for current period
        await LoadCurrentPeriodPostings(startDate, endDate);

        // Calculate comparison date range
        var (comparisonStartDate, comparisonEndDate) = GetComparisonDateRange(startDate, endDate, comparisonPeriod);
        if (comparisonStartDate > comparisonEndDate)
            (comparisonStartDate, comparisonEndDate) = (comparisonEndDate, comparisonStartDate);

        // Load comparison postings
        await LoadComparisonPeriodPostings(comparisonStartDate, comparisonEndDate);

        // Get cash and cash equivalents balances
        var beginningCashBalance = await GetBeginningCashBalance(startDate, comparisonStartDate);
        result.Add(beginningCashBalance);

        // Get operating activities cash flow
        var operatingActivities = await GetOperatingActivitiesCashFlow();
        result.Add(operatingActivities);

        // Get investing activities cash flow
        var investingActivities = await GetInvestingActivitiesCashFlow();
        result.Add(investingActivities);

        // Get financing activities cash flow
        var financingActivities = await GetFinancingActivitiesCashFlow();
        result.Add(financingActivities);

        // Calculate net increase/decrease in cash
        var netChange = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Net Increase/Decrease in Cash",
                CurrentAmount = operatingActivities.Data.CurrentAmount + investingActivities.Data.CurrentAmount +
                                financingActivities.Data.CurrentAmount,
                PreviousAmount = operatingActivities.Data.PreviousAmount + investingActivities.Data.PreviousAmount +
                                 financingActivities.Data.PreviousAmount,
                GroupType = CashFlowGroupEnum.NetChange
            },
            Children = null
        };
        result.Add(netChange);

        // Calculate ending cash balance
        var endingCashBalance = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Cash and Cash Equivalents, End of Period",
                CurrentAmount = beginningCashBalance.Data.CurrentAmount + netChange.Data.CurrentAmount,
                PreviousAmount = beginningCashBalance.Data.PreviousAmount + netChange.Data.PreviousAmount,
                GroupType = CashFlowGroupEnum.EndingBalance
            },
            Children = null
        };
        result.Add(endingCashBalance);

        return result;
    }

    //public async Task<FileDto> CashFlowExportToExcel(string fromMiti = null, string toMiti = null, string comparisonPeriod = null)
    //{
    //    var data = await GetCashFlowReport(fromMiti, toMiti, comparisonPeriod);
    //    var flatRows = FlattenHierarchy(data);
    //    return excelExporter.ExportToFile(flatRows);
    //}

    public async Task<byte[]> GetPdfDownload(string fromMiti = null, string toMiti = null, string comparisonPeriod = null)
    {
        var data = await GetCashFlowReport(fromMiti, toMiti, comparisonPeriod);
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
		var report = new CashFlowPdfDto
        {
            CompanyInfo = branchInfo,

			CashFlowDetails = FlattenHierarchy(data),
            ReportParameters = new ReportParametersDto
            {
                StartDate = fromMiti,
                EndDate = toMiti,
                ComparisonPeriod = comparisonPeriod
            }
        };

        var document = new CashFlowReportPdf(report);
        return document.GeneratePdf();
    }

    private async Task LoadAccountLedgers()
    {
        AccountLedgerList = await accountLedgerRepository.GetAll()
            .Where(e => e.TenantId == AbpSession.TenantId)
            .AsNoTracking()
            .Select(x => new AccountLedgerDto
			{
                Id = x.Id,
                DisplayName = x.Name,
                AccountGroupId = x.AccountGroupId
            })
            .ToListAsync();
    }

    private async Task LoadCurrentPeriodPostings(DateTime startDate, DateTime endDate)
    {
        // Build posting query
        var query = ledgerPostingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Date >= startDate && x.Date <= endDate && x.FinancialYearId == FinancialYearId)
            .Include(x => x.AccountLedgerFk)
            .Include(x => x.VoucherTypeFk)
            .AsSplitQuery();

        // Map to DTOs
        CurrentPeriodPostings = await query
            .Select(x => new LedgerPostingForReportDto
            {
                Debit = x.Debit,
                Credit = x.Credit,
                LedgerId = x.LedgerId,
                AccountGroupId = x.AccountLedgerFk.AccountGroupId,
                VoucherName = x.VoucherTypeFk.Name,
                Date = x.Date
            })
            .OrderBy(x => x.AccountGroupId)
            .ToListAsync();
    }

    private async Task LoadComparisonPeriodPostings(DateTime startDate, DateTime endDate)
    {
        // Build posting query for comparison period
        var query = ledgerPostingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Date >= startDate && x.Date <= endDate && x.FinancialYearId == FinancialYearId)
            .Include(x => x.AccountLedgerFk)
            .Include(x => x.VoucherTypeFk)
            .AsSplitQuery();

        // Map to DTOs
        PreviousPeriodPostings = await query
            .Select(x => new LedgerPostingForReportDto
            {
                Debit = x.Debit,
                Credit = x.Credit,
                LedgerId = x.LedgerId,
                AccountGroupId = x.AccountLedgerFk.AccountGroupId,
                VoucherName = x.VoucherTypeFk.Name,
                Date = x.Date
            })
            .OrderBy(x => x.AccountGroupId)
            .ToListAsync();
    }

    private (DateTime startDate, DateTime endDate) GetComparisonDateRange(DateTime startDate, DateTime endDate,
        string comparisonPeriod)
    {
        return comparisonPeriod switch
        {
            "previous-year" => (startDate.AddYears(-1), endDate.AddYears(-1)),
            "previous-quarter" => (startDate.AddMonths(-3), endDate.AddMonths(-3)),
            "previous-month" => (startDate.AddMonths(-1), endDate.AddMonths(-1)),
            _ => (startDate.AddYears(-1), endDate.AddYears(-1))
        };
    }

    private async Task<CashFlowStatementDto> GetBeginningCashBalance(DateTime currentStartDate,
        DateTime comparisonStartDate)
    {
        // Find cash-related account groups (usually under Assets nature)
        var cashGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Assets &&
                        (x.Name.Contains("Cash") || x.Name.Contains("Bank") || x.Name.Contains("Cash Equivalent")))
            .ToListAsync();

        if (!cashGroups.Any())
            return new CashFlowStatementDto
            {
                Id = Guid.NewGuid(),
                Data = new CashFlowStatementDetail
                {
                    Id = Guid.NewGuid(),
                    Name = "Cash and Cash Equivalents, Beginning of Period",
                    CurrentAmount = 0,
                    PreviousAmount = 0,
                    GroupType = CashFlowGroupEnum.BeginningBalance
                },
                Children = null
            };

        // Find all cash-related ledger accounts
        var cashGroupIds = cashGroups.Select(x => x.Id).ToList();
        var cashLedgerIds = AccountLedgerList
            .Where(x => cashGroupIds.Contains(x.AccountGroupId))
            .Select(x => x.Id)
            .ToList();

        // Get beginning balance for current period (transactions before start date)
        var currentBeginningBalance = await ledgerPostingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => cashLedgerIds.Contains(x.LedgerId) && x.Date < currentStartDate &&
                        x.FinancialYearId == FinancialYearId)
            .SumAsync(x => x.Debit - x.Credit);

        // Get beginning balance for comparison period
        var previousBeginningBalance = await ledgerPostingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => cashLedgerIds.Contains(x.LedgerId) && x.Date < comparisonStartDate &&
                        x.FinancialYearId == FinancialYearId)
            .SumAsync(x => x.Debit - x.Credit);

        return new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Cash and Cash Equivalents, Beginning of Period",
                CurrentAmount = currentBeginningBalance,
                PreviousAmount = previousBeginningBalance,
                GroupType = CashFlowGroupEnum.BeginningBalance
            },
            Children = await GetCashAccountsBreakdown(cashGroups, currentStartDate, comparisonStartDate)
        };
    }

    private async Task<List<CashFlowStatementDto>> GetCashAccountsBreakdown(
        List<AccountGroup> cashGroups,
        DateTime currentStartDate,
        DateTime comparisonStartDate)
    {
        var result = new List<CashFlowStatementDto>();

        foreach (var group in cashGroups)
        {
            // Get ledgers for this group
            var ledgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // Get balances for current period
            var currentGroupBalance = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => ledgerIds.Contains(x.LedgerId) && x.Date < currentStartDate &&
                            x.FinancialYearId == FinancialYearId)
                .SumAsync(x => x.Debit - x.Credit);

            // Get balances for comparison period
            var previousGroupBalance = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => ledgerIds.Contains(x.LedgerId) && x.Date < comparisonStartDate &&
                            x.FinancialYearId == FinancialYearId)
                .SumAsync(x => x.Debit - x.Credit);

            // Only add if there are balances
            if (currentGroupBalance != 0 || previousGroupBalance != 0)
            {
                var groupNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = group.Name,
                        CurrentAmount = currentGroupBalance,
                        PreviousAmount = previousGroupBalance,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    Children = await GetLedgerAccountsForCashBalance(group.Id, currentStartDate, comparisonStartDate)
                };

                result.Add(groupNode);
            }
        }

        return result;
    }

    private async Task<List<CashFlowStatementDto>> GetLedgerAccountsForCashBalance(
        Guid accountGroupId,
        DateTime currentStartDate,
        DateTime comparisonStartDate)
    {
        var result = new List<CashFlowStatementDto>();

        // Get ledger accounts for this group
        var ledgerIds = AccountLedgerList
            .Where(x => x.AccountGroupId == accountGroupId)
            .Select(x => x.Id)
            .ToList();

        foreach (var ledgerId in ledgerIds)
        {
            // Get balances for current period
            var currentLedgerBalance = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.LedgerId == ledgerId && x.Date < currentStartDate && x.FinancialYearId == FinancialYearId)
                .SumAsync(x => x.Debit - x.Credit);

            // Get balances for comparison period
            var previousLedgerBalance = await ledgerPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.LedgerId == ledgerId && x.Date < comparisonStartDate &&
                            x.FinancialYearId == FinancialYearId)
                .SumAsync(x => x.Debit - x.Credit);

            // Only add if there are balances
            if (currentLedgerBalance != 0 || previousLedgerBalance != 0)
            {
                var ledgerAccount = AccountLedgerList.FirstOrDefault(x => x.Id == ledgerId);
                var ledgerNode = new CashFlowStatementDto
                {
                    Id = ledgerId,
                    Data = new CashFlowStatementDetail
                    {
                        Id = ledgerId,
                        Name = ledgerAccount?.DisplayName ?? "Unknown Account",
                        CurrentAmount = currentLedgerBalance,
                        PreviousAmount = previousLedgerBalance,
                        GroupType = CashFlowGroupEnum.AccountLedger
                    },
                    Children = null
                };

                result.Add(ledgerNode);
            }
        }

        return result;
    }

    private async Task<CashFlowStatementDto> GetOperatingActivitiesCashFlow()
    {
        var result = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Children = [],
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Cash Flow from Operating Activities",
                CurrentAmount = 0,
                PreviousAmount = 0,
                GroupType = CashFlowGroupEnum.OperatingActivities
            }
        };

        // 1. Add Net Income/Loss section
        var netIncomeNode = await GetNetIncomeCashFlow();
        result.Children.Add(netIncomeNode);

        // 2. Add Adjustments section for non-cash items
        var adjustmentsNode = await GetAdjustmentsForNonCashItems();
        result.Children.Add(adjustmentsNode);

        // 3. Add changes in working capital (current assets and liabilities)
        var workingCapitalNode = await GetChangesInWorkingCapital();
        result.Children.Add(workingCapitalNode);

        // Calculate total for operating activities
        result.Data.CurrentAmount = result.Children.Sum(x => x.Data.CurrentAmount);
        result.Data.PreviousAmount = result.Children.Sum(x => x.Data.PreviousAmount);

        return result;
    }

    private async Task<CashFlowStatementDto> GetNetIncomeCashFlow()
    {
        // Calculate net income from income and expense groups
        var currentNetIncome = await CalculateNetIncome(CurrentPeriodPostings);
        var previousNetIncome = await CalculateNetIncome(PreviousPeriodPostings);

        return new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Net Income / (Loss)",
                CurrentAmount = currentNetIncome,
                PreviousAmount = previousNetIncome,
                GroupType = CashFlowGroupEnum.AccountGroup
            },
            Children = null
        };
    }

    private async Task<decimal> CalculateNetIncome(List<LedgerPostingForReportDto> postings)
    {
        // Get income account groups
        var incomeGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Income)
            .ToListAsync();

        // Get expense account groups
        var expenseGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Expenses)
            .ToListAsync();

        decimal incomeTotal = 0;
        decimal expenseTotal = 0;

        // Calculate income (typically Credit - Debit)
        foreach (var group in incomeGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            incomeTotal += postings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);
        }

        // Calculate expenses (typically Debit - Credit)
        foreach (var group in expenseGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            expenseTotal += postings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Debit - x.Credit);
        }

        // Net income = Income - Expenses
        return incomeTotal - expenseTotal;
    }

    private async Task<CashFlowStatementDto> GetAdjustmentsForNonCashItems()
    {
        var result = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Children = [],
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Adjustments for Non-Cash Items",
                CurrentAmount = 0,
                PreviousAmount = 0,
                GroupType = CashFlowGroupEnum.AccountGroup
            }
        };

        // Get non-cash expense groups (like Depreciation, Amortization)
        var depreciationGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Expenses &&
                        (x.Name.Contains("Depreciation") ||
                         x.Name.Contains("Amortization") ||
                         x.Name.Contains("Provisions")))
            .ToListAsync();

        foreach (var group in depreciationGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // Calculate current period amount
            var currentAmount = CurrentPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Debit - x.Credit);

            // Calculate previous period amount
            var previousAmount = PreviousPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Debit - x.Credit);

            // Only add if there is activity
            if (currentAmount != 0 || previousAmount != 0)
            {
                var adjustmentNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = group.Name,
                        CurrentAmount = currentAmount,
                        PreviousAmount = previousAmount,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    // expenses should be shown as positive adjustments (added back)
                    Children = await GetLedgerAccountsForGroup(group.Id, CurrentPeriodPostings, PreviousPeriodPostings,
                        AccountAmountMode.DebitMinusCredit)
                };

                result.Children.Add(adjustmentNode);
                result.Data.CurrentAmount += currentAmount;
                result.Data.PreviousAmount += previousAmount;
            }
        }

        return result;
    }

    private async Task<CashFlowStatementDto> GetChangesInWorkingCapital()
    {
        var result = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Children = [],
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Changes in Working Capital",
                CurrentAmount = 0,
                PreviousAmount = 0,
                GroupType = CashFlowGroupEnum.AccountGroup
            }
        };

        // Get current asset groups (excluding cash and cash equivalents)
        var currentAssetGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Assets &&
                        !(x.Name.Contains("Cash") || x.Name.Contains("Bank") || x.Name.Contains("Cash Equivalent")) &&
                        (x.Name.Contains("Current") || x.Name.Contains("Inventory") ||
                         x.Name.Contains("Receivable") || x.Name.Contains("Prepaid")))
            .ToListAsync();

        // Get current liability groups
        var currentLiabilityGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Liabilities &&
                        (x.Name.Contains("Current") || x.Name.Contains("Payable") ||
                         x.Name.Contains("Accrued")))
            .ToListAsync();

        // Process current assets (increase is negative cash flow, decrease is positive)
        foreach (var group in currentAssetGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // For assets, Debit increases (negative cash flow), Credit decreases (positive cash flow)
            // Calculate net change for current period
            var currentNetChange = CurrentPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit); // Reverse sign for cash flow

            // Calculate net change for previous period
            var previousNetChange = PreviousPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit); // Reverse sign for cash flow

            // Only add if there is activity
            if (currentNetChange != 0 || previousNetChange != 0)
            {
                var assetNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = $"Change in {group.Name}",
                        CurrentAmount = currentNetChange,
                        PreviousAmount = previousNetChange,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    Children = await GetLedgerAccountsForGroup(group.Id, CurrentPeriodPostings, PreviousPeriodPostings,
                        AccountAmountMode.CreditMinusDebit)
                };

                result.Children.Add(assetNode);
                result.Data.CurrentAmount += currentNetChange;
                result.Data.PreviousAmount += previousNetChange;
            }
        }

        // Process current liabilities (increase is positive cash flow, decrease is negative)
        foreach (var group in currentLiabilityGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // For liabilities, Credit increases (positive cash flow), Debit decreases (negative cash flow)
            // Calculate net change for current period
            var currentNetChange = CurrentPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            // Calculate net change for previous period
            var previousNetChange = PreviousPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            // Only add if there is activity
            if (currentNetChange != 0 || previousNetChange != 0)
            {
                var liabilityNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = $"Change in {group.Name}",
                        CurrentAmount = currentNetChange,
                        PreviousAmount = previousNetChange,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    Children = await GetLedgerAccountsForGroup(group.Id, CurrentPeriodPostings, PreviousPeriodPostings,
                        AccountAmountMode.CreditMinusDebit)
                };

                result.Children.Add(liabilityNode);
                result.Data.CurrentAmount += currentNetChange;
                result.Data.PreviousAmount += previousNetChange;
            }
        }

        return result;
    }

    private async Task<CashFlowStatementDto> GetInvestingActivitiesCashFlow()
    {
        var result = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Children = [],
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Cash Flow from Investing Activities",
                CurrentAmount = 0,
                PreviousAmount = 0,
                GroupType = CashFlowGroupEnum.InvestingActivities
            }
        };

        // Get fixed asset and investment groups
        var investingGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => x.Nature == AccountGroupNature.Assets &&
                        !(x.Name.Contains("Current") || x.Name.Contains("Cash") ||
                          x.Name.Contains("Bank") || x.Name.Contains("Receivable")) &&
                        (x.Name.Contains("Fixed") || x.Name.Contains("Property") ||
                         x.Name.Contains("Equipment") || x.Name.Contains("Investment") ||
                         x.Name.Contains("Capital")))
            .ToListAsync();

        foreach (var group in investingGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // assets: cashflow = credit - debit
            var currentNetChange = CurrentPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            var previousNetChange = PreviousPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            // Only add if there is activity
            if (currentNetChange != 0 || previousNetChange != 0)
            {
                string activityName;
                if (currentNetChange < 0)
                    // Net Debit means purchase/acquisition
                    activityName = $"Purchase of {group.Name}";
                else
                    // Net Credit means sale/disposal
                    activityName = $"Sale of {group.Name}";

                var investingNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = activityName,
                        CurrentAmount = currentNetChange,
                        PreviousAmount = previousNetChange,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    Children = await GetLedgerAccountsForGroup(group.Id, CurrentPeriodPostings, PreviousPeriodPostings,
                        AccountAmountMode.CreditMinusDebit)
                };

                result.Children.Add(investingNode);
                result.Data.CurrentAmount += currentNetChange;
                result.Data.PreviousAmount += previousNetChange;
            }
        }

        return result;
    }

    private async Task<CashFlowStatementDto> GetFinancingActivitiesCashFlow()
    {
        var result = new CashFlowStatementDto
        {
            Id = Guid.NewGuid(),
            Children = [],
            Data = new CashFlowStatementDetail
            {
                Id = Guid.NewGuid(),
                Name = "Cash Flow from Financing Activities",
                CurrentAmount = 0,
                PreviousAmount = 0,
                GroupType = CashFlowGroupEnum.FinancingActivities
            }
        };

        // Get long-term liabilities and equity groups
        var financingGroups = await accountGroupRepository.GetAll()
            .Where(x => x.TenantId == AbpSession.TenantId)
            .Where(x => (x.Nature == AccountGroupNature.Liabilities &&
                         !(x.Name.Contains("Current") || x.Name.Contains("Payable") ||
                           x.Name.Contains("Accrued"))) ||
                        x.Name.Contains("Capital") || x.Name.Contains("Equity") ||
                        x.Name.Contains("Share") || x.Name.Contains("Retained") ||
                        x.Name.Contains("Dividend"))
            .ToListAsync();

        foreach (var group in financingGroups)
        {
            var groupLedgerIds = AccountLedgerList
                .Where(x => x.AccountGroupId == group.Id)
                .Select(x => x.Id)
                .ToList();

            // liabilities: credit = increase (cash in), debit = decrease (cash out) => cashflow = credit - debit
            var currentNetChange = CurrentPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            var previousNetChange = PreviousPeriodPostings
                .Where(x => groupLedgerIds.Contains(x.LedgerId))
                .Sum(x => x.Credit - x.Debit);

            // Only add if there is activity
            if (currentNetChange != 0 || previousNetChange != 0)
            {
                string activityName;
                if (group.Nature == AccountGroupNature.Liabilities)
                    activityName = currentNetChange > 0
                        ? $"Proceeds from {group.Name}"
                        : $"Repayment of {group.Name}";
                else
                    activityName = currentNetChange > 0
                        ? $"Issuance of {group.Name}"
                        : $"Payment of {group.Name}";

                var financingNode = new CashFlowStatementDto
                {
                    Id = group.Id,
                    Data = new CashFlowStatementDetail
                    {
                        Id = group.Id,
                        Name = activityName,
                        CurrentAmount = currentNetChange,
                        PreviousAmount = previousNetChange,
                        GroupType = CashFlowGroupEnum.AccountGroup
                    },
                    Children = await GetLedgerAccountsForGroup(group.Id, CurrentPeriodPostings, PreviousPeriodPostings,
                        AccountAmountMode.CreditMinusDebit)
                };

                result.Children.Add(financingNode);
                result.Data.CurrentAmount += currentNetChange;
                result.Data.PreviousAmount += previousNetChange;
            }
        }

        return result;
    }

    private enum AccountAmountMode
    {
        CreditMinusDebit,
        DebitMinusCredit
    }

    private Task<List<CashFlowStatementDto>> GetLedgerAccountsForGroup(
        Guid accountGroupId,
        List<LedgerPostingForReportDto> currentPostings,
        List<LedgerPostingForReportDto> previousPostings,
        AccountAmountMode mode)
    {
        var result = new List<CashFlowStatementDto>();

        // Get ledger accounts for this group
        var ledgerIds = AccountLedgerList
            .Where(x => x.AccountGroupId == accountGroupId)
            .Select(x => x.Id)
            .ToList();

        foreach (var ledgerId in ledgerIds)
        {
            decimal currentAmount;
            decimal previousAmount;

            if (mode == AccountAmountMode.DebitMinusCredit)
            {
                currentAmount = currentPostings.Where(x => x.LedgerId == ledgerId).Sum(x => x.Debit - x.Credit);
                previousAmount = previousPostings.Where(x => x.LedgerId == ledgerId).Sum(x => x.Debit - x.Credit);
            }
            else
            {
                currentAmount = currentPostings.Where(x => x.LedgerId == ledgerId).Sum(x => x.Credit - x.Debit);
                previousAmount = previousPostings.Where(x => x.LedgerId == ledgerId).Sum(x => x.Credit - x.Debit);
            }

            // Only include if there is activity
            if (currentAmount != 0 || previousAmount != 0)
            {
                var ledgerAccount = AccountLedgerList.FirstOrDefault(x => x.Id == ledgerId);
                var childNode = new CashFlowStatementDto
                {
                    Id = ledgerId,
                    Data = new CashFlowStatementDetail
                    {
                        Id = ledgerId,
                        Name = ledgerAccount?.DisplayName ?? "Unknown Account",
                        CurrentAmount = currentAmount,
                        PreviousAmount = previousAmount,
                        GroupType = CashFlowGroupEnum.AccountLedger
                    },
                    Children = null
                };

                result.Add(childNode);
            }
        }

        return Task.FromResult(result);
    }

    private List<CashFlowStatementDetail> FlattenHierarchy(List<CashFlowStatementDto> hierarchicalData, int level = 0)
    {
        var result = new List<CashFlowStatementDetail>();

        foreach (var item in hierarchicalData)
        {
            // Add indentation based on level
            var paddedName = new string(' ', level * 2) + item.Data.Name;

            var flatItem = new CashFlowStatementDetail
            {
                Id = item.Data.Id,
                Name = paddedName,
                CurrentAmount = item.Data.CurrentAmount,
                PreviousAmount = item.Data.PreviousAmount,
                GroupType = item.Data.GroupType,
                Level = level
            };

            result.Add(flatItem);

            // Process children if they exist
            if (item.Children != null && item.Children.Count > 0)
                result.AddRange(FlattenHierarchy(item.Children, level + 1));
        }

        return result;
    }
}


public class AccountLedgerDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public Guid AccountGroupId { get; set; }
}
