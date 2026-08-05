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
using NextWave.Erp.Transaction.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using NextWave.Erp.Transaction.Exporting;



namespace NextWave.Erp.Transaction
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesPDCPayables)]
    public class PDCPayablesAppService(
    IRepository<PDCPayable, Guid> pdcPayableRepository,
    IRepository<User, long> userRepository,
    IRepository<PDCClearance, Guid> pdcClearanceRepository,
    IRepository<Branch, Guid> branchRepository,
 //   IBranchsAppService branchService,
    PdcPayablesExcelExporter pdcPayablesExcelExporter,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IDocumentsAppService documentsAppService)
    : ErpAppServiceBase, IPDCPayablesAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetPDCPayableForViewDto>> GetAll(GetAllPDCPayablesInput input)
        {
            //   var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var clearedPDCPayables = await pdcClearanceRepository.GetAll().Select(x => x.PDCPayableId).ToListAsync();
            var pdcClearance = await pdcClearanceRepository.GetAll().Select(x => new
            {
                x.Id,
                x.PDCPayableId,
                x.Status
            }).ToListAsync();
            var filteredPDCPayables = pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.LedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    x => x.LedgerFk.Name.Contains(input.Filter.Trim()) || x.ChequeNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.Date,
                    x.DateMiti,
                    x.Amount,
                    x.ChequeNo,
                    x.ChequeMiti,
                    x.Description,
                    x.BankId,
                    x.VoucherTypeId,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    x.LedgerId,
                    LedgerName = x.LedgerFk.Name
                });

            var pagedAndFilteredPDCPayables = filteredPDCPayables
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var pdcPayables = from o in pagedAndFilteredPDCPayables
                              join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                  o.CreateUserId equals o6.Id into j6
                              from s6 in j6.DefaultIfEmpty()
                              join o7 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                  o.UpdateUserId equals o7.Id into j7
                              from s7 in j7.DefaultIfEmpty()
                              select new GetPDCPayableForViewDto
                              {
                                  VoucherNo = o.VoucherNo,
                                  Date = o.Date,
                                  Amount = o.Amount,
                                  ChequeNo = o.ChequeNo,
                                  ChequeMiti = o.ChequeMiti,
                                  Description = o.Description,
                                  BankId = o.BankId,
                                  DateMiti = o.DateMiti,
                                  Id = o.Id,
                                  LedgerId = o.LedgerId,
                                  Editable = !clearedPDCPayables.Contains(o.Id),
                                  CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                  UpdateUser = s7 == null || s7.Name == null ? "" : s7.Name,
                                  LedgerName = o.LedgerName
                              };
            var finalPDCPayables = await pdcPayables.ToListAsync();
            var accountLedgers = await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().ToListAsync();
            foreach (var a in finalPDCPayables)
            {
                var bankName = accountLedgers.FirstOrDefault(x => x.Id == a.BankId);
                if (bankName != null)
                    a.BankName = bankName.Name;
                a.StatusString = pdcClearance.Select(x => x.PDCPayableId).Contains(a.Id)
                    ? pdcClearance.FirstOrDefault(x => x.PDCPayableId == a.Id).Status.ToString()
                    : "Pending";
            }

            var totalCount = await filteredPDCPayables.CountAsync();

            return new PagedResultDto<GetPDCPayableForViewDto>(
                totalCount, finalPDCPayables
            );
        }

        public async Task<GetPDCPayableForViewDto> GetPDCPayableForView(Guid id)
        {
            var pdcPayable = await pdcPayableRepository.GetAsync(id);

            var output = new GetPDCPayableForViewDto
            {
                Id = pdcPayable.Id,
                VoucherNo = pdcPayable.VoucherNo,
                Date = pdcPayable.Date,
                DateMiti = pdcPayable.DateMiti,
                Amount = pdcPayable.Amount,
                ChequeNo = pdcPayable.ChequeNo,
                BankId = pdcPayable.BankId,
                ChequeMiti = pdcPayable.ChequeMiti,
                Description = pdcPayable.Description,
                LedgerId = pdcPayable.LedgerId
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

        [AbpAuthorize(AppPermissions.PagesPDCPayablesEdit)]
        public async Task<GetPDCPayableForEditOutput> GetPDCPayableForEdit(EntityDto<Guid> input)
        {
            var pdcPayable = await pdcPayableRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetPDCPayableForEditOutput
            {
                Id = pdcPayable.Id,
                VoucherNo = pdcPayable.VoucherNo,
                Amount = pdcPayable.Amount,
                ChequeNo = pdcPayable.ChequeNo,
                BankId = pdcPayable.BankId,
                ChequeMiti = pdcPayable.ChequeMiti,
                Description = pdcPayable.Description,
                DateMiti = pdcPayable.DateMiti,
                LedgerId = pdcPayable.LedgerId
            };

            if (output.LedgerId != null)
            {
                var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPDCPayableDto input)
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

        [AbpAuthorize(AppPermissions.PagesPDCPayablesDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var pdcPay =
                await pdcPayableRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.Id == input.Id);
            var pdcClearance = await pdcClearanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PDCPayableId == input.Id).ToListAsync();
            if (pdcClearance.Count > 0)
                throw new UserFriendlyException("This PDC is already cleared, so it cann't be deleted.");

            await pdcPayableRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPDCPayablesToExcel(GetAllPDCPayablesForExcelInput input)
        {
            var pdcClearance = await pdcClearanceRepository.GetAll().Select(x => new { x.PDCPayableId, x.Status })
                .ToListAsync();
            var filteredPDCPayables = pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.LedgerFk);

            var pdcPayableListDtos = (await filteredPDCPayables.ToListAsync()).Select(o => new GetPDCPayableForViewDto
            {
                VoucherNo = o.VoucherNo,
                Date = o.Date,
                Amount = o.Amount,
                ChequeNo = o.ChequeNo,
                ChequeMiti = o.ChequeMiti,
                Description = o.Description,
                DateMiti = o.DateMiti,
                Id = o.Id,
                BankId = o.BankId,
                Editable = pdcClearance.Select(x => x.PDCPayableId).Contains(o.Id) ? true : false,
                Status = pdcClearance.Select(x => x.PDCPayableId).Contains(o.Id)
                    ? pdcClearance.FirstOrDefault(x => x.PDCPayableId == o.Id).Status
                    : PDCClearanceType.Bounced,
                LedgerName = o.LedgerFk.Name
            }).ToList();
            foreach (var a in pdcPayableListDtos)
                a.BankName = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == a.BankId)).Name;

            return pdcPayablesExcelExporter.ExportToFile(pdcPayableListDtos);
        }

        public async Task<GetPdfForPDCPayable> GetPDCPayablesForPdf(EntityDto<Guid> input)
        {
            var pdcPayables = await pdcPayableRepository.FirstOrDefaultAsync(input.Id);
            //    var companyInfo = await branchService.GetBranchForView();
            var mainCompany =
                await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var ledgerInfo = await accountLedgerRepository.FirstOrDefaultAsync(pdcPayables.BankId);
            var output = new GetPdfForPDCPayable
            {
                Logo1 = mainCompany.Image1,
                //    BranchName = companyInfo.CompanyName,
                //   BranchAddress = companyInfo.Address,
                //    BranchContact = companyInfo.CompanyContact,
                VoucherNo = pdcPayables.VoucherNo,
                Date = pdcPayables.Date,
                TotalAmountInWord = CurrencyToAmount.NumberToText((int)pdcPayables.Amount),
                TotalAmount = pdcPayables.Amount,
                Bank = ledgerInfo.Name,
                ChequeNo = pdcPayables.ChequeNo,
                ChequeMiti = pdcPayables.ChequeMiti,
                Narration = pdcPayables.Description,
                DateMiti = pdcPayables.DateMiti,
                LedgerId = pdcPayables.LedgerId
            };
            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPDCPayablesPrint)]
        public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        {
            var model = await GetPDCPayablesForPdf(input);
            var document = new PDCPayablePdf(model);
            var file = new FileDto("PDCPayable.pdf", MimeTypeNames.ApplicationPdf);
            return document.GeneratePdf();
        }

        public async Task GetPdfDownload1(EntityDto<Guid> input)
        {
            var filePath = "PDCPayablePdf.pdf";
            var model = await GetPDCPayablesForPdf(input);
            //List<PdfForStockTransferModel> list = new List<PdfForStockTransferModel>();
            //list.Add(model);
            var document = new PDCPayablePdf(model);
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

        public async Task<bool> GetCheckVoucherNo(Guid branchId, string voucherNo)
        {
            if (await pdcPayableRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) >
                0) return true;

            return false;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCPayable");
            var pdcPayables = await pdcPayableRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId &&
                x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = pdcPayables.VoucherTypeId,
                VoucherNo = pdcPayables.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public async Task<string> GetPDCPayableVoucherNo()
        {
            var data = await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCPayable");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await pdcPayableRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PDCPayable");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PDCPayable");
        }

        [AbpAuthorize(AppPermissions.PagesPDCPayablesCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPDCPayableDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PDCPayable");
            var voucherNumbering = await GetInlineVoucherNo();
            var postingNumbering = PostingNumbering;
            var voucherNo = string.Empty;
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

            if (await GetVoucherGenerateType() == "Automatic")
            {
                if (await pdcPayableRepository.CountAsync(x =>
                        x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                    throw new UserFriendlyException("Voucher number already exist");
                voucherNo = await GetPDCPayableVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await pdcPayableRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PDCPayable VoucherNo is Duplicate");
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate") voucherNo = input.VoucherNo;

            var pdcPayable = new PDCPayable
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                Amount = input.Amount,
                ChequeNo = input.ChequeNo,
                TenantId = tenantId,
                ChequeMiti = input.ChequeMiti,
                Description = input.Description,
                DateMiti = input.DateMiti,
                BankId = input.BankId,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                FinancialYearId = FinancialYearId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };

            var masterId = await pdcPayableRepository.InsertAndGetIdAsync(pdcPayable);
            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPDCPayablesEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPDCPayableDto input)
        {
            var pdcPayable = await pdcPayableRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (pdcPayable != null)
            {
                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await pdcPayableRepository.CountAsync(x =>
                            x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                        0) throw new UserFriendlyException("PDCPayables VoucherNo is Duplicate");
                    pdcPayable.VoucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate") pdcPayable.VoucherNo = input.VoucherNo;
                pdcPayable.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                pdcPayable.Amount = input.Amount;
                pdcPayable.BankId = input.BankId;
                pdcPayable.ChequeNo = input.ChequeNo;
                pdcPayable.ChequeMiti = input.ChequeMiti;
                pdcPayable.Description = input.Description;
                pdcPayable.DateMiti = input.DateMiti;
                pdcPayable.LedgerId = input.LedgerId;
                pdcPayable.UpdateUserId = AbpSession.UserId;
                await pdcPayableRepository.UpdateAsync(pdcPayable);
            }
            else
            {
                throw new UserFriendlyException("Data not found");
            }

            return pdcPayable.Id;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPDCPayables)]
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
        [AbpAuthorize(AppPermissions.PagesPDCPayables)]
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