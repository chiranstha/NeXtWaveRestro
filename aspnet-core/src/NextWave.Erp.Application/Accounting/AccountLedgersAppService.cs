using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.IO.Extensions;
using Abp.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Storage;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Accounting.Exporting;
using NextWave.Erp.Transaction;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.Accounting.Importing;
using NepDate;

namespace NextWave.Erp.Accounting
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesAccountLedgers)]
    public class AccountLedgersAppService(
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IAccountLedgersExcelExporter accountLedgersExcelExporter,
        IRepository<NewPartyBalance, Guid> partyBalanceRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<Tax, Guid> taxRepository,
        IBackgroundJobManager backgroundJobManager,
        IBinaryObjectManager binaryObjectManager,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IAccountLedgersAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        [DisableAuditing]
        public async Task<PagedResultDto<GetAccountLedgerForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredAccountLedgers = accountLedgerRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete).AsNoTracking().Include(e => e.AccountGroupFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    x => x.Name.Contains(input.Filter.Trim()) || x.Pan.Contains(input.Filter.Trim()) ||
                         x.Address.Contains(input.Filter.Trim()));

            var pagedAndFilteredAccountLedgers = filteredAccountLedgers
                .OrderBy(input.Sorting ?? "id desc")
                .PageBy(input);

            var accountGroups = from o in pagedAndFilteredAccountLedgers
                                select new GetAccountLedgerForViewDto
                                {
                                    Id = o.Id,
                                    AccountGroupName = o.AccountGroupFk.Name,
                                    CreditLimit = o.CreditLimit,
                                    Phone = o.Phone,
                                    IsDefault = o.IsDefault,
                                    PaNumber = o.Pan,
                                    AccountGroupId = o.AccountGroupId,
                                    Name = o.Name,
                                    Address = o.Address
                                };
            // await LedgerFixed();
            var totalCount = await filteredAccountLedgers.CountAsync();
            return new PagedResultDto<GetAccountLedgerForViewDto>(
                totalCount,
                await accountGroups.ToListAsync()
            );
        }

        public async Task<GetAccountLedgerForViewDto> GetAccountLedgerForView(Guid id)
        {
            await Testing();
            var accountLedger = await accountLedgerRepository.GetAll()
                .Include(e => e.AccountGroupFk)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (accountLedger == null) throw new UserFriendlyException("Data not found");
            var output = new GetAccountLedgerForViewDto
            {
                Id = accountLedger.Id,
                Name = accountLedger.Name,
                Address = accountLedger.Address,
                PaNumber = accountLedger.Pan,
                Phone = accountLedger.Phone,
                CreditLimit = accountLedger.CreditLimit,
                IsDefault = accountLedger.IsDefault,
                AccountGroupId = accountLedger.AccountGroupId,
                AccountGroupName = accountLedger.AccountGroupFk.Name
            };


            return output;
        }

        [AbpAuthorize(AppPermissions.PagesAccountLedgersEdit)]
        public async Task<GetAccountLedgerForEditOutput> GetAccountLedgerForEdit(EntityDto<Guid> input)
        {
            var accountLedger = await accountLedgerRepository.GetAll()
                .Include(x => x.AccountGroupFk)
                .FirstOrDefaultAsync(x => x.Id == input.Id);
            var output = new GetAccountLedgerForEditOutput();
            if (accountLedger != null)
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");
                var posting = await ledgerPostingRepository.GetAll().AsNoTracking()
                    .Where(x => x.LedgerId == input.Id && x.VoucherTypeId == voucherTypeId &&
                                x.FinancialYearId == FinancialYearId)
                    .ToListAsync();

                var isBank =
                    accountLedger.AccountGroupFk.Name is "Bank Account" or "Bank OD A/C" or "Sundry Creditors"
                        or "Sundry Debtors";

                output = new GetAccountLedgerForEditOutput
                {
                    Id = accountLedger.Id,
                    Name = accountLedger.Name,
                    OpeningBalance = null,

                    IsDefault = accountLedger.IsDefault,

                    CrOrDr = accountLedger.CrOrDr,

                    Narration = accountLedger.Narration,

                    Address = accountLedger.Address,

                    Phone = accountLedger.Phone,
                    IsBank = isBank,
                    Email = accountLedger.Email,
                    IsCompany = accountLedger.IsCompany,
                    CreditPeriod = accountLedger.CreditPeriod,

                    CreditLimit = accountLedger.CreditLimit,

                    IsBillByBill = accountLedger.IsBillByBill,

                    Pan = accountLedger.Pan,

                    Status = accountLedger.Status,

                    AccountGroupId = accountLedger.AccountGroupId,
                    AccountGroupName = accountLedger.AccountGroupFk.Name
                };

                {
                    var accountGroup =
                        await accountGroupRepository.FirstOrDefaultAsync(output.AccountGroupId);
                    output.AccountGroupName = accountGroup?.Name;
                }

                if (posting.Sum(x => x.Debit) > posting.Sum(x => x.Credit))
                {
                    output.OpeningBalance = posting.Sum(x => x.Debit);
                    output.CrOrDr = DrOrCr.Dr;
                }
                else
                {
                    output.OpeningBalance = posting.Sum(x => x.Credit);
                    output.CrOrDr = DrOrCr.Cr;
                }
            }

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditAccountLedgerDto input)
        {
           // input.OpeningMiti = DateTime.Now.ToNepaliDate().ToString();
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesAccountLedgersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");
            if ((await accountLedgerRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.Id && x.TenantId == AbpSession.GetTenantId())).IsDefault)
                throw new UserFriendlyException("Default Ledger Can't be Delete");
            if (await taxRepository.CountAsync(x => x.LedgerId == input.Id) > 0)
                throw new UserFriendlyException("Ledger Used in Tax");
            if (await ledgerPostingRepository.CountAsync(x =>
                    x.LedgerId == input.Id && x.VoucherTypeId != voucherTypeId) > 0)
                throw new UserFriendlyException("Ledger Reference is Used");

            await ledgerPostingRepository.DeleteAsync(x => x.LedgerId == input.Id && x.VoucherTypeId == voucherTypeId);
            await partyBalanceRepository.DeleteAsync(x => x.LedgerId == input.Id && x.VoucherTypeId == voucherTypeId);
            var accounledger = await accountLedgerRepository.FirstOrDefaultAsync(input.Id);
            accounledger.IsDelete = true;
            await accountLedgerRepository.UpdateAsync(accounledger);
        }

        public async Task<FileDto> GetAccountLedgersToExcel(GetAllUniversalInput input)
        {
            var filteredAccountLedgers = accountLedgerRepository.GetAll().Include(e => e.AccountGroupFk);
            var query = from o in filteredAccountLedgers
                        select new GetAccountLedgerForExportDto
                        {
                            Name = o.Name,
                            Phone = o.Phone,
                            OpeningBalance = o.OpeningBalance,
                            CrOrDr = o.CrOrDr,
                            CreditLimit = o.CreditLimit,
                            Id = o.Id,
                            AccountGroupName = o.AccountGroupFk.Name,
                            Pan = o.Pan,
                            Address = o.Address
                        };

            var accountLedgerListDtos = await query.ToListAsync();

            return accountLedgersExcelExporter.ExportToFile(accountLedgerListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesAccountLedgers)]
        public async Task<List<AccountLedgerAccountGroupTableDto>> GetAllAccountGroupForTableDropdown()
        {
            var data = await accountGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.Name != "Primary" && x.TenantId == AbpSession.GetTenantId())
                .Select(accountGroup => new AccountLedgerAccountGroupTableDto
                {
                    Id = accountGroup.Id,
                    DisplayName = accountGroup == null || accountGroup.Name == null ? "" : accountGroup.Name.ToString(),
                    Nature = accountGroup.Nature.ToString(),
                    IsBank = accountGroup.Name == "Bank Account" || accountGroup.Name == "Bank OD A/C" ||
                             accountGroup.Name == "Sundry Creditors" || accountGroup.Name == "Sundry Debtors"
                }).ToListAsync();
            foreach (var datum in data)
                if (datum.DisplayName == "Sundry Debtors")
                    datum.Order = 1;
                else if (datum.DisplayName == "Sundry Creditors")
                    datum.Order = 2;
                else
                    datum.Order = 3;
            var ordered = data.OrderBy(x => x.Order).ToList();
            return ordered;
        }

        protected async Task Testing()
        {
            using (unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var accountLedgers = await accountLedgerRepository.GetAll().AsNoTracking()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync();

                var accountId = accountLedgers.Select(x => x.Id).ToList();

                var missData = await ledgerPostingRepository.GetAll().AsSplitQuery()
                    .Include(x => x.AccountLedgerFk)
                    .Where(x => x.TenantId == AbpSession.TenantId && !accountId.Contains(x.LedgerId))
                    .Select(x => new { x.Id, x.LedgerId, x.AccountLedgerFk.Name, x.TenantId })
                    .AsNoTracking().ToListAsync();


                foreach (var item in missData)
                {
                    var accountLedger = accountLedgers.FirstOrDefault(x => x.Name == item.Name);
                    if (accountLedger != null)
                    {
                        var salesMaster = await ledgerPostingRepository.FirstOrDefaultAsync(item.Id);
                        salesMaster.LedgerId = accountLedger.Id;
                        await ledgerPostingRepository.UpdateAsync(salesMaster);
                    }
                }
            }
        }

        public async Task ImportAccountLedgerFromExcel(IFormFile file)
        {
            if (file == null) throw new UserFriendlyException(L("File_Empty_Error"));
            if (file.Length > 1048576 * 100)
                throw new UserFriendlyException(L("File size is greater than 100KB"));

            byte[] fileBytes;
            await using (var stream = file.OpenReadStream())
            {
                fileBytes = stream.GetAllBytes();
            }

            var tenantId = AbpSession.TenantId;
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.Now} import from excel file.");
            await BinaryObjectManager.SaveAsync(fileObject);

            await BackgroundJobManager.EnqueueAsync<ImportAccountLedgerToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                });
        }
        

        [AbpAuthorize(AppPermissions.PagesAccountLedgersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditAccountLedgerDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var accountLedgerList = accountLedgerRepository.GetAll()
                .Where(x => x.TenantId == tenantId);

            if (await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.DuplicateLedgerName, tenantId) !=
                "True")
                if (accountLedgerList.Count(x => x.Name == input.Name) > 0)
                    throw new UserFriendlyException("This LedgerName Already Exists");

            if (input.Pan != null)
                if (!string.IsNullOrEmpty(input.Pan.Trim()))
                    if (await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.DuplicatePAN,
                            tenantId) !=
                        "True")
                        if (accountLedgerList.Count(x => x.Pan == input.Pan) > 0)
                            throw new UserFriendlyException("This PAN Already Exists");

            if (input.Phone != null)
                if (!string.IsNullOrEmpty(input.Phone.Trim()))
                    if (accountLedgerList.Count(x => x.Phone == input.Phone) > 0)
                        throw new UserFriendlyException("This MobileNo Already Exists");

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");

            var accountLedger = new AccountLedger
            {
                TenantId = tenantId,
                Name = input.Name,
                OpeningBalance = input.OpeningBalance,
                IsDefault = false,
                CrOrDr = input.CrOrDr,
                Narration = input.Narration,
                Address = input.Address,
                Phone = input.Phone,
                Email = input.Email,
                CreditPeriod = input.CreditPeriod,
                CreditLimit = input.CreditLimit,
                IsBillByBill = input.IsBillByBill,
                IsDelete = false,
                Pan = input.Pan,
                UserId = null,
                Status = true,
                IsCompany = input.IsCompany,
                AccountGroupId = input.AccountGroupId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                ParentId = null
            };

            var accountId = await accountLedgerRepository.InsertAndGetIdAsync(accountLedger);

            if (input.IsBillByBill)
            {
                var partyBalance = new NewPartyBalance
                {
                    VoucherNumbering = 0,
                    Date = accountLedger.OpeningDate ?? DateTime.Now,
                    LedgerId = accountId,
                    FinancialYearId = FinancialYearId,
                    VoucherTypeId = voucherTypeId,
                    VoucherNo = "",
                    AgainstVoucherTypeId = null,
                    AgainstVoucherNo = "",
                    AgainstVoucherNumbering = 0,
                    DueDate = DateTime.Now,
                    IsFullySettled = true,
                    IsMain = true,
                    IsPartiallySettled = true,
                    MasterPartyBalanceId = null,
                    MasterId = accountId,
                    DetailId = Guid.Empty,
                    Debit = input.CrOrDr == DrOrCr.Dr ? input.OpeningBalance : 0,
                    Credit = input.CrOrDr == DrOrCr.Cr ? input.OpeningBalance : 0,
                    TenantId = tenantId
                };
                await partyBalanceRepository.InsertAsync(partyBalance);
            }

            var ledgerPosting = new LedgerPosting
            {
                VendorVoucherNo = "",
                DetailIds = "",
                VoucherNumbering = 0,
                TenantId = tenantId,
                Date = DateTime.Now,
                DetailId = Guid.Empty,
                Debit = input.CrOrDr == DrOrCr.Dr ? input.OpeningBalance : 0,
                Credit = input.CrOrDr == DrOrCr.Cr ? input.OpeningBalance : 0,
                VoucherNo = "",
                InvoiceNo = "",
                DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                LedgerId = accountId,
                MasterId = accountId,
                FinancialYearId = FinancialYearId,
                VoucherTypeId = voucherTypeId,
                PostingNumber = 0
            };
            await ledgerPostingRepository.InsertAsync(ledgerPosting);
            return accountId;
        }

        public async Task<List<AccountLedgerZeroOpeningBalanceDto>> GetAccountLedgerWithZeroOpeningBalance(
            Guid accountGroupId)
        {
            return (await accountLedgerRepository.GetAll()
                .Where(x => x.AccountGroupId == accountGroupId && x.OpeningBalance <= 0)
                .ToListAsync()).Select(x => new AccountLedgerZeroOpeningBalanceDto
                {
                    AccountLedgerId = x.Id,
                    LedgerName = x.Name,
                    OpeningBalance = x.OpeningBalance,
                    DrOrCr = x.CrOrDr
                }).ToList();
        }

        public async Task CreateAccountLedgerWithNonZeroOpeningBalance(List<AccountLedgerZeroOpeningBalanceDto> data)
        {
            foreach (var datum in data)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == datum.AccountLedgerId);
                if (accountLedger == null) continue;
                {
                    accountLedger.OpeningBalance = datum.OpeningBalance;
                    accountLedger.CrOrDr = datum.DrOrCr;
                    await accountLedgerRepository.UpdateAsync(accountLedger);

                    var ledgerPosting =
                        await ledgerPostingRepository.FirstOrDefaultAsync(x => x.LedgerId == datum.AccountLedgerId);
                    if (ledgerPosting == null) continue;
                    ledgerPosting.Debit = datum.DrOrCr == DrOrCr.Dr ? datum.OpeningBalance : 0;
                    ledgerPosting.Credit = datum.DrOrCr == DrOrCr.Cr ? datum.OpeningBalance : 0;
                    await ledgerPostingRepository.UpdateAsync(ledgerPosting);
                }
            }
        }

        public async Task CreateMultipleAccountLedger(MultipleAccountLedgerCreateDto data)
        {
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

            var objaccountLedger = await accountLedgerRepository.GetAll().ToListAsync();

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");

            foreach (var input in data.LedgerList)
                if (objaccountLedger.Count(x => x.Name == input.LedgerName) == 0)
                {
                    var accountLedger = new AccountLedger
                    {
                        TenantId = tenantId,
                        Name = input.LedgerName,
                        OpeningBalance = input.OpeningBalance,
                        IsDefault = false,
                        CrOrDr = input.DrOrCr,
                        Narration = "",
                        Address = "",
                        Phone = input.MobileNo,
                        Email = "",
                        CreditPeriod = input.CreditPeriod,
                        CreditLimit = input.CreditLimit,
                        IsBillByBill = false,
                        Pan = input.PanNumber,
                        Status = true,
                        UserId = null,
                        AccountGroupId = data.AccountGroupId
                    };

                    var accountId = await accountLedgerRepository.InsertAndGetIdAsync(accountLedger);
                    var cashAccount = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "Cash");
                    if (cashAccount == null) throw new UserFriendlyException("cash Ledger not found");
                    if (input.DrOrCr == DrOrCr.Dr)
                    {
                        //    await _accountingRepository.LedgerPostingAdd(tenantId, cashAccount.Id, accountId, input.OpeningBalance, 0, data.BranchId, FinancialYearId);
                        //    await _accountingRepository.LedgerPostingAdd(tenantId, accountId, accountId, 0, input.OpeningBalance, data.BranchId, FinancialYearId);
                    }

                    //await _accountingRepository.LedgerPostingAdd(tenantId, accountId, accountId, input.OpeningBalance, 0, data.BranchId, FinancialYearId);
                    //await _accountingRepository.LedgerPostingAdd(tenantId, cashAccount.Id, accountId, 0, input.OpeningBalance, data.BranchId, FinancialYearId);
                    var ledgerPosting = new LedgerPosting
                    {
                        VoucherNumbering = 0,
                        TenantId = tenantId,
                        Date = DateTime.Today,
                        DetailId = Guid.Empty,
                        Debit = input.DrOrCr == DrOrCr.Dr ? input.OpeningBalance : 0,
                        Credit = input.DrOrCr == DrOrCr.Cr ? input.OpeningBalance : 0,
                        VoucherNo = "Opening Balance",
                        InvoiceNo = "",
                        DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                        LedgerId = accountId,
                        MasterId = accountId,
                        FinancialYearId = FinancialYearId,
                        VoucherTypeId = voucherTypeId,
                        PostingNumber = 0
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }
        }

        [AbpAuthorize(AppPermissions.PagesAccountLedgersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditAccountLedgerDto input)
        {
            var tenantId = AbpSession.GetTenantId();

            var accountLedgerList = await accountLedgerRepository.GetAll()
                .Where(x => x.Id != input.Id).ToListAsync();

            if (await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.DuplicateLedgerName, tenantId) !=
               "True")
                if (accountLedgerList.Count(x => x.Name == input.Name) > 0)
                    throw new UserFriendlyException("This LedgerName Already Exists");

            if (input.Pan != null)
                if (!string.IsNullOrEmpty(input.Pan.Trim()))
                    if (await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.DuplicatePAN,
                            tenantId) !=
                        "True")
                        if (accountLedgerList.Count(x => x.Pan == input.Pan) > 0)
                            throw new UserFriendlyException("This PAN Already Exists");

            if (input.Phone != null)
                if (!string.IsNullOrEmpty(input.Phone.Trim()))
                    if (accountLedgerList.Count(x => x.Phone == input.Phone) > 0)
                        throw new UserFriendlyException("This MobileNo is Already Exists");

            //if (AbpSession.TenantId != null &&
            //        await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.DuplicateLedgerName,
            //            AbpSession.GetTenantId()) != "True")
            //    if (accountledgerlist.Count(x => x.Name == input.Name.Trim()) > 0)
            //        throw new UserFriendlyException("This LedgerName " + input.Name + " already Exists");

            //if (input.Pan != null)
            //    if (!string.IsNullOrEmpty(input.Pan.Trim()))
            //        if (accountledgerlist.Count(x => x.Pan == input.Pan) > 0)
            //            throw new UserFriendlyException("This PAN No is Already Exists");

            //if (input.Mobile != null)
            //    if (!string.IsNullOrEmpty(input.Mobile.Trim()))
            //        if (accountledgerlist.Count(x => x.Mobile == input.Mobile) > 0)
            //            throw new UserFriendlyException("This MobileNo is Already Exists");

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");
            if (input.Id != null)
            {
                var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
                if (accountLedger != null)
                {
                    var accountId = (Guid)input.Id;
                    accountLedger.Name = input.Name;
                    accountLedger.OpeningBalance = input.OpeningBalance;
                    accountLedger.CrOrDr = input.CrOrDr;
                    accountLedger.IsBillByBill = input.IsBillByBill;
                    accountLedger.AccountGroupId = input.AccountGroupId;
                    accountLedger.CrOrDr = input.CrOrDr;
                    accountLedger.Narration = input.Narration;
                    accountLedger.Address = input.Address;
                    accountLedger.IsCompany = input.IsCompany;
                    accountLedger.Phone = input.Phone;
                    accountLedger.Email = input.Email;
                    accountLedger.CreditPeriod = input.CreditPeriod;
                    accountLedger.CreditLimit = input.CreditLimit;
                    accountLedger.IsBillByBill = input.IsBillByBill;
                    accountLedger.Pan = input.Pan;
                    accountLedger.AccountGroupId = input.AccountGroupId;
                    accountLedger.UpdateUserId = AbpSession.UserId;
                    await accountLedgerRepository.UpdateAsync(accountLedger);

                    await ledgerPostingRepository.DeleteAsync(x =>
                        x.LedgerId == input.Id && x.VoucherTypeId == voucherTypeId &&
                        x.FinancialYearId == FinancialYearId);
                    await partyBalanceRepository.DeleteAsync(x =>
                        x.LedgerId == input.Id && x.VoucherTypeId == voucherTypeId &&
                        x.VoucherTypeId == voucherTypeId && x.FinancialYearId == FinancialYearId &&
                        x.MasterId == accountId);
                    if (input.IsBillByBill)
                    {
                        var partyBalance = new NewPartyBalance
                        {
                            VoucherNumbering = 0,
                            Date = accountLedger.OpeningDate ?? DateTime.Now,
                            LedgerId = accountId,
                            FinancialYearId = FinancialYearId,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = "",
                            AgainstVoucherTypeId = null,
                            AgainstVoucherNo = "",
                            AgainstVoucherNumbering = 0,
                            DueDate = DateTime.Now,
                            IsFullySettled = true,
                            IsMain = true,
                            IsPartiallySettled = true,
                            MasterPartyBalanceId = null,
                            MasterId = accountId,
                            DetailId = Guid.Empty,
                            Debit = input.CrOrDr == DrOrCr.Dr ? input.OpeningBalance : 0,
                            Credit = input.CrOrDr == DrOrCr.Cr ? input.OpeningBalance : 0,
                            TenantId = tenantId
                        };
                        await partyBalanceRepository.InsertAsync(partyBalance);
                    }

                    var ledgerPosting = new LedgerPosting
                    {
                        VoucherNumbering = 0,
                        TenantId = tenantId,
                        Date = DateTime.Today,
                        DetailId = Guid.Empty,
                        Debit = input.CrOrDr == DrOrCr.Dr ? input.OpeningBalance : 0,
                        Credit = input.CrOrDr == DrOrCr.Cr ? input.OpeningBalance : 0,
                        VoucherNo = "Opening Balance",
                        InvoiceNo = "",
                        VendorVoucherNo = "",
                        DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                        LedgerId = accountId,
                        MasterId = accountId,
                        FinancialYearId = FinancialYearId,
                        VoucherTypeId = voucherTypeId,
                        PostingNumber = 0
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }
                else
                {
                    throw new UserFriendlyException("Account ledger for id " + input.Id + " not found.");
                }
            }

            return (Guid)input.Id;
        }

        public async Task<bool> CheckPanAndMobile(string pan, string mobileNo)
        {
            var accountLedger =
                await accountLedgerRepository.FirstOrDefaultAsync(x => x.Pan == pan && x.Phone == mobileNo);
            return accountLedger != null;
        }

        [DisableAuditing]
        public async Task<PagedResultDto<GetAccountLedgerForViewDto>> GetAllAccountLedgersByAccountGroupId(
            Guid accountGroupId)
        {
            var accountLedgers = accountLedgerRepository.GetAll()
                .Include(e => e.AccountGroupFk).Where(x => x.AccountGroupId == accountGroupId);

            var accountLedgerlist = new List<GetAccountLedgerForViewDto>();
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");
            var ledgerposting = await ledgerPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId).ToListAsync();
            foreach (var item in accountLedgers)
            {
                var accountledger = new GetAccountLedgerForViewDto();

                var posting = ledgerposting
                    .FirstOrDefault(x => x.LedgerId == item.Id && x.VoucherTypeId == voucherTypeId);
                if (posting != null)
                {
                    if (posting.Debit > posting.Credit)
                    {
                    }

                    if (posting.Debit < posting.Credit)
                    {
                    }
                }

                accountledger.Name = item.Name;
                accountledger.CreditLimit = item.CreditLimit;
                accountledger.Id = item.Id;
                accountledger.Phone = item.Phone;
                accountledger.PaNumber = item.Pan;
                accountledger.AccountGroupName = item.AccountGroupFk.Name;
                accountLedgerlist.Add(accountledger);
            }

            var totalCount = accountLedgerlist.Count();

            return new PagedResultDto<GetAccountLedgerForViewDto>(
                totalCount,
                accountLedgerlist
            );
        }
    }
}
