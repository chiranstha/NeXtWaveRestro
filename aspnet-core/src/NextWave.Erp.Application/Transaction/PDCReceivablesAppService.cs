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
using NextWave.Erp.Common;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Notifications;
using NextWave.Erp.Transaction.Dtos;
using NextWave.Erp.Transaction.Enums;
using NextWave.Erp.Transaction.Exporting;
using NextWave.Erp.Transaction.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using QuestPDF.Fluent;


namespace NextWave.Erp.Transaction
{
    public class PDCReceivablesAppService(
    IRepository<PDCReceivable, Guid> pdcReceivableRepository,
    IRepository<User, long> userRepository,
    IPDCReceivablesExcelExporter pdcReceivablesExcelExporter,
    IRepository<PDCClearance, Guid> pdcClearanceRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<FinancialYear, Guid> financialYearRepository,
    IRepository<Branch, Guid> branchRepository,
    //IBranchsAppService branchService,
    AppFolders appFolders,
    IHttpContextAccessor httpContextAccessor,
    IDocumentsAppService documentsAppService,
    IRepository<VoucherType, Guid> voucherTypeRepository,
    IAppNotifier appNotifier)
    : ErpAppServiceBase, IPDCReceivablesAppService
    {
        private readonly AppFolders _appFolders = appFolders;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IRepository<VoucherType, Guid> _voucherTypeRepository = voucherTypeRepository;

        [DisableAuditing]
        public async Task<PagedResultDto<GetPDCReceivableForViewDto>> GetAll(GetAllPDCReceivablesInput input)
        {
            //  var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var clearedPDCPayables = await pdcClearanceRepository.GetAll().Select(x => x.PDCReceivableId).ToListAsync();
            var pdcClearance = await pdcClearanceRepository.GetAll().Select(x => new
            {
                x.Id,
                x.PDCReceivableId,
                x.Status
            }).ToListAsync();
            var filteredPDCReceivables = pdcReceivableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(e => e.LedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    x => x.LedgerFk.Name.Contains(input.Filter.Trim()) || x.ChequeNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.Amount,
                    x.LedgerId,
                    x.ChequeNo,
                    x.ChequeDate,
                    x.Description,
                    x.VoucherTypeId,
                    x.Date,
                    x.DateMiti,
                    x.CreateUserId,
                    x.BankId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.LedgerFk.Name
                });
            var pagedAndFilteredPDCReceivables = filteredPDCReceivables
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var pdcReceivables = from o in pagedAndFilteredPDCReceivables
                                 join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.CreateUserId equals o6.Id into j6
                                 from s6 in j6.DefaultIfEmpty()
                                 join o1 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.BankId equals o1.Id into j8
                                 from s8 in j8.DefaultIfEmpty()
                                 join o7 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.UpdateUserId equals o7.Id into j7
                                 from s7 in j7.DefaultIfEmpty()
                                 select new GetPDCReceivableForViewDto
                                 {
                                     VoucherNo = o.VoucherNo,
                                     Date = o.Date,
                                     Amount = o.Amount,
                                     ChequeNo = o.ChequeNo,
                                     ChequeDate = o.ChequeDate,
                                     Description = o.Description,
                                     DateMiti = o.DateMiti,
                                     BankId = o.BankId,
                                     BankName = s8.Name,
                                     LedgerId = o.LedgerId,
                                     Id = o.Id,
                                     Editable = !clearedPDCPayables.Contains(o.Id),
                                     CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                     UpdateUser = s7 == null || s7.Name == null ? "" : s7.Name,
                                     LedgerName = o.LedgerName
                                 };

            var totalCount = await filteredPDCReceivables.CountAsync();
            var result = await pdcReceivables.ToListAsync();
            foreach (var o in result)
                o.StatusString = pdcClearance.Select(x => x.PDCReceivableId).Contains(o.Id)
                    ? pdcClearance.FirstOrDefault(x => x.PDCReceivableId == o.Id).Status.ToString()
                    : "Pending";
            return new PagedResultDto<GetPDCReceivableForViewDto>(
                totalCount, result
            );
        }

        public async Task<GetPDCReceivableForViewDto> GetPDCReceivableForView(Guid id)
        {
            var pdcReceivable = await pdcReceivableRepository.GetAsync(id);
            var output = new GetPDCReceivableForViewDto
            {
                Id = pdcReceivable.Id,
                VoucherNo = pdcReceivable.VoucherNo,
                Date = pdcReceivable.Date,
                Amount = pdcReceivable.Amount,
                ChequeNo = pdcReceivable.ChequeNo,
                ChequeDate = pdcReceivable.ChequeDate,
                ChequeMiti = DateConverter.ConvertToNepali((DateTime)pdcReceivable.ChequeDate),
                Description = pdcReceivable.Description,
                DateMiti = pdcReceivable.DateMiti,
                BankId = pdcReceivable.BankId,
                LedgerId = pdcReceivable.LedgerId
            };

            if (output.LedgerId != null)
            {
                var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            if (output.BankId != null)
            {
                var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(output.BankId);
                output.BankName = accountLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPDCReceivablesEdit)]
        public async Task<GetPDCReceivableForEditOutput> GetPDCReceivableForEdit(EntityDto<Guid> input)
        {
            var pdcReceivable = await pdcReceivableRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetPDCReceivableForEditOutput
            {
                Id = pdcReceivable.Id,
                VoucherNo = pdcReceivable.VoucherNo,
                Amount = pdcReceivable.Amount,
                ChequeNo = pdcReceivable.ChequeNo,
                ChequeMiti = DateConverter.ConvertToNepali((DateTime)pdcReceivable.ChequeDate),
                Description = pdcReceivable.Description,
                DateMiti = pdcReceivable.DateMiti,
                BankId = pdcReceivable.BankId,
                LedgerId = pdcReceivable.LedgerId
            };

            if (output.LedgerId != null)
            {
                var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPDCReceivableDto input)
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

        [AbpAuthorize(AppPermissions.PagesPDCReceivablesDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var pdcReceive = await pdcReceivableRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            var pdcClearance = await pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PDCReceivableId == input.Id).ToListAsync();
            if (pdcClearance.Count > 0)
                throw new UserFriendlyException("This PDC is already cleared, so it cann't be deleted.");

            await pdcReceivableRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPDCReceivablesToExcel(GetAllPDCReceivablesInput input)
        {
            var pdcClearance = await pdcClearanceRepository.GetAll().Select(x => new { x.PDCReceivableId, x.Status })
                .ToListAsync();

            var filteredPDCReceivables = pdcReceivableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(e => e.LedgerFk).Where(x => x.FinancialYearId == FinancialYearId);

            var pdcReceivableListDtos = (await filteredPDCReceivables.ToListAsync()).Select(o =>
                new GetPDCReceivableForViewDto
                {
                    VoucherNo = o.VoucherNo,
                    Date = o.Date,
                    Amount = o.Amount,
                    ChequeNo = o.ChequeNo,
                    ChequeDate = o.ChequeDate,
                    Description = o.Description,
                    DateMiti = o.DateMiti,
                    ChequeMiti = DateConverter.ConvertToNepali((DateTime)o.ChequeDate),
                    Editable = pdcClearance.Select(x => x.PDCReceivableId).Contains(o.Id) ? true : false,
                    Status = pdcClearance.Select(x => x.PDCReceivableId).Contains(o.Id)
                        ? pdcClearance.FirstOrDefault(x => x.PDCReceivableId == o.Id).Status
                        : PDCClearanceType.Bounced,
                    BankId = o.BankId,
                    Id = o.Id,
                    LedgerName = o.LedgerFk.Name
                }).ToList();
            foreach (var a in pdcReceivableListDtos)
                a.BankName = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == a.BankId)).Name;
            return pdcReceivablesExcelExporter.ExportToFile(pdcReceivableListDtos);
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCReceivables)]
        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Include(x => x.AccountGroupFk)
                .Where(x => (x.TenantId == AbpSession.TenantId && x.AccountGroupFk.Name == "Sundry Creditors") ||
                            x.AccountGroupFk.Name == "Sundry Debtors")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null ? "" : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCReceivables)]
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

        public async Task<GetPdfForPDCReceivables> GetPDCReceivablesForPdf(EntityDto<Guid> input)
        {
            var pdcReceivables = await pdcReceivableRepository.FirstOrDefaultAsync(input.Id);
            //  var companyInfo = await branchService.GetBranchForView(pdcReceivables.BranchId);
            var mainCompany =
                await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var ledgerInfo = await accountLedgerRepository.FirstOrDefaultAsync(pdcReceivables.BankId);
            var output = new GetPdfForPDCReceivables
            {
                //    BranchName = companyInfo.CompanyName,
                //    Logo1 = mainCompany.Image1,
                //   BranchAddress = companyInfo.Address,
                //    BranchContact = companyInfo.CompanyContact,
                VoucherNo = pdcReceivables.VoucherNo,
                Date = pdcReceivables.Date,
                TotalAmountInWord = CurrencyToAmount.NumberToText((int)pdcReceivables.Amount),
                TotalAmount = pdcReceivables.Amount,
                Bank = ledgerInfo.Name,
                ChequeNo = pdcReceivables.ChequeNo,
                ChequeDate = pdcReceivables.ChequeDate,
                ChequeMiti = DateConverter.ConvertToNepali((DateTime)pdcReceivables.ChequeDate),
                Narration = pdcReceivables.Description,
                DateMiti = pdcReceivables.DateMiti,
                //    BranchId = pdcReceivables.BranchId,
                LedgerId = pdcReceivables.LedgerId
            };
            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPDCReceivablesPrint)]
        public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        {
            var model = await GetPDCReceivablesForPdf(input);
            var document = new PDCReceivablePdf(model);
            var file = new FileDto("PDCReceivable.pdf", MimeTypeNames.ApplicationPdf);
            return document.GeneratePdf();
        }

        public async Task GetPdfDownload1(EntityDto<Guid> input)
        {
            var filePath = "PDCPayablePdf.pdf";
            var model = await GetPDCReceivablesForPdf(input);
            //List<PdfForStockTransferModel> list = new List<PdfForStockTransferModel>();
            //list.Add(model);
            var document = new PDCReceivablePdf(model);
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
            if (await pdcReceivableRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) >
                0) return true;

            return false;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCReceivable");
            var pdcReceivables = await pdcReceivableRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId &&
                x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = pdcReceivables.VoucherTypeId,
                VoucherNo = pdcReceivables.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public async Task<string> GetPDCReceivableVoucherNo()
        {
            var data = await pdcReceivableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCReceivable");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await pdcReceivableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCReceivable");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
            ;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PDCReceivable");
        }

        [AbpAuthorize(AppPermissions.PagesPDCReceivablesCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPDCReceivableDto input)
        {
            var postingNumbering = PostingNumbering;
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCReceivable");
            var voucherNumbering = await GetInlineVoucherNo();
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                if (await pdcReceivableRepository.CountAsync(x =>
                        x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                    throw new UserFriendlyException("Voucher number already exist");
                voucherNo = await GetPDCReceivableVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await pdcReceivableRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PDCReceivable VoucherNo is Duplicate");
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate") voucherNo = input.VoucherNo;
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            var pdcReceivable = new PDCReceivable
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                Amount = input.Amount,
                ChequeNo = input.ChequeNo,
                ChequeDate = DateConverter.ConvertToEnglish(input.ChequeMiti),
                Description = input.Description,
                DateMiti = input.DateMiti,
                TenantId = tenantId,
                VoucherTypeId = voucherTypeId,
                BankId = input.BankId,
                LedgerId = input.LedgerId,
                FinancialYearId = FinancialYearId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };
            var masterId = await pdcReceivableRepository.InsertAndGetIdAsync(pdcReceivable);
            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPDCReceivablesEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPDCReceivableDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCReceivable");
            var pdcReceivable = await pdcReceivableRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (pdcReceivable != null)
            {
                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await pdcReceivableRepository.CountAsync(x =>
                            x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                        0) throw new UserFriendlyException("PDCReceivables VoucherNo is Duplicate");
                    pdcReceivable.VoucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate") pdcReceivable.VoucherNo = input.VoucherNo;
                pdcReceivable.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                pdcReceivable.Amount = input.Amount;
                pdcReceivable.ChequeNo = input.ChequeNo;
                pdcReceivable.ChequeDate = DateConverter.ConvertToEnglish(input.ChequeMiti);
                pdcReceivable.Description = input.Description;
                pdcReceivable.DateMiti = input.DateMiti;
                pdcReceivable.BankId = input.BankId;
                pdcReceivable.LedgerId = input.LedgerId;
                pdcReceivable.UpdateUserId = AbpSession.UserId;
                await pdcReceivableRepository.UpdateAsync(pdcReceivable);
            }
            else
            {
                throw new UserFriendlyException("Data not found");
            }

            return pdcReceivable.Id;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCReceivables)]
        public async Task<List<UniversalDropdownDto>> GetAllBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.AccountGroupFk.Name == "Bank Account")
                .Include(x => x.AccountGroupFk).Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null ? "" : accountLedger.Name.ToString()
                }).ToListAsync();
        }
    }
}