using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase;
using NextWave.Erp.Sales;
using NextWave.Erp.Transaction;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.ControlPanel
{
    public class PartyBalanceService(
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
        IRepository<SalesReturnMaster, Guid> salesReturnRepository,
        IRepository<PaymentMaster, Guid> paymentMasterRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<ReceiptMaster, Guid> receiptMasterRepository,
        IRepository<NewPartyBalance, Guid> newPartyBalanceRepository) : ErpAppServiceBase
    {

        // Purchase
        public async Task CreatePurchaseEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

            var partyBalanceEntry = new NewPartyBalance
            {
                Date = input.Date,
                DueDate = input.DueDate,
                LedgerId = input.LedgerId,
                FinancialYearId = FinancialYearId,
                VoucherNo = input.VoucherNo,
                VoucherNumbering = input.VoucherNumbering,
                VoucherTypeId = voucherTypeId,
                AgainstVoucherNo = string.Empty,
                AgainstVoucherNumbering = 0,
                AgainstVoucherTypeId = Guid.Empty,
                Debit = 0,
                Credit = input.Amount,
                IsMain = true,
                IsFullySettled = false,
                IsPartiallySettled = false,
                MasterId = input.MasterId,
                DetailId = Guid.Empty,
                MasterPartyBalanceId = null,
                TenantId = AbpSession.TenantId,
            };
            await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
        }

        public async Task UpdatePurchaseEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

            var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherTypeId == voucherTypeId
                && x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId).ToListAsync();

            var previousData = relatedData.Where(x => x.MasterId == input.MasterId).ToList();
            if (previousData.Count != 0)
            {
                var settledBalances = previousData.Where(x => x.DetailId != Guid.Empty);
                if (settledBalances.Count() > 0)
                    throw new UserFriendlyException("This voucher is already settled.");
                var mainEntry = previousData.FirstOrDefault(x => x.IsMain);
                var ledgerId = mainEntry?.LedgerId;
                if (input.LedgerId != ledgerId)
                {
                    foreach (var partyBalance in previousData.Where(x => x.DetailId == Guid.Empty))
                    {
                        await newPartyBalanceRepository.DeleteAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.DueDate,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = voucherTypeId,
                        AgainstVoucherNo = string.Empty,
                        AgainstVoucherNumbering = 0,
                        AgainstVoucherTypeId = Guid.Empty,
                        Debit = 0,
                        Credit = input.Amount,
                        IsMain = true,
                        IsFullySettled = false,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = Guid.Empty,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
                else
                {
                    if (input.Amount > previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit))
                    {
                        foreach (var newPartyBalance in relatedData)
                        {
                            newPartyBalance.IsFullySettled = false;
                            await newPartyBalanceRepository.UpdateAsync(newPartyBalance);
                        }

                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = input.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = string.Empty,
                            AgainstVoucherNumbering = 0,
                            AgainstVoucherTypeId = Guid.Empty,
                            Debit = 0,
                            Credit = input.Amount - previousData.Where(x => x.DetailId == Guid.Empty)
                                .Sum(x => x.Credit - x.Debit),
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                    else if (input.Amount <
                             previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit))
                    {
                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = input.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = string.Empty,
                            AgainstVoucherNumbering = 0,
                            AgainstVoucherTypeId = Guid.Empty,
                            Debit = previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit) -
                                    input.Amount,
                            Credit = 0,
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                }

            }
        }

        public async Task DeletePurchaseEntryAsync(Guid id)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
            var partyBalanceEntry = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.MasterId == id && x.VoucherTypeId == voucherTypeId && x.FinancialYearId == FinancialYearId)
                .ToListAsync();
            foreach (var partyBalance in partyBalanceEntry)
            {
                if (partyBalance != null)
                {
                    await newPartyBalanceRepository.DeleteAsync(partyBalance);
                }
            }
        }

        // Purchase Return
        public async Task CreatePurchaseReturnEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var purchase = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == input.AgainstId);
            if (purchase != null)
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.DueDate,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = purchase.VoucherNo,
                    VoucherNumbering = purchase.VoucherNumbering,
                    VoucherTypeId = purchase.VoucherTypeId,
                    AgainstVoucherNo = input.VoucherNo,
                    AgainstVoucherNumbering = input.VoucherNumbering,
                    AgainstVoucherTypeId = voucherTypeId,
                    Debit = input.Amount,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = Guid.Empty,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
            else
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.DueDate,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = input.VoucherNumbering,
                    VoucherTypeId = voucherTypeId,
                    AgainstVoucherNo = string.Empty,
                    AgainstVoucherNumbering = 0,
                    AgainstVoucherTypeId = Guid.Empty,
                    Debit = input.Amount,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = Guid.Empty,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task UpdatePurchaseReturnEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var purchaseReturn = await purchaseReturnRepository.FirstOrDefaultAsync(x => x.Id == input.MasterId);
            var purchase =
                await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturn.PurchaseMasterId);

            var previousData = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == voucherTypeId && x.VoucherNo == purchase.VoucherNo && x.MasterId == input.MasterId &&
                x.AgainstVoucherTypeId == againstVoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
            if (previousData.Count != 0)
            {
                var settledBalances = previousData.Where(x => x.DetailId != Guid.Empty);
                if (settledBalances.Count() > 0)
                    throw new UserFriendlyException("This voucher is already settled.");
                var mainEntry = previousData.FirstOrDefault(x => x.IsMain);
                var ledgerId = mainEntry?.LedgerId;
                if (input.LedgerId != ledgerId)
                {
                    foreach (var partyBalance in previousData.Where(x => x.DetailId == Guid.Empty))
                    {
                        await newPartyBalanceRepository.DeleteAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.DueDate,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = purchase.VoucherNo,
                        VoucherNumbering = purchase.VoucherNumbering,
                        VoucherTypeId = purchase.VoucherTypeId,
                        AgainstVoucherNo = input.VoucherNo,
                        AgainstVoucherNumbering = input.VoucherNumbering,
                        AgainstVoucherTypeId = againstVoucherTypeId,
                        Debit = input.Amount,
                        Credit = 0,
                        IsMain = true,
                        IsFullySettled = false,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = Guid.Empty,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
                else
                {
                    if (input.Amount > previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit))
                    {
                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = purchase.VoucherNo,
                            VoucherNumbering = purchase.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = input.VoucherNo,
                            AgainstVoucherNumbering = input.VoucherNumbering,
                            AgainstVoucherTypeId = againstVoucherTypeId,
                            Debit = input.Amount - previousData.Where(x => x.DetailId == Guid.Empty)
                                .Sum(x => x.Debit - x.Credit),
                            Credit = 0,
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                    else if (input.Amount <
                             previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit))
                    {
                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = input.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = string.Empty,
                            AgainstVoucherNumbering = 0,
                            AgainstVoucherTypeId = Guid.Empty,
                            Debit = 0,
                            Credit = previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit) -
                                     input.Amount,
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                }

            }
        }

        public async Task DeletePurchaseReturnEntryAsync(Guid id)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var partyBalanceEntry = await newPartyBalanceRepository.FirstOrDefaultAsync(x =>
                x.MasterId == id && x.AgainstVoucherTypeId == voucherTypeId && x.FinancialYearId == FinancialYearId);
            if (partyBalanceEntry != null)
            {
                await newPartyBalanceRepository.DeleteAsync(partyBalanceEntry);
            }
        }

        // Sales
        public async Task CreateSalesEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var partyBalanceEntry = new NewPartyBalance
            {
                Date = input.Date,
                DueDate = input.DueDate,
                LedgerId = input.LedgerId,
                FinancialYearId = FinancialYearId,
                VoucherNo = input.VoucherNo,
                VoucherNumbering = input.VoucherNumbering,
                VoucherTypeId = voucherTypeId,
                AgainstVoucherNo = string.Empty,
                AgainstVoucherNumbering = 0,
                AgainstVoucherTypeId = Guid.Empty,
                Debit = input.Amount,
                Credit = 0,
                IsMain = true,
                IsFullySettled = false,
                IsPartiallySettled = false,
                MasterId = input.MasterId,
                DetailId = Guid.Empty,
                MasterPartyBalanceId = null,
                TenantId = AbpSession.TenantId,
            };
            await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
        }

        public async Task UpdateSalesEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherTypeId == voucherTypeId
                 && x.VoucherNo == input.VoucherNo && x.VoucherNumbering == input.VoucherNumbering && x.FinancialYearId == FinancialYearId).ToListAsync();
            var previousData = relatedData.Where(x => x.MasterId == input.MasterId).ToList();
            if (previousData.Count() != 0)
            {
                var mainEntry = previousData.FirstOrDefault(x => x.IsMain);
                var ledgerId = mainEntry?.LedgerId;
                if (input.LedgerId != ledgerId)
                {
                    foreach (var partyBalance in previousData.Where(x => x.DetailId == Guid.Empty))
                    {
                        await newPartyBalanceRepository.DeleteAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.DueDate,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = voucherTypeId,
                        AgainstVoucherNo = string.Empty,
                        AgainstVoucherNumbering = 0,
                        AgainstVoucherTypeId = Guid.Empty,
                        Debit = input.Amount,
                        Credit = 0,
                        IsMain = true,
                        IsFullySettled = false,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = Guid.Empty,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
                else
                {
                    if (input.Amount > previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit))
                    {
                        foreach (var newPartyBalance in relatedData)
                        {
                            newPartyBalance.IsFullySettled = false;
                            await newPartyBalanceRepository.UpdateAsync(newPartyBalance);
                        }
                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = input.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = string.Empty,
                            AgainstVoucherNumbering = 0,
                            AgainstVoucherTypeId = Guid.Empty,
                            Debit = input.Amount - previousData.Where(x => x.DetailId == Guid.Empty)
                                .Sum(x => x.Debit - x.Credit),
                            Credit = 0,
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                    else if (input.Amount <
                             previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit))
                    {
                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = input.VoucherNumbering,
                            VoucherTypeId = voucherTypeId,
                            AgainstVoucherNo = string.Empty,
                            AgainstVoucherNumbering = 0,
                            AgainstVoucherTypeId = Guid.Empty,
                            Debit = 0,
                            Credit = previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit) -
                                     input.Amount,
                            IsMain = false,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = mainEntry.Id,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                }

            }
        }

        public async Task DeleteSalesEntryAsync(Guid id)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var partyBalanceEntry = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.MasterId == id && x.VoucherTypeId == voucherTypeId && x.FinancialYearId == FinancialYearId)
                .ToListAsync();
            foreach (var partyBalance in partyBalanceEntry)
            {
                await newPartyBalanceRepository.DeleteAsync(partyBalance);
            }
        }

        // Sales Return
        public async Task CreateSalesReturnEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            var salesVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var sales = await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == input.AgainstId);

            var partyBalanceEntry = new NewPartyBalance
            {
                Date = input.Date,
                DueDate = input.DueDate,
                LedgerId = input.LedgerId,
                FinancialYearId = FinancialYearId,
                VoucherNo = sales?.VoucherNo,
                VoucherNumbering = sales == null ? 0 : sales.VoucherNumbering,
                VoucherTypeId = sales == null ? salesVoucherTypeId : sales.VoucherTypeId,
                AgainstVoucherNo = input.VoucherNo,
                AgainstVoucherNumbering = input.VoucherNumbering,
                AgainstVoucherTypeId = voucherTypeId,
                Debit = 0,
                Credit = input.Amount,
                IsMain = true,
                IsFullySettled = false,
                IsPartiallySettled = false,
                MasterId = input.MasterId,
                DetailId = Guid.Empty,
                MasterPartyBalanceId = null,
                TenantId = AbpSession.TenantId,
            };
            await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
        }

        public async Task UpdateSalesReturnEntryAsync(PartyBalanceNewEntryDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");

            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            var salesReturn = await salesReturnRepository.FirstOrDefaultAsync(x => x.Id == input.MasterId);
            SalesMaster sales =
                await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == salesReturn.SalesMasterId);
            if (sales != null)
            {
                var previousData = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == voucherTypeId && x.VoucherNo == sales.VoucherNo && x.MasterId == input.MasterId &&
                x.AgainstVoucherTypeId == againstVoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                if (previousData.Count != 0)
                {
                    var settledBalances = previousData.Where(x => x.DetailId != Guid.Empty);
                    if (settledBalances.Count() > 0)
                        throw new UserFriendlyException("This voucher is already settled.");
                    var mainEntry = previousData.FirstOrDefault(x => x.IsMain);
                    var ledgerId = mainEntry?.LedgerId;
                    if (input.LedgerId != ledgerId)
                    {
                        foreach (var partyBalance in previousData.Where(x => x.DetailId == Guid.Empty))
                        {
                            await newPartyBalanceRepository.DeleteAsync(partyBalance);
                        }

                        var partyBalanceEntry = new NewPartyBalance
                        {
                            Date = input.Date,
                            DueDate = input.DueDate,
                            LedgerId = input.LedgerId,
                            FinancialYearId = FinancialYearId,
                            VoucherNo = sales.VoucherNo,
                            VoucherNumbering = sales.VoucherNumbering,
                            VoucherTypeId = sales.VoucherTypeId,
                            AgainstVoucherNo = input.VoucherNo,
                            AgainstVoucherNumbering = input.VoucherNumbering,
                            AgainstVoucherTypeId = againstVoucherTypeId,
                            Debit = 0,
                            Credit = input.Amount,
                            IsMain = true,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = input.MasterId,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = null,
                            TenantId = AbpSession.TenantId,
                        };
                        await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    }
                    else
                    {
                        if (input.Amount > previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit))
                        {
                            var partyBalanceEntry = new NewPartyBalance
                            {
                                Date = input.Date,
                                DueDate = input.DueDate,
                                LedgerId = input.LedgerId,
                                FinancialYearId = FinancialYearId,
                                VoucherNo = sales.VoucherNo,
                                VoucherNumbering = sales.VoucherNumbering,
                                VoucherTypeId = voucherTypeId,
                                AgainstVoucherNo = input.VoucherNo,
                                AgainstVoucherNumbering = input.VoucherNumbering,
                                AgainstVoucherTypeId = againstVoucherTypeId,
                                Debit = 0,
                                Credit = input.Amount - previousData.Where(x => x.DetailId == Guid.Empty)
                                    .Sum(x => x.Credit - x.Debit),
                                IsMain = false,
                                IsFullySettled = false,
                                IsPartiallySettled = false,
                                MasterId = input.MasterId,
                                DetailId = Guid.Empty,
                                MasterPartyBalanceId = mainEntry.Id,
                                TenantId = AbpSession.TenantId,
                            };
                            await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                        }
                        else if (input.Amount <
                                 previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit))
                        {
                            var partyBalanceEntry = new NewPartyBalance
                            {
                                Date = input.Date,
                                DueDate = input.DueDate,
                                LedgerId = input.LedgerId,
                                FinancialYearId = FinancialYearId,
                                VoucherNo = input.VoucherNo,
                                VoucherNumbering = input.VoucherNumbering,
                                VoucherTypeId = voucherTypeId,
                                AgainstVoucherNo = sales.VoucherNo,
                                AgainstVoucherNumbering = sales.VoucherNumbering,
                                AgainstVoucherTypeId = againstVoucherTypeId,
                                Debit = previousData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit) -
                                        input.Amount,
                                Credit = 0,
                                IsMain = false,
                                IsFullySettled = false,
                                IsPartiallySettled = false,
                                MasterId = input.MasterId,
                                DetailId = Guid.Empty,
                                MasterPartyBalanceId = mainEntry.Id,
                                TenantId = AbpSession.TenantId,
                            };
                            await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                        }
                    }
                }
            }
            else
            {
                var salesVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                await newPartyBalanceRepository.DeleteAsync(x => x.MasterId == input.MasterId && x.VoucherTypeId == salesVoucherTypeId
                && x.AgainstVoucherNo == input.VoucherNo && x.AgainstVoucherTypeId == voucherTypeId && x.LedgerId == input.LedgerId && x.FinancialYearId == FinancialYearId);
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.DueDate,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = "",
                    VoucherNumbering = 0,
                    VoucherTypeId = salesVoucherTypeId,
                    AgainstVoucherNo = input.VoucherNo,
                    AgainstVoucherNumbering = input.VoucherNumbering,
                    AgainstVoucherTypeId = voucherTypeId,
                    Debit = 0,
                    Credit = input.Amount,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = Guid.Empty,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task DeleteSalesReturnEntryAsync(Guid id)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var partyBalanceEntry = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.MasterId == id && x.VoucherTypeId == voucherTypeId && x.FinancialYearId == FinancialYearId)
                .OrderBy(x => x)
                .ToListAsync();
            foreach (var partyBalance in partyBalanceEntry)
            {
                await newPartyBalanceRepository.DeleteAsync(partyBalance);
            }
        }

        // Payment
        public async Task CreatePaymentTaskAsync(AdjustmentEntryDto input)
        {
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PaymentVoucher");
            if (input.OnAccountPaid > 0)
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.MasterVoucherNo,
                    VoucherNumbering = input.MasterVoucherNumbering,
                    VoucherTypeId = againstVoucherTypeId,
                    AgainstVoucherNo = "",
                    AgainstVoucherNumbering = 0,
                    AgainstVoucherTypeId = Guid.Empty,
                    Debit = input.OnAccountPaid,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                return;
            }

            var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering
                                                       && x.VoucherNo == input.VoucherNo &&
                                                       x.FinancialYearId == FinancialYearId).ToListAsync();
            var totalAmount = getPartyBalances.Sum(x => x.Credit - x.Debit);
            if (totalAmount <= input.Amount)
            {
                foreach (var partyBalance in getPartyBalances)
                {
                    partyBalance.IsFullySettled = true;
                    await newPartyBalanceRepository.UpdateAsync(partyBalance);
                }

                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = input.VoucherNumbering,
                    VoucherTypeId = input.VoucherTypeId,
                    AgainstVoucherNo = input.MasterVoucherNo,
                    AgainstVoucherNumbering = input.MasterVoucherNumbering,
                    AgainstVoucherTypeId = againstVoucherTypeId,
                    Debit = input.Amount,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = true,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
            else
            {
                foreach (var partyBalance in getPartyBalances)
                {
                    partyBalance.IsPartiallySettled = true;
                    await newPartyBalanceRepository.UpdateAsync(partyBalance);
                }

                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = input.VoucherNumbering,
                    VoucherTypeId = input.VoucherTypeId,
                    AgainstVoucherNo = input.MasterVoucherNo,
                    AgainstVoucherNumbering = input.MasterVoucherNumbering,
                    AgainstVoucherTypeId = againstVoucherTypeId,
                    Debit = input.Amount,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task UpdatePaymentTaskAsync(AdjustmentEntryDto input)
        {
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PaymentVoucher");
            if (input.Id != null && input.Id != Guid.Empty)
            {
                var partyBalanceData = await newPartyBalanceRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
                if (partyBalanceData != null)
                {
                    if (input.OnAccountPaid > 0)
                    {
                        partyBalanceData.Debit = input.OnAccountPaid;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                        return;
                    }

                    var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                        x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering &&
                        x.DetailId == Guid.Empty
                        && x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId).ToListAsync();
                    var totalAmount = getPartyBalances.Sum(x => x.Credit - x.Debit);
                    if (totalAmount <= input.Amount)
                    {
                        foreach (var partyBalance in getPartyBalances)
                        {
                            partyBalance.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(partyBalance);
                        }

                        partyBalanceData.Debit = input.Amount;
                        partyBalanceData.IsFullySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                    }
                    else
                    {
                        foreach (var partyBalance in getPartyBalances)
                        {
                            partyBalance.IsFullySettled = false;
                            partyBalance.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(partyBalance);
                        }

                        partyBalanceData.Debit = input.Amount;
                        partyBalanceData.IsFullySettled = false;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                    }
                }
            }
            else
            {
                var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering &&
                    x.DetailId == Guid.Empty
                    && x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId).ToListAsync();
                var totalAmount = getPartyBalances.Sum(x => x.Credit - x.Debit);
                if (totalAmount <= input.Amount)
                {
                    foreach (var partyBalance in getPartyBalances)
                    {
                        partyBalance.IsFullySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.Date,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = input.VoucherTypeId,
                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = againstVoucherTypeId,
                        Debit = input.Amount,
                        Credit = 0,
                        IsMain = true,
                        IsFullySettled = true,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
                else
                {
                    foreach (var partyBalance in getPartyBalances)
                    {
                        partyBalance.IsFullySettled = false;
                        partyBalance.IsPartiallySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.Date,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = input.VoucherTypeId,

                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = againstVoucherTypeId,
                        Debit = input.Amount,
                        Credit = 0,
                        IsMain = true,
                        IsFullySettled = false,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
            }
        }

        public async Task DeletePaymentTaskAsync(Guid id)
        {
            var payment = await paymentMasterRepository.FirstOrDefaultAsync(x => x.Id == id);
            var allBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.AgainstVoucherTypeId == payment.VoucherTypeId && x.AgainstVoucherNo == payment.VoucherNo &&
                    x.AgainstVoucherNumbering == payment.VoucherNumbering && x.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var allPaymentEntries = allBalances.Where(x => x.MasterId == id).ToList();
            var allOnAccount = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == payment.VoucherTypeId && x.VoucherNo == payment.VoucherNo &&
                x.VoucherNumbering == payment.VoucherNumbering && x.FinancialYearId == FinancialYearId &&
                x.MasterId == id).ToListAsync();

            foreach (var balance in allBalances)
            {
                balance.IsFullySettled = false;
                await newPartyBalanceRepository.UpdateAsync(balance);
            }

            if (allPaymentEntries != null || allOnAccount != null)
            {
                if (allPaymentEntries.Count > 0)
                    foreach (var allPaymentEntry in allPaymentEntries)
                    {
                        await newPartyBalanceRepository.DeleteAsync(allPaymentEntry);
                    }

                if (allOnAccount.Count > 0)
                    foreach (var newPartyBalance in allOnAccount)
                    {
                        await newPartyBalanceRepository.DeleteAsync(newPartyBalance);
                    }
            }
        }

        // Receipt
        public async Task CreateReceiptTaskAsync(AdjustmentEntryDto input)
        {
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            if (input.OnAccountPaid > 0)
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.MasterVoucherNo,
                    VoucherNumbering = input.MasterVoucherNumbering,
                    VoucherTypeId = againstVoucherTypeId,
                    AgainstVoucherNo = "",
                    AgainstVoucherNumbering = 0,
                    AgainstVoucherTypeId = Guid.Empty,
                    Debit = 0,
                    Credit = input.OnAccountPaid,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                return;
            }

            var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering
                                                       && x.VoucherNo == input.VoucherNo &&
                                                       x.FinancialYearId == FinancialYearId).ToListAsync();
            var totalAmount = getPartyBalances.Sum(x => x.Debit - x.Credit);
            if (totalAmount <= input.Amount)
            {
                foreach (var partyBalance in getPartyBalances)
                {
                    partyBalance.IsFullySettled = true;
                    await newPartyBalanceRepository.UpdateAsync(partyBalance);
                }

                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = input.VoucherNumbering,
                    VoucherTypeId = input.VoucherTypeId,
                    AgainstVoucherNo = input.MasterVoucherNo,
                    AgainstVoucherNumbering = input.MasterVoucherNumbering,
                    AgainstVoucherTypeId = againstVoucherTypeId,
                    Debit = 0,
                    Credit = input.Amount,
                    IsMain = true,
                    IsFullySettled = true,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
            else
            {
                foreach (var partyBalance in getPartyBalances)
                {
                    partyBalance.IsPartiallySettled = true;
                    await newPartyBalanceRepository.UpdateAsync(partyBalance);
                }

                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = input.VoucherNumbering,
                    VoucherTypeId = input.VoucherTypeId,
                    AgainstVoucherNo = input.MasterVoucherNo,
                    AgainstVoucherNumbering = input.MasterVoucherNumbering,
                    AgainstVoucherTypeId = againstVoucherTypeId,
                    Debit = 0,
                    Credit = input.Amount,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task UpdateReceiptTaskAsync(AdjustmentEntryDto input)
        {
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            if (input.Id != null && input.Id != Guid.Empty)
            {
                var partyBalanceData = await newPartyBalanceRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
                if (partyBalanceData != null)
                {
                    if (input.OnAccountPaid > 0)
                    {
                        partyBalanceData.Credit = input.OnAccountPaid;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                        return;
                    }

                    var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                        x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering &&
                        x.DetailId == Guid.Empty
                        && x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId).ToListAsync();
                    var totalAmount = getPartyBalances.Sum(x => x.Credit - x.Debit);
                    if (totalAmount <= input.Amount)
                    {
                        foreach (var partyBalance in getPartyBalances)
                        {
                            partyBalance.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(partyBalance);
                        }

                        partyBalanceData.Credit = input.Amount;
                        partyBalanceData.IsFullySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                    }
                    else
                    {
                        foreach (var partyBalance in getPartyBalances)
                        {
                            partyBalance.IsFullySettled = false;
                            partyBalance.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(partyBalance);
                        }

                        partyBalanceData.Credit = input.Amount;
                        partyBalanceData.IsFullySettled = false;
                        await newPartyBalanceRepository.UpdateAsync(partyBalanceData);
                    }
                }
            }
            else
            {
                var getPartyBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.VoucherTypeId == input.VoucherTypeId && x.VoucherNumbering == input.VoucherNumbering &&
                    x.DetailId == Guid.Empty
                    && x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId).ToListAsync();
                var totalAmount = getPartyBalances.Sum(x => x.Debit - x.Credit);
                if (totalAmount <= input.Amount)
                {
                    foreach (var partyBalance in getPartyBalances)
                    {
                        partyBalance.IsFullySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.Date,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = input.VoucherTypeId,

                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = againstVoucherTypeId,
                        Debit = 0,
                        Credit = input.Amount,
                        IsMain = true,
                        IsFullySettled = true,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
                else
                {
                    foreach (var partyBalance in getPartyBalances)
                    {
                        partyBalance.IsFullySettled = false;
                        partyBalance.IsPartiallySettled = true;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }

                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = input.Date,
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = input.VoucherNo,
                        VoucherNumbering = input.VoucherNumbering,
                        VoucherTypeId = input.VoucherTypeId,

                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = againstVoucherTypeId,
                        Debit = 0,
                        Credit = input.Amount,
                        IsMain = true,
                        IsFullySettled = false,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                }
            }
        }

        public async Task DeleteReceiptTaskAsync(Guid id)
        {
            var receipt = await receiptMasterRepository.FirstOrDefaultAsync(x => x.Id == id);
            var allBalances = await newPartyBalanceRepository.GetAll().Where(x =>
                    x.AgainstVoucherTypeId == receipt.VoucherTypeId && x.AgainstVoucherNo == receipt.VoucherNo &&
                    x.AgainstVoucherNumbering == receipt.VoucherNumbering && x.FinancialYearId == FinancialYearId)
                .ToListAsync();
            var allReceiptEntries = allBalances.Where(x => x.MasterId == id).ToList();
            var allOnAccount = await newPartyBalanceRepository.GetAll().Where(x =>
                x.VoucherTypeId == receipt.VoucherTypeId && x.VoucherNo == receipt.VoucherNo &&
                x.VoucherNumbering == receipt.VoucherNumbering && x.FinancialYearId == FinancialYearId &&
                x.MasterId == id).ToListAsync();

            foreach (var balance in allBalances)
            {
                balance.IsFullySettled = false;
                await newPartyBalanceRepository.UpdateAsync(balance);
            }

            if (allReceiptEntries != null || allOnAccount != null)
            {
                if (allReceiptEntries.Count > 0)
                    foreach (var allReceiptEntry in allReceiptEntries)
                    {
                        await newPartyBalanceRepository.DeleteAsync(allReceiptEntry);
                    }

                if (allOnAccount.Count > 0)
                    foreach (var newPartyBalance in allOnAccount)
                    {
                        await newPartyBalanceRepository.DeleteAsync(newPartyBalance);
                    }
            }
        }

        public async Task<List<UniversalDropdownDto>> GetAllRemainingPayments(Guid ledgerId,
            PaymentOptions paymentTypes)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll()
                .Where(x => !x.IsFullySettled && x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId);

            if (paymentTypes == PaymentOptions.Purchase)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "PurchaseInvoice");
            if (paymentTypes == PaymentOptions.Receipt)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "ReceiptVoucher");

            var partyBalances = await partyBalanceQuery.ToListAsync();
            var mains = partyBalances.Where(x => x.IsMain && x.AgainstVoucherTypeId == Guid.Empty).ToList();
            var results = new List<UniversalDropdownDto>();

            results.AddRange(mains.Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.VoucherNo
            }));

            return results;
        }

        public async Task<decimal> GetAllRemainingPaymentBalance(Guid ledgerId, PaymentOptions paymentTypes,
            string voucherNo)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll()
                .Where(x => !x.IsFullySettled && x.LedgerId == ledgerId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId);
            if (paymentTypes == PaymentOptions.Purchase)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "PurchaseInvoice");
            if (paymentTypes == PaymentOptions.Receipt)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "ReceiptVoucher");
            var partyBalances = await partyBalanceQuery.ToListAsync();
            return partyBalances.Sum(x => x.Credit - x.Debit);
        }

        public async Task<List<UniversalDropdownDto>> GetAllRemainingReceiptss(Guid ledgerId,
            PaymentOptions paymentTypes)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll()
                .Where(x => !x.IsFullySettled && x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId);

            if (paymentTypes == PaymentOptions.Purchase)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "SalesInvoice");
            if (paymentTypes == PaymentOptions.Receipt)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "PaymentVoucher");

            var partyBalances = await partyBalanceQuery.ToListAsync();
            var mains = partyBalances.Where(x => x.IsMain && x.AgainstVoucherTypeId == Guid.Empty).ToList();
            var results = new List<UniversalDropdownDto>();

            results.AddRange(mains.Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.VoucherNo
            }));

            return results;
        }

        public async Task<decimal> GetAllRemainingReceiptBalance(Guid ledgerId, PaymentOptions paymentTypes,
            string voucherNo)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll()
                .Where(x => !x.IsFullySettled && x.LedgerId == ledgerId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId);
            if (paymentTypes == PaymentOptions.Purchase)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "SalesInvoice");
            if (paymentTypes == PaymentOptions.Receipt)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.VoucherTypeFk.Name == "PaymentVoucher");
            var partyBalances = await partyBalanceQuery.ToListAsync();
            return partyBalances.Sum(x => x.Debit - x.Credit);
        }

        public async Task<GetReceiptAgainstMasterDto> GetReceiptAgainstAmount(Guid ledgerId, decimal amount, Guid detailId)
        {
            var master = new GetReceiptAgainstMasterDto();
            var result = new List<GetReceiptAgainstDto>();
            decimal tempAmount = amount;

            if (detailId != null && detailId != Guid.Empty)
            {
                var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.DetailId == detailId && x.AgainstVoucherTypeId != Guid.Empty)
                    .Include(x => x.VoucherTypeFk).OrderBy(x => x.Date).Select(x => new GetReceiptAgainstDto
                    {
                        PartyBalanceId = x.Id,
                        BillDate = DateConverter.ConvertToNepali(x.Date),
                        DueDate = DateConverter.ConvertToNepali(x.DueDate),
                        VoucherType = x.VoucherTypeFk.Name,
                        VoucherNo = x.VoucherNo,
                        VoucherNumbering = x.VoucherNumbering,
                        VoucherTypeId = x.VoucherTypeId,
                        IsSettled = x.IsFullySettled
                    }).ToListAsync();
                foreach (var oldDatum in oldData)
                {
                    var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == oldDatum.VoucherNo && x.VoucherTypeId == oldDatum.VoucherTypeId && x.VoucherNumbering == oldDatum.VoucherNumbering && x.FinancialYearId == FinancialYearId).ToListAsync();
                    oldDatum.BillAmount = relatedData.Where(x => x.Id != oldDatum.PartyBalanceId && x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit);
                    oldDatum.PaidAmount = relatedData.Where(x => x.Id != oldDatum.PartyBalanceId && x.DetailId != Guid.Empty).Sum(x => x.Credit - x.Debit);
                    oldDatum.BalanceAmount = oldDatum.BillAmount - oldDatum.PaidAmount;
                    oldDatum.Adjust = tempAmount > oldDatum.BalanceAmount ? oldDatum.BalanceAmount : tempAmount;
                    oldDatum.IsSettled = oldDatum.Adjust == oldDatum.BalanceAmount ? true : false;
                    tempAmount -= oldDatum.Adjust;
                }
                result.AddRange(oldData);
            }

            var remainingQuery = newPartyBalanceRepository.GetAll().Where(x => x.LedgerId == ledgerId && !x.IsFullySettled);
            if (detailId != null && detailId != Guid.Empty)
                remainingQuery = remainingQuery.Where(x => !x.IsPartiallySettled);

            var remainingData = await remainingQuery
                .Include(x => x.VoucherTypeFk).ToListAsync();
            var mains = remainingData.Where(x => x.IsMain && x.DetailId == Guid.Empty && x.Debit > 0 && x.AgainstVoucherTypeId == Guid.Empty).OrderBy(x => x.Date).ToList();
            int sn = 0;
            var paymentsOnly = remainingData.Where(x => x.VoucherTypeFk.Name == "PaymentVoucher" && x.IsMain && x.Debit > 0 && x.AgainstVoucherTypeId == Guid.Empty).ToList();
            mains.AddRange(paymentsOnly);
            foreach (var main in mains)
            {
                sn++;
                var relatedData = remainingData.Where(x => x.VoucherTypeId == main.VoucherTypeId && x.VoucherNo == main.VoucherNo
                     && x.FinancialYearId == FinancialYearId && x.VoucherNumbering == main.VoucherNumbering && x.Id != main.Id).ToList();
                var data = new GetReceiptAgainstDto
                {
                    SN = sn,
                    BillDate = DateConverter.ConvertToNepali(main.Date),
                    DueDate = DateConverter.ConvertToNepali(main.DueDate),
                    VoucherType = main.VoucherTypeFk.Name,
                    VoucherNo = main.VoucherNo,
                    BillAmount = main.Debit - relatedData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit),
                    VoucherNumbering = main.VoucherNumbering,
                    VoucherTypeId = main.VoucherTypeId,
                    PaidAmount = relatedData.Where(x => x.DetailId != Guid.Empty).Sum(x => x.Credit - x.Debit),
                };
                data.BalanceAmount = data.BillAmount - data.PaidAmount;
                data.Adjust = tempAmount > data.BalanceAmount ? data.BalanceAmount : tempAmount;
                data.IsSettled = data.Adjust == data.BalanceAmount ? true : false;
                tempAmount -= data.Adjust;
                result.Add(data);
            }
            master.Details = result;
            master.NewReferenceAmount = tempAmount;
            return master;
        }

        private async Task<bool> ValidateReceiptSettlement(Guid ledgerId, string voucherNo, int voucherNumbering, Guid voucherTypeId, decimal amount, Guid id)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll().Where(x => x.LedgerId == ledgerId && x.VoucherNo == voucherNo && x.VoucherTypeId == voucherTypeId && x.VoucherNumbering == voucherNumbering);
            if (id != Guid.Empty)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.Id != id);
            var data = await partyBalanceQuery.ToListAsync();
            var sum = data.Sum(x => x.Debit - x.Credit);
            if (Math.Round(sum, 0) == Math.Round(amount, 0))
                return true;
            else
                return false;
        }

        private async Task<bool> ValidatePaymentSettlement(Guid ledgerId, string voucherNo, int voucherNumbering, Guid voucherTypeId, decimal amount, Guid id)
        {
            var partyBalanceQuery = newPartyBalanceRepository.GetAll().Where(x => x.LedgerId == ledgerId && x.VoucherNo == voucherNo && x.VoucherTypeId == voucherTypeId && x.VoucherNumbering == voucherNumbering);
            if (id != Guid.Empty)
                partyBalanceQuery = partyBalanceQuery.Where(x => x.Id != id);
            var data = await partyBalanceQuery.ToListAsync();
            var sum = data.Sum(x => x.Credit - x.Debit);
            if (Math.Round(sum, 0) == Math.Round(amount, 0))
                return true;
            else
                return false;
        }


        public async Task PostReceipts(CreateReceiptAgainstMasterDto input)
        {
            var oldReferenceData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == input.MasterVoucherNo && x.VoucherNumbering == input.MasterVoucherNumbering && x.VoucherTypeId == input.MasterVoucherTypeId && x.FinancialYearId == FinancialYearId && x.DetailId == input.DetailId).ToListAsync();
            foreach (var oldReferenceDatum in oldReferenceData)
                await newPartyBalanceRepository.DeleteAsync(oldReferenceDatum);
            foreach (var data in input.Data.Details)
            {
                var settled = await ValidateReceiptSettlement(input.LedgerId, data.VoucherNo, data.VoucherNumbering, data.VoucherTypeId, data.Adjust, data.PartyBalanceId);

                if (data.PartyBalanceId != null && data.PartyBalanceId != Guid.Empty)
                {
                    var partyBalance = await newPartyBalanceRepository.FirstOrDefaultAsync(x => x.Id == data.PartyBalanceId);
                    if (partyBalance != null)
                    {
                        partyBalance.Credit = data.Adjust;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }
                    if (settled)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    else
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = false;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    if (data.Adjust > 1)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    continue;
                }
                if (data.Adjust > 1)
                {
                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = DateConverter.ConvertToEnglish(data.DueDate),
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = data.VoucherNo,
                        VoucherNumbering = data.VoucherNumbering,
                        VoucherTypeId = data.VoucherTypeId,
                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = input.MasterVoucherTypeId,
                        Debit = 0,
                        Credit = data.Adjust,
                        IsMain = true,
                        IsFullySettled = settled,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    if (settled)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    else if (data.Adjust > 1)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                }
            }
            if (input.Data.NewReferenceAmount > 0)
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.MasterVoucherNo,
                    VoucherNumbering = input.MasterVoucherNumbering,
                    VoucherTypeId = input.MasterVoucherTypeId,
                    AgainstVoucherNo = "",
                    AgainstVoucherNumbering = 0,
                    AgainstVoucherTypeId = Guid.Empty,
                    Debit = 0,
                    Credit = input.Data.NewReferenceAmount,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task PostPayments(CreateReceiptAgainstMasterDto input)
        {
            var oldReferenceData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == input.MasterVoucherNo && x.VoucherNumbering == input.MasterVoucherNumbering && x.VoucherTypeId == input.MasterVoucherTypeId && x.FinancialYearId == FinancialYearId && x.DetailId == input.DetailId).ToListAsync();
            foreach (var oldReferenceDatum in oldReferenceData)
                await newPartyBalanceRepository.DeleteAsync(oldReferenceDatum);
            foreach (var data in input.Data.Details)
            {
                var settled = await ValidatePaymentSettlement(input.LedgerId, data.VoucherNo, data.VoucherNumbering, data.VoucherTypeId, data.Adjust, data.PartyBalanceId);

                if (data.PartyBalanceId != null && data.PartyBalanceId != Guid.Empty)
                {
                    var partyBalance = await newPartyBalanceRepository.FirstOrDefaultAsync(x => x.Id == data.PartyBalanceId);
                    if (partyBalance != null)
                    {
                        partyBalance.Debit = data.Adjust;
                        await newPartyBalanceRepository.UpdateAsync(partyBalance);
                    }
                    if (settled)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    else
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = false;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    if (data.Adjust > 1)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    continue;
                }
                if (data.Adjust > 1)
                {
                    var partyBalanceEntry = new NewPartyBalance
                    {
                        Date = input.Date,
                        DueDate = DateConverter.ConvertToEnglish(data.DueDate),
                        LedgerId = input.LedgerId,
                        FinancialYearId = FinancialYearId,
                        VoucherNo = data.VoucherNo,
                        VoucherNumbering = data.VoucherNumbering,
                        VoucherTypeId = data.VoucherTypeId,
                        AgainstVoucherNo = input.MasterVoucherNo,
                        AgainstVoucherNumbering = input.MasterVoucherNumbering,
                        AgainstVoucherTypeId = input.MasterVoucherTypeId,
                        Debit = data.Adjust,
                        Credit = 0,
                        IsMain = true,
                        IsFullySettled = settled,
                        IsPartiallySettled = false,
                        MasterId = input.MasterId,
                        DetailId = input.DetailId,
                        MasterPartyBalanceId = null,
                        TenantId = AbpSession.TenantId,
                    };
                    await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
                    if (settled)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsFullySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                    else if (data.Adjust > 1)
                    {
                        var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == data.VoucherNo && x.VoucherNumbering == data.VoucherNumbering
                        && x.VoucherTypeId == data.VoucherTypeId && x.FinancialYearId == FinancialYearId).ToListAsync();
                        foreach (var oldDatum in oldData)
                        {
                            oldDatum.IsPartiallySettled = true;
                            await newPartyBalanceRepository.UpdateAsync(oldDatum);
                        }
                    }
                }
            }
            if (input.Data.NewReferenceAmount > 0)
            {
                var partyBalanceEntry = new NewPartyBalance
                {
                    Date = input.Date,
                    DueDate = input.Date,
                    LedgerId = input.LedgerId,
                    FinancialYearId = FinancialYearId,
                    VoucherNo = input.MasterVoucherNo,
                    VoucherNumbering = input.MasterVoucherNumbering,
                    VoucherTypeId = input.MasterVoucherTypeId,
                    AgainstVoucherNo = "",
                    AgainstVoucherNumbering = 0,
                    AgainstVoucherTypeId = Guid.Empty,
                    Debit = input.Data.NewReferenceAmount,
                    Credit = 0,
                    IsMain = true,
                    IsFullySettled = false,
                    IsPartiallySettled = false,
                    MasterId = input.MasterId,
                    DetailId = input.DetailId,
                    MasterPartyBalanceId = null,
                    TenantId = AbpSession.TenantId,
                };
                await newPartyBalanceRepository.InsertAsync(partyBalanceEntry);
            }
        }

        public async Task<GetReceiptAgainstMasterDto> GetPaymentAgainstAmount(Guid ledgerId, decimal amount, Guid detailId)
        {
            var master = new GetReceiptAgainstMasterDto();
            var result = new List<GetReceiptAgainstDto>();
            decimal tempAmount = amount;

            if (detailId != null && detailId != Guid.Empty)
            {
                var oldData = await newPartyBalanceRepository.GetAll().Where(x => x.DetailId == detailId && x.AgainstVoucherTypeId != Guid.Empty)
                    .Include(x => x.VoucherTypeFk).OrderBy(x => x.Date).Select(x => new GetReceiptAgainstDto
                    {
                        PartyBalanceId = x.Id,
                        BillDate = DateConverter.ConvertToNepali(x.Date),
                        DueDate = DateConverter.ConvertToNepali(x.DueDate),
                        VoucherType = x.VoucherTypeFk.Name,
                        VoucherNo = x.VoucherNo,
                        VoucherNumbering = x.VoucherNumbering,
                        VoucherTypeId = x.VoucherTypeId,
                        IsSettled = x.IsFullySettled
                    }).ToListAsync();
                foreach (var oldDatum in oldData)
                {
                    var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == oldDatum.VoucherNo && x.VoucherTypeId == oldDatum.VoucherTypeId && x.VoucherNumbering == oldDatum.VoucherNumbering && x.FinancialYearId == FinancialYearId).ToListAsync();
                    oldDatum.BillAmount = relatedData.Where(x => x.Id != oldDatum.PartyBalanceId && x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit);
                    oldDatum.PaidAmount = relatedData.Where(x => x.Id != oldDatum.PartyBalanceId && x.DetailId != Guid.Empty).Sum(x => x.Debit - x.Credit);
                    oldDatum.BalanceAmount = oldDatum.BillAmount - oldDatum.PaidAmount;
                    oldDatum.Adjust = tempAmount > oldDatum.BalanceAmount ? oldDatum.BalanceAmount : tempAmount;
                    oldDatum.IsSettled = oldDatum.Adjust == oldDatum.BalanceAmount ? true : false;
                    tempAmount -= oldDatum.Adjust;
                }
                result.AddRange(oldData);
            }

            var remainingQuery = newPartyBalanceRepository.GetAll().Where(x => x.LedgerId == ledgerId && !x.IsFullySettled);
            if (detailId != null && detailId != Guid.Empty)
                remainingQuery = remainingQuery.Where(x => !x.IsPartiallySettled);

            var remainingData = await remainingQuery
                .Include(x => x.VoucherTypeFk).ToListAsync();
            var mains = remainingData.Where(x => x.IsMain && x.DetailId == Guid.Empty && x.Credit > 0 && x.AgainstVoucherTypeId == Guid.Empty).OrderBy(x => x.Date).ToList();
            int sn = 0;
            var receiptsOnly = remainingData.Where(x => x.VoucherTypeFk.Name == "ReceiptVoucher" && x.IsMain && x.Credit > 0 && x.AgainstVoucherTypeId == Guid.Empty).ToList();
            mains.AddRange(receiptsOnly);
            foreach (var main in mains)
            {
                sn++;
                var relatedData = remainingData.Where(x => x.VoucherTypeId == main.VoucherTypeId && x.VoucherNo == main.VoucherNo
                     && x.FinancialYearId == FinancialYearId && x.VoucherNumbering == main.VoucherNumbering && x.Id != main.Id).ToList();
                var data = new GetReceiptAgainstDto
                {
                    SN = sn,
                    BillDate = DateConverter.ConvertToNepali(main.Date),
                    DueDate = DateConverter.ConvertToNepali(main.DueDate),
                    VoucherType = main.VoucherTypeFk.Name,
                    VoucherNo = main.VoucherNo,
                    BillAmount = main.Credit - relatedData.Where(x => x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit),
                    VoucherNumbering = main.VoucherNumbering,
                    VoucherTypeId = main.VoucherTypeId,
                    PaidAmount = relatedData.Where(x => x.DetailId != Guid.Empty).Sum(x => x.Debit - x.Credit),
                };
                data.BalanceAmount = data.BillAmount - data.PaidAmount;
                data.Adjust = tempAmount > data.BalanceAmount ? data.BalanceAmount : tempAmount;
                data.IsSettled = data.Adjust == data.BalanceAmount ? true : false;
                tempAmount -= data.Adjust;
                result.Add(data);
            }
            master.Details = result;
            master.NewReferenceAmount = tempAmount;
            return master;
        }




        ///// <summary>
        ///// Migrates all data from old PartyBalance table to new NewPartyBalance table
        ///// Handles all voucher types: Opening Balance, Sales, Sales Return, Purchase, Purchase Return, Receipt, Payment, Journal
        ///// </summary>
        //public async Task<bool> MigrateOldPartyBalanceToNewPartyBalance()
        //{
        //    try
        //    {
        //        // Clear existing data in new table (optional - remove if you want to preserve existing data)
        //        var existingNewBalances = await newPartyBalanceRepository.GetAll().ToListAsync();
        //        foreach (var balance in existingNewBalances)
        //        {
        //            await newPartyBalanceRepository.DeleteAsync(balance);
        //        }

        //        // Get all old party balance data ordered by date for proper processing
        //        var oldPartyBalances = await partyBalanceRepository.GetAll()
        //            .Include(x => x.VoucherTypeFk)
        //            .OrderBy(x => x.Date)
        //            .ThenBy(x => x.VoucherNumbering)
        //            .ToListAsync();

        //        // Group by voucher for settlement calculation
        //        var voucherGroups = oldPartyBalances
        //            .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo, x.VoucherNumbering, x.FinancialYearId })
        //            .ToList();

        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessVoucherGroup(group.ToList());
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception
        //        Logger.Error($"Error migrating party balance data: {ex.Message}", ex);
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Processes a group of party balance entries for the same voucher
        ///// </summary>
        //private async Task ProcessVoucherGroup(List<PartyBalance> voucherEntries)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalance(oldEntry, voucherType, isFullySettled);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}


        //private async Task ProcessPurchaseVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalancePurchase(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}

        //private async Task ProcessPurchaseReturnVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalancePurchaseReturn(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}

        //private async Task ProcessSalesVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalanceSales(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}
        //private async Task ProcessSalesReturnVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalanceSalesReturn(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}

        //private async Task ProcessPaymentVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalancePayment(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}

        //private async Task ProcessReceiptVoucherGroup(List<PartyBalance> voucherEntries, int t)
        //{
        //    if (!voucherEntries.Any()) return;

        //    var firstEntry = voucherEntries.First();
        //    var voucherType = firstEntry.VoucherTypeFk?.Name ?? "Unknown";

        //    // Calculate settlement status for the voucher
        //    var totalDebit = voucherEntries.Sum(x => x.Debit);
        //    var totalCredit = voucherEntries.Sum(x => x.Credit);
        //    var balance = totalDebit - totalCredit;
        //    var isFullySettled = Math.Abs(balance) < 0.01m; // Consider settled if balance is less than 1 paisa

        //    foreach (var oldEntry in voucherEntries)
        //    {
        //        var newEntry = await ConvertToNewPartyBalanceReceipt(oldEntry, voucherType, isFullySettled, t);
        //        if (newEntry != null)
        //        {
        //            await newPartyBalanceRepository.InsertAsync(newEntry);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Converts old PartyBalance entry to new NewPartyBalance entry
        ///// </summary>
        //private async Task<NewPartyBalance> ConvertToNewPartyBalance(PartyBalance oldEntry, string voucherType, bool isFullySettled)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;

        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = GetAgainstVoucherNumbering(oldEntry),
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = oldEntry.AgainstVoucherNo ?? "",
        //        AgainstVoucherNumbering = oldEntry.VoucherNumbering,
        //        AgainstVoucherTypeId = againstVoucherTypeId,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = oldEntry.DetailId,
        //        MasterPartyBalanceId = GetMasterPartyBalanceId(oldEntry),
        //        TenantId = oldEntry.TenantId
        //    };

        //    return newEntry;
        //}
        //private async Task<NewPartyBalance> ConvertToNewPartyBalancePurchase(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;

        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = oldEntry.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = "",
        //        AgainstVoucherNumbering = 0,
        //        AgainstVoucherTypeId = Guid.Empty,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = oldEntry.DetailId,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}

        //private async Task<NewPartyBalance> ConvertToNewPartyBalancePurchaseReturn(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;
        //    var purchaseReturn = await purchaseReturnRepository.FirstOrDefaultAsync(x => x.VoucherNo == oldEntry.AgainstVoucherNo && x.FinancialYearId == oldEntry.FinancialYearId);
        //    var purchase = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturn.PurchaseMasterId);
        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = purchase.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = oldEntry.InvoiceNo,
        //        AgainstVoucherNumbering = purchaseReturn.VoucherNumbering,
        //        AgainstVoucherTypeId = oldEntry.AgainstVoucherTypeId,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = Guid.Empty,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}

        //private async Task<NewPartyBalance> ConvertToNewPartyBalanceSales(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;

        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = oldEntry.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = "",
        //        AgainstVoucherNumbering = 0,
        //        AgainstVoucherTypeId = Guid.Empty,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = Guid.Empty,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}

        //private async Task<NewPartyBalance> ConvertToNewPartyBalanceSalesReturn(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;
        //    var sales = await salesMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == oldEntry.VoucherNo && x.FinancialYearId == FinancialYearId);
        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = sales.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = oldEntry.AgainstVoucherNo,
        //        AgainstVoucherNumbering = oldEntry.VoucherNumbering,
        //        AgainstVoucherTypeId = oldEntry.AgainstVoucherTypeId,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = Guid.Empty,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}


        //private async Task<NewPartyBalance> ConvertToNewPartyBalancePayment(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;
        //    var payment = await paymentMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == oldEntry.VoucherNo && x.FinancialYearId == FinancialYearId);
        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = payment.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = oldEntry.AgainstVoucherNo,
        //        AgainstVoucherNumbering = oldEntry.VoucherNumbering,
        //        AgainstVoucherTypeId = oldEntry.AgainstVoucherTypeId,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = oldEntry.DetailId,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}



        //private async Task<NewPartyBalance> ConvertToNewPartyBalanceReceipt(PartyBalance oldEntry, string voucherType, bool isFullySettled, int t)
        //{
        //    // Determine if this is a main entry or adjustment entry
        //    var isMainEntry = DetermineIfMainEntry(oldEntry, voucherType);

        //    // Calculate due date
        //    var dueDate = oldEntry.CreditPeriod > 0 ? oldEntry.Date.AddDays(oldEntry.CreditPeriod) : oldEntry.Date;

        //    // Determine against voucher type ID
        //    var againstVoucherTypeId = oldEntry.AgainstVoucherTypeId ?? Guid.Empty;
        //    var receipt = await receiptMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == oldEntry.VoucherNo && x.FinancialYearId == FinancialYearId);
        //    var newEntry = new NewPartyBalance
        //    {
        //        Id = Guid.NewGuid(),
        //        Date = oldEntry.Date,
        //        DueDate = dueDate,
        //        LedgerId = oldEntry.LedgerId,
        //        FinancialYearId = oldEntry.FinancialYearId,
        //        VoucherNo = oldEntry.VoucherNo ?? "",
        //        VoucherNumbering = receipt.VoucherNumbering,
        //        VoucherTypeId = oldEntry.VoucherTypeId ?? Guid.Empty,
        //        BranchId = oldEntry.BranchId,
        //        AgainstVoucherNo = oldEntry.AgainstInvoiceNo,
        //        AgainstVoucherNumbering = oldEntry.VoucherNumbering,
        //        AgainstVoucherTypeId = oldEntry.AgainstVoucherTypeId,
        //        Debit = oldEntry.Debit,
        //        Credit = oldEntry.Credit,
        //        IsMain = isMainEntry,
        //        IsFullySettled = isFullySettled,
        //        IsPartiallySettled = !isFullySettled && (oldEntry.Debit > 0 || oldEntry.Credit > 0),
        //        MasterId = oldEntry.MasterId,
        //        DetailId = oldEntry.DetailId,
        //        MasterPartyBalanceId = null,
        //        TenantId = t
        //    };

        //    return newEntry;
        //}
        ///// <summary>
        ///// Determines if an entry should be marked as main entry based on voucher type and reference type
        ///// </summary>
        //private bool DetermineIfMainEntry(PartyBalance oldEntry, string voucherType)
        //{
        //    // Main entries are typically:
        //    // 1. Original invoices (Sales, Purchase)
        //    // 2. Opening balances
        //    // 3. Primary voucher entries (not against entries)

        //    if (oldEntry.ReferenceType == "New" || oldEntry.ReferenceType == "OnAccount")
        //        return true;

        //    if (string.IsNullOrEmpty(oldEntry.AgainstVoucherNo) && oldEntry.AgainstVoucherTypeId == null)
        //        return true;

        //    // For specific voucher types
        //    switch (voucherType?.ToLower())
        //    {
        //        case "openingbalance":
        //        case "salesinvoice":
        //        case "purchaseinvoice":
        //        case "journal":
        //            return !oldEntry.IsAgainst;

        //        case "receiptvoucher":
        //        case "paymentvoucher":
        //        case "salesreturn":
        //        case "purchasereturn":
        //            return oldEntry.ReferenceType == "OnAccount" || string.IsNullOrEmpty(oldEntry.AgainstVoucherNo);

        //        default:
        //            return !oldEntry.IsAgainst;
        //    }
        //}

        ///// <summary>
        ///// Gets the against voucher numbering from old entry
        ///// </summary>
        //private int GetAgainstVoucherNumbering(PartyBalance oldEntry)
        //{
        //    // This might need to be extracted from master voucher or calculated
        //    // For now, return 0 if no against voucher, otherwise try to derive it
        //    if (string.IsNullOrEmpty(oldEntry.AgainstVoucherNo))
        //        return 0;

        //    // You might need to implement logic to get the numbering from the against voucher
        //    // This could involve querying the master tables or having a lookup
        //    return 0; // Placeholder - implement based on your business logic
        //}

        ///// <summary>
        ///// Gets the master party balance ID for related entries
        ///// </summary>
        //private Guid? GetMasterPartyBalanceId(PartyBalance oldEntry)
        //{
        //    // This would typically be null for main entries
        //    // and reference the main entry's ID for adjustment entries
        //    if (oldEntry.IsAgainst && !string.IsNullOrEmpty(oldEntry.AgainstVoucherNo))
        //    {
        //        // You might need to implement logic to find the related main entry
        //        // This is complex and might require additional queries
        //        return null; // Placeholder - implement based on your business logic
        //    }

        //    return null;
        //}

        ///// <summary>
        ///// Migrates specific voucher types in order
        ///// </summary>
        //public async Task<bool> MigratePartyBalanceByVoucherTypes()
        //{
        //    try
        //    {
        //        var voucherTypesToMigrate = new[]
        //        {
        //            "OpeningBalance",
        //            "SalesInvoice",
        //            "SalesReturn",
        //            "PurchaseInvoice",
        //            "PurchaseReturn",
        //            "ReceiptVoucher",
        //            "PaymentVoucher",
        //            "JournalVoucher"
        //        };
        //        foreach (var voucherTypeName in voucherTypesToMigrate)
        //        {
        //            await MigrateByVoucherType(voucherTypeName);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Error migrating party balance by voucher types: {ex.Message}", ex);
        //        return false;
        //    }
        //}

        //public async Task<bool> MigratePartyBalanceByVoucherTypesNew(int t)
        //{
        //    try
        //    {
        //        var voucherTypesToMigrate = new[]
        //        {
        //            "OpeningBalance",
        //            "SalesInvoice",
        //            "SalesReturn",
        //            "PurchaseInvoice",
        //            "PurchaseReturn",
        //            "ReceiptVoucher",
        //            "PaymentVoucher",
        //            "JournalVoucher"
        //        };
        //        var voucherTypes = await voucherTypeRepository.GetAll()
        //            .Where(x => voucherTypesToMigrate.Contains(x.Name))
        //            .ToListAsync();
        //        foreach (var voucherType in voucherTypes)
        //        {
        //            await MigrateByVoucherTypeNew(voucherType.Id, voucherType.Name, t);
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Error migrating party balance by voucher types: {ex.Message}", ex);
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Migrates party balance entries for a specific voucher type
        ///// </summary>
        //private async Task MigrateByVoucherType(string voucherTypeName)
        //{
        //    var oldEntries = await partyBalanceRepository.GetAll()
        //        .Include(x => x.VoucherTypeFk)
        //        .Where(x => x.VoucherTypeFk.Name == voucherTypeName)
        //        .OrderBy(x => x.Date)
        //        .ThenBy(x => x.VoucherNumbering)
        //        .ToListAsync();

        //    var voucherGroups = oldEntries
        //        .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo, x.VoucherNumbering, x.FinancialYearId })
        //        .ToList();

        //    foreach (var group in voucherGroups)
        //    {
        //        await ProcessVoucherGroup(group.ToList());
        //    }

        //    Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //}

        //private async Task MigrateByVoucherTypeNew(Guid voucherTypeId, string voucherTypeName, int t)
        //{
        //    var oldEntries = await partyBalanceRepository.GetAll()
        //        .Include(x => x.VoucherTypeFk)
        //        .Where(x => x.MasterVoucherTypeId == voucherTypeId)
        //        .OrderBy(x => x.Date)
        //        .ThenBy(x => x.VoucherNumbering)
        //        .ToListAsync();
        //    var voucherGroups = oldEntries
        //        .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo, x.VoucherNumbering, x.FinancialYearId })
        //        .ToList();
        //    if (voucherTypeName == "PurchaseInvoice")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessPurchaseVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //    if (voucherTypeName == "PurchaseReturn")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessPurchaseReturnVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //    if (voucherTypeName == "SalesInvoice")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessSalesVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //    if (voucherTypeName == "SalesReturn")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessSalesReturnVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //    if (voucherTypeName == "PaymentVoucher")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessPaymentVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //    if (voucherTypeName == "ReceiptVoucher")
        //    {
        //        foreach (var group in voucherGroups)
        //        {
        //            await ProcessPaymentVoucherGroup(group.ToList(), t);
        //        }
        //        Logger.Info($"Migrated {oldEntries.Count} entries for voucher type: {voucherTypeName}");
        //    }
        //}

        ///// <summary>
        ///// Validates the migration by comparing totals
        ///// </summary>
        //public async Task<bool> ValidateMigration()
        //{
        //    try
        //    {
        //        var oldTotalDebit = await partyBalanceRepository.GetAll().SumAsync(x => x.Debit);
        //        var oldTotalCredit = await partyBalanceRepository.GetAll().SumAsync(x => x.Credit);

        //        var newTotalDebit = await newPartyBalanceRepository.GetAll().SumAsync(x => x.Debit);
        //        var newTotalCredit = await newPartyBalanceRepository.GetAll().SumAsync(x => x.Credit);

        //        var debitMatches = Math.Abs(oldTotalDebit - newTotalDebit) < 0.01m;
        //        var creditMatches = Math.Abs(oldTotalCredit - newTotalCredit) < 0.01m;

        //        if (debitMatches && creditMatches)
        //        {
        //            Logger.Info("Migration validation successful - totals match");
        //            return true;
        //        }
        //        else
        //        {
        //            Logger.Error($"Migration validation failed - Old: Debit={oldTotalDebit}, Credit={oldTotalCredit} | New: Debit={newTotalDebit}, Credit={newTotalCredit}");
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Error validating migration: {ex.Message}", ex);
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Public method to execute the complete migration process
        ///// </summary>
        //public async Task<string> ExecutePartyBalanceMigration()
        //{
        //    try
        //    {
        //        // Step 1: Migrate by voucher types in proper order
        //        var migrationSuccess = await MigratePartyBalanceByVoucherTypes();

        //        if (!migrationSuccess)
        //        {
        //            return "Migration failed during data transfer process";
        //        }

        //        // Step 2: Validate the migration
        //        var validationSuccess = await ValidateMigration();

        //        if (!validationSuccess)
        //        {
        //            return "Migration completed but validation failed - please review the data";
        //        }

        //        // Step 3: Get migration summary
        //        var oldCount = await partyBalanceRepository.GetAll().CountAsync();
        //        var newCount = await newPartyBalanceRepository.GetAll().CountAsync();

        //        return $"Migration completed successfully! Migrated {oldCount} old entries to {newCount} new entries";
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Migration execution failed: {ex.Message}", ex);
        //        return $"Migration failed with error: {ex.Message}";
        //    }
        //}

        //public async Task<string> ExecutePartyBalanceMigrationNew(Tenant t)
        //{
        //    try
        //    {
        //        int tenantId = t.Id;
        //        // Step 1: Migrate by voucher types in proper order
        //        var migrationSuccess = await MigratePartyBalanceByVoucherTypesNew(tenantId);

        //        if (!migrationSuccess)
        //        {
        //            return "Migration failed during data transfer process";
        //        }

        //        // Step 2: Validate the migration
        //        var validationSuccess = await ValidateMigration();

        //        if (!validationSuccess)
        //        {
        //            return "Migration completed but validation failed - please review the data";
        //        }

        //        // Step 3: Get migration summary
        //        var oldCount = await partyBalanceRepository.GetAll().CountAsync();
        //        var newCount = await newPartyBalanceRepository.GetAll().CountAsync();

        //        return $"Migration completed successfully! Migrated {oldCount} old entries to {newCount} new entries";
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Migration execution failed: {ex.Message}", ex);
        //        return $"Migration failed with error: {ex.Message}";
        //    }
        //}

        ///// <summary>
        ///// Gets migration progress and statistics
        ///// </summary>
        //public async Task<object> GetMigrationStatus()
        //{
        //    try
        //    {
        //        var oldCount = await partyBalanceRepository.GetAll().CountAsync();
        //        var newCount = await newPartyBalanceRepository.GetAll().CountAsync();

        //        var oldTotal = await partyBalanceRepository.GetAll()
        //            .Select(x => new { Debit = x.Debit, Credit = x.Credit })
        //            .ToListAsync();

        //        var newTotal = await newPartyBalanceRepository.GetAll()
        //            .Select(x => new { Debit = x.Debit, Credit = x.Credit })
        //            .ToListAsync();

        //        return new
        //        {
        //            OldTableRecords = oldCount,
        //            NewTableRecords = newCount,
        //            OldTableTotals = new
        //            {
        //                TotalDebit = oldTotal.Sum(x => x.Debit),
        //                TotalCredit = oldTotal.Sum(x => x.Credit)
        //            },
        //            NewTableTotals = new
        //            {
        //                TotalDebit = newTotal.Sum(x => x.Debit),
        //                TotalCredit = newTotal.Sum(x => x.Credit)
        //            },
        //            MigrationNeeded = oldCount > 0 && newCount == 0
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error($"Error getting migration status: {ex.Message}", ex);
        //        return new { Error = ex.Message };
        //    }
        //}
    }
}
