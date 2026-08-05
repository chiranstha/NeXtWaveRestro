using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Purchase;
using NextWave.Erp.Sales;
using NextWave.Erp.Tenants.Dashboard.Dto;
using NextWave.Erp.Transaction;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.DashboardReport
{
    public class DashboardReportData(
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository
        ) : ErpAppServiceBase
    {
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
}
