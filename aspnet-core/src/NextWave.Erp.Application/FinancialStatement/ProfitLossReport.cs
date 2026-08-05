using Abp.Authorization;
using Abp.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration.Tenants;
using NextWave.Erp.ControlPanel;
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



[AbpAuthorize(AppPermissions.PagesProfitAndLossReport)]
public class ProfitAndLossReport(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<StockPosting, Guid> stockPostingRepository,
    ITenantSettingsAppService tenantSettingsApp,
    IRepository<UnitConversion, Guid> unitConversionRepository,
       IRepository<Branch, Guid> branchRepository,
    IProfitLossExcelExporter excelExporter)
    : ErpAppServiceBase
{
    protected List<UniversalDropdownDto> AccountLedgerList = [];
    protected decimal ClosingStock;
    protected decimal OpeningStock;
    protected decimal TotalExpenseAmount;
    protected decimal TotalRevenueAmount;
    protected List<LedgerPostingForReportDto> PostingList = [];


    public async Task<List<FinancialStatementDto>> GetReport([CanBeNull] string fromMiti = null, [CanBeNull] string toMiti = null)
    {


        var fromDate = FinancialYear.FromDate;
        var toDate = FinancialYear.ToDate;
        if (fromMiti != null && fromMiti.Trim() != string.Empty)
        {
            fromDate = DateConverter.ConvertToEnglish(fromMiti);
        }

        if (toMiti != null && toMiti.Trim() != string.Empty)
        {
            toDate = DateConverter.ConvertToEnglish(toMiti);
        }

        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var returnList = new List<FinancialStatementDto>();


        // Clear previous data

        PostingList.Clear();

        // Get stock calculation method from settings
        var stockCalculation = "Average";
            //(await tenantSettingsApp.GetAllSettings()).AllSettingsBundleDto.StockCalculation.ToLower();

        // Build posting query
        var postingQuery = ledgerPostingRepository.GetAll().AsNoTracking()
            .Where(x => x.FinancialYearId == FinancialYearId
                        && x.TenantId == AbpSession.TenantId);

        // Apply branch filter if specified


        // Get postings with related data
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
            .Where(e => e.Date >= fromDate.Date && e.Date <= toDate.Date)
            .ToListAsync();

        // Get account ledger list
        AccountLedgerList = await accountLedgerRepository.GetAll()
            .Where(e => e.TenantId == AbpSession.TenantId).AsNoTracking()
            .Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            })
            .ToListAsync();

        // Calculate opening and closing stock
        await CalculateStockValues(stockCalculation, fromDate, toDate);

        // Get primary account group
        var primaryAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Primary");

        // Add Opening Stock

        var openingStockDto = new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Opening Stock",
                Debit = OpeningStock,
                Credit = 0,
                GroupType = TrailBalanceGroupEnum.OpeningStock
            },
            Children = null
        };
        returnList.Add(openingStockDto);


        var directIncomeAccounts = await GetAccountsBySection(primaryAccountGroup.Id, ProfitLossSection.DirectIncome);
        var directExpenseAccounts = await GetAccountsBySection(primaryAccountGroup.Id, ProfitLossSection.DirectExpense);
        var indirectIncomeAccounts = await GetAccountsBySection(primaryAccountGroup.Id, ProfitLossSection.IndirectIncome);
        var indirectExpenseAccounts = await GetAccountsBySection(primaryAccountGroup.Id, ProfitLossSection.IndirectExpense);

        if (directExpenseAccounts.Any())
        {
            returnList.AddRange(directExpenseAccounts);
        }

        if (directIncomeAccounts.Any())
        {
            returnList.AddRange(directIncomeAccounts);
        }

        // Add Closing Stock (reduces COGS)

        var closingStock = new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Less: Closing Stock",
                Debit = 0,
                Credit = ClosingStock,
                GroupType = TrailBalanceGroupEnum.ClosingStock
            },
            Children = null
        };
        returnList.Add(closingStock);

        var directIncome = GetCreditBalance(directIncomeAccounts);
        var directExpenses = GetDebitBalance(directExpenseAccounts);
        var grossProfit = directIncome + ClosingStock - OpeningStock - directExpenses;

        // Add Gross Profit line
        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = grossProfit >= 0 ? "Gross Profit" : "Gross Loss",
                Debit = grossProfit < 0 ? Math.Abs(grossProfit) : 0,
                Credit = grossProfit >= 0 ? grossProfit : 0,
                GroupType = TrailBalanceGroupEnum.GrossProfit
            }
        });

        if (indirectExpenseAccounts.Any())
        {
            returnList.AddRange(indirectExpenseAccounts);
        }

        if (indirectIncomeAccounts.Any())
        {
            returnList.AddRange(indirectIncomeAccounts);
        }

        // Calculate Net Profit
        var indirectExpenses = GetDebitBalance(indirectExpenseAccounts);
        var indirectIncome = GetCreditBalance(indirectIncomeAccounts);
        var netProfit = grossProfit - indirectExpenses + indirectIncome;
        TotalRevenueAmount = Math.Round(directIncome + indirectIncome, 2);
        TotalExpenseAmount = Math.Round(OpeningStock + directExpenses - ClosingStock + indirectExpenses, 2);

        // Add Net Profit/Loss line
        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = netProfit >= 0 ? "Net Profit" : "Net Loss",
                Debit = netProfit < 0 ? Math.Abs(Math.Round(netProfit, 2)) : 0,
                Credit = netProfit >= 0 ? Math.Round(netProfit, 2) : 0,
                GroupType = TrailBalanceGroupEnum.NetProfit
            }
        });

        return returnList.Where(x => x.Data.Debit != 0 || x.Data.Credit != 0).ToList();
    }

    /// <summary>
    ///     Calculate opening and closing stock values
    /// </summary>
    private async Task CalculateStockValues(string stockCalculation, DateTime fromDate, DateTime toDate)
    {
        var unitConversion = await unitConversionRepository.GetAll().AsNoTracking()
            .Include(x => x.UnitFk)
            .Select(x => new UnitConversionDto
            {
                ProductId = x.ProductId,
                UnitId = x.UnitId,
                PrimaryQty = x.PrimaryQty,
                Qty = x.Qty,
                UnitName = x.UnitFk.Name,
                ConversionRate = x.ConversionRate
            }).ToListAsync();

        // Build stock posting query
        var stockPostingQuery = stockPostingRepository.GetAll().AsNoTracking()
            .Where(x => x.FinancialYearId == FinancialYearId
                        && x.TenantId == AbpSession.TenantId
                        && !x.IsDeleted);


        // Apply branch filter if specified


        var stockPostingList = await stockPostingQuery
            .Include(x => x.VoucherTypeFk)
            .Include(x => x.ProductFk)
            .Where(x => x.Date <= toDate.Date)
            .Select(x => new StockPostingDto
            {
                ProductId = x.ProductId,
                VoucherType = x.VoucherTypeFk.Name,
                Date = x.Date,
                ProductName = x.ProductFk.Name,
                Amount = x.Amount,
                Rate = x.Rate,
                UnitId = x.UnitId,
                InWardQty = x.InWardQty,
                OutWardQty = x.OutWardQty
            }).ToListAsync();

        // Calculate opening stock (as of fromDate)
        var openingStock = stockPostingList
            .Where(x => x.VoucherType == "OpeningStock")
            .Sum(e => e.InWardQty * e.Rate);

        var date = fromDate;
        var beforePeriodStock = stockPostingList
            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < date.Date)
            .Sum(e => e.InWardQty * e.Rate);
        var beforePeriodMinStock = stockPostingList
            .Where(x => x.VoucherType != "OpeningStock" && x.Date.Date < date.Date)
            .Sum(e => e.OutWardQty * e.Rate);

        OpeningStock = Math.Round(openingStock + beforePeriodStock - beforePeriodMinStock, 2);



        // Calculate closing stock (as of toDate) using the specified method
        await CalculateClosingStock(stockPostingList, stockCalculation, unitConversion);
    }

    /// <summary>
    ///     Calculate closing stock based on the specified method (FIFO, LIFO, or Average)
    /// </summary>
    private async Task CalculateClosingStock(List<StockPostingDto> stockPostingList,
        string stockCalculation, List<UnitConversionDto> unitConversion)
    {
        var posting = new List<ProductReportDto>();

        // Process stock postings up to the end date
        var relevantPostings = stockPostingList.ToList();

        foreach (var x in relevantPostings)
        {
            var product = new ProductReportDto();
            var minUnit = unitConversion.Where(a => a.ProductId == x.ProductId).MinBy(a => a.ConversionRate);
            var unitConversionDetail =
                unitConversion.FirstOrDefault(a => a.ProductId == x.ProductId && a.UnitId == x.UnitId);

            product.ProductId = x.ProductId;
            product.ProductName = x.ProductName;
            product.UnitId = minUnit?.UnitId ?? x.UnitId;
            product.VoucherType = x.VoucherType;
            product.Date = x.Date;
            product.Rate = x.Rate;
            product.InWardQty = x.InWardQty;
            product.OutWardQty = x.OutWardQty;
            product.InWardValue = x.InWardQty * x.Rate;
            product.OutWardValue = x.OutWardQty * x.Rate;

            if (minUnit != null && unitConversionDetail != null)
            {
                product.Rate = x.Rate * minUnit.PrimaryQty * unitConversionDetail.Qty /
                               (minUnit.Qty * unitConversionDetail.PrimaryQty);
                product.InWardQty = x.InWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                    (unitConversionDetail.Qty * minUnit.PrimaryQty);
                product.OutWardQty = x.OutWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                     (unitConversionDetail.Qty * minUnit.PrimaryQty);
            }

            product.CurrentStock = product.InWardQty - product.OutWardQty;
            posting.Add(product);
        }

        decimal closingStockAmt = 0;
        foreach (var product in relevantPostings.DistinctBy(a => a.ProductId).ToList())
        {
            var productUniqueStock = posting.Where(p => p.ProductId == product.ProductId).ToList();
            if (!productUniqueStock.Any())
            {
                continue;
            }

            decimal stockValue = 0;
            decimal stockQty = 0;

            // Apply stock calculation method
            if (stockCalculation.ToLower() == "fifo")
            {
                stockValue = CalculateFifoStockValue(productUniqueStock);
            }
            else if (stockCalculation.ToLower() == "lifo")
            {
                stockValue = CalculateLifoStockValue(productUniqueStock);
            }
            else
            // Average method
            {
                stockValue = CalculateAverageStockValue(productUniqueStock, out stockQty);
            }

            closingStockAmt += stockValue;
        }
        ClosingStock = closingStockAmt;
    }

    private decimal CalculateFifoStockValue(List<ProductReportDto> productStock)
    {
        var layers = new List<(decimal Qty, decimal Rate)>();

        foreach (var product in productStock.OrderBy(x => x.Date.Date))
        {
            if (product.InWardQty > 0)
            {
                layers.Add((product.InWardQty, product.Rate));
            }

            var outQty = product.OutWardQty;
            while (outQty > 0 && layers.Any())
            {
                var first = layers[0];
                var consumedQty = Math.Min(first.Qty, outQty);
                first.Qty -= consumedQty;
                outQty -= consumedQty;

                if (first.Qty == 0)
                {
                    layers.RemoveAt(0);
                }
                else
                {
                    layers[0] = first;
                }
            }
        }

        return layers.Sum(x => x.Qty * x.Rate);
    }

    private decimal CalculateLifoStockValue(List<ProductReportDto> productStock)
    {
        var layers = new List<(decimal Qty, decimal Rate)>();

        foreach (var product in productStock.OrderBy(x => x.Date.Date))
        {
            if (product.InWardQty > 0)
            {
                layers.Add((product.InWardQty, product.Rate));
            }

            var outQty = product.OutWardQty;
            while (outQty > 0 && layers.Any())
            {
                var lastIndex = layers.Count - 1;
                var last = layers[lastIndex];
                var consumedQty = Math.Min(last.Qty, outQty);
                last.Qty -= consumedQty;
                outQty -= consumedQty;

                if (last.Qty == 0)
                {
                    layers.RemoveAt(lastIndex);
                }
                else
                {
                    layers[lastIndex] = last;
                }
            }
        }

        return layers.Sum(x => x.Qty * x.Rate);
    }

    private decimal CalculateAverageStockValue(List<ProductReportDto> productStock, out decimal stockQty)
    {
        var inward = productStock
            .Where(x => x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt"
                or "PurchaseInvoice" or "PurchaseReturn")
            .Where(x => x.InWardQty > 0).ToList();

        decimal productRate = 0;
        if (inward.Any())
        {
            var inWardAmt = inward.Sum(x => x.InWardValue);
            var inWardQty = inward.Sum(x => x.InWardQty);
            if (inWardQty > 0)
            {
                productRate = inWardAmt / inWardQty;
            }
        }

        stockQty = productStock.Sum(b => b.InWardQty) - productStock.Sum(b => b.OutWardQty);
        return productRate * stockQty;
    }

    private enum ProfitLossSection
    {
        DirectIncome,
        DirectExpense,
        IndirectIncome,
        IndirectExpense
    }

    /// <summary>
    ///     Get accounts by accounting nature and top-level P&L section.
    /// </summary>
    private async Task<List<FinancialStatementDto>> GetAccountsBySection(Guid primaryGroupId, ProfitLossSection section)
    {
        var accountGroups = await accountGroupRepository.GetAll().AsNoTracking()
            .Where(x => x.GroupUnder == primaryGroupId
                        && x.Name != "Primary"
                        && x.TenantId == AbpSession.TenantId
                        && x.AffectGrossProfit)
            .ToListAsync();

        accountGroups = accountGroups.Where(x => IsInSection(x, section)).ToList();

        var result = new List<FinancialStatementDto>();
        foreach (var accountGroup in accountGroups)
        {
            var groupResult = await BuildAccountGroupData(accountGroup);
            if (groupResult != null && (groupResult.Data.Debit != 0 || groupResult.Data.Credit != 0))
            {
                result.Add(groupResult);
            }
        }

        return result;
    }

    private static bool IsInSection(AccountGroup accountGroup, ProfitLossSection section)
    {
        var isIncome = accountGroup.Nature == AccountGroupNature.Income;
        var isExpense = accountGroup.Nature == AccountGroupNature.Expenses;
        var isIndirect = IsIndirectSection(accountGroup.Name);

        return section switch
        {
            ProfitLossSection.DirectIncome => isIncome && !isIndirect,
            ProfitLossSection.DirectExpense => isExpense && !isIndirect,
            ProfitLossSection.IndirectIncome => isIncome && isIndirect,
            ProfitLossSection.IndirectExpense => isExpense && isIndirect,
            _ => false
        };
    }

    private static bool IsIndirectSection(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            return false;
        }

        var normalizedName = accountName.ToLowerInvariant();
        return normalizedName.Contains("indirect") || normalizedName.Contains("other");
    }

    private static decimal GetCreditBalance(IEnumerable<FinancialStatementDto> accounts)
    {
        return accounts.Sum(x => x.Data.Credit - x.Data.Debit);
    }

    private static decimal GetDebitBalance(IEnumerable<FinancialStatementDto> accounts)
    {
        return accounts.Sum(x => x.Data.Debit - x.Data.Credit);
    }

    /// <summary>
    ///     Build account group data recursively
    /// </summary>
    private async Task<FinancialStatementDto> BuildAccountGroupData(AccountGroup accountGroup)
    {
        var ledgerPostings = PostingList.Where(x => x.AccountGroupId == accountGroup.Id).ToList();
        var calcAmount = ledgerPostings.Sum(x => x.Debit - x.Credit);

        var result = new FinancialStatementDto
        {
            Data = new FinancialStatementDetail
            {
                Id = accountGroup.Id,
                Name = accountGroup.Name,
                GroupType = TrailBalanceGroupEnum.AccountGroup
            },
            Id = accountGroup.Id,
            Children = []
        };

        // Get child groups
        var childGroups = await accountGroupRepository.GetAll().AsNoTracking()
            .Where(x => x.GroupUnder == accountGroup.Id && x.TenantId == AbpSession.TenantId)
            .ToListAsync();

        foreach (var childGroup in childGroups)
        {
            var childResult = await BuildAccountGroupData(childGroup);
            if (childResult != null && (childResult.Data.Debit != 0 || childResult.Data.Credit != 0))
            {
                result.Children.Add(childResult);
                calcAmount += childResult.Data.Debit - childResult.Data.Credit;
            }
        }

        // Get ledgers for this group
        var distinctLedgerIds = ledgerPostings.Select(x => x.LedgerId).Distinct().ToList();
        foreach (var ledgerId in distinctLedgerIds)
        {
            var ledgerPostingsForLedger = ledgerPostings.Where(x => x.LedgerId == ledgerId).ToList();
            var ledgerAmount = ledgerPostingsForLedger.Sum(x => x.Debit) - ledgerPostingsForLedger.Sum(x => x.Credit);

            if (ledgerAmount != 0)
            {
                var ledgerData = new FinancialStatementDto
                {
                    Id = Guid.Empty,
                    Data = new FinancialStatementDetail
                    {
                        Id = ledgerId,
                        Name = AccountLedgerList.FirstOrDefault(x => x.Id == ledgerId)?.DisplayName,
                        Debit = ledgerAmount > 0 ? ledgerAmount : 0,
                        Credit = ledgerAmount < 0 ? Math.Abs(ledgerAmount) : 0,
                        GroupType = TrailBalanceGroupEnum.AccountLedger
                    }
                };
                result.Children.Add(ledgerData);
            }
        }

        result.Data.Debit = calcAmount > 0 ? Math.Round(calcAmount, 2) : 0;
        result.Data.Credit = calcAmount < 0 ? Math.Round(Math.Abs(calcAmount), 2) : 0;

        return result;
    }

    /// <summary>
    ///     Export to Excel
    /// </summary>
    public async Task<FileDto> ProfitAndLossExportToExcel([CanBeNull] string fromMiti = null, [CanBeNull] string toMiti = null)
    {
        var result = new List<FinancialStatementDetail>();
        var dataList = await GetReport(fromMiti, toMiti);
        const int level = 0;
        foreach (var rep in dataList)
            result.AddRange(await GetRecursiveChild(rep, level));
        return excelExporter.ExportToFile(result);
    }

    /// <summary>
    ///     Generate PDF
    /// </summary>
    public async Task<byte[]> GetPdfDownload([CanBeNull] string fromMiti = null, [CanBeNull] string toMiti = null)
    {
        var result = new List<FinancialStatementDetail>();
        var dataList = await GetReport(fromMiti, toMiti);
        const int level = 0;
        foreach (var rep in dataList)
            result.AddRange(await GetRecursiveChild(rep, level));
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
        var report = new ProfitLossPdfDto
        {
            FromMiti = fromMiti,
            ToMiti = toMiti,
            CompanyInfo =branchInfo,
            ProfitLossDetails = result
        };

        var document = new ProfitLossPdf(report);
        return document.GeneratePdf();
    }

    /// <summary>
    ///     Get recursive child for flattening hierarchy
    /// </summary>
    private async Task<List<FinancialStatementDetail>> GetRecursiveChild(FinancialStatementDto child, int level)
    {
        var space = "";
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
        {
            foreach (var rep in child.Children)
                result.AddRange(await GetRecursiveChild(rep, level));
        }

        return result;
    }

    /// <summary>
    ///     Get all account groups for dropdown
    /// </summary>
    public async Task<List<UniversalDropdownDto>> GetAllAccountGroupsForTableDropdown()
    {
        return (await accountGroupRepository.GetAll().ToListAsync()).Select(x =>
            new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).ToList();
    }

    public async Task<ProfitAndLossAgGridResultDto> GetReportForAgGrid([CanBeNull] string fromMiti, [CanBeNull] string toMiti)
    {
        // NOTE: Current report builder does not yet apply date filtering.
        // We still accept these params to be API-compatible. When report logic is updated,
        // just pass the dates into posting/stock queries.

        var tree = await GetReport(fromMiti, toMiti);
        var rows = FlattenForAgGrid(tree);

        var grossProfit = 0m;
        var netProfit = 0m;

        // Prefer semantic parsing by GroupType when possible; fallback to Name.
        foreach (var r in rows)
        {
            if (r.GroupType == TrailBalanceGroupEnum.GrossProfit || string.Equals(r.Name, "Gross Profit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Name, "Gross Loss", StringComparison.OrdinalIgnoreCase))
            {
                grossProfit = r.Credit - r.Debit;
            }

            if (r.GroupType == TrailBalanceGroupEnum.NetProfit || string.Equals(r.Name, "Net Profit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Name, "Net Loss", StringComparison.OrdinalIgnoreCase))
            {
                netProfit = r.Credit - r.Debit;
            }
        }

        return new ProfitAndLossAgGridResultDto
        {
            Rows = rows,
            TotalDebit = rows.Sum(x => x.Debit),
            TotalCredit = rows.Sum(x => x.Credit),
            GrossProfit = Math.Round(grossProfit, 2),
            NetProfit = Math.Round(netProfit, 2),
            TotalRevenue = TotalRevenueAmount,
            TotalExpenses = TotalExpenseAmount
        };
    }

    private static List<ProfitAndLossAgGridRowDto> FlattenForAgGrid(List<FinancialStatementDto> tree)
    {
        var rows = new List<ProfitAndLossAgGridRowDto>();

        void Visit(IEnumerable<FinancialStatementDto> nodes, List<string> parentPath)
        {
            foreach (var n in nodes)
            {
                var name = n?.Data?.Name ?? string.Empty;
                var currentPath = new List<string>(parentPath);

                // Keep paths stable and unique-ish even if names repeat.
                // AG Grid treeData uses Path segments, so duplicates at same level can collide.
                // Append a short id for duplicates/empty names.
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = n.Id == Guid.Empty ? "(Blank)" : n.Id.ToString("N")[..8];
                }

                currentPath.Add(name);

                var detail = n.Data;
                rows.Add(new ProfitAndLossAgGridRowDto
                {
                    Id = n.Id,
                    DetailId = detail.Id,
                    Name = detail.Name,
                    Debit = detail.Debit,
                    Credit = detail.Credit,
                    OpeningDr = detail.OpeningDr,
                    OpeningCr = detail.OpeningCr,
                    ClosingDr = detail.ClosingDr,
                    ClosingCr = detail.ClosingCr,
                    GroupType = detail.GroupType,
                    Path = currentPath,
                    IsLeaf = n.Children == null || n.Children.Count == 0
                });

                if (n.Children is { Count: > 0 })
                {
                    Visit(n.Children, currentPath);
                }
            }
        }

        Visit(tree ?? [], []);
        return rows;
    }
}


public class StockPostingDto
{
    public Guid ProductId { get; set; }
    public string VoucherType { get; set; }
    public DateTime Date { get; set; }
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public Guid UnitId { get; set; }
    public decimal InWardQty { get; set; }
    public decimal OutWardQty { get; set; }
}

public class UnitConversionDto
{
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public decimal PrimaryQty { get; set; }
    public decimal Qty { get; set; }
    public string UnitName { get; set; }
    public decimal ConversionRate { get; set; }
}

public class ProductReportDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid UnitId { get; set; }
    public string VoucherType { get; set; }
    public DateTime Date { get; set; }
    public decimal Rate { get; set; }
    public decimal InWardQty { get; set; }
    public decimal OutWardQty { get; set; }
    public decimal InWardValue { get; set; }
    public decimal OutWardValue { get; set; }
    public decimal CurrentStock { get; set; }
}

public class ProfitAndLossAgGridRowDto
{
    public Guid Id { get; set; }
    public Guid DetailId { get; set; }
    public string Name { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal OpeningDr { get; set; }
    public decimal OpeningCr { get; set; }
    public decimal ClosingDr { get; set; }
    public decimal ClosingCr { get; set; }
    public TrailBalanceGroupEnum GroupType { get; set; }

    public List<string> Path { get; set; } = [];

    public bool IsLeaf { get; set; }
}

public class ProfitAndLossAgGridResultDto
{
    public List<ProfitAndLossAgGridRowDto> Rows { get; set; } = [];

    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }

    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
}
