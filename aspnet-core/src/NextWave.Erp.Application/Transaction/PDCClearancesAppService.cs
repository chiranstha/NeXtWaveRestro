using Abp.Application.Services.Dto;
using Abp.AspNetZeroCore.Net;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Transaction.Dtos;
using NextWave.Erp.Transaction.Enums;
using NextWave.Erp.Transaction.Exporting;
using NextWave.Erp.Transaction.Pdf;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace NextWave.Erp.Transaction
{
    public class PDCClearancesAppService(
    IRepository<PDCClearance, Guid> pdcClearanceRepository,
    IPDCClearancesExcelExporter pdcClearancesExcelExporter,
    IRepository<User, long> userRepository,
    IRepository<PDCReceivable, Guid> pdcReceivablePRepository,
    IRepository<PartyBalance, Guid> partyBalanceRepository,
    //IRepository<Branch, Guid> branchRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<PDCPayable, Guid> pdcPayableRepository,
    IRepository<PDCReceivable, Guid> pdcReceivableRepository,
    IRepository<FinancialYear, Guid> financialYearRepository,
    //IBranchsAppService branchService,
    AppFolders appFolders,
    IHttpContextAccessor httpContextAccessor,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IDocumentsAppService documentsAppService,
    IRepository<VoucherType, Guid> voucherTypeRepository)
    : ErpAppServiceBase, IPDCClearancesAppService
    {
        private readonly AppFolders _appFolders = appFolders;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        [DisableAuditing]
        public async Task<PagedResultDto<GetPDCClearanceForViewDto>> GetAll(GetAllPDCClearancesInput input)
        {
            //    var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var filteredPDCClearances = pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(e => e.LedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    x => x.LedgerFk.Name.Contains(input.Filter.Trim()) || x.ChequeNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.AgainstMode,
                    x.Description,
                    x.Status,
                    x.BankId,
                    x.Amount,
                    x.LedgerId,
                    x.AgainstLedgerId,
                    x.Date,
                    x.DateMiti,
                    x.VoucherNumbering,
                    x.ChequeMiti,
                    x.ChequeNo,
                    x.CreateUserId,
                    x.UpdateUserId,
                    LedgerName = x.LedgerFk.Name
                });

            var pagedAndFilteredPDCClearances = filteredPDCClearances
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var pdcClearances = from o in pagedAndFilteredPDCClearances
                                join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                    o.CreateUserId equals o6.Id into j6
                                from s6 in j6.DefaultIfEmpty()
                                join o7 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                    o.UpdateUserId equals o7.Id into j7
                                from s7 in j7.DefaultIfEmpty()
                                join o8 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                    o.BankId equals o8.Id into j8
                                from s8 in j8.DefaultIfEmpty()
                                select new GetPDCClearanceForViewDto
                                {
                                    VoucherNo = o.VoucherNo,
                                    Date = o.Date,
                                    AgainstMode = o.AgainstMode,
                                    AgainstModeName = o.AgainstMode == PDCClearanceAgainstMode.PDCPayable
                                        ? "PDC Payable"
                                        : o.AgainstMode == PDCClearanceAgainstMode.PDCReceivable
                                            ? "PDC Receivable"
                                            : "",
                                    Description = o.Description,
                                    Status = o.Status,
                                    StatusName = o.Status == PDCClearanceType.Clearance
                                        ? "Cleared"
                                        : o.Status == PDCClearanceType.Bounced
                                            ? "Bounced"
                                            : "",
                                    DateMiti = o.DateMiti,
                                    BankId = o.BankId,
                                    Amount = o.Amount,
                                    Id = o.Id,
                                    LedgerId = o.LedgerId,
                                    AgainstLedgerId = o.AgainstLedgerId,
                                    ChequeMiti = o.ChequeMiti,
                                    ChequeNo = o.ChequeNo,
                                    CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                    UpdateUser = s7 == null || s7.Name == null ? "" : s7.Name,
                                    BankName = s8 == null || s8.Name == null ? null : s8.Name,
                                    LedgerName = o.LedgerName
                                };
            var finalPDCClearance = await pdcClearances.ToListAsync();
            var accountLedgers = await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().ToListAsync();
            foreach (var a in finalPDCClearance)
            {
                var bank = accountLedgers.FirstOrDefault(x => x.Id == a.BankId);
                if (bank != null) a.BankName = bank.Name;
            }

            var totalCount = await filteredPDCClearances.CountAsync();

            return new PagedResultDto<GetPDCClearanceForViewDto>(
                totalCount, finalPDCClearance);
        }

        public async Task<GetPDCClearanceForViewDto> GetPDCClearanceForView(Guid id)
        {
            var pdcClearance = (await pdcClearanceRepository.GetAll().Include(x => x.LedgerFk)
                .Where(x => x.Id == id).ToListAsync()).FirstOrDefault();

            var output = new GetPDCClearanceForViewDto
            {
                Id = pdcClearance.Id,
                VoucherNo = pdcClearance.VoucherNo,
                Date = pdcClearance.Date,
                AgainstMode = pdcClearance.AgainstMode,
                Description = pdcClearance.Description,
                Status = pdcClearance.Status,
                DateMiti = pdcClearance.DateMiti,
                Amount = pdcClearance.Amount,
                ChequeMiti = pdcClearance.ChequeMiti,
                ChequeNo = pdcClearance.ChequeNo,
                BankId = pdcClearance.BankId,
                AgainstLedgerId = pdcClearance.AgainstLedgerId,
                LedgerId = pdcClearance.LedgerId,
                LedgerName = pdcClearance.LedgerFk.Name
            };

            if (output.BankId != null)
            {
                var ledger = await accountLedgerRepository.FirstOrDefaultAsync(output.BankId);
                output.BankName = ledger?.Name;
            }

            if (output.AgainstLedgerId != Guid.Empty)
            {
                var againstLedger = await accountLedgerRepository.FirstOrDefaultAsync(output.AgainstLedgerId);
                if (againstLedger != null)
                    output.ReceivedInOrPaidFrom = againstLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPDCClearancesEdit)]
        public async Task<GetPDCClearanceForEditOutput> GetPDCClearanceForEdit(EntityDto<Guid> input)
        {
            var pdcClearance = await pdcClearanceRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetPDCClearanceForEditOutput
            {
                Id = pdcClearance.Id,
                VoucherNo = pdcClearance.VoucherNo,
                Date = pdcClearance.Date,
                AgainstMode = pdcClearance.AgainstMode,
                Description = pdcClearance.Description,
                Status = pdcClearance.Status,
                Amount = pdcClearance.Amount,
                ChequeNo = pdcClearance.ChequeNo,
                ChequeMiti = pdcClearance.ChequeMiti,
                VoucherName = pdcClearance.VoucherNo,
                AgainstId = pdcClearance.AgainstMode == PDCClearanceAgainstMode.PDCPayable
                    ? (Guid)pdcClearance.PDCPayableId
                    : (Guid)pdcClearance.PDCReceivableId,
                DateMiti = pdcClearance.DateMiti,
                BankId = pdcClearance.BankId,
                AgainstLedgerId = pdcClearance.AgainstLedgerId,
                LedgerId = pdcClearance.LedgerId
            };
            if (output.AgainstLedgerId != Guid.Empty)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.AgainstLedgerId);
                output.AgainstLedgerName = accountLedger?.Name;
            }

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPDCClearanceDto input)
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

        [AbpAuthorize(AppPermissions.PagesPDCClearancesDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var pdcClearance = await pdcClearanceRepository.FirstOrDefaultAsync(input.Id);
            await ledgerPostingRepository.DeleteAsync(x => x.VoucherTypeId == pdcClearance.VoucherTypeId
                                                           && x.VoucherNo == pdcClearance.VoucherNo &&
                                                           x.VoucherNumbering == pdcClearance.VoucherNumbering &&
                                                           x.FinancialYearId == pdcClearance.FinancialYearId);

            await pdcClearanceRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPDCClearancesToExcel(GetAllPDCClearancesInput input)
        {
            var filteredPDCClearances = pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(e => e.LedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId);

            var query = from o in filteredPDCClearances
                        join o2 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                            o.BankId equals o2.Id into j2
                        from s2 in j2.DefaultIfEmpty()
                        join o3 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                            o.AgainstLedgerId equals o3.Id into j3
                        from s3 in j3.DefaultIfEmpty()
                        select new GetPDCClearanceForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Date = o.Date,
                            AgainstMode = o.AgainstMode,
                            Description = o.Description,
                            Status = o.Status,
                            DateMiti = o.DateMiti,
                            ChequeNo = o.ChequeNo,
                            ChequeMiti = o.ChequeMiti,
                            BankId = o.BankId,
                            BankName = s2 == null || s2.Name == null ? "" : s2.Name,
                            Amount = o.Amount,
                            Id = o.Id,
                            ReceivedInOrPaidFrom = s3 == null || s3.Name == null ? "" : s3.Name,
                            LedgerName = o.LedgerFk.Name
                        };

            var pdcClearanceListDtos = await query.ToListAsync();

            return pdcClearancesExcelExporter.ExportToFile(pdcClearanceListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCClearances)]
        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => (x.TenantId == AbpSession.TenantId && x.AccountGroupFk.Name == "Sundry Creditors") ||
                            x.AccountGroupFk.Name == "Sundry Debtors")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCClearances)]
        public async Task<List<UniversalDropdownDto>> GetAllBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.AccountGroupFk.Name == "Bank Account")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCClearances)]
        public async Task<List<UniversalDropdownDto>> GetAllPDCPayableForTableDropdown()
        {
            return await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(pdcPayable => new UniversalDropdownDto
                {
                    Id = pdcPayable.Id,
                    DisplayName = pdcPayable == null || pdcPayable.VoucherNo == null
                        ? ""
                        : pdcPayable.VoucherNo.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCClearances)]
        public async Task<List<UniversalDropdownDto>> GetAllPDCReceivableForTableDropdown()
        {
            return await pdcReceivableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(pdcReceivable => new UniversalDropdownDto
                {
                    Id = pdcReceivable.Id,
                    DisplayName = pdcReceivable == null || pdcReceivable.VoucherNo == null
                        ? ""
                        : pdcReceivable.VoucherNo.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCClearances)]
        public async Task<List<UniversalDropdownDto>> GetAllFinancialYearForTableDropdown()
        {
            return await financialYearRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(financialYear => new UniversalDropdownDto
                {
                    Id = financialYear.Id,
                    DisplayName = financialYear == null || financialYear.FromMiti == null
                        ? ""
                        : financialYear.FromMiti.ToString()
                }).ToListAsync();
        }

        public async Task<GetPdfForPDCClearances> GetPDCClearancesForPdf(EntityDto<Guid> input)
        {
            var pdcClerances = await pdcClearanceRepository.FirstOrDefaultAsync(input.Id);
            //    var companyInfo = await branchService.GetBranchForView(pdcClerances.BranchId);
            //  var mainCompany =
            //       await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var ledgerInfo = await accountLedgerRepository.FirstOrDefaultAsync(pdcClerances.BankId);
            var againstLedgerInfo = await accountLedgerRepository.FirstOrDefaultAsync(pdcClerances.AgainstLedgerId);
            var againstLedger = "";
            if (againstLedgerInfo != null)
                againstLedger = againstLedgerInfo.Name;
            var output = new GetPdfForPDCClearances
            {
                //       Logo1 = mainCompany.Image1,
                //      BranchName = companyInfo.CompanyName,
                //     BranchAddress = companyInfo.Address,
                //     BranchContact = companyInfo.CompanyContact,
                VoucherNo = pdcClerances.VoucherNo,
                Date = pdcClerances.Date,
                ChequeMiti = pdcClerances.ChequeMiti,
                ChequeNo = pdcClerances.ChequeNo,
                Status = pdcClerances.Status,
                AgainstBank = againstLedger,
                AgainstMode = pdcClerances.AgainstMode,
                TotalAmount = pdcClerances.Amount,
                Bank = ledgerInfo.Name,
                Narration = pdcClerances.Description,
                DateMiti = pdcClerances.DateMiti,
                //      BranchId = pdcClerances.BranchId,
                LedgerId = pdcClerances.LedgerId
            };
            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPDCClearancesPrint)]
        public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        {
            var model = await GetPDCClearancesForPdf(input);
            var document = new PDCClearancePdf(model);
            var file = new FileDto("PDCClearance.pdf", MimeTypeNames.ApplicationPdf);
            return document.GeneratePdf();
        }

        public async Task GetPdfDownload1(EntityDto<Guid> input)
        {
            var filePath = "PDCPayablePdf.pdf";
            var model = await GetPDCClearancesForPdf(input);
            var document = new PDCClearancePdf(model);
            document.GeneratePdf(filePath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", filePath);
            }
            else
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                Console.WriteLine($"Output PDF file is available here: {fullPath}");
            }
        }

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            if (await pdcClearanceRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) >
                0) return true;

            return false;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCClearance");
            var pdcClearance = await pdcClearanceRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId &&
                x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = pdcClearance.VoucherTypeId,
                VoucherNo = pdcClearance.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
                    x.AccountGroupFk.Name == "Bank Account" || x.AccountGroupFk.Name == "Cash-in Hand")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetPDCByLedgerId(Guid ledgerId,
            PDCClearanceAgainstMode againstMode)
        {
            if (againstMode == PDCClearanceAgainstMode.PDCPayable)
            {
                var clearedPDCPayableIds = await pdcClearanceRepository.GetAll().Select(x => x.PDCPayableId).ToListAsync();
                var pdcPayables =
                    (await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
                            x.LedgerId == ledgerId && !clearedPDCPayableIds.Contains(x.Id))
                        .ToListAsync()).Select(x => new UniversalDropdownDto
                        {
                            Id = x.Id,
                            DisplayName = x.VoucherNo
                        }).ToList();
                return pdcPayables;
            }

            if (againstMode == PDCClearanceAgainstMode.PDCReceivable)
            {
                var clearedPDCReceivableIds =
                    await pdcClearanceRepository.GetAll().Select(x => x.PDCReceivableId).ToListAsync();
                var pdcReceivable =
                    (await pdcReceivablePRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
                            x.LedgerId == ledgerId && !clearedPDCReceivableIds.Contains(x.Id))
                        .ToListAsync()).Select(x => new UniversalDropdownDto
                        {
                            Id = x.Id,
                            DisplayName = x.VoucherNo
                        }).ToList();
                return pdcReceivable;
            }

            return null;
        }

        public async Task<List<PDCClearanceDetailDto>> GetPDCDetails(Guid ledgerId, PDCClearanceAgainstMode againstMode,
            Guid id)
        {
            if (againstMode == PDCClearanceAgainstMode.PDCPayable)
            {
                var pdcPayables = (await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.LedgerId == ledgerId && x.Id == id).ToListAsync()).Select(x =>
                    new PDCClearanceDetailDto
                    {
                        Amount = x.Amount ?? 0,
                        ChequeMiti = x.ChequeMiti,
                        ChequeNo = x.ChequeNo,
                        BankId = x.BankId,
                        VoucherName = x.VoucherNo
                    }).ToList();
                return pdcPayables;
            }

            if (againstMode == PDCClearanceAgainstMode.PDCReceivable)
            {
                var pdcReceivable = (await pdcReceivablePRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.LedgerId == ledgerId && x.Id == id).ToListAsync()).Select(x =>
                    new PDCClearanceDetailDto
                    {
                        Amount = x.Amount ?? 0,
                        ChequeMiti = DateConverter.ConvertToNepali((DateTime)x.ChequeDate),
                        ChequeNo = x.ChequeNo,
                        BankId = x.BankId,
                        VoucherName = x.VoucherNo
                    }).ToList();
                return pdcReceivable;
            }

            return null;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PDCClearance");
        }

        [AbpAuthorize(AppPermissions.PagesPDCClearancesCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPDCClearanceDto input)
        {
            var postingNumbering = PostingNumbering;
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCClearance");
            var voucherNumbering = await GetInlineVoucherNo();
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;

            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                if (await pdcClearanceRepository.CountAsync(x =>
                        x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                    throw new UserFriendlyException("Voucher number already exists.");
                voucherNo = await GetPDCClearanceVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await pdcClearanceRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PDCClearance VoucherNo is Duplicate");
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate") voucherNo = input.VoucherNo;
            var againstId = Guid.Empty;
            if (input.Status == PDCClearanceType.Clearance)
            {
                if (input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable)
                    againstId = input.AgainstLedgerId;
                if (input.AgainstMode == PDCClearanceAgainstMode.PDCPayable)
                    againstId = input.BankId;
            }

            var pdcClearance = new PDCClearance
            {
                VoucherNumbering = voucherNumbering,
                TenantId = tenantId,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                AgainstMode = input.AgainstMode,
                Description = input.Description,
                Status = input.Status,
                DateMiti = input.DateMiti,
                BankId = input.BankId,
                Amount = input.Amount,
                ChequeMiti = input.ChequeMiti,
                ChequeNo = input.ChequeNo,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                PDCPayableId = input.AgainstMode == PDCClearanceAgainstMode.PDCPayable ? input.AgainstId : null,
                PDCReceivableId = input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable ? input.AgainstId : null,
                FinancialYearId = FinancialYearId,
                CreateUserId = AbpSession.UserId,
                AgainstLedgerId = againstId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };

            var masterId = await pdcClearanceRepository.InsertAndGetIdAsync(pdcClearance);

            if (input.Status == PDCClearanceType.Clearance)
            {
                if (input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable)
                {
                    var ledger = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        VendorVoucherNo = "",
                        LedgerId = input.AgainstLedgerId,
                        DetailId = input.LedgerId,
                        MasterId = masterId,
                        Debit = input.Amount,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = "",
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledger);

                    var ledger1 = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        VendorVoucherNo = "",
                        LedgerId = input.LedgerId,
                        DetailId = input.LedgerId,
                        MasterId = masterId,
                        Debit = 0,
                        FinancialYearId = FinancialYearId,
                        Credit = input.Amount,
                        InvoiceNo = "",
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledger1);
                }

                if (input.AgainstMode == PDCClearanceAgainstMode.PDCPayable)
                {
                    var ledger = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        VendorVoucherNo = "",
                        LedgerId = input.LedgerId,
                        DetailId = input.BankId,
                        MasterId = masterId,
                        Debit = input.Amount,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = "",
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledger);

                    var ledger1 = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        VendorVoucherNo = "",
                        LedgerId = input.BankId,
                        DetailId = input.LedgerId,
                        MasterId = masterId,
                        Debit = 0,
                        FinancialYearId = FinancialYearId,
                        Credit = input.Amount,
                        InvoiceNo = "",
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledger1);
                }
            }

            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPDCClearancesEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPDCClearanceDto input)
        {
            var pdcClearance = await pdcClearanceRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (pdcClearance != null)
            {
                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await pdcClearanceRepository.CountAsync(x =>
                            x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                        0) throw new UserFriendlyException("PDCClearances VoucherNo is Duplicate");
                    pdcClearance.VoucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate") pdcClearance.VoucherNo = input.VoucherNo;
                var againstId = Guid.Empty;
                if (input.Status == PDCClearanceType.Clearance)
                {
                    if (input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable)
                        againstId = input.AgainstLedgerId;
                    if (input.AgainstMode == PDCClearanceAgainstMode.PDCPayable)
                        againstId = input.BankId;
                }

                pdcClearance.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                pdcClearance.AgainstMode = input.AgainstMode;
                pdcClearance.Description = input.Description;
                pdcClearance.Status = input.Status;
                pdcClearance.DateMiti = input.DateMiti;
                pdcClearance.BankId = input.BankId;
                pdcClearance.LedgerId = input.LedgerId;
                pdcClearance.ChequeNo = input.ChequeNo;
                pdcClearance.AgainstLedgerId = input.AgainstId;
                pdcClearance.Amount = input.Amount;
                pdcClearance.ChequeMiti = input.ChequeMiti;
                pdcClearance.PDCPayableId =
                    input.AgainstMode == PDCClearanceAgainstMode.PDCPayable ? input.AgainstId : null;
                pdcClearance.PDCReceivableId =
                    input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable ? input.AgainstId : null;
                pdcClearance.UpdateUserId = AbpSession.UserId;
                await pdcClearanceRepository.UpdateAsync(pdcClearance);

                await ledgerPostingRepository.DeleteAsync(x => x.VoucherTypeId == pdcClearance.VoucherTypeId
                                                               && x.VoucherNo == pdcClearance.VoucherNo &&
                                                               x.VoucherNumbering == pdcClearance.VoucherNumbering &&
                                                               x.FinancialYearId == pdcClearance.FinancialYearId);

                if (input.Status == PDCClearanceType.Clearance)
                {
                    if (input.AgainstMode == PDCClearanceAgainstMode.PDCReceivable)
                    {
                        var ledger = new LedgerPosting
                        {
                            VoucherNumbering = pdcClearance.VoucherNumbering,
                            TenantId = pdcClearance.TenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = pdcClearance.VoucherTypeId,
                            VoucherNo = pdcClearance.VoucherNo,
                            VendorVoucherNo = "",
                            LedgerId = input.AgainstLedgerId,
                            DetailId = input.LedgerId,
                            MasterId = input.Id,
                            Debit = input.Amount,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = "",
                            PostingNumber = pdcClearance.PostingNumbering
                        };
                        await ledgerPostingRepository.InsertAsync(ledger);

                        var ledger1 = new LedgerPosting
                        {
                            VoucherNumbering = pdcClearance.VoucherNumbering,
                            TenantId = pdcClearance.TenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = pdcClearance.VoucherTypeId,
                            VoucherNo = pdcClearance.VoucherNo,
                            VendorVoucherNo = "",
                            LedgerId = input.LedgerId,
                            DetailId = input.LedgerId,
                            MasterId = input.Id,
                            Debit = 0,
                            FinancialYearId = FinancialYearId,
                            Credit = input.Amount,
                            InvoiceNo = "",
                            PostingNumber = pdcClearance.PostingNumbering
                        };
                        await ledgerPostingRepository.InsertAsync(ledger1);
                    }

                    if (input.AgainstMode == PDCClearanceAgainstMode.PDCPayable)
                    {
                        var ledger = new LedgerPosting
                        {
                            VoucherNumbering = pdcClearance.VoucherNumbering,
                            TenantId = pdcClearance.TenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = pdcClearance.VoucherTypeId,
                            VoucherNo = pdcClearance.VoucherNo,
                            VendorVoucherNo = "",
                            LedgerId = input.LedgerId,
                            DetailId = input.BankId,
                            MasterId = input.Id,
                            Debit = input.Amount,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = "",
                            PostingNumber = pdcClearance.PostingNumbering
                        };
                        await ledgerPostingRepository.InsertAsync(ledger);

                        var ledger1 = new LedgerPosting
                        {
                            VoucherNumbering = pdcClearance.VoucherNumbering,
                            TenantId = pdcClearance.TenantId,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = pdcClearance.VoucherTypeId,
                            VoucherNo = pdcClearance.VoucherNo,
                            VendorVoucherNo = "",
                            LedgerId = input.BankId,
                            DetailId = input.LedgerId,
                            MasterId = input.Id,
                            Debit = 0,
                            FinancialYearId = FinancialYearId,
                            Credit = input.Amount,
                            InvoiceNo = "",
                            PostingNumber = pdcClearance.PostingNumbering
                        };
                        await ledgerPostingRepository.InsertAsync(ledger1);
                    }
                }
            }
            else
            {
                throw new UserFriendlyException("Data not found");
            }

            return pdcClearance.Id;
        }

        public async Task<string> GetPDCClearanceVoucherNo()
        {
            var data = await pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCClearance");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCClearance");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
            ;
        }

        public async Task<List<Guid>> GetAgainstVoucherNumber(PDCClearanceAgainstMode against)
        {
            if (against == PDCClearanceAgainstMode.PDCPayable)
                return await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Select(x => x.Id).ToListAsync();
            if (against == PDCClearanceAgainstMode.PDCReceivable)
                return await pdcReceivablePRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Select(x => x.Id).ToListAsync();
            return null;
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllVoucherType(Guid ledgerId)
        {
            var result = new List<UniversalDropdownDto>();
            var data = await partyBalanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.LedgerId == ledgerId).Select(x => x.VoucherTypeId).ToListAsync();
            var distinctVoucherTypes = data.Distinct();
            foreach (var distinctVoucherType in distinctVoucherTypes)
                if (distinctVoucherType != null)
                {
                    var aa = new UniversalDropdownDto
                    {
                        Id = (Guid)distinctVoucherType,
                        DisplayName = (await voucherTypeRepository.FirstOrDefaultAsync(x =>
                            x.TenantId == AbpSession.TenantId && x.Id == distinctVoucherType)).Name
                    };
                    result.Add(aa);
                }

            return result;
        }

        public async Task<List<int>> GetVoucherNumbers(Guid ledgerId, Guid voucherTypeId)
        {
            var partyBalance = await partyBalanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId &&
                            x.VoucherTypeId == voucherTypeId).ToListAsync();
            return partyBalance.Select(x => x.VoucherNumbering).Distinct().ToList();
        }

        public async Task<decimal> GetPendingAmount(string voucherNo, Guid accountLedgerId, Guid voucherTypeId)
        {
            //  var supplierLedgerId = (await _supplierRepository.FirstOrDefaultAsync(x =>x.TenantId == AbpSession.TenantId && x.Id == supplierId)).LedgerId;
            var partyBalance = await partyBalanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(
                    x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo &&
                        x.VoucherTypeId == voucherTypeId && x.LedgerId == accountLedgerId)
                .ToListAsync();

            var result = partyBalance.Select(x => x.Credit).Sum() - partyBalance.Select(x => x.Debit).Sum();
            return result;
        }
    }
}