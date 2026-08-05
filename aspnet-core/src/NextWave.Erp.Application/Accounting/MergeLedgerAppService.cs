using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Sales;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting
{
    [AbpAuthorize(AppPermissions.PagesMergeLedger)]
    public class MergeLedgerAppService(
    IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<SalesMaster, Guid> salesMasterRepository,
    IRepository<PartyBalance, Guid> partyBalanceRepository,
    IRepository<ContraDetail, Guid> contraDetailsRepository,
    IRepository<PaymentMaster, Guid> paymentMasterRepository,
    IRepository<PurchaseOrderMaster, Guid> purchaseOrderMasterRepository,
    IRepository<SalesProductCancelMaster, Guid> salesProductCancelMasterRepository,
    IRepository<ReceiptMaster, Guid> receiptMasterRepository,
    IRepository<PaymentDetail, Guid> paymentDetailsRepository,
    IRepository<Tax, Guid> taxRepository,
    IRepository<PurchaseProductCancelMaster, Guid> purchaseProductCancelMasterRepository,
    IRepository<JournalDetail, Guid> journalDetailsRepository,
    IRepository<ReceiptDetail, Guid> receiptDetailsRepository,
    IRepository<AdditionalCost, Guid> additionalCostRepository,
    IRepository<ContraMaster, Guid> contraMasterRepository,
    IRepository<StockPosting, Guid> stockPostingRepository,
    IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
    IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository)
    : ErpAppServiceBase
    {
        public async Task PostMergeLedger(Guid requestLedgerId, Guid ledgerId)
        {
            var accountledger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == requestLedgerId);
            if (accountledger == null) throw new UserFriendlyException("Request Ledger is not Found");
           

            foreach (var x in await purchaseMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await purchaseMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await salesMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await salesMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await partyBalanceRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await partyBalanceRepository.UpdateAsync(x);
            }


            foreach (var x in await contraDetailsRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await contraDetailsRepository.UpdateAsync(x);
            }

            foreach (var x in await paymentMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await paymentMasterRepository.UpdateAsync(x);
            }


            foreach (var x in await purchaseOrderMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await purchaseOrderMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await salesProductCancelMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await salesProductCancelMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await receiptMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await receiptMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await paymentDetailsRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await paymentDetailsRepository.UpdateAsync(x);
            }


            foreach (var x in await taxRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await taxRepository.UpdateAsync(x);
            }


            foreach (var x in await purchaseProductCancelMasterRepository.GetAllListAsync(
                         x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await purchaseProductCancelMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await journalDetailsRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await journalDetailsRepository.UpdateAsync(x);
            }

            foreach (var x in await receiptDetailsRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await receiptDetailsRepository.UpdateAsync(x);
            }

            foreach (var x in await additionalCostRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await additionalCostRepository.UpdateAsync(x);
            }


            foreach (var x in await contraMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await contraMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await stockPostingRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await stockPostingRepository.UpdateAsync(x);
            }

            foreach (var x in await salesReturnMasterRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await salesReturnMasterRepository.UpdateAsync(x);
            }

            foreach (var x in await purchaseReturnRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await purchaseReturnRepository.UpdateAsync(x);
            }


            foreach (var x in await ledgerPostingRepository.GetAllListAsync(x => x.LedgerId == requestLedgerId))
            {
                x.LedgerId = ledgerId;
                await ledgerPostingRepository.UpdateAsync(x);
            }

            accountledger.IsDelete = true;
            await accountLedgerRepository.UpdateAsync(accountledger);
        }


        public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllRequestLedger()
        {
            return await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors" || x.AccountGroupFk.Name == "Sundry Debtors")
                .Where(x => !x.IsDelete)
                .Select(x => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = x.Id,
                    DisplayName = x.Name,
                    MobileNo = x.Phone,
                    PanNo = x.Pan,
                    Address = x.Address
                }).ToListAsync();
        }


        public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllAccountLedger(Guid requestLedgerId)
        {
            return await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors" ||
                            (x.AccountGroupFk.Name == "Sundry Debtors" && x.Id != requestLedgerId))
                .Where(x => !x.IsDelete)
                .Select(x => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = x.Id,
                    DisplayName = x.Name,
                    MobileNo = x.Phone,
                    PanNo = x.Pan,
                    Address = x.Address
                }).ToListAsync();
        }
    }
}
