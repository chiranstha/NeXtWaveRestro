using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using NextWave.Erp.Accounting.Importing.Dto;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Importing
{
    internal class ImportAccountLedgerToExcelJob(
     IUnitOfWorkManager unitOfWorkManager,
     IBinaryObjectManager binaryObjectManager,
     IAccountLedgerListExcelDataReader accountLedgerListExcelDataReader,
     IRepository<AccountLedger, Guid> accountLedgerRepository,
     IRepository<AccountGroup, Guid> accountGroupRepository,
     IRepository<Branch, Guid> branchRepository,
     IRepository<FinancialYear, Guid> financialYearRepository,
     IRepository<LedgerPosting, Guid> ledgerPostingRepository,
     IRepository<VoucherType, Guid> voucherTypeRepository)
     : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var account = GetAccountLedgerListFromExcelOrNull(args);
            if (account == null || !account.Any())
            {
                SendInvalidExcelNotification(args);
                return;
            }

            CreateAccountLedger(args, account);
        }

        private List<ImportAccountLedgerDto> GetAccountLedgerListFromExcelOrNull(ImportUniversalFromExcelJobArgs args)
        {
            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    try
                    {
                        var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                        return accountLedgerListExcelDataReader.GetAccountLedgerFromExcel(file.Bytes);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        uow.Complete();
                    }
                }
            }
        }

        private void SendInvalidExcelNotification(ImportUniversalFromExcelJobArgs args)
        {
            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                }

                uow.Complete();
            }
        }

        private void CreateAccountLedger(ImportUniversalFromExcelJobArgs args,
            List<ImportAccountLedgerDto> accountLedgers)
        {
            var invalidAccountLedgers = new List<ImportAccountLedgerDto>();
            foreach (var accountLedger in accountLedgers)
            {
                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                        // if (accountLedger.CanBeImported())
                        try
                        {
                            AsyncHelper.RunSync(() => CreateAccountLedgerAsync(accountLedger));
                        }
                        catch (UserFriendlyException exception)
                        {
                            accountLedger.Exception = exception.Message;
                            invalidAccountLedgers.Add(accountLedger);
                        }
                        catch (Exception exception)
                        {
                            accountLedger.Exception = exception.ToString();
                            invalidAccountLedgers.Add(accountLedger);
                        }
                        //else
                        //    invalidAccountLedgers.Add(accountLedger);
                    }

                    uow.Complete();
                }

                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                    }

                    uow.Complete();
                }
            }
        }

        private async Task CreateAccountLedgerAsync(ImportAccountLedgerDto input)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var ledger =
                await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == input.Name && x.TenantId == tenantId);
            if (ledger == null)
            {
                var accountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == input.AccountGroupName);

                if (accountGroup != null)
                {
                    var account = new AccountLedger
                    {
                        Name = input.Name,
                        OpeningBalance = (decimal)input.OpeningBalance,
                        IsDefault = false,
                        CrOrDr = input.CrOrDr,
                        Narration = null,
                        Address = input.Address,
                        Phone = input.Phone,
                        Email = null,
                        CreditPeriod = 0,
                        CreditLimit = (decimal)input.CreditLimit,
                        IsBillByBill = true,
                        Pan = input.Pan,
                        Status = true,
                        IsDelete = false,
                        UserId = null,
                        CreateUserId = null,
                        UpdateUserId = null,
                        ParentId = null,
                        AccountGroupId = (Guid)accountGroup?.Id,
                        TenantId = tenantId
                    };
                    var ledgerId = await accountLedgerRepository.InsertAndGetIdAsync(account);

                    var voucherType = await voucherTypeRepository.FirstOrDefaultAsync(x => x.Name == "OpeningBalance");
                    var financialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.Status);
                    var ledgerPosting = new LedgerPosting
                    {
                        VoucherNumbering = 0,
                        TenantId = tenantId,
                        Date = DateTime.Today,
                      //  DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                        VoucherTypeId = voucherType.Id,
                        VoucherNo = "Opening Balance",
                        VendorVoucherNo = "",
                        LedgerId = ledgerId,
                        DetailIds = "",
                        DetailId = Guid.Empty,
                        MasterId = ledgerId,
                        Debit = input.CrOrDr == DrOrCr.Dr ? (decimal)input.OpeningBalance : 0,
                        Credit = input.CrOrDr == DrOrCr.Cr ? (decimal)input.OpeningBalance : 0,
                        FinancialYearId = financialYear.Id,
                        InvoiceNo = "",
                        PostingNumber = 0
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }
            }
        }
    }
}
