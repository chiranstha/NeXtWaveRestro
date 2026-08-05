using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
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
    [Audited]
    [AbpAuthorize(AppPermissions.PagesContraMasters)]
    public class ContraMastersAppService(
    IRepository<ContraMaster, Guid> contraMasterRepository,
    IContraMastersExcelExporter contraMastersExcelExporter,
    IRepository<User, long> userRepository,
    IUnitOfWorkManager unitOfWorkManager,
    //IRepository<InterestCalculation, Guid> interestCalculationRepository,
    IRepository<ContraDetail, Guid> contraDetailRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IDocumentsAppService documentsAppService,
    IAppNotifier appNotifier,
    UserManager userManager,
    IRepository<UserBranch, Guid> userBranchRepository)
    : ErpAppServiceBase, IContraMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetContraMasterForViewDto>> GetAll(GetAllContraMastersInput input)
        {
            // var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var filteredContraMasters = contraMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrEmpty(input.Filter),
                    x => x.AccountLedgerFk.Name.Contains(input.Filter.Trim()) || x.VoucherNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.TotalAmount,
                    x.LedgerId,
                    x.Narration,
                    x.Date,
                    x.DateMiti,
                    x.Type,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredContraMasters = filteredContraMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredContraMasters = filteredContraMasters.Where(x => x.Date <= date);
            }


            var pagedAndFilteredContraMasters = filteredContraMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var contraMasters = from o in pagedAndFilteredContraMasters
                                join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                    o.CreateUserId equals o6.Id into j6
                                from s6 in j6.DefaultIfEmpty()
                                join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                    o.UpdateUserId equals o5.Id into j5
                                from s5 in j5.DefaultIfEmpty()
                                select new GetContraMasterForViewDto
                                {
                                    Id = o.Id,
                                    VoucherNo = o.VoucherNo,
                                    TotalAmount = o.TotalAmount,
                                    LedgerId = o.LedgerId,
                                    Narration = o.Narration,
                                    DateMiti = o.DateMiti,
                                    Type = o.Type,
                                    CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                    UpdateUser = s5 == null || s5.Name == null ? "" : s5.Name,
                                    LedgerName = o.LedgerName,
                                };

            var totalCount = await filteredContraMasters.CountAsync();
            // Contraposting();
            //await UpdatemasterId();
            return new PagedResultDto<GetContraMasterForViewDto>(
                totalCount,
                await contraMasters.ToListAsync()
            );
        }

        public async Task<GetContraMasterForViewDto> GetContraMasterForView(Guid id)
        {
            var contraMaster = await contraMasterRepository.GetAsync(id);

            var output = new GetContraMasterForViewDto
            {
                Id = contraMaster.Id,
                Type = contraMaster.Type,
                LedgerId = contraMaster.LedgerId,
                VoucherNo = contraMaster.VoucherNo,
                TotalAmount = contraMaster.TotalAmount,
                Narration = contraMaster.Narration
            };

            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesContraMastersEdit)]
        public async Task<GetContraMasterForEditOutput> GetContraMasterForEdit(EntityDto<Guid> input)
        {
            var contraMaster = await contraMasterRepository.FirstOrDefaultAsync(input.Id);
            var contraDetails = await contraDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ContraMasterId == contraMaster.Id).Include(x => x.AccountLedgerFk).AsNoTracking()
                .ToListAsync();
            var output = new GetContraMasterForEditOutput
            {
                Id = contraMaster.Id,
                Type = contraMaster.Type,
                VoucherNo = contraMaster.VoucherNo,
                Date = contraMaster.Date,
                TotalAmount = contraMaster.TotalAmount,
                Narration = contraMaster.Narration,
                DateMiti = contraMaster.DateMiti,
                LedgerId = contraMaster.LedgerId,
                ContraDetails = contraDetails.Select(x => new GetContraDetailForEditOutput
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    ChequeNo = x.ChequeNo,
                    ChequeMiti = x.ChequeMiti,
                    LedgerId = x.LedgerId,
                    LedgerName = x.AccountLedgerFk.Name
                }).ToList()
            };

            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditContraMasterDto input)
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

        [AbpAuthorize(AppPermissions.PagesContraMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var master = await contraMasterRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.Id == input.Id && x.FinancialYearId == FinancialYearId);
            if (master == null) throw new UserFriendlyException("Data  for Id : " + input.Id + " not Found.");

            await contraDetailRepository.DeleteAsync(x => x.ContraMasterId == input.Id);

            await ledgerPostingRepository.DeleteAsync(x => x.MasterId == input.Id &&
                                                           x.VoucherNo == master.VoucherNo &&
                                                           x.VoucherNumbering == master.VoucherNumbering &&
                                                           x.FinancialYearId == master.FinancialYearId &&
                                                           x.VoucherTypeId == master.VoucherTypeId);          

            await contraMasterRepository.DeleteAsync(master);
        }

        public async Task<FileDto> GetContraMastersToExcel(GetAllContraMastersForExcelInput input)
        {
            var filteredContraMasters = contraMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk)
                .OrderBy(x => x.VoucherNumbering);

            var query = from o in filteredContraMasters
                        join o1 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on o.LedgerId
                            equals o1.Id into j1
                        from s1 in j1.DefaultIfEmpty()
                        select new GetContraMasterForViewDto
                        {
                            Type = o.Type,
                            VoucherNo = o.VoucherNo,
                            TotalAmount = o.TotalAmount,
                            DateMiti = o.DateMiti,
                            Narration = o.Narration,
                            LedgerName = s1 == null || s1.Name == null ? "" : s1.Name,
                        };

            var contraMasterListDtos = await query.ToListAsync();

            return contraMastersExcelExporter.ExportToFile(contraMasterListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesContraMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
                    x.AccountGroupFk.Name == "Cash-in Hand" || x.AccountGroupFk.Name == "Bank Account" ||
                    x.AccountGroupFk.Name == "Bank OD A/C")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        public async Task<ContraMasterForViewNewDto> GetContraMasterForViewNew(Guid id)
        {
            var contraMaster = (await contraMasterRepository.GetAll().Where(x => x.Id == id)
                .Include(x => x.AccountLedgerFk).AsNoTracking().ToListAsync()).FirstOrDefault();
            if (contraMaster != null)
            {
                var result = new ContraMasterForViewNewDto
                {
                    Id = contraMaster.Id,
                    Type = contraMaster.Type.ToString(),
                    LedgerId = contraMaster.LedgerId,
                    LedgerName = contraMaster.AccountLedgerFk.Name,
                    DateMiti = contraMaster.DateMiti,
                    Narration = contraMaster.Narration,
                    VoucherNo = contraMaster.VoucherNo,
                    TotalAmount = contraMaster.TotalAmount,
                    Details = await contraDetailRepository.GetAll().Where(x => x.ContraMasterId == id)
                        .Include(x => x.AccountLedgerFk).Select(x => new ContraDetailsForViewNewDto
                        {
                            Id = x.Id,
                            Amount = x.Amount,
                            ChequeNo = x.ChequeNo,
                            ChequeMiti = x.ChequeMiti,
                            LedgerId = x.LedgerId,
                            LedgerName = x.AccountLedgerFk.Name
                        }).AsNoTracking().ToListAsync()
                };
                return result;
            }

            return null;
        }


        [DisableAuditing]
        public async Task<string> GetLedgerBalanceStatus(Guid ledgerId)
        {
            var ledgerPostings = await ledgerPostingRepository.GetAll()
                .Where(x => x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId)
                .AsNoTracking().ToListAsync();

            var balance = ledgerPostings.Sum(x => x.Debit - x.Credit);
            return balance > 0 ? balance + "Dr" : Math.Abs(balance) + "Cr";
        }

        public async Task<GetPdfForContraMaster> GetContraMasterForPdf(EntityDto<Guid> input)
        {
            var serial = 1;
            var contraMaster = await contraMasterRepository.FirstOrDefaultAsync(input.Id);
            //var companyInfo = await branchService.GetBranchForView(contraMaster.BranchId);
            //var mainBranch = await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var contraDetails = await contraDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ContraMasterId == contraMaster.Id)
                .Include(x => x.AccountLedgerFk).AsNoTracking()
                .ToListAsync();
            var output = new GetPdfForContraMaster
            {
                Type = contraMaster.Type,
                //CompanyName = companyInfo.CompanyName,
                //Logo1 = mainBranch.Image1,
                //CompanyAddress = companyInfo.Address,
                //CompanyContact = companyInfo.CompanyContact,
                VoucherNo = contraMaster.VoucherNo,
                Date = contraMaster.Date,
                TotalAmountInWord = CurrencyToAmount.NumberToText((long)contraMaster.TotalAmount),
                TotalAmount = contraMaster.TotalAmount,
                Narration = contraMaster.Narration,
                DateMiti = contraMaster.DateMiti,
                LedgerId = contraMaster.LedgerId,

                ContraDetails = contraDetails.Select(x => new GetPdfForContraDetail
                {
                    SlNo = serial++,
                    Amount = x.Amount,
                    ChequeNo = x.ChequeNo,
                    ChequeDate = x.ChequeMiti,
                    LedgerId = x.LedgerId,
                    LedgerName = x.AccountLedgerFk.Name
                }).ToList()
            };

            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }
            return output;
        }

        //[AbpAuthorize(AppPermissions.PagesContraMastersPrint)]
        //public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        //{
        //    var model = await GetContraMasterForPdf(input);
        //    var document = new ContraMasterPdf(model);
        //    // var file = new FileDto("ContraMaster.pdf", MimeTypeNames.ApplicationPdf);
        //    return document.GeneratePdf();
        //}

        public async Task<bool> GetCheckVoucherNo(Guid branchId, string voucherNo)
        {
            if (await contraMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) >
                0) return true;

            return false;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ContraVoucher");
            var contraMaster = await contraMasterRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId &&
                x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = contraMaster.VoucherTypeId,
                VoucherNo = contraMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }


        [AbpAuthorize(AppPermissions.PagesContraMasters)]
        public async Task<string> GetContraMasterVoucherNo()
        {
            var data = await contraMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ContraVoucher");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await contraMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ContraVoucher");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesContraMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
                    x.AccountGroupFk.Name == "Cash-in Hand" || x.AccountGroupFk.Name == "Bank Account" ||
                    x.AccountGroupFk.Name == "Bank OD A/C")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }


        public void GetDateUpdate()
        {
            var data = contraMasterRepository.GetAllList();
            foreach (var item in data)
            {
                item.DateMiti = DateConverter.ConvertToNepali(Convert.ToDateTime(item.Date));
                contraMasterRepository.UpdateAsync(item);
            }
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "ContraVoucher");
        }

        [AbpAuthorize(AppPermissions.PagesContraMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditContraMasterDto input)
        {
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                decimal? postingNumbering = PostingNumbering;
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ContraVoucher");
                var tenantId = AbpSession.TenantId;
                if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

                //VoucherGeneration 
                var voucherNumbering = 0;
                var voucherNo = string.Empty;
                if (await GetVoucherGenerateType() == "Automatic")
                {
                    voucherNumbering = await GetInlineVoucherNo();
                    voucherNo = await GetContraMasterVoucherNo();
                }

                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await contraMasterRepository.CountAsync(x =>
                            x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                        0) throw new UserFriendlyException("ContraVoucher VoucherNo is Duplicate");
                    voucherNumbering = await GetInlineVoucherNo();
                    voucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate")
                {
                    voucherNumbering = await GetInlineVoucherNo();
                    voucherNo = input.VoucherNo;
                }

                var contraMasterData = new ContraMaster
                {
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    TenantId = tenantId,
                    Type = input.Type,
                    VoucherTypeId = voucherTypeId,
                    FinancialYearId = FinancialYearId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    TotalAmount = input.TotalAmount,
                    Narration = input.Narration,
                    DateMiti = input.DateMiti,
                    LedgerId = input.LedgerId,
                    CreateUserId = AbpSession.UserId,
                    UpdateUserId = null,
                    PostingNumbering = postingNumbering
                };
                var contraMasterId = await contraMasterRepository.InsertAndGetIdAsync(contraMasterData);

                var ledgerPosting = new LedgerPosting
                {
                    VoucherNumbering = voucherNumbering,
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DetailId = input.ContraDetails.Select(x => x.LedgerId).FirstOrDefault(),
                    VoucherNo = voucherNo,
                    Debit = input.Type == ContraType.Deposite ? input.TotalAmount : 0,
                    Credit = input.Type == ContraType.WithDraw ? input.TotalAmount : 0,
                    InvoiceNo = voucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.LedgerId,
                    MasterId = contraMasterId,
                    PostingNumber = postingNumbering
                };

                await ledgerPostingRepository.InsertAsync(ledgerPosting);

                foreach (var detail in input.ContraDetails)
                {
                    var data = new ContraDetail
                    {
                        TenantId = tenantId,
                        Amount = detail.Amount,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                            ? null
                            : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                        ContraMasterId = contraMasterId,
                        LedgerId = detail.LedgerId
                    };
                    await contraDetailRepository.InsertAndGetIdAsync(data);

                    var ledgerPostingDetail = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        VoucherNo = voucherNo,
                        DetailId = input.LedgerId,
                        Debit = input.Type == ContraType.WithDraw ? detail.Amount : 0,
                        Credit = input.Type == ContraType.Deposite ? detail.Amount : 0,
                        InvoiceNo = voucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = detail.LedgerId,
                        MasterId = contraMasterId,
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPostingDetail);

                    await unitOfWork.CompleteAsync();
                }

                return contraMasterId;
            }
        }

        [AbpAuthorize(AppPermissions.PagesContraMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditContraMasterDto input)
        {
            var unitOfWork = unitOfWorkManager.Begin();
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ContraVoucher");

            var tenantId = AbpSession.TenantId;
            var contraMaster = await contraMasterRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

            if (contraMaster == null) throw new UserFriendlyException("Data for this not found");
            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await contraMasterRepository.CountAsync(x =>
                        x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("ContraMaster VoucherNo is Duplicate");
                contraMaster.VoucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate") contraMaster.VoucherNo = input.VoucherNo;
            var postingNumbering = contraMaster.PostingNumbering;
            contraMaster.Type = input.Type;
            contraMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
            contraMaster.TotalAmount = input.TotalAmount;
            contraMaster.Narration = input.Narration;
            contraMaster.DateMiti = input.DateMiti;
            contraMaster.LedgerId = input.LedgerId;
            contraMaster.UpdateUserId = AbpSession.UserId;
            await contraMasterRepository.UpdateAsync(contraMaster);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherNo == contraMaster.VoucherNo && x.VoucherTypeId == contraMaster.VoucherTypeId &&
                x.VoucherNumbering == contraMaster.VoucherNumbering && x.FinancialYearId == FinancialYearId);

            var ledgerPosting = new LedgerPosting
            {
                VoucherNumbering = contraMaster.VoucherNumbering,
                TenantId = tenantId,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DetailId = input.ContraDetails.Select(x => x.LedgerId).FirstOrDefault(),
                VoucherNo = contraMaster.VoucherNo,
                Debit = input.Type == ContraType.Deposite ? input.TotalAmount : 0,
                Credit = input.Type == ContraType.WithDraw ? input.TotalAmount : 0,
                InvoiceNo = contraMaster.VoucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                MasterId = contraMaster.Id,
                PostingNumber = postingNumbering
            };
            await ledgerPostingRepository.InsertAsync(ledgerPosting);

            var detailsIds = input.ContraDetails.Select(x => x.Id).ToList();
            var detailsDataBaseIds = await contraDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ContraMasterId == input.Id).Select(x => x.Id).ToListAsync();

            foreach (var detailsDataBaseId in detailsDataBaseIds)
                if (!detailsIds.Contains(detailsDataBaseId))
                    await contraDetailRepository.DeleteAsync(detailsDataBaseId);

            //await _interestCalculationRepository.DeleteAsync(x => x.VoucherNumbering == contraMaster.VoucherNumbering &&
            //    x.VoucherTypeId == voucherTypeId && x.VoucherNo == contraMaster.VoucherNo &&
            //    x.FinancialYearId == FinancialYearId && x.BranchId == input.BranchId);

            foreach (var detail in input.ContraDetails)
                if (detail.Id == Guid.Empty || detail.Id == null)
                {
                    var data = new ContraDetail
                    {
                        TenantId = tenantId,
                        Amount = detail.Amount,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                            ? null
                            : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                        ContraMasterId = contraMaster.Id,
                        LedgerId = detail.LedgerId
                    };
                    await contraDetailRepository.InsertAsync(data);

                    var ledgerPostingDetail = new LedgerPosting
                    {
                        VoucherNumbering = contraMaster.VoucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        VoucherNo = contraMaster.VoucherNo,
                        DetailId = input.LedgerId,
                        Debit = input.Type == ContraType.Deposite ? 0 : detail.Amount,
                        Credit = input.Type == ContraType.WithDraw ? 0 : detail.Amount,
                        InvoiceNo = contraMaster.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = detail.LedgerId,
                        MasterId = contraMaster.Id,
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPostingDetail);

                    //var interestRate =
                    //    await _interestRateRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId &&
                    //        x.LedgerId == detail.LedgerId && x.IsActive);

                    //var interest = new InterestCalculation
                    //{
                    //    VoucherNumbering = contraMaster.VoucherNumbering,
                    //    LoanNo = 0,
                    //    InterestAmount = 0,
                    //    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    //    DateMiti = input.DateMiti,
                    //    VoucherTypeId = voucherTypeId,
                    //    VoucherNo = contraMaster.VoucherNo,
                    //    InterestCalculationEnum = InterestCalculationEnum.OD,
                    //    LedgerId = detail.LedgerId,
                    //    Debit = input.Type == ContraType.Deposite ? detail.Amount : 0,
                    //    Credit = input.Type == ContraType.WithDraw ? detail.Amount : 0,
                    //    InterestRate = interestRate?.Rate ?? 0,
                    //    FinancialYearId = FinancialYearId,
                    //    BranchId = input.BranchId,
                    //    IsPaid = false,
                    //    TenantId = tenantId
                    //};
                    //await _interestCalculationRepository.InsertAsync(interest);
                }
                else
                {
                    var data = await contraDetailRepository.GetAsync((Guid)detail.Id);
                    data.Amount = detail.Amount;
                    data.ChequeNo = detail.ChequeNo;
                    data.ChequeDate = DateConverter.ConvertToEnglish(detail.ChequeMiti);
                    data.ChequeMiti = detail.ChequeMiti;
                    data.LedgerId = detail.LedgerId;
                    await contraDetailRepository.UpdateAsync(data);

                    var ledgerPostingDetail = new LedgerPosting
                    {
                        VoucherNumbering = contraMaster.VoucherNumbering,
                        TenantId = tenantId,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        VoucherNo = contraMaster.VoucherNo,
                        DetailId = input.LedgerId,
                        Debit = input.Type == ContraType.Deposite ? 0 : detail.Amount,
                        Credit = input.Type == ContraType.WithDraw ? 0 : detail.Amount,
                        InvoiceNo = contraMaster.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = detail.LedgerId,
                        MasterId = contraMaster.Id,
                        PostingNumber = postingNumbering
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPostingDetail);

                }

            await unitOfWork.CompleteAsync();
            return contraMaster.Id;
        }
    }
}