using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
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
using NextWave.Erp.Transaction.Dtos;
using NextWave.Erp.Transaction.Enums;
using NextWave.Erp.Transaction.Exporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesJournalMasters)]
    public class JournalMastersAppService(
        IRepository<JournalMaster, Guid> journalMasterRepository,
        IRepository<JournalDetail, Guid> journalDetailRepository,
        IRepository<User, long> userRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        PartyBalanceService partyBalanceService,
        IRepository<PartyBalance, Guid> partyBalanceRepository,
        IRepository<NewPartyBalance, Guid> newPartyBalanceRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IJournalMastersExcelExporter journalMastersExcelExporter,
        IRepository<Branch, Guid> branchRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
     //   IBranchsAppService branchService,
        IDocumentsAppService documentsAppService,
        //UserManager userManager,
        IRepository<UserBranch, Guid> userBranchRepository)
        : ErpAppServiceBase, IJournalMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetJournalMasterForViewDto>> GetAll(GetAllJournalMastersInput input)
        {
         //   var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var filteredJournalMasters = journalMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.VoucherNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.Date,
                    x.DateMiti,
                    x.DebitTotal,
                    x.CreditTotal,
                    x.Description,
                    x.ReferenceNo,
                    x.VoucherTypeId,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredJournalMasters = filteredJournalMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredJournalMasters = filteredJournalMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredJournalMasters = filteredJournalMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var journalMasters = from o in pagedAndFilteredJournalMasters
                                 join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.CreateUserId equals o6.Id into j6
                                 from s6 in j6.DefaultIfEmpty()
                                 join o7 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.UpdateUserId equals o7.Id into j7
                                 from s7 in j7.DefaultIfEmpty()
                                 select new GetJournalMasterForViewDto
                                 {
                                     VoucherNo = o.VoucherNo,
                                     Date = o.Date,
                                     DebitTotal = o.DebitTotal,
                                     CreditTotal = o.CreditTotal,
                                     Description = o.Description,
                                     ReferenceNo = o.ReferenceNo,
                                     DateMiti = o.DateMiti,
                                     VoucherTypeId = o.VoucherTypeId,
                                     Id = o.Id,
                                     CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                     UpdateUser = s7 == null || s7.Name == null ? "" : s7.Name,
                                 };

            var totalCount = await filteredJournalMasters.CountAsync();
            return new PagedResultDto<GetJournalMasterForViewDto>(
                totalCount,
                await journalMasters.ToListAsync()
            );
        }

        public async Task<GetJournalMasterForViewDto> GetJournalMasterForView(Guid id)
        {
            var journalMaster = await journalMasterRepository.GetAsync(id);

            var output = new GetJournalMasterForViewDto
            {
                Id = journalMaster.Id,
                VoucherNo = journalMaster.VoucherNo,
                Date = journalMaster.Date,
                ReferenceNo = journalMaster.ReferenceNo,
                DebitTotal = journalMaster.DebitTotal,
                CreditTotal = journalMaster.CreditTotal,
                Description = journalMaster.Description,
                DateMiti = journalMaster.DateMiti,
                VoucherTypeId = journalMaster.VoucherTypeId,
            };

            output.GetJournalDetail =
                await journalDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.JournalMasterId == id).Include(x => x.AccountLedgerFk)
                    .AsNoTracking().Select(x =>
                        new JournamDetailsForView
                        {
                            Id = x.Id,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            DrOrCr = x.Debit == 0 ? DrOrCr.Cr : DrOrCr.Dr,
                            Amount = x.Debit == 0 ? x.Credit : x.Debit,
                            ChequeNo = x.ChequeNo,
                            IsBillByBill = x.AccountLedgerFk.IsBillByBill,
                            LedgerName = x.AccountLedgerFk.Name,
                            ChequeMiti = x.ChequeMiti,
                            LedgerId = x.LedgerId
                        }).ToListAsync();
            foreach (var objDetail in output.GetJournalDetail)
                objDetail.PartyBalances = (await partyBalanceRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(z => z.LedgerId == objDetail.LedgerId &&
                                    z.FinancialYearId == journalMaster.FinancialYearId &&
                                    z.AgainstVoucherNo == journalMaster.VoucherNo &&
                                    z.AgainstVoucherTypeId == journalMaster.VoucherTypeId)
                        .Include(x => x.VoucherTypeFk).ToListAsync())
                    .Select(a => new PartyaBalanceForPaymentMasterDto
                    {
                        VoucherNo = a.VoucherNo,
                        VoucherTypeId = a.VoucherTypeId ?? Guid.Empty,
                        VoucherTypeName = a.VoucherTypeFk?.Name,
                        ReferenceType = a.ReferenceType,
                        Amount = a.Debit == 0 ? a.Credit : a.Debit
                    }).ToList();
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesJournalMastersEdit)]
        [AbpAuthorize(AppPermissions.PagesJournalMastersEdit)]
        public async Task<CreateOrEditJournalMasterDto> GetJournalMasterForEdit(EntityDto<Guid> input)
        {
            // Get journal master with a single query
            var journalMaster = await journalMasterRepository.FirstOrDefaultAsync(input.Id);
            if (journalMaster == null)
                throw new UserFriendlyException("Data not found");

            // Create output DTO
            var output = new CreateOrEditJournalMasterDto
            {
                Id = journalMaster.Id,
                VoucherNo = journalMaster.VoucherNo,
                DebitTotal = journalMaster.DebitTotal,
                CreditTotal = journalMaster.CreditTotal,
                Description = journalMaster.Description,
                DateMiti = journalMaster.DateMiti,
                ReferenceNo = journalMaster.ReferenceNo
            };

            // Load journal details with AccountLedger in a single query
            var journalDetails = await journalDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.JournalMasterId == input.Id)
                .Include(e => e.AccountLedgerFk)
                .ToListAsync();

            // Get all ledger IDs for consolidated party balance queries
            var ledgerIds = journalDetails.Where(e => e.AccountLedgerFk.IsBillByBill).Select(x => x.LedgerId).ToList();

            // Prefetch all relevant PartyBalance data in one query
            var allPartyBalances = await newPartyBalanceRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.FinancialYearId == FinancialYearId &&
                            ledgerIds.Contains(x.LedgerId))
                .Include(e => e.VoucherTypeFk)
                .AsNoTracking()
                .ToListAsync();

            // Get all paid party balances for these journal details
            var paidPartyBalances = allPartyBalances
                .Where(x => x.MasterId == journalMaster.Id).ToList();

            // Collect all paid party IDs for quick lookup
            var paidPartyIds = new HashSet<Guid>(paidPartyBalances.Select(x => x.Id));

            // Process journal details
            var details = new List<CreateOrEditJournalDetailDto>();
            foreach (var objDetail in journalDetails)
            {
                // Determine Dr/Cr
                var drOrcr = objDetail.Debit > 0 ? DrOrCr.Dr : DrOrCr.Cr;

                // Create detail DTO
                var data = new CreateOrEditJournalDetailDto
                {
                    Id = objDetail.Id,
                    Amount = objDetail.Credit == 0 ? objDetail.Debit : objDetail.Credit,
                    Credit = objDetail.Credit,
                    Debit = objDetail.Debit,
                    DrOrCr = drOrcr,
                    ChequeNo = objDetail.ChequeNo,
                    IsBillByBill = objDetail.AccountLedgerFk.IsBillByBill,
                    ChequeMiti = objDetail.ChequeMiti,
                    LedgerId = objDetail.LedgerId
                };

                // Only process PartyBalance for bill-by-bill ledgers
                if (objDetail.AccountLedgerFk.IsBillByBill)
                {
                    // Get party balances for this specific ledger
                    var ledgerPartyBalances = allPartyBalances
                        .Where(e => e.LedgerId == objDetail.LedgerId)
                        .ToList();

                    // Process differently based on Dr/Cr status
                    //if (drOrcr == DrOrCr.Cr)
                    //    data.PartyBalanceDetail = GetPartyBalanceAdjustments(
                    //        ledgerPartyBalances,
                    //        paidPartyIds,
                    //        true); // true for credit entries
                    //else // Dr
                    //    data.PartyBalanceDetail = GetPartyBalanceAdjustments(
                    //        ledgerPartyBalances,
                    //        paidPartyIds,
                    //        false); // false for debit entries
                }

                details.Add(data);
            }

            output.JournalDetail = details;
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditJournalMasterDto input)
        {
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            if (FinancialYear.FromDate > date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            if (FinancialYear.ToDate < date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");
            if (input.Id == null || input.Id == Guid.Empty) return await Create(input);

            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesJournalMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var journalMaster =
                await journalMasterRepository.FirstOrDefaultAsync(
                    x => x.TenantId == AbpSession.TenantId && x.Id == input.Id);
            if (journalMaster == null) throw new UserFriendlyException("Data Can not found");

            await journalDetailRepository.DeleteAsync(x => x.JournalMasterId == input.Id);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherNo == journalMaster.VoucherNo && x.VoucherTypeId == journalMaster.VoucherTypeId &&
                x.VoucherNumbering == journalMaster.VoucherNumbering && x.FinancialYearId == FinancialYearId);

            await partyBalanceRepository.DeleteAsync(x =>
                x.MasterVoucherNo == journalMaster.VoucherNo &&
                x.VoucherNumbering == journalMaster.VoucherNumbering &&
                x.FinancialYearId == journalMaster.FinancialYearId &&
                x.AgainstVoucherTypeId == journalMaster.VoucherTypeId);


            await journalMasterRepository.DeleteAsync(x => x.Id == input.Id && x.FinancialYearId == FinancialYearId);
        }

        public async Task<FileDto> GetJournalMastersToExcel(GetAllJournalMastersForExcelInput input)
        {
            var filteredJournalMasters = journalMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.VoucherTypeFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.Description.Contains(input.Filter) || e.DateMiti.Contains(input.Filter));

            var query = from o in filteredJournalMasters                       
                        select new GetJournalMasterForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Date = o.Date,
                            DebitTotal = o.DebitTotal,
                            CreditTotal = o.CreditTotal,
                            ReferenceNo = o.ReferenceNo,
                            Description = o.Description,
                            VoucherType = o.VoucherTypeFk.Name,
                            DateMiti = o.DateMiti,
                            VoucherTypeId = o.VoucherTypeId,
                            Id = o.Id,
                        };

            var journalMasterListDtos = await query.ToListAsync();

            return journalMastersExcelExporter.ExportToFile(journalMasterListDtos);
        }

        // Helper method to extract party balance adjustments
        private List<PartyBalanceAdjustDto> GetPartyBalanceAdjustments(
            IEnumerable<PartyBalance> partyBalances,
            HashSet<Guid> paidPartyIds,
            bool isCredit)
        {
            var result = new List<PartyBalanceAdjustDto>();

            // Filter new and on account entries not already paid
            var filteredBalances = partyBalances
                .Where(e => (e.ReferenceType == "New" || e.ReferenceType == "OnAccount") &&
                            !paidPartyIds.Contains(e.Id))
                .ToList();

            // Group by voucher for fast lookups
            var balancesByVoucher = partyBalances
                .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var detail in filteredBalances.OrderBy(e => e.Date))
            {
                if (!detail.VoucherTypeId.HasValue || string.IsNullOrEmpty(detail.VoucherNo))
                    continue;

                var key = new { detail.VoucherTypeId, detail.VoucherNo };

                // Calculate totals differently for credit vs debit entries
                var totalAmt = isCredit ? detail.Debit - detail.Credit : detail.Credit - detail.Debit;

                // Find related entries for this voucher
                if (!balancesByVoucher.TryGetValue(key, out var relatedEntries))
                    relatedEntries = new List<PartyBalance>();

                // Calculate paid amount
                var paidAmt = relatedEntries
                    .Where(x => x.ReferenceType == ReferenceType.Against.ToString() &&
                                !paidPartyIds.Contains(x.Id))
                    .Sum(e => isCredit ? e.Credit - e.Debit : e.Debit - e.Credit);

                // Calculate adjusted amount for this voucher
                var adjustAmt = relatedEntries
                    .Where(x => x.ReferenceType == ReferenceType.Against.ToString() &&
                                paidPartyIds.Contains(x.Id))
                    .Sum(e => isCredit ? e.Credit - e.Debit : e.Debit - e.Credit);

                // Calculate balance
                var balanceAmt = totalAmt - paidAmt;
                if (balanceAmt == 0) continue;

                // Add to result
                result.Add(new PartyBalanceAdjustDto
                {
                    Id = detail.Id,
                    VoucherNo = detail.VoucherNo ?? "",
                    VoucherTypeId = (Guid)detail.VoucherTypeId,
                    VoucherTypeName = detail.VoucherTypeFk?.Name ?? "Unknown",
                    Type = detail.ReferenceType,
                    PayDate = DateConverter.ConvertToNepali(detail.Date),
                    DueDate = DateConverter.ConvertToNepali(detail.Date.AddDays(detail.CreditPeriod)),
                    BalanceString = $"{Math.Abs(balanceAmt):N2} {(balanceAmt > 0 ? "Dr" : "Cr")}",
                    Balance = balanceAmt,
                    BillAmt = Math.Abs(totalAmt),
                    Paid = paidAmt,
                    Adjust = adjustAmt,
                    IsDisable = balanceAmt < 0 || adjustAmt < 0
                });
            }

            return result.OrderBy(e => e.PayDate).ToList();
        }


        public async Task<GetPdfForJournalMaster> GetJournalMasterForPdf(EntityDto<Guid> input)
        {
            var serial = 1;
            var journalMaster = await journalMasterRepository.FirstOrDefaultAsync(input.Id);
         //   var companyInfo = await branchService.GetBranchForView();
            var mainBranch = await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var journalDetails = await journalDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.JournalMasterId == journalMaster.Id)
                .Include(x => x.AccountLedgerFk).AsNoTracking().ToListAsync();
            var output = new GetPdfForJournalMaster
            {
                Logo1 = mainBranch.Image1,
                CompanyName = mainBranch.CompanyName,
                BranchAddress = mainBranch.Address,
                BranchContact = mainBranch.PhoneNo1,
                VoucherNo = journalMaster.VoucherNo,
                ReferenceNo = journalMaster.ReferenceNo,
                Date = journalMaster.Date,
                TotalAmountInWord = CurrencyToAmount.NumberToText((int)journalMaster.DebitTotal),
                TotalAmount = journalMaster.DebitTotal,
                Narration = journalMaster.Description,
                DateMiti = journalMaster.DateMiti,
                JournalDetails = journalDetails.Select(x => new GetPdfForJournalDetail
                {
                    SlNo = serial++,
                    ChequeNo = x.ChequeNo,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    LedgerName = x.AccountLedgerFk.Name
                }).ToList()
            };          

            return output;
        }

        //[AbpAuthorize(AppPermissions.PagesJournalMastersPrint)]
        //public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        //{
        //    var model = await GetJournalMasterForPdf(input);
        //    var document = new JournalMasterPdf(model);
        //    return document.GeneratePdf();
        //}

        //public async Task<FileDto> GetPdfdownload(EntityDto<Guid> input)
        //{
        //    var model = await GetJournalMasterForPdf(input);
        //    var document = new JournalMasterPdf(model);
        //    var fileName = "JournalMaster" + model.VoucherNo * 100 + ".pdf";
        //    if (!Directory.Exists(_appFolders.JournalMasterFolder))
        //        Directory.CreateDirectory(_appFolders.JournalMasterFolder);
        //    var filePath = Path.Combine(_appFolders.JournalMasterFolder, fileName);
        //    document.GeneratePdf(filePath);
        //    var file = new FileDto(filePath, MimeTypeNames.ApplicationPdf);
        //    var context = _httpContextAccessor.HttpContext;
        //    var domain = context.Request.Host.Host.ToLowerInvariant();
        //    var port = context.Request.Host.Port;
        //    var scheme = context.Request.Scheme;
        //    file.FileName = $"{scheme}://{domain.Trim('/')}:{port}/{"/Purchase/JournalMaster/"}/{fileName}";
        //    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        //    return file;
        //}
        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            return await journalMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("JournalVoucher");
            var journalMaster = await journalMasterRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId &&
                x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = journalMaster.VoucherTypeId,
                VoucherNo = journalMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
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

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesJournalMasters)]
        public async Task<List<JournalDetailAccountLedgerTableDto>> GetAllLedgerForLookupTable(Guid branchId)
        {
            var query = await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).ToListAsync();
            return query.Select(accountLedger => new JournalDetailAccountLedgerTableDto
            {
                Id = accountLedger.Id,
                DisplayName = accountLedger.Name,
                IsBillByBill = accountLedger.IsBillByBill
            }).ToList();
        }


        [AbpAuthorize(AppPermissions.PagesJournalMasters)]
        public async Task<string> GetJournalMasterVoucherNo()
        {
            var data = await journalMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "JournalVoucher");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await journalMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x =>  x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering( FinancialYearId, "JournalVoucher");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
        }


        [DisableAuditing]
        public async Task<List<PartyBalanceAdjustDto>> GetPartyBalanceDebit(Guid ledgerId)
        {
            var partyBalanceQuery = await partyBalanceRepository.GetAll().AsNoTracking()
                .Where(e => e.LedgerId == ledgerId && e.FinancialYearId == FinancialYearId)
                .ToListAsync();

            var getAllVoucher = await voucherTypeRepository.GetAllListAsync();
            var returnDebit = (from detail in partyBalanceQuery.Where(e => e.ReferenceType is "New" or "OnAccount")
                               join voucher in getAllVoucher on detail.MasterVoucherTypeId equals voucher.Id
                               let totalAmt = detail.Debit - detail.Credit
                               let paidAmt = partyBalanceQuery.Where(x =>
                                       x.FinancialYearId == FinancialYearId && x.VoucherTypeId == detail.VoucherTypeId &&
                                       x.VoucherNo == detail.VoucherNo && x.ReferenceType == ReferenceType.Against.ToString())
                                   .Sum(e => e.Credit - e.Debit)
                               let balanceAmt = totalAmt - paidAmt
                               select new PartyBalanceAdjustDto
                               {
                                   Id = detail.Id,
                                   VoucherNo = detail.VoucherNo ?? "",
                                   VoucherTypeId = (Guid)detail.VoucherTypeId,
                                   VoucherTypeName = voucher.Name,
                                   Type = detail.ReferenceType,
                                   PayDate = DateConverter.ConvertToNepali(detail.Date),
                                   DueDate = DateConverter.ConvertToNepali(detail.Date.AddDays(detail.CreditPeriod)),
                                   BalanceString = Math.Abs(balanceAmt) + "-" + (balanceAmt > 0 ? " Dr" : " Cr"),
                                   Balance = balanceAmt,
                                   BillAmt = Math.Abs(totalAmt),
                                   Paid = paidAmt,
                                   Adjust = 0,
                                   IsDisable = balanceAmt < 0
                               }).Where(e => e.Balance > 0)
                .OrderBy(e => e.PayDate).ToList();

            return returnDebit;
        }


        [DisableAuditing]
        public async Task<List<PartyBalanceAdjustDto>> GetPartyBalanceCredit(Guid ledgerId)
        {
            var partyBalanceQuery = await partyBalanceRepository.GetAll().AsNoTracking()
                .Where(e => e.LedgerId == ledgerId && e.FinancialYearId == FinancialYearId)
                .ToListAsync();

            var getAllVoucher = await voucherTypeRepository.GetAllListAsync();
            var returnDebit = (from detail in partyBalanceQuery.Where(e => e.ReferenceType is "New" or "OnAccount")
                               join voucher in getAllVoucher on detail.MasterVoucherTypeId equals voucher.Id
                               let totalAmt = detail.Credit - detail.Debit
                               let paidAmt = partyBalanceQuery.Where(x =>
                                       x.FinancialYearId == FinancialYearId && x.VoucherTypeId == detail.VoucherTypeId &&
                                       x.VoucherNo == detail.VoucherNo && x.ReferenceType == ReferenceType.Against.ToString())
                                   .Sum(e => e.Debit - e.Credit)
                               let balanceAmt = totalAmt - paidAmt
                               select new PartyBalanceAdjustDto
                               {
                                   Id = detail.Id,
                                   VoucherNo = detail.VoucherNo ?? "",
                                   VoucherTypeId = (Guid)detail.VoucherTypeId,
                                   VoucherTypeName = voucher.Name,
                                   Type = detail.ReferenceType,
                                   PayDate = DateConverter.ConvertToNepali(detail.Date),
                                   DueDate = DateConverter.ConvertToNepali(detail.Date.AddDays(detail.CreditPeriod)),
                                   BalanceString = Math.Abs(balanceAmt) + "-" + (balanceAmt > 0 ? " Dr" : " Cr"),
                                   Balance = balanceAmt,
                                   BillAmt = Math.Abs(totalAmt),
                                   Paid = paidAmt,
                                   Adjust = 0,
                                   IsDisable = balanceAmt < 0
                               }).Where(e => e.Balance > 0)
                .OrderBy(e => e.PayDate).ToList();

            return returnDebit;
        }


        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "JournalVoucher");
        }

        [AbpAuthorize(AppPermissions.PagesJournalMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditJournalMasterDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            if (input.DebitTotal != input.CreditTotal) throw new UserFriendlyException("Debit and Credit are not equal");
            if (await journalMasterRepository.CountAsync(x =>
                    x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                throw new UserFriendlyException("Voucher number already exist");
            decimal? postingNumbering = PostingNumbering;
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("JournalVoucher");
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            //int voucherNumbering = await GetInlineVoucherNo(input.BranchId);
            //string voucherNo = await GetJournalMasterVoucherNo(input.BranchId);

            //VoucherGeneration 
            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetJournalMasterVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await journalMasterRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("JournalVoucher VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var journalMaster = new JournalMaster
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                FinancialYearId = FinancialYearId,
                DebitTotal = input.DebitTotal,
                CreditTotal = input.CreditTotal,
                Description = input.Description,
                ReferenceNo = input.ReferenceNo,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                TenantId = tenantId,
                PostingNumbering = postingNumbering
            };
            var masterId = await journalMasterRepository.InsertAndGetIdAsync(journalMaster);
            foreach (var detail in input.JournalDetail)
            {
                var odAccountGroup =
                    await accountGroupRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Name == "Bank OD A/C");
                var accountLedgerDetails =
                    await accountLedgerRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId);
                
                detail.Debit = detail.DrOrCr == DrOrCr.Dr ? detail.Amount : 0;
                detail.Credit = detail.DrOrCr == DrOrCr.Cr ? detail.Amount : 0;
                var data = new JournalDetail
                {
                    TenantId = tenantId,
                    Credit = detail.Credit,
                    Debit = detail.Debit,
                    ChequeNo = detail.ChequeNo,
                    JournalMasterId = masterId,
                    LedgerId = detail.LedgerId
                };
                var detailId = await journalDetailRepository.InsertAndGetIdAsync(data);

                var ledgerPosting = new LedgerPosting
                {
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    FinancialYearId = FinancialYearId,
                    LedgerId = detail.LedgerId,
                    DetailId = detailId,
                    Debit = detail.Debit,
                    Credit = detail.Credit,
                    InvoiceNo = input.VoucherNo,
                    MasterId = masterId,
                    PostingNumber = postingNumbering
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting);

                if (!accountLedgerDetails.IsBillByBill) continue;
                if (detail.PartyBalanceDetail == null) continue;
                if (detail.DrOrCr == DrOrCr.Dr)
                {
                    await partyBalanceService.PostPayments(new CreateReceiptAgainstMasterDto
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
                if (detail.DrOrCr == DrOrCr.Cr)
                {
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
            }

            await unitOfWork.CompleteAsync();
            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesJournalMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditJournalMasterDto input)
        {
            if (input.DebitTotal != input.CreditTotal) throw new UserFriendlyException("Debit and Credit are not equal");
            var tenantId = AbpSession.GetTenantId();
            var journalMaster = await journalMasterRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (journalMaster == null) throw new UserFriendlyException("Data not found");
            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await journalMasterRepository.CountAsync(x =>
                        x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("JournalMaster VoucherNo is Duplicate");
                journalMaster.VoucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate") journalMaster.VoucherNo = input.VoucherNo;
            var postingNumbering = journalMaster.PostingNumbering;
            journalMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
            journalMaster.DebitTotal = input.DebitTotal;
            journalMaster.CreditTotal = input.CreditTotal;
            journalMaster.Description = input.Description;
            journalMaster.DateMiti = input.DateMiti;
            journalMaster.ReferenceNo = input.ReferenceNo;
            journalMaster.UpdateUserId = AbpSession.UserId;
            var voucherTypeId = journalMaster.VoucherTypeId;
            await journalMasterRepository.UpdateAsync(journalMaster);

            var detailsIds = input.JournalDetail.Select(x => x.Id).ToList();
            var detailsDataBaseIds = await journalDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.JournalMasterId == input.Id).Select(x => x.Id).ToListAsync();

            foreach (var detailsDataBaseId in detailsDataBaseIds.Where(detailsDataBaseId =>
                         !detailsIds.Contains(detailsDataBaseId)))
                await journalDetailRepository.DeleteAsync(detailsDataBaseId);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherNo == journalMaster.VoucherNo && x.VoucherTypeId == journalMaster.VoucherTypeId &&
                x.VoucherNumbering == journalMaster.VoucherNumbering && x.FinancialYearId == FinancialYearId);

            await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == journalMaster.VoucherNo &&
                                                          x.MasterVoucherTypeId == journalMaster.VoucherTypeId &&
                                                          x.FinancialYearId == FinancialYearId &&
                                                          x.MasterId == journalMaster.Id);

            foreach (var detail in input.JournalDetail)
            {
                Guid newDetailId;
                detail.Debit = detail.DrOrCr == DrOrCr.Dr ? detail.Amount : 0;
                detail.Credit = detail.DrOrCr == DrOrCr.Cr ? detail.Amount : 0;
                if (detail.Id == null || detail.Id == Guid.Empty)
                {
                    var odAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Name == "Bank OD A/C");
                    var accountLedgerDetails =
                        await accountLedgerRepository.FirstOrDefaultAsync(x =>
                            x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId);
                    
                    var data = new JournalDetail
                    {
                        TenantId = tenantId,
                        Credit = detail.Credit,
                        Debit = detail.Debit,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        ChequeDate = DateConverter.ConvertToEnglish(detail.ChequeMiti),
                            
                        JournalMasterId = journalMaster.Id,
                        LedgerId = detail.LedgerId
                    };
                    newDetailId = await journalDetailRepository.InsertAndGetIdAsync(data);

                    var ledgerPosting2 = new LedgerPosting
                    {
                        VoucherNumbering = journalMaster.VoucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = input.VoucherNo,
                        LedgerId = detail.LedgerId,
                        DetailId = newDetailId,
                        Debit = detail.Debit,
                        FinancialYearId = FinancialYearId,
                        Credit = detail.Credit,
                        InvoiceNo = input.VoucherNo,
                        MasterId = journalMaster.Id,
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting2);
                }
                else
                {
                    var data = await journalDetailRepository.GetAsync((Guid)detail.Id);

                    if (data == null) continue;
                    data.Credit = detail.Credit;
                    data.Debit = detail.Debit;
                    data.ChequeNo = detail.ChequeNo;
                    data.ChequeMiti = detail.ChequeMiti;
                    data.ChequeDate = DateConverter.ConvertToEnglish(detail.ChequeMiti);
                    data.JournalMasterId = journalMaster.Id;
                    data.LedgerId = detail.LedgerId;
                    await journalDetailRepository.UpdateAsync(data);
                    newDetailId = (Guid)detail.Id;

                    var odAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Name == "Bank OD A/C");
                    var accountLedgerDetails =
                        await accountLedgerRepository.FirstOrDefaultAsync(x =>
                            x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId);
                   
                    var ledgerPosting = new LedgerPosting
                    {
                        VoucherNumbering = journalMaster.VoucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = input.VoucherNo,
                        LedgerId = detail.LedgerId,
                        FinancialYearId = journalMaster.FinancialYearId,
                        DetailId = newDetailId,
                        Debit = detail.Debit,
                        Credit = detail.Credit,
                        InvoiceNo = input.VoucherNo,
                        MasterId = journalMaster.Id,
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }


                if (!(await accountLedgerRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId)).IsBillByBill) continue;
                if (detail.PartyBalanceDetail == null) continue;
                if (detail.DrOrCr == DrOrCr.Dr)
                {
                    await partyBalanceService.PostPayments(new CreateReceiptAgainstMasterDto
                    {
                        MasterId = journalMaster.Id,
                        MasterVoucherNo = input.VoucherNo,
                        MasterVoucherTypeId = voucherTypeId,
                        MasterVoucherNumbering = journalMaster.VoucherNumbering,
                        LedgerId = detail.LedgerId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        Data = detail.PartyBalanceDetail,
                        DetailId = newDetailId
                    });
                }
                if (detail.DrOrCr == DrOrCr.Cr)
                {
                    await partyBalanceService.PostReceipts(new CreateReceiptAgainstMasterDto
                    {
                        MasterId = journalMaster.Id,
                        MasterVoucherNo = input.VoucherNo,
                        MasterVoucherTypeId = voucherTypeId,
                        MasterVoucherNumbering = journalMaster.VoucherNumbering,
                        LedgerId = detail.LedgerId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        Data = detail.PartyBalanceDetail,
                        DetailId = newDetailId
                    });
                }
            }

            return journalMaster.Id;
        }

        public async Task<GetReceiptAgainstMasterDto> GetPaymentAgainst(Guid ledgerId, decimal amount, Guid detailId)
        {
            return await partyBalanceService.GetPaymentAgainstAmount(ledgerId, amount, detailId);
        }

        public async Task<GetReceiptAgainstMasterDto> GetReceiptAgainst(Guid ledgerId, decimal amount, Guid detailId)
        {
            return await partyBalanceService.GetReceiptAgainstAmount(ledgerId, amount, detailId);
        }
    }
}
