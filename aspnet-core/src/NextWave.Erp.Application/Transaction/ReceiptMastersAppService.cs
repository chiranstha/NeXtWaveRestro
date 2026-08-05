using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Notifications;
using NextWave.Erp.Transaction.Dtos;
using NextWave.Erp.Transaction.Enums;
using NextWave.Erp.Transaction.Exporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    public class ReceiptMastersAppService(
    IRepository<ReceiptMaster, Guid> receiptMasterRepository,
    IReceiptMastersExcelExporter receiptMastersExcelExporter,
    IRepository<User, long> userRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<ReceiptDetail, Guid> receiptDetailRepository,
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<NewPartyBalance, Guid> newPartyBalanceRepository,
    IRepository<VoucherType, Guid> voucherTypeRepository,
    PartyBalanceService partyBalanceService,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<PartyBalance, Guid> partyBalanceRepository,
    IRepository<Branch, Guid> branchRepository,
    IDocumentsAppService documentsAppService,
    IAppNotifier appNotifier,
    UserManager userManager,
    IRepository<UserBranch, Guid> userBranchRepository)
    : ErpAppServiceBase, IReceiptMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetReceiptMasterForViewDto>> GetAll(GetAllReceiptMastersInput input)
        {
            // await FixedError1();
            //  var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var filteredReceiptMasters = receiptMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.VoucherNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.TotalAmount,
                    x.Description,
                    x.VoucherTypeId,
                    x.LedgerId,
                    x.Date,
                    x.DateMiti,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredReceiptMasters = filteredReceiptMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredReceiptMasters = filteredReceiptMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredReceiptMasters = filteredReceiptMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var receiptMasters = from o in pagedAndFilteredReceiptMasters
                                 join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.CreateUserId equals o6.Id into j6
                                 from s6 in j6.DefaultIfEmpty()
                                 join o7 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.UpdateUserId equals o7.Id into j7
                                 from s7 in j7.DefaultIfEmpty()
                                 select new GetReceiptMasterForViewDto
                                 {
                                     VoucherNo = o.VoucherNo,
                                     Date = o.Date,
                                     DateMiti = o.DateMiti,
                                     TotalAmount = o.TotalAmount,
                                     Description = o.Description,
                                     VoucherTypeId = o.VoucherTypeId,
                                     Id = o.Id,
                                     CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                     UpdateUser = s7 == null || s7.Name == null ? "" : s7.Name,
                                     LedgerName = o.LedgerName,
                                 };

            var totalCount = await filteredReceiptMasters.CountAsync();
            await FixedReceiptVoucherError();
            return new PagedResultDto<GetReceiptMasterForViewDto>(
                totalCount,
                await receiptMasters.ToListAsync()
            );
        }


        [AbpAuthorize(AppPermissions.PagesReceiptMastersEdit)]
        public async Task<CreateOrEditReceiptMasterDto> GetReceiptMasterForEditOld(Guid id)
        {
            using var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false });
            try
            {
                // 1. Load receipt master with minimal data needed - avoid over-fetching
                var receiptMaster = await receiptMasterRepository.GetAsync(id);

                // 2. Prepare the result DTO early
                var result = new CreateOrEditReceiptMasterDto
                {
                    Id = receiptMaster.Id,
                    VoucherNo = receiptMaster.VoucherNo,
                    DateMiti = receiptMaster.DateMiti,
                    TotalAmount = receiptMaster.TotalAmount,
                    Description = receiptMaster.Description,
                    LedgerId = receiptMaster.LedgerId,
                };

                // 3. Load receipt details with account ledger data in a single query with projection
                var receiptDetails = await receiptDetailRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.ReceiptMasterId == id && x.TenantId == AbpSession.TenantId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Amount,
                        x.ChequeNo,
                        x.ChequeMiti,
                        x.LedgerId,
                        x.AccountLedgerFk.IsBillByBill
                    })
                    .ToListAsync();

                // 4. Identify bill-by-bill ledger IDs to query party balances efficiently
                var billByBillLedgerIds = receiptDetails
                    .Where(x => x.IsBillByBill)
                    .Select(x => x.LedgerId)
                    .Distinct()
                    .ToList();

                // 5. Load party balances for all relevant ledgers in a single batch
                var partyBalancesMap = new Dictionary<Guid, List<PartyBalance>>();

                // Only query if we have bill-by-bill ledgers
                if (billByBillLedgerIds.Any())
                {
                    // Create an index for paid party balances by ledger ID for quick lookup
                    var paidBalancesByLedgerId = new Dictionary<Guid, HashSet<Guid>>();

                    // Get all paid party balances for this receipt in a single query
                    var paidPartyBalances = await partyBalanceRepository.GetAll()
                        .AsNoTracking()
                        .Where(x =>
                            x.MasterVoucherNo == receiptMaster.VoucherNo &&
                            x.MasterVoucherTypeId == receiptMaster.VoucherTypeId &&
                            billByBillLedgerIds.Contains(x.LedgerId))
                        .ToListAsync();

                    // Organize paid balances by ledger ID for efficient lookup
                    foreach (var pb in paidPartyBalances)
                    {
                        if (!paidBalancesByLedgerId.TryGetValue(pb.LedgerId, out var idSet))
                        {
                            idSet = new HashSet<Guid>();
                            paidBalancesByLedgerId[pb.LedgerId] = idSet;
                        }

                        idSet.Add(pb.Id);
                    }

                    // Get all relevant party balances in a single query
                    var allPartyBalances = await partyBalanceRepository.GetAll()
                        .AsNoTracking()
                        .Where(e =>
                            billByBillLedgerIds.Contains(e.LedgerId) &&
                            e.FinancialYearId == FinancialYearId)
                        .ToListAsync();

                    // 6. Preload all relevant voucher types to avoid N+1 queries
                    var allVoucherTypeIds = allPartyBalances
                        .Where(pb => pb.VoucherTypeId.HasValue)
                        .Select(pb => pb.VoucherTypeId.Value)
                        .Distinct()
                        .ToList();

                    var voucherTypeNames = await voucherTypeRepository.GetAll()
                        .AsNoTracking()
                        .Where(v => allVoucherTypeIds.Contains(v.Id))
                        .ToDictionaryAsync(v => v.Id, v => v.Name);

                    // 7. Group party balances by ledger ID for fast lookups
                    foreach (var ledgerId in billByBillLedgerIds)
                    {
                        // Calculate all payment data per ledger in memory
                        var ledgerBalances = allPartyBalances
                            .Where(pb => pb.LedgerId == ledgerId)
                            .ToList();

                        partyBalancesMap[ledgerId] = ledgerBalances;
                    }
                }

                // 8. Process details with preloaded data
                var detailsDto = new List<CreateOrEditReceiptDetailDto>();

                foreach (var detail in receiptDetails)
                {
                    var detailDto = new CreateOrEditReceiptDetailDto
                    {
                        Id = detail.Id,
                        Amount = detail.Amount,
                        IsBillByBill = detail.IsBillByBill,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        LedgerId = detail.LedgerId
                    };

                    // Only process party balance for bill-by-bill ledgers
                    if (detail.IsBillByBill && partyBalancesMap.TryGetValue(detail.LedgerId, out var balances))
                        //detailDto.PartyBalanceDetail = await ProcessPartyBalanceForLedger(
                        //    balances,
                        //    detail.LedgerId,
                        //    receiptMaster.BranchId,
                        //    receiptMaster.VoucherNo,
                        //    receiptMaster.VoucherTypeId);

                        detailsDto.Add(detailDto);
                }

                result.ReceiptDetails = detailsDto;
                await uow.CompleteAsync();
                return result;
            }
            catch (Exception ex)
            {
                // Replace the line with RollbackAsync as it is not a valid method for IUnitOfWorkCompleteHandle
                // Use Dispose() to rollback the transaction as per the ABP framework's unit of work pattern.
                uow.Dispose();
                Logger.Error("Error in GetReceiptMasterForEdit", ex);
                throw;
            }
        }

        [AbpAuthorize(AppPermissions.PagesReceiptMastersEdit)]
        public async Task<CreateOrEditReceiptMasterDto> GetReceiptMasterForEdit(Guid id)
        {
            using var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false });
            try
            {
                var receiptMaster = await receiptMasterRepository.GetAsync(id);

                var result = new CreateOrEditReceiptMasterDto
                {
                    Id = receiptMaster.Id,
                    VoucherNo = receiptMaster.VoucherNo,
                    DateMiti = receiptMaster.DateMiti,
                    TotalAmount = receiptMaster.TotalAmount,
                    Description = receiptMaster.Description,
                    LedgerId = receiptMaster.LedgerId,
                };
                var receiptDetails = await receiptDetailRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.ReceiptMasterId == id && x.TenantId == AbpSession.TenantId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Amount,
                        x.ChequeNo,
                        x.ChequeMiti,
                        x.LedgerId,
                        x.AccountLedgerFk.IsBillByBill
                    })
                    .ToListAsync();
                var detailsDto = new List<CreateOrEditReceiptDetailDto>();

                var databaseData = await newPartyBalanceRepository.GetAll().Where(x => x.MasterId == id)
                    .Include(x => x.VoucherTypeFk).ToListAsync();
                int sn = 0;
                foreach (var detail in receiptDetails)
                {
                    var det = new GetReceiptAgainstMasterDto();
                    var details = databaseData.Where(x => x.DetailId == detail.Id && x.AgainstVoucherTypeId != Guid.Empty).Select(x => new GetReceiptAgainstDto
                    {
                        PartyBalanceId = x.Id,
                        BillDate = DateConverter.ConvertToNepali(x.Date),
                        DueDate = DateConverter.ConvertToNepali(x.DueDate),
                        VoucherType = x.VoucherTypeFk.Name ?? "Unknown",
                        VoucherNo = x.VoucherNo ?? "Unknown",
                        VoucherNumbering = x.VoucherNumbering,
                        VoucherTypeId = x.VoucherTypeId,
                        BillAmount = 0,
                        PaidAmount = 0,
                        BalanceAmount = 0,
                        Adjust = x.Credit,
                        IsSettled = x.IsFullySettled
                    }).ToList();
                    foreach (var deta in details)
                    {
                        var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == deta.VoucherNo && x.VoucherTypeId == deta.VoucherTypeId && x.VoucherNumbering == deta.VoucherNumbering && x.FinancialYearId == FinancialYearId).ToListAsync();
                        deta.BillAmount = relatedData.Where(x => x.Id != deta.PartyBalanceId && x.DetailId == Guid.Empty).Sum(x => x.Debit - x.Credit);
                        deta.PaidAmount = relatedData.Where(x => x.Id != deta.PartyBalanceId && x.DetailId != Guid.Empty).Sum(x => x.Credit - x.Debit);
                        deta.BalanceAmount = deta.BillAmount - deta.PaidAmount;
                    }
                    details.AddRange((await partyBalanceService.GetReceiptAgainstAmount(detail.LedgerId, detail.Amount, detail.Id)).Details);
                    det.Details = details;
                    det.NewReferenceAmount = databaseData.Where(x => x.VoucherTypeId == receiptMaster.VoucherTypeId && x.VoucherNo == receiptMaster.VoucherNo && x.VoucherNumbering == receiptMaster.VoucherNumbering).Sum(x => x.Credit);
                    detailsDto.Add(new CreateOrEditReceiptDetailDto
                    {
                        Id = detail.Id,
                        Amount = detail.Amount,
                        IsBillByBill = detail.IsBillByBill,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        LedgerId = detail.LedgerId,
                        PartyBalanceDetail = det
                    });
                }

                result.ReceiptDetails = detailsDto;
                await uow.CompleteAsync();
                return result;
            }
            catch (Exception ex)
            {
                // Replace the line with RollbackAsync as it is not a valid method for IUnitOfWorkCompleteHandle
                // Use Dispose() to rollback the transaction as per the ABP framework's unit of work pattern.
                uow.Dispose();
                Logger.Error("Error in GetReceiptMasterForEdit", ex);
                throw;
            }
        }



        public async Task<Guid> CreateOrEdit(CreateOrEditReceiptMasterDto input)
        {
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            if (FinancialYear.FromDate > date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            if (FinancialYear.ToDate < date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);

            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesReceiptMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var master = await receiptMasterRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.Id == input.Id && x.FinancialYearId == FinancialYearId);
            if (master != null)
            {
                await receiptDetailRepository.DeleteAsync(x => x.ReceiptMasterId == input.Id);
                await ledgerPostingRepository.DeleteAsync(x =>
                    x.VoucherNo == master.VoucherNo &&
                    x.VoucherNumbering == master.VoucherNumbering &&
                    x.VoucherTypeId == master.VoucherTypeId && x.FinancialYearId == master.FinancialYearId);

                await receiptMasterRepository.DeleteAsync(input.Id);

                //await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == master.VoucherNo &&
                //                                              x.MasterVoucherTypeId == master.VoucherTypeId &&
                //                                              x.FinancialYearId == FinancialYearId &&
                //                                              x.BranchId == master.BranchId);
                await partyBalanceService.DeleteReceiptTaskAsync(input.Id);
            }
        }

        public async Task<FileDto> GetReceiptMastersToExcel(GetAllReceiptMastersForExcelInput input)
        {
            var filteredReceiptMasters = receiptMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk).Include(x => x.VoucherTypeFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.Description.Contains(input.Filter)).OrderBy(x => x.VoucherNumbering);

            var query = from o in filteredReceiptMasters
                        join o1 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on o.LedgerId
                            equals o1.Id into j1
                        from s1 in j1.DefaultIfEmpty()
                        select new GetReceiptMasterForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Date = o.Date,
                            TotalAmount = o.TotalAmount,
                            Description = o.Description,
                            Id = o.Id,
                            LedgerName = s1 == null || s1.Name == null ? "" : s1.Name,
                        };

            var receiptMasterListDtos = await query.ToListAsync();
            return receiptMastersExcelExporter.ExportToFile(receiptMasterListDtos);
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesReceiptMasters)]
        public async Task<List<ReceiptMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Include(x => x.AccountGroupFk)
                .Select(accountLedger => new ReceiptMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name.ToString(),
                    AccountGroup = accountLedger.AccountGroupFk.Name,
                    IsBillByBill = accountLedger.IsBillByBill
                }).ToListAsync();
        }

        private async Task FixedReceiptVoucherError()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .Where(lp => !receiptMasterRepository.GetAll()
                        .Any(sm => sm.VoucherTypeId == lp.VoucherTypeId &&
                                   sm.FinancialYearId == lp.FinancialYearId &&
                                   sm.VoucherNo == lp.VoucherNo &&
                                   sm.TenantId == lp.TenantId))
                    .Select(lp => lp.Id)
                    .ToListAsync();

                // Batch delete orphaned ledger postings in chunks
                if (orphanedLedgerPostingIds.Any())
                {
                    const int batchSize = 1000;
                    for (var i = 0; i < orphanedLedgerPostingIds.Count; i += batchSize)
                    {
                        var batchIds = orphanedLedgerPostingIds.Skip(i).Take(batchSize).ToList();
                        await ledgerPostingRepository.DeleteAsync(p => batchIds.Contains(p.Id));
                        await UnitOfWorkManager.Current.SaveChangesAsync(); // Save each batch
                    }
                }

                await FixedLedgerPosting();
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately
                Logger.Debug($"An error occurred: {ex.Message}");
            }
        }

        public async Task FixedError()
        {
            // Fetch only the first 15 sets of duplicate vouchers to minimize data transfer
            var duplicateVoucherGroups = await receiptMasterRepository.GetAll()
                .GroupBy(v => v.VoucherNo)
                .Where(g => g.Count() > 1)
                //.Take(15) // Limit to 15 groups right at the database level
                .Select(g => g.ToList())
                .ToListAsync();
            //   var branchId = ErpCommonManager.GetMainBranchId();
            foreach (var voucherGroup in duplicateVoucherGroups)
            {
                var numb = await GetInlineVoucherNo();
                // Skip the first voucher (keep its original number)
                for (var i = 1; i < voucherGroup.Count; i++)
                {
                    var voucher = voucherGroup[i];


                    // Fetch new voucher numbers only once per branch to reduce database calls
                    voucher.VoucherNo = "BA" + numb;
                    voucher.VoucherNumbering = numb;
                    numb++;
                }
            }

            // Use bulk update if available in your repository to reduce round trips
            // If not available, batch updates in smaller chunks
            foreach (var voucherGroup in duplicateVoucherGroups)
                // Skip the first voucher in each group
                for (var i = 1; i < voucherGroup.Count; i++)
                    await receiptMasterRepository.UpdateAsync(voucherGroup[i]);
        }

        public async Task FixedError1()
        {
            var receiptDetail = await receiptMasterRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.GetTenantId())
                .ToListAsync();
            var num = 1;
            foreach (var detail in receiptDetail.OrderBy(e => e.Date).ToList())
            {
                detail.VoucherNo = "BR" + num;
                detail.VoucherNumbering = num;
                num++;
                await receiptMasterRepository.UpdateAsync(detail);
            }
        }


        public async Task FixedLedgerPosting()
        {
            try
            {
                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await receiptMasterRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId)
                    .Where(lp => !ledgerPostingRepository.GetAll()
                        .Any(sm => sm.VoucherTypeId == lp.VoucherTypeId &&
                                   sm.FinancialYearId == lp.FinancialYearId &&
                                   sm.VoucherNo == lp.VoucherNo &&
                                   sm.VoucherTypeId == lp.VoucherTypeId &&
                                   sm.TenantId == lp.TenantId))
                    .Select(lp => lp.Id)
                    .ToListAsync();

                // Batch delete orphaned ledger postings in chunks
                if (orphanedLedgerPostingIds.Any())
                {
                    const int batchSize = 1000;
                    for (var i = 0; i < orphanedLedgerPostingIds.Count; i += batchSize)
                    {
                        var batchIds = orphanedLedgerPostingIds.Skip(i).Take(batchSize).ToList();


                        // Fetch all necessary data in single queries with optimized selectors
                        var receiptMasters = await receiptMasterRepository.GetAll()
                            .Where(e => e.TenantId == AbpSession.TenantId && e.FinancialYearId == FinancialYearId &&
                                        batchIds.Contains(e.Id))
                            .Select(e => new
                            {
                                e.Id,
                                e.TenantId,
                                e.DateMiti,
                                e.VoucherNumbering,
                                e.VoucherNo,
                                e.TotalAmount,
                                e.LedgerId,
                                e.PostingNumbering,
                                e.VoucherTypeId
                            })
                            .AsNoTracking()
                            .ToListAsync();

                        var receiptDetails = await receiptDetailRepository.GetAll()
                            .Where(e => e.TenantId == AbpSession.TenantId &&
                                        e.ReceiptMasterFk.FinancialYearId == FinancialYearId)
                            .Select(e => new
                            {
                                e.ReceiptMasterId,
                                e.Amount,
                                e.LedgerId,
                                e.ChequeNo,
                                e.ChequeMiti
                            })
                            .AsNoTracking()
                            .ToListAsync();

                        // Group details by master ID for faster lookup
                        var detailsByMasterId = receiptDetails.GroupBy(d => d.ReceiptMasterId)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        // Prepare a batch of ledger postings for bulk insert
                        var ledgerPostings = new List<LedgerPosting>();

                        foreach (var master in receiptMasters)
                        {
                            // Skip if no details found (defensive programming)
                            if (!detailsByMasterId.TryGetValue(master.Id, out var details) || !details.Any())
                                continue;

                            // Create credit entries for each detail
                            ledgerPostings.AddRange(details.Select(detail => new LedgerPosting
                            {
                                TenantId = master.TenantId,
                                Date = DateConverter.ConvertToEnglish(master.DateMiti),
                                VoucherNumbering = master.VoucherNumbering,
                                VoucherNo = master.VoucherNo,
                                Debit = 0,
                                Credit = detail.Amount,
                                InvoiceNo = master.VoucherNo,
                                FinancialYearId = FinancialYearId,
                                DetailId = master.LedgerId,
                                DateMiti = master.DateMiti,
                                VoucherTypeId = master.VoucherTypeId,
                                LedgerId = detail.LedgerId,
                                MasterId = master.Id,
                                PostingNumber = master.PostingNumbering
                            }));

                            // Create the debit entry
                            ledgerPostings.Add(new LedgerPosting
                            {
                                TenantId = master.TenantId,
                                Date = DateConverter.ConvertToEnglish(master.DateMiti),
                                VoucherNumbering = master.VoucherNumbering,
                                VoucherNo = master.VoucherNo,
                                Debit = master.TotalAmount,
                                Credit = 0,
                                InvoiceNo = master.VoucherNo,
                                FinancialYearId = FinancialYearId,
                                DetailId = details.FirstOrDefault().LedgerId,
                                DateMiti = master.DateMiti,
                                VoucherTypeId = master.VoucherTypeId,
                                LedgerId = master.LedgerId,
                                MasterId = master.Id,
                                PostingNumber = master.PostingNumbering
                            });
                        }

                        // Perform bulk insert instead of individual inserts
                        if (ledgerPostings.Any()) await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);

                        await UnitOfWorkManager.Current.SaveChangesAsync(); // Save each batch
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately
                Logger.Debug($"An error occurred: {ex.Message}");
            }
        }

        private async Task<List<PartyBalanceAdjustDto>> ProcessPartyBalanceForLedger(
            List<PartyBalance> partyBalances,
            Guid ledgerId,
            Guid branchId,
            string currentMasterVoucherNo,
            Guid currentMasterVoucherTypeId)
        {
            // Use the already loaded balances to avoid a new query
            // Get all voucher types for reference - use a task to load this data only once and cache it
            var voucherTypesTask = voucherTypeRepository.GetAll()
                .AsNoTracking()
                .Where(vt => vt.TenantId == AbpSession.TenantId)
                .ToDictionaryAsync(v => v.Id, v => v.Name);

            // Get paid party balances from the passed balances
            var paidPartyBalances = partyBalances
                .Where(x => x.MasterVoucherNo == currentMasterVoucherNo &&
                            x.MasterVoucherTypeId == currentMasterVoucherTypeId)
                .ToList();

            // Get the IDs of paid party balances to exclude them
            var paidPartyIds = new HashSet<Guid>(paidPartyBalances.Select(p => p.Id));

            // Filter to only get New or OnAccount entries that aren't already paid
            var filteredPartyBalances = partyBalances
                .Where(e => (e.ReferenceType == "New" || e.ReferenceType == "OnAccount") &&
                            !paidPartyIds.Contains(e.Id))
                .ToList();

            // Dictionary to track payments by voucher for efficient lookups
            var paidTransactionsByVoucher = new Dictionary<(Guid?, string), decimal>();
            foreach (var item in partyBalances.Where(x =>
                         x.ReferenceType == ReferenceType.Against.ToString() &&
                         !paidPartyIds.Contains(x.Id)))
            {
                var key = (item.VoucherTypeId, item.VoucherNo ?? string.Empty);
                if (!paidTransactionsByVoucher.TryGetValue(key, out _)) paidTransactionsByVoucher[key] = 0;

                paidTransactionsByVoucher[key] += item.Credit - item.Debit;
            }

            // Dictionary to track adjustments by voucher
            var adjustedAmountByVoucher = new Dictionary<(Guid?, string), decimal>();
            foreach (var item in paidPartyBalances.Where(x =>
                         x.ReferenceType == ReferenceType.Against.ToString()))
            {
                var key = (item.VoucherTypeId, item.VoucherNo ?? string.Empty);
                if (!adjustedAmountByVoucher.TryGetValue(key, out _)) adjustedAmountByVoucher[key] = 0;

                adjustedAmountByVoucher[key] += item.Credit - item.Debit;
            }

            var returnDebit = new List<PartyBalanceAdjustDto>();

            // Await voucher types only once
            var voucherTypes = await voucherTypesTask;

            // Process each filtered balance entry
            foreach (var detail in filteredPartyBalances.OrderBy(e => e.Date))
            {
                if (!detail.VoucherTypeId.HasValue)
                    continue;

                voucherTypes.TryGetValue(detail.VoucherTypeId.Value, out var voucherName);
                var totalAmt = detail.Debit - detail.Credit;

                var voucherKey = (detail.VoucherTypeId, detail.VoucherNo ?? string.Empty);

                // Check if any paid transactions exist for this voucher
                paidTransactionsByVoucher.TryGetValue(voucherKey, out var paidTransactions);

                // Check if any adjusted amount exists for this voucher
                adjustedAmountByVoucher.TryGetValue(voucherKey, out var adjustedAmount);

                var balanceAmt = totalAmt - paidTransactions;

                // Skip if balance is zero
                if (balanceAmt == 0) continue;

                returnDebit.Add(new PartyBalanceAdjustDto
                {
                    Id = detail.Id,
                    VoucherNo = detail.VoucherNo ?? "",
                    VoucherTypeId = (Guid)detail.VoucherTypeId,
                    VoucherTypeName = voucherName ?? "Unknown",
                    Type = detail.ReferenceType,
                    PayDate = DateConverter.ConvertToNepali(detail.Date),
                    DueDate = DateConverter.ConvertToNepali(detail.Date.AddDays(detail.CreditPeriod)),
                    BalanceString = $"{Math.Abs(balanceAmt):N2}{(balanceAmt > 0 ? " Dr" : " Cr")}",
                    Balance = balanceAmt,
                    BillAmt = Math.Abs(totalAmt),
                    Paid = paidTransactions,
                    Adjust = adjustedAmount,
                    IsDisable = balanceAmt < 0 || adjustedAmount < 0
                });
            }

            return returnDebit.OrderBy(e => e.PayDate).ToList();
        }

        public async Task<PaymentMasterForViewNewDto> GetReceiptMasterForViewNew(Guid id)
        {
            var result = new PaymentMasterForViewNewDto();
            var receiptMaster = await receiptMasterRepository.GetAll()
                .Where(x => x.Id == id).Include(x => x.AccountLedgerFk).FirstOrDefaultAsync();
            if (receiptMaster != null)
            {
                result.VoucherNo = receiptMaster.VoucherNo;
                result.DateMiti = receiptMaster.DateMiti;
                result.TotalAmount = receiptMaster.TotalAmount;
                result.Description = receiptMaster.Description;
                result.LedgerName = receiptMaster.AccountLedgerFk.Name;
            }

            var receiptDetails = await receiptDetailRepository.GetAll()
                .Where(x => x.ReceiptMasterId == id).Include(x => x.AccountLedgerFk).Select(x =>
                    new PaymentDetailForViewDetailDto
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        ChequeMiti = x.ChequeMiti,
                        ChequeNo = x.ChequeNo,
                        LedgerId = x.LedgerId,
                        LedgerName = x.AccountLedgerFk.Name,
                        IsBillByBill = x.AccountLedgerFk.IsBillByBill
                    }).ToListAsync();
            var voucherTypes = await voucherTypeRepository.GetAllListAsync();
            foreach (var bill in receiptDetails.Where(x => x.IsBillByBill))
            {
                var partyBalances = await partyBalanceRepository.GetAll()
                    .Where(x => x.LedgerId == bill.LedgerId && x.FinancialYearId == FinancialYearId &&
                                x.VoucherNumbering == receiptMaster.VoucherNumbering &&
                                /*((x.IsAgainst && x.VoucherNo == result.VoucherNo && x.VoucherTypeId == receiptMaster.VoucherTypeId) ||
                                (!x.IsAgainst && x.AgainstVoucherNo == result.VoucherNo && x.AgainstVoucherTypeId == receiptMaster.VoucherTypeId))*/
                                x.MasterVoucherNo == receiptMaster.VoucherNo &&
                                x.MasterVoucherTypeId == receiptMaster.VoucherTypeId).ToListAsync();

                var finalPb = new List<PartyaBalanceForPaymentMasterDto>();

                foreach (var x in partyBalances)
                    finalPb.Add(new PartyaBalanceForPaymentMasterDto
                    {
                        VoucherNo = x.VoucherNo,
                        VoucherTypeId = x.IsAgainst ? x.AgainstVoucherTypeId : x.VoucherTypeId,
                        VoucherTypeName = x.IsAgainst
                            ? voucherTypes.FirstOrDefault(y => y.Id == x.AgainstVoucherTypeId)?.Name
                            : x.VoucherTypeFk.Name,
                        ReferenceType = x.ReferenceType,
                        Amount = x.Debit == 0 ? x.Credit : x.Debit
                    });
                bill.PartyBalances = finalPb;
            }

            result.Details = receiptDetails;
            return result;
        }

        [DisableAuditing]
        public async Task<string> GetLedgerBalanceStatus(Guid ledgerId)
        {
            var ledgerPostings = await ledgerPostingRepository.GetAll()
                .Where(x => x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId)
                .ToListAsync();

            var balance = ledgerPostings.Sum(x => x.Debit - x.Credit);
            return balance > 0 ? balance + "Dr" : Math.Abs(balance) + "Cr";
        }


        public async Task<GetPdfForReceiptMaster> GetReceiptMasterForPdf(EntityDto<Guid> input)
        {
            var serial = 1;
            var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(input.Id);
            var mainCompany =
                await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            //  var companyInfo = await branchService.GetBranchForView(receiptMaster.BranchId);
            var receiptDetails = await receiptDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ReceiptMasterId == receiptMaster.Id)
                .Include(x => x.AccountLedgerFk).AsNoTracking().ToListAsync();
            var output = new GetPdfForReceiptMaster
            {
                Logo1 = mainCompany.Image1,
                CompanyName = mainCompany.CompanyName,
                BranchAddress = mainCompany.Address,
                BranchContact = mainCompany.PhoneNo1,
                VoucherNo = receiptMaster.VoucherNo,
                Date = receiptMaster.Date,
                TotalAmountInWord = CurrencyToAmount.NumberToText((int)receiptMaster.TotalAmount),
                TotalAmount = receiptMaster.TotalAmount,
                Narration = receiptMaster.Description,
                DateMiti = receiptMaster.DateMiti,
                LedgerId = receiptMaster.LedgerId,
                ReceiptDetails = receiptDetails.Select(x => new GetPdfForReceiptDetail
                {
                    SlNo = serial++,
                    Amount = x.Amount,
                    ChequeNo = x.ChequeNo,
                    ChequeMiti = x.ChequeMiti,
                    LedgerName = x.AccountLedgerFk.Name
                }).ToList()
            };
            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        //[AbpAuthorize(AppPermissions.PagesReceiptMastersPrint)]
        //public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        //{
        //    //var filePath = "ReceiptMaster.pdf";
        //    var model = await GetReceiptMasterForPdf(input);
        //    var document = new ReceiptMasterPdf(model);
        //    Stream stream = new MemoryStream(document.GeneratePdf());
        //    var mailMessage = new StringBuilder();

        //    mailMessage.AppendLine("<b>" + L("Message") + "</b>: " +
        //                           L("",
        //                               " UTC") + "<br />");
        //    mailMessage.AppendLine("<br />");
        //    return document.GeneratePdf();
        //    //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    //{
        //    //    Process.Start("explorer.exe", filePath);
        //    //}
        //    //else
        //    //{
        //    //    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        //    //    Console.WriteLine($"Output PDF file is available here: {fullPath}");
        //    //}
        //}

        //public async Task GetPdfDownload(EntityDto<Guid> input)
        //{
        //    var filePath = "ReceiptMaster.pdf";
        //    var model = await GetReceiptMasterForPdf(input);
        //    //List<PdfForStockTransferModel> list = new List<PdfForStockTransferModel>();
        //    //list.Add(model);
        //    var document = new ReceiptMasterPdf(model);
        //    document.GeneratePdf(filePath);

        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        Process.Start("explorer.exe", filePath);
        //    }
        //    else
        //    {
        //        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        //        Console.WriteLine($"Output PDF file is available here: {fullPath}");
        //    }
        //}

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            return await receiptMasterRepository.CountAsync(x =>
                x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId &&
                x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = receiptMaster.VoucherTypeId,
                VoucherNo = receiptMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }


        [DisableAuditing]
        public async Task<List<PartyBalanceAdjustDto>> GetPartyBalanceDebit(Guid ledgerId)
        {
            // Get all relevant party balances in a single query
            var partyBalances = await partyBalanceRepository.GetAll()
                .AsNoTracking()
                .Where(e => e.LedgerId == ledgerId && e.FinancialYearId == FinancialYearId)
                .ToListAsync();

            // Get all voucher types for reference
            var voucherTypes = await voucherTypeRepository.GetAll()
                .AsNoTracking()
                .ToDictionaryAsync(v => v.Id, v => v.Name);

            // Separate original entries from against entries
            var originalEntries = partyBalances
                .Where(e => e.ReferenceType is "New" or "OnAccount" &&
                            e.VoucherTypeId.HasValue &&
                            !string.IsNullOrEmpty(e.VoucherNo))
                .ToList();

            // Create a lookup of payments organized by what they're against
            var paymentsLookup = partyBalances
                .Where(e => e.ReferenceType == ReferenceType.Against.ToString() &&
                            e.AgainstVoucherTypeId.HasValue &&
                            !string.IsNullOrEmpty(e.AgainstVoucherNo))
                .GroupBy(e => new { e.AgainstVoucherTypeId, e.AgainstVoucherNo })
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Credit - e.Debit)
                );

            // Generate the result list
            var result = new List<PartyBalanceAdjustDto>();
            foreach (var entry in originalEntries)
            {
                // Calculate total amount
                var totalAmt = entry.Debit - entry.Credit;

                // REMOVE THIS LINE to include zero amount entries
                // if (totalAmt == 0) continue;

                // Get paid amount from the lookup
                decimal paidAmt = 0;

                // Try to find payments made against this entry
                if (entry.VoucherTypeId.HasValue)
                {
                    var lookupKey = new { AgainstVoucherTypeId = entry.VoucherTypeId, AgainstVoucherNo = entry.VoucherNo };
                    paymentsLookup.TryGetValue(lookupKey, out paidAmt);
                }

                // Calculate balance
                var balanceAmt = totalAmt - paidAmt;

                // REMOVE THIS LINE to include zero balance entries
                // if (balanceAmt == 0) continue;

                // Get voucher type name
                var voucherTypeName = "Unknown";
                if (entry.VoucherTypeId.HasValue)
                {
                    voucherTypes.TryGetValue(entry.VoucherTypeId.Value, out voucherTypeName);
                    voucherTypeName ??= "Unknown";
                }

                // Add to result
                result.Add(new PartyBalanceAdjustDto
                {
                    Id = entry.Id,
                    VoucherNo = entry.VoucherNo ?? "",
                    VoucherTypeId = (Guid)entry.VoucherTypeId,
                    VoucherTypeName = voucherTypeName,
                    Type = entry.ReferenceType,
                    PayDate = DateConverter.ConvertToNepali(entry.Date),
                    DueDate = DateConverter.ConvertToNepali(entry.Date.AddDays(entry.CreditPeriod)),
                    BalanceString = $"{Math.Abs(balanceAmt):N2}{(balanceAmt > 0 ? " Dr" : balanceAmt < 0 ? " Cr" : "")}",
                    Balance = balanceAmt,
                    BillAmt = Math.Abs(totalAmt),
                    Paid = paidAmt,
                    Adjust = 0,
                    IsDisable = balanceAmt <= 0
                });
            }

            // Return ordered by payment date
            return result.OrderBy(e => e.PayDate).ToList();
        }

        [DisableAuditing]
        public async Task<List<ReceiptMasterCustomersList>> GetAllCustomersForTableDropdown(Guid branchId)
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Debtors" || x.Name == "Cash" ||
                            x.AccountGroupFk.Name == "Sundry Creditors")
                .Select(accountLedger => new ReceiptMasterCustomersList
                {
                    LedgerId = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    IsBillByBill = accountLedger.IsBillByBill
                }).AsNoTracking().ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesReceiptMasters)]
        public async Task<string> GetReceiptMasterVoucherNo()
        {
            // Get the next voucher number and convert to string format
            var nextVoucherNumber = await GetInlineVoucherNo();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ReceiptVoucher");

            return $"{voucherNumbering.Prefix}{nextVoucherNumber}{voucherNumbering.Postfix}";
        }

        private async Task<int> GetInlineVoucherNo()
        {
            // Query executed only once with Max() performed at database level
            var maxVoucherNumber = receiptMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering)
                .AsEnumerable()
                .DefaultIfEmpty(0)
                .Max();

            switch (maxVoucherNumber)
            {
                case 0:
                    {
                        // No existing vouchers, use starting index from voucher numbering
                        var voucherNumbering =
                            await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ReceiptVoucher");
                        return voucherNumbering.StartIndex;
                    }
                default:
                    return maxVoucherNumber + 1;
            }
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x => x.AccountGroupFk.Name == "Cash-in Hand" ||
                                                           x.AccountGroupFk.Name == "Bank Account" ||
                                                           x.AccountGroupFk.Name == "Bank OD A/C")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "ReceiptVoucher");
        }


        [AbpAuthorize(AppPermissions.PagesReceiptMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditReceiptMasterDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            decimal? postingNumbering = PostingNumbering;
            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetReceiptMasterVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await receiptMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId &&
                                                                  x.VoucherNo == input.VoucherNo) > 0)
                    throw new UserFriendlyException("ReceiptVoucher VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var receiptMaster = new ReceiptMaster
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                TenantId = tenantId,
                FinancialYearId = FinancialYearId,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                TotalAmount = input.TotalAmount,
                DateMiti = input.DateMiti,
                Description = input.Description,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };

            var masterId = await receiptMasterRepository.InsertAndGetIdAsync(receiptMaster);

            var ledgerPosting = new LedgerPosting
            {
                TenantId = tenantId,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Debit = input.TotalAmount,
                Credit = 0,
                InvoiceNo = input.VoucherNo,
                FinancialYearId = FinancialYearId,
                DetailId = input.ReceiptDetails.Select(x => x.LedgerId).FirstOrDefault(),
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                MasterId = masterId,
                PostingNumber = postingNumbering
            };
            await ledgerPostingRepository.InsertAsync(ledgerPosting);

            var againstVoucherTypeId =
                await voucherTypeRepository.FirstOrDefaultAsync(x =>
                    x.TenantId == AbpSession.TenantId && x.Name == "PaymentVoucher");
            if (againstVoucherTypeId == null)
                throw new UserFriendlyException("VoucherType of \"PaymentVoucher\" not found");
            foreach (var detail in input.ReceiptDetails)
            {
                var receiptDetail = new ReceiptDetail
                {
                    TenantId = tenantId,
                    Amount = detail.Amount,
                    ChequeNo = detail.ChequeNo,
                    ChequeMiti = detail.ChequeMiti,
                    ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                        ? null
                        : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                    LedgerId = detail.LedgerId,
                    ReceiptMasterId = masterId
                };
                var detailId = await receiptDetailRepository.InsertAndGetIdAsync(receiptDetail);


                var ledger = new LedgerPosting
                {
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    Debit = 0,
                    Credit = detail.Amount,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = input.LedgerId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = detail.LedgerId,
                    MasterId = masterId,
                    PostingNumber = postingNumbering
                };
                await ledgerPostingRepository.InsertAsync(ledger);

                if (!(await accountLedgerRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId)).IsBillByBill) continue;
                if (detail.PartyBalanceDetail == null) continue;

                await partyBalanceService.PostReceipts(new CreateReceiptAgainstMasterDto
                {
                    MasterId = masterId,
                    MasterVoucherNo = input.VoucherNo,
                    MasterVoucherTypeId = voucherTypeId,
                    MasterVoucherNumbering = voucherNumbering,
                    LedgerId = detail.LedgerId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    Data = detail.PartyBalanceDetail,
                    DetailId = detailId
                });
            }

            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesReceiptMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditReceiptMasterDto input)
        {
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (receiptMaster != null)
            {
                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await receiptMasterRepository.CountAsync(x =>
                            x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) > 0) throw new UserFriendlyException("ReceiptMaster VoucherNo is Duplicate");
                    receiptMaster.VoucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate")
                    receiptMaster.VoucherNo = input.VoucherNo;
                var postingNumbering = receiptMaster.PostingNumbering;
                receiptMaster.DateMiti = input.DateMiti;
                receiptMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                receiptMaster.TotalAmount = input.TotalAmount;
                receiptMaster.Description = input.Description;
                receiptMaster.LedgerId = input.LedgerId;
                receiptMaster.UpdateUserId = AbpSession.UserId;
                await receiptMasterRepository.UpdateAsync(receiptMaster);

                var detailsIds = input.ReceiptDetails.Select(x => x.Id).ToList();
                var detailsDataBaseIds = await receiptDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.ReceiptMasterId == input.Id).Select(x => x.Id).ToListAsync();

                foreach (var detailsDataBaseId in detailsDataBaseIds.Where(detailsDataBaseId =>
                             !detailsIds.Contains(detailsDataBaseId)))
                    await receiptDetailRepository.DeleteAsync(detailsDataBaseId);

                await ledgerPostingRepository.DeleteAsync(x => x.VoucherNumbering == receiptMaster.VoucherNumbering &&
                                                               x.FinancialYearId == FinancialYearId &&
                                                               x.VoucherNo == input.VoucherNo &&
                                                               x.VoucherTypeId == voucherTypeId);



                var ledgerPosting = new LedgerPosting
                {
                    VoucherNumbering = receiptMaster.VoucherNumbering,
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = receiptMaster.VoucherNo,
                    Debit = input.TotalAmount,
                    Credit = 0,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = input.ReceiptDetails.Select(x => x.LedgerId).FirstOrDefault(),
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.LedgerId,
                    MasterId = receiptMaster.Id,
                    PostingNumber = postingNumbering
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting);

                foreach (var detail in input.ReceiptDetails)
                {
                    Guid detailId;
                    if (detail.Id == null || detail.Id == Guid.Empty)
                    {
                        var receiptDetail = new ReceiptDetail
                        {
                            TenantId = tenantId,
                            Amount = detail.Amount,
                            ChequeNo = detail.ChequeNo,
                            ChequeMiti = detail.ChequeMiti,
                            ChequeDate = string.IsNullOrWhiteSpace(detail.ChequeMiti)
                                ? null
                                : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                            LedgerId = detail.LedgerId,
                            ReceiptMasterId = receiptMaster.Id
                        };
                        detailId = await receiptDetailRepository.InsertAndGetIdAsync(receiptDetail);

                        var ledger = new LedgerPosting
                        {
                            VoucherNumbering = receiptMaster.VoucherNumbering,
                            TenantId = tenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            VoucherNo = receiptMaster.VoucherNo,
                            Debit = 0,
                            Credit = detail.Amount,
                            InvoiceNo = input.VoucherNo,
                            DetailId = input.LedgerId,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            FinancialYearId = FinancialYearId,
                            LedgerId = detail.LedgerId,
                            MasterId = receiptMaster.Id,
                            PostingNumber = postingNumbering
                        };
                        await ledgerPostingRepository.InsertAsync(ledger);

                        await partyBalanceService.PostReceipts(new CreateReceiptAgainstMasterDto
                        {
                            MasterId = receiptMaster.Id,
                            MasterVoucherNo = input.VoucherNo,
                            MasterVoucherTypeId = voucherTypeId,
                            MasterVoucherNumbering = receiptMaster.VoucherNumbering,
                            LedgerId = detail.LedgerId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            Data = detail.PartyBalanceDetail,
                            DetailId = detailId
                        });
                    }
                    else
                    {
                        var getDetail = await receiptDetailRepository.FirstOrDefaultAsync(e => e.Id == detail.Id);
                        if (getDetail == null) continue;
                        getDetail.TenantId = tenantId;
                        getDetail.Amount = detail.Amount;
                        getDetail.ChequeNo = detail.ChequeNo;
                        getDetail.ChequeMiti = detail.ChequeMiti;
                        getDetail.ChequeDate = string.IsNullOrWhiteSpace(detail.ChequeMiti)
                            ? null
                            : DateConverter.ConvertToEnglish(detail.ChequeMiti);
                        getDetail.LedgerId = detail.LedgerId;
                        getDetail.ReceiptMasterId = receiptMaster.Id;
                        await receiptDetailRepository.UpdateAsync(getDetail);

                        var ledger = new LedgerPosting
                        {
                            VoucherNumbering = receiptMaster.VoucherNumbering,
                            TenantId = tenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            VoucherNo = receiptMaster.VoucherNo,
                            Debit = 0,
                            Credit = detail.Amount,
                            InvoiceNo = input.VoucherNo,
                            DetailId = input.LedgerId,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            FinancialYearId = FinancialYearId,
                            MasterId = receiptMaster.Id,
                            LedgerId = detail.LedgerId
                        };
                        await ledgerPostingRepository.InsertAsync(ledger);
                        detailId = getDetail.Id;
                    }


                    //// party balance posting
                    if (!(await accountLedgerRepository.FirstOrDefaultAsync(x =>
                            x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId)).IsBillByBill) continue;
                    if (detail.PartyBalanceDetail == null) continue;
                    await partyBalanceService.PostReceipts(new CreateReceiptAgainstMasterDto
                    {
                        MasterId = receiptMaster.Id,
                        MasterVoucherNo = input.VoucherNo,
                        MasterVoucherTypeId = voucherTypeId,
                        MasterVoucherNumbering = receiptMaster.VoucherNumbering,
                        LedgerId = detail.LedgerId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        Data = detail.PartyBalanceDetail,
                        DetailId = detailId
                    });
                }
            }

            return receiptMaster.Id;
        }

        public async Task<List<UniversalDropdownDto>> GetAllDueReceiptVouchers(Guid ledgerId, PaymentOptions paymentOptions)
        {
            return (await partyBalanceService.GetAllRemainingReceiptss(ledgerId, paymentOptions));
        }

        public async Task<decimal> GetAllRemainingReceiptBalance(Guid ledgerId, PaymentOptions paymentTypes,
            string voucherNo)
        {
            return await partyBalanceService.GetAllRemainingReceiptBalance(ledgerId, paymentTypes, voucherNo);
        }

        public async Task<GetReceiptAgainstMasterDto> GetReceiptAgainst(Guid ledgerId, decimal amount, Guid detailId)
        {
            return await partyBalanceService.GetReceiptAgainstAmount(ledgerId, amount, detailId);
        }
    }
}