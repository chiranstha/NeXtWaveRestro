using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Purchase;
using NextWave.Erp.Sales;
using NextWave.Erp.Tenants.Dashboard.Dto;
using NextWave.Erp.Transaction;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Tenants.Dashboard;

[DisableAuditing]
[AbpAuthorize(AppPermissions.Pages_Tenant_Dashboard)]
public class TenantDashboardAppService(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository
    //IRepository<SalesMaster, Guid> salesMasterRepository,
    //IRepository<SalesMaster, Guid> salesMasterRepository,
    ) : ErpAppServiceBase, ITenantDashboardAppService
{
    public GetMemberActivityOutput GetMemberActivity()
    {
        return new GetMemberActivityOutput
        (
            DashboardRandomDataGenerator.GenerateMemberActivities()
        );
    }

    public GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input)
    {
        var output = new GetDashboardDataOutput
        {
            TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
            NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
            NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
            NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500),
            SalesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod),
            Expenses = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
            Growth = DashboardRandomDataGenerator.GetRandomInt(5000, 10000),
            Revenue = DashboardRandomDataGenerator.GetRandomInt(1000, 9000),
            TotalSales = DashboardRandomDataGenerator.GetRandomInt(10000, 90000),
            TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
            NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
            BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
            DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50),
            ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
        };

        return output;
    }

    public GetTopStatsOutput GetTopStats()
    {
        return new GetTopStatsOutput
        {
            TotalProfit = DashboardRandomDataGenerator.GetRandomInt(5000, 9000),
            NewFeedbacks = DashboardRandomDataGenerator.GetRandomInt(1000, 5000),
            NewOrders = DashboardRandomDataGenerator.GetRandomInt(100, 900),
            NewUsers = DashboardRandomDataGenerator.GetRandomInt(50, 500)
        };
    }

    public GetProfitShareOutput GetProfitShare()
    {
        return new GetProfitShareOutput
        {
            ProfitShares = DashboardRandomDataGenerator.GetRandomPercentageArray(3)
        };
    }

    public GetDailySalesOutput GetDailySales()
    {
        return new GetDailySalesOutput
        {
            DailySales = DashboardRandomDataGenerator.GetRandomArray(30, 10, 50)
        };
    }

    public GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input)
    {
        var salesSummary = DashboardRandomDataGenerator.GenerateSalesSummaryData(input.SalesSummaryDatePeriod);
        return new GetSalesSummaryOutput(salesSummary)
        {
            Expenses = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
            Growth = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
            Revenue = DashboardRandomDataGenerator.GetRandomInt(0, 3000),
            TotalSales = DashboardRandomDataGenerator.GetRandomInt(0, 3000)
        };
    }

    public GetRegionalStatsOutput GetRegionalStats()
    {
        return new GetRegionalStatsOutput(
            DashboardRandomDataGenerator.GenerateRegionalStat()
        );
    }

    public GetGeneralStatsOutput GetGeneralStats()
    {
        return new GetGeneralStatsOutput
        {
            TransactionPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
            NewVisitPercent = DashboardRandomDataGenerator.GetRandomInt(10, 100),
            BouncePercent = DashboardRandomDataGenerator.GetRandomInt(10, 100)
        };
    }

    public async Task<GetRealDashboardDataOutput> GetRealDashboardData()
    {
        var result = new GetRealDashboardDataOutput
        {
            TotalSales = await GetTotalSalesAsync(),
            TotalPurchase = await GetTotalPurchaseAsync(),
            TotalCash = await GetTotalCashAsync(),
            TotalBank = await GetTotalBankAsync(),
            TotalExpenses = await GetTotalExpensesAsync(),
            TotalService = GetTotalServiceAsync()
        };
        return result;
    }

    private async Task<decimal> GetTotalSalesAsync()
    {
        var data = await salesMasterRepository.GetAll()
            .Where(x => x.FinancialYearId == FinancialYearId).SumAsync(x => x.GrandTotal);

        data += await salesReturnMasterRepository.GetAll().Where(x => x.FinancialYearId == FinancialYearId)
            .Select(x => x.DebitOrCreditNote ? x.GrandTotal : -x.GrandTotal).SumAsync();
        return data;
    }
    private async Task<decimal> GetTotalPurchaseAsync()
    {
        var data = purchaseMasterRepository.GetAll()
            .Where(x => x.FinancialYearId == FinancialYearId).Sum(x => x.GrandTotal);
        data += await purchaseReturnRepository.GetAll().Where(x => x.FinancialYearId == FinancialYearId)
             .Select(x => x.DebitOrCreditNote ? x.GrandTotal : -x.GrandTotal).SumAsync();
        return data;
    }
    private async Task<decimal> GetTotalCashAsync()
    {
        return await ledgerPostingRepository.GetAll().Include(x => x.AccountLedgerFk)
             .ThenInclude(x => x.AccountGroupFk)
             .Where(x => x.FinancialYearId == FinancialYearId &&
                         x.AccountLedgerFk.AccountGroupFk.Name == "Cash-in Hand")
             .SumAsync(x => x.Debit - x.Credit);
    }
    private async Task<decimal> GetTotalBankAsync()
    {
        return await ledgerPostingRepository.GetAll().Include(x => x.AccountLedgerFk)
            .ThenInclude(x => x.AccountGroupFk)
            .Where(x => x.FinancialYearId == FinancialYearId &&
                        x.AccountLedgerFk.AccountGroupFk.Name == "Bank Account")
            .SumAsync(x => x.Debit - x.Credit);
    }
    private async Task<decimal> GetTotalExpensesAsync()
    {
        return await ledgerPostingRepository.GetAll().Include(x => x.AccountLedgerFk)
            .ThenInclude(x => x.AccountGroupFk)
            .Where(x => (x.FinancialYearId == FinancialYearId &&
                         x.AccountLedgerFk.AccountGroupFk.Name == "Direct Expenses") ||
                        x.AccountLedgerFk.AccountGroupFk.Name == "Indirect Expenses")
            .SumAsync(x => x.Debit - x.Credit);
    }
    private decimal GetTotalServiceAsync()
    {
        // Implement your logic to get total service from the database
        return 0.00m;
    }

}
