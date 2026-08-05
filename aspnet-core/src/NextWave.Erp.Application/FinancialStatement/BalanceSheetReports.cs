using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using NextWave.Erp.Accounting;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration.Tenants;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.FinancialStatement.Exporting;
using NextWave.Erp.FinancialStatement.Pdf;
using NextWave.Erp.FinancialStatement.Pdf.Dto;
using NextWave.Erp.Inventory;
using NextWave.Erp.Transaction;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.FinancialStatement;

[AbpAuthorize(AppPermissions.PagesBalanceSheetReport)]
public class BalanceSheetReport(
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<StockPosting, Guid> stockPostingRepository,
      IRepository<Branch, Guid> branchRepository,
    ITenantSettingsAppService tenantSettingsApp,
    IRepository<UnitConversion, Guid> unitConversionRepository,
    IBalanceSheetExcelExporter excelExporter)
    : ErpAppServiceBase
{
    protected List<UniversalDropdownDto> AccountLedgerList = [];
    protected List<FinancialStatementDto> ClStockReports = [];
    protected List<LedgerPostingForReportDto> PostingList = [];

    public Task<decimal> ProfitLossAmount()
    {
        return CalculateProfitLossAmount(FinancialYear.ToDate);
    }

    private async Task<decimal> CalculateProfitLossAmount(DateTime toDate)
    {
        var returnList = new List<FinancialStatementDto>();
        PostingList.Clear();
        ClStockReports.Clear();

        var stockCalculation = "Average";
           // (await tenantSettingsApp.GetAllSettings()).AllSettingsBundleDto.StockCalculation.ToLower();
        PostingList = await ledgerPostingRepository.GetAll().AsNoTracking()
            .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId && x.Date <= toDate.Date)
            .Include(x => x.AccountLedgerFk).Include(x => x.VoucherTypeFk)
            .AsSplitQuery()
            .Select(x => new LedgerPostingForReportDto
            {

                Debit = x.Debit,
                Credit = x.Credit,
                Date = x.Date,
                LedgerId = x.LedgerId,
                AccountGroupId = x.AccountLedgerFk.AccountGroupId,
                VoucherName = x.VoucherTypeFk.Name
            }).OrderBy(x => x.AccountGroupId).ToListAsync();

        AccountLedgerList = await accountLedgerRepository.GetAll()
            .Where(e => e.TenantId == AbpSession.TenantId).AsNoTracking()
            .Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            })
            .ToListAsync();

        //start stock calculation
        var unitConversion = await unitConversionRepository.GetAll().AsNoTracking()
            .Include(x => x.UnitFk).Select(x => new
            {
                x.ProductId,
                x.UnitId,
                x.PrimaryQty,
                x.Qty,
                UnitName = x.UnitFk.Name,
                x.ConversionRate
            }).ToListAsync();
        var stockPostingList = await stockPostingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.FinancialYearId == FinancialYearId &&
                        x.TenantId == AbpSession.TenantId && x.Date <= toDate.Date && !x.IsDeleted)
            .Include(x => x.VoucherTypeFk)
            .Include(x => x.ProductFk)
            .Where(e => e.VoucherTypeFk.Name != "StockReceipt" || e.VoucherTypeFk.Name != "StockTransfer")
            .AsSplitQuery()
            .Select(x => new
            {
                x.ProductId,
                VoucherType = x.VoucherTypeFk.Name,
                x.Date,
                ProductName = x.ProductFk.Name,
                x.Amount,
                x.Rate,
                x.UnitId,
                x.InWardQty,
                x.OutWardQty
            }).ToListAsync();

        var posting = new List<NewProductReport>();
        foreach (var x in stockPostingList)
        {
            var product = new NewProductReport();
            var minUnit = unitConversion.Where(a => a.ProductId == x.ProductId).MinBy(a => a.ConversionRate);
            var unitConversionDetail =
                unitConversion.FirstOrDefault(a => a.ProductId == x.ProductId && a.UnitId == x.UnitId);
            if (minUnit == null)
            {
                var units = new UnitConversion
                {
                    Id = Guid.Empty,
                    IsDeleted = false,
                    ConversionRate = 1,
                    Qty = 1,
                    PrimaryQty = 1,
                    ProductId = x.ProductId,
                    UnitId = x.UnitId,
                    TenantId = AbpSession.TenantId
                };
                await unitConversionRepository.InsertAsync(units);
            }
            else
            {
                product.ProductId = x.ProductId;
                product.ProductName = x.ProductName;
                product.UnitId = minUnit.UnitId;
                product.VoucherType = x.VoucherType;
                product.Date = x.Date;
                if (unitConversionDetail != null)
                {
                    product.Rate = x.Rate * minUnit.PrimaryQty * unitConversionDetail.Qty /
                                   (minUnit.Qty * unitConversionDetail.PrimaryQty);
                    product.InWardQty = x.InWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                        (unitConversionDetail.Qty * minUnit.PrimaryQty);
                    product.OutWardQty = x.OutWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                         (unitConversionDetail.Qty * minUnit.PrimaryQty);
                    product.InWardValue = x.InWardQty * x.Rate;
                    product.OutWardValue = x.OutWardQty * x.Rate;
                }

                product.CurrentStock = product.InWardQty - product.OutWardQty;
                posting.Add(product);
            }
        }

        var openingStockList = stockPostingList.Where(x => x.InWardQty > 0 && x.VoucherType == "OpeningStock")
            .Select(x => new FinancialStatementDto
            {
                Id = x.ProductId,
                Data = new FinancialStatementDetail
                {
                    Id = x.ProductId,
                    Name = x.ProductName,
                    Debit = x.InWardQty * x.Rate,
                    Credit = 0,
                    GroupType = TrailBalanceGroupEnum.Product
                },
                Children = null
            }).ToList();

        foreach (var product in stockPostingList.DistinctBy(a => a.ProductId).ToList())
        {
            var productUniqueStock = posting.Where(p => p.ProductId == product.ProductId).ToList();
            if (!productUniqueStock.Any()) continue;
            decimal stockValue = 0;
            decimal stockQty = 0;
            if (stockCalculation.ToLower() == "fifo")
            {
                var datedProductUniqueStock = productUniqueStock.OrderBy(x => x.Date.Date).ToList();
                var soldQty = datedProductUniqueStock.Where(x => x.InWardQty == 0).Sum(x => x.OutWardQty);
                foreach (var datedProduct in datedProductUniqueStock)
                {
                    if (soldQty > datedProduct.InWardQty)
                    {
                        soldQty -= datedProduct.InWardQty;
                        continue;
                    }

                    if (soldQty <= datedProduct.InWardQty)
                    {
                        var qty = datedProduct.InWardQty - soldQty;
                        if (qty > 0)
                        {
                            stockValue += qty * datedProduct.Rate;
                            soldQty = 0;
                            continue;
                        }
                    }

                    if (soldQty == 0) stockValue += datedProduct.InWardValue;
                }
            }

            if (stockCalculation.ToLower() == "lifo")
            {
                var datedProductUniqueStock = productUniqueStock.OrderBy(x => x.Date.Date).ToList();
                var newList = new List<NewProductReport>();
                foreach (var pro in datedProductUniqueStock)
                {
                    if (pro.InWardQty == 0)
                    {
                        var tempQty = pro.OutWardQty;
                        while (tempQty > 0)
                        {
                            var last = newList.LastOrDefault();
                            if (last != null && last.InWardQty < tempQty)
                            {
                                newList.Remove(last);
                                tempQty -= last.InWardQty;
                                continue;
                            }

                            if (last == null || last.InWardQty < tempQty) continue;
                            newList.Remove(last);
                            last.InWardQty -= tempQty;
                            newList.Add(last);
                            tempQty = 0;
                        }
                    }

                    if (pro.OutWardQty == 0) newList.Add(pro);
                }

                stockValue = newList.Sum(x => x.InWardQty * x.Rate);
            }
            else
            {
                decimal productRate = 0;
                var inward = productUniqueStock
                    .Where(x => x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
                        or "PurchaseReturn")
                    .Where(x => x.InWardQty > 0).ToList();
                if (inward.Count > 0)
                {
                    var inWardAmt = inward.Sum(x => x.InWardValue);
                    var inWardQty = inward.Sum(x => x.InWardQty);
                    productRate = inWardAmt / inWardQty;
                }

                stockQty = productUniqueStock.Sum(b => b.InWardQty) -
                           productUniqueStock.Sum(b => b.OutWardQty);

                stockValue = productRate * stockQty;
            }

            if (stockValue == 0) continue;
            var productData = new FinancialStatementDto
            {
                Id = product.ProductId,
                Data = new FinancialStatementDetail
                {
                    Id = product.ProductId,
                    Name = product.ProductName + "  Qty-" + Math.Round(stockQty, 2),
                    Debit = 0,
                    Credit = Math.Round(stockValue, 2),
                    GroupType = TrailBalanceGroupEnum.Product
                }
            };
            ClStockReports.Add(productData);
        }

        var opStock = new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Opening Stock",
                Debit = openingStockList.Sum(x => x.Data.Debit),
                Credit = 0,
                GroupType = TrailBalanceGroupEnum.OpeningStock
            },
            Children = openingStockList.Where(x => x.Data.Debit > 0).ToList()
        };
        returnList.Add(opStock);
        var primaryAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Primary");

        var result = await FuncRecursiveGroupId(primaryAccountGroup.Id, true);
        returnList.AddRange(result);
        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Children = ClStockReports,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "" +
                       "Closing" +
                       "" +
                       "" +
                       " Stock",
                Credit = ClStockReports.Sum(x => x.Data.Credit),
                Debit = ClStockReports.Sum(x => x.Data.Debit),
                GroupType = TrailBalanceGroupEnum.ClosingStock
            }
        });
        var profitLossAmount = returnList.Sum(x => x.Data.Debit - x.Data.Credit);
        return profitLossAmount;
    }

    public async Task<List<FinancialStatementDto>> GetReport(string fromMiti = null, string toMiti = null)
    {
        var (_, toDate) = ResolveDateRange(fromMiti, toMiti);
        var returnList = new List<FinancialStatementDto>();
        var profitLossAmount = await CalculateProfitLossAmount(toDate);
        var primaryAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Primary");

        var opVoucherId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
        var stockQuery = stockPostingRepository.GetAll();

        var openingStockValue = await stockQuery.AsNoTracking()
            .Include(x => x.ProductFk).Include(x => x.UnitFk)
            .Where(x => x.InWardQty > 0 && x.VoucherTypeId == opVoucherId && x.FinancialYearId == FinancialYearId &&
                        x.TenantId == AbpSession.TenantId && x.Date <= toDate.Date && !x.IsDeleted)
            .SumAsync(x => x.InWardQty * x.Rate);

        var result = await FuncRecursiveGroupId(primaryAccountGroup.Id, false);
        returnList.AddRange(result);
        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = profitLossAmount >= 0 ? "Net Loss" : "Net Profit",
                Credit = profitLossAmount <= 0 ? Math.Abs(Math.Round(profitLossAmount, 2)) : 0,
                Debit = profitLossAmount >= 0 ? Math.Abs(Math.Round(profitLossAmount, 2)) : 0,
                GroupType = TrailBalanceGroupEnum.None
            },
            Children = null
        });

        var openingDifference = PostingList.Where(x => x.VoucherName == "OpeningBalance").Sum(x => x.Debit - x.Credit);

        var balance = Math.Round(openingDifference + openingStockValue, 2);
        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Differences in the opening balances",
                Debit = balance < 0 ? Math.Abs(balance) : 0,
                Credit = balance > 0 ? Math.Abs(balance) : 0,
                GroupType = TrailBalanceGroupEnum.None
            }
        });

        returnList.Add(new FinancialStatementDto
        {
            Id = Guid.Empty,
            Data = new FinancialStatementDetail
            {
                Id = Guid.Empty,
                Name = "Total ",
                Credit = returnList.Sum(x => x.Data.Debit),
                Debit = returnList.Sum(x => x.Data.Credit),
                GroupType = TrailBalanceGroupEnum.ClosingStock
            },
            Children = null
        });
        return returnList.Where(x => x.Data.Debit != 0 || x.Data.Credit != 0).ToList();
    }

    public async Task<FileDto> BalanceSheetExportToExcel(string fromMiti = null, string toMiti = null)
    {
        var result = new List<FinancialStatementDetail>();
        var dataList = await GetReport(fromMiti, toMiti);
        const int level = 0;
        foreach (var rep in dataList) result.AddRange(await GetRecursiveChild(rep, level));
        return excelExporter.ExportToFile(result);
    }

    public async Task<byte[]> GetPdfDownload(string fromMiti = null, string toMiti = null)
    {
        //var filePath = "BalanceSheet.pdf";
        var result = new List<FinancialStatementDetail>();
        var dataList = await GetReport(fromMiti, toMiti);
        var level = 0;
        foreach (var rep in dataList) result.AddRange(await GetRecursiveChild(rep, level));

        var branchInfo = await branchRepository.FirstOrDefaultAsync(x => x.IsMain && x.TenantId == AbpSession.TenantId);

        var report = new BalanceSheetPdfDto
        {
            FromMiti = fromMiti,
            ToMiti = toMiti,
            CompanyInfo = branchInfo,
            BalanceSheetDetails = result
        };
        var document = new BalanceSheetPdf(report);
        return document.GeneratePdf();
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //{
        //    Process.Start("explorer.exe", filePath);
        //}
        //else
        //{
        //    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        //    Console.WriteLine($"Output PDF file is available here: {fullPath}");
        //}
    }

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
            foreach (var rep in child.Children)
                result.AddRange(await GetRecursiveChild(rep, level));

        return result;
    }

    public async Task<List<UniversalDropdownDto>> GetAllAccountGroupsForTableDropdown()
    {
        return (await accountGroupRepository.GetAll().ToListAsync()).Select(x =>
            new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).ToList();
    }

    private (DateTime FromDate, DateTime ToDate) ResolveDateRange(string fromMiti, string toMiti)
    {
        var fromDate = FinancialYear.FromDate;
        var toDate = FinancialYear.ToDate;

        if (!string.IsNullOrWhiteSpace(fromMiti))
            fromDate = DateConverter.ConvertToEnglish(fromMiti);

        if (!string.IsNullOrWhiteSpace(toMiti))
            toDate = DateConverter.ConvertToEnglish(toMiti);

        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        return (fromDate, toDate);
    }

    private async Task<List<FinancialStatementDto>> FuncRecursiveGroupId(Guid accountGroupId, bool isProfitLoss)
    {
        var result = new List<FinancialStatementDto>();
        var accountGroups = accountGroupRepository.GetAll().AsNoTracking()
            .Where(x => x.GroupUnder == accountGroupId && x.Name != "Primary" && x.AffectGrossProfit == isProfitLoss);

        foreach (var accountGroup in await accountGroups.ToListAsync())
        {
            var ledgerPostings =
                PostingList.Where(x => x.AccountGroupId == accountGroup.Id).ToList();
            var calcAmount = ledgerPostings.Sum(x => x.Debit) - ledgerPostings.Sum(x => x.Credit);
            var data = new FinancialStatementDetail
            {
                Id = accountGroup.Id,
                Name = accountGroup.Name,
                Debit = calcAmount > 0 ? calcAmount : 0,
                Credit = calcAmount < 0 ? Math.Abs((int)calcAmount) : 0,
                GroupType = TrailBalanceGroupEnum.AccountGroup
            };
            var a = new FinancialStatementDto
            {
                Data = data,
                Id = accountGroup.Id
            };
            a.Children = a.Id != Guid.Empty ? await FuncRecursiveGroupId(accountGroup.Id, isProfitLoss) : null;

            var distinctLedgerIds = ledgerPostings.Select(x => x.LedgerId).Distinct().ToList();
            var ledgersList = new List<FinancialStatementDto>();
            foreach (var distinctLedgerId in distinctLedgerIds)
            {
                var m = new FinancialStatementDto();
                var calcLedgerAmount = ledgerPostings.Where(x => x.LedgerId == distinctLedgerId).Sum(x => x.Debit)
                                       - ledgerPostings.Where(x => x.LedgerId == distinctLedgerId).Sum(x => x.Credit);
                var ledger = new FinancialStatementDetail
                {
                    Id = distinctLedgerId,
                    Name = AccountLedgerList.FirstOrDefault(x => x.Id == distinctLedgerId)?.DisplayName,
                    Debit = calcLedgerAmount > 0 ? calcLedgerAmount : 0,
                    Credit = calcLedgerAmount < 0 ? Math.Abs((int)calcLedgerAmount) : 0,
                    GroupType = TrailBalanceGroupEnum.AccountLedger
                };
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
                    Name = "Closing Stock",
                    // OpeningAmount = OpStock.Sum(x=>x.Data.OpeningAmount),
                    Debit = ClStockReports.Sum(x => x.Data.Credit),
                    Credit = ClStockReports.Sum(x => x.Data.Debit),
                    GroupType = TrailBalanceGroupEnum.ClosingStock
                    //  Amount = 
                };
                // data.OpeningAmount += a.Data.OpeningAmount = a.Children.Sum(x => x.Data.OpeningAmount);
                data.Debit += ledger.Debit;
                data.Credit += ledger.Credit;
                m.Id = Guid.Empty;
                m.Data = ledger;
                m.Children = ClStockReports;
                ledgersList.Add(m);
            }

            if (a.Children != null)
            {
                if (a.Children.Count > 0)
                {
                    data.Debit += a.Data.Debit = a.Children.Sum(x => x.Data.Debit); /*+ a.DebitAmount*/
                    data.Credit += a.Data.Credit = a.Children.Sum(x => x.Data.Credit); /*+ a.CreditAmount*/
                }

                var fAmt = data.Debit - data.Credit;
                data.Debit = fAmt > 0 ? fAmt : 0;
                data.Credit = fAmt < 0 ? Math.Abs(fAmt) : 0;
                a.Children.AddRange(ledgersList);
            }

            result.Add(a);
        }

        return result;
    }
}
