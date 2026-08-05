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
    public class PaymentMastersAppService(
    IRepository<PaymentMaster, Guid> paymentMasterRepository,
    IPaymentMastersExcelExporter paymentMastersExcelExporter,
    IRepository<User, long> userRepository,
    IRepository<NewPartyBalance, Guid> newPartyBalanceRepository,
    IRepository<Branch, Guid> branchRepository,
    IRepository<PaymentDetail, Guid> paymentDetailRepository,
    IRepository<PartyBalance, Guid> partyBalanceRepository,
    IRepository<VoucherType, Guid> voucherTypeRepository,
    IRepository<LedgerPosting, Guid> ledgerPostingRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    PartyBalanceService partyBalanceService,
    IDocumentsAppService documentsAppService,
    IAppNotifier appNotifier,
    UserManager userManager,
    IRepository<UserBranch, Guid> userBranchRepository)
    : ErpAppServiceBase, IPaymentMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetPaymentMasterForViewDto>> GetAll(GetAllPaymentMastersInput input)
        {
            await FixedErrorPayment();
            // var branch = await ErpCommonManager.GetAllUserBranch(AbpSession.GetUserId());
            var filteredPaymentMasters = paymentMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.VoucherNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.TotalAmount,
                    x.LedgerId,
                    x.Date,
                    x.DateMiti,
                    x.VoucherTypeId,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPaymentMasters = filteredPaymentMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPaymentMasters = filteredPaymentMasters.Where(x => x.Date <= date);
            }


            var pagedAndFilteredPaymentMasters = filteredPaymentMasters
                .OrderByDescending(e => e.Date.Date).ThenByDescending(e => e.VoucherNumbering).PageBy(input);

            var paymentMasters = from o in pagedAndFilteredPaymentMasters
                                 join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.CreateUserId equals o6.Id into j6
                                 from s6 in j6.DefaultIfEmpty()
                                 join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                     o.UpdateUserId equals o5.Id into j5
                                 from s5 in j5.DefaultIfEmpty()
                                 select new GetPaymentMasterForViewDto
                                 {
                                     VoucherNo = o.VoucherNo,
                                     LedgerName = o.LedgerName,
                                     Date = o.Date,
                                     TotalAmount = o.TotalAmount,
                                     DateMiti = o.DateMiti,
                                     VoucherTypeId = o.VoucherTypeId,
                                     Id = o.Id,
                                     CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                     UpdateUser = s5 == null || s5.Name == null ? "" : s5.Name,
                                 };

            var totalCount = await filteredPaymentMasters.CountAsync();
            return new PagedResultDto<GetPaymentMasterForViewDto>(totalCount, await paymentMasters.ToListAsync());
        }


        [AbpAuthorize(AppPermissions.PagesPaymentMastersEdit)]
        public async Task<CreateOrEditPaymentMasterDto> GetPaymentMasterForEdit(Guid id)
        {
            using var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false });
            try
            {
                var paymentMaster = await paymentMasterRepository.GetAsync(id);

                var result = new CreateOrEditPaymentMasterDto
                {
                    Id = paymentMaster.Id,
                    VoucherNo = paymentMaster.VoucherNo,
                    DateMiti = paymentMaster.DateMiti,
                    TotalAmount = paymentMaster.TotalAmount,
                    Description = paymentMaster.Description,
                    LedgerId = paymentMaster.LedgerId,
                };
                var paymentDetails = await paymentDetailRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.PaymentMasterId == id && x.TenantId == AbpSession.TenantId)
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
                foreach (var detail in paymentDetails)
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
                        Adjust = x.Debit,
                        IsSettled = x.IsFullySettled
                    }).ToList();
                    foreach (var deta in details)
                    {
                        var relatedData = await newPartyBalanceRepository.GetAll().Where(x => x.VoucherNo == deta.VoucherNo && x.VoucherTypeId == deta.VoucherTypeId && x.VoucherNumbering == deta.VoucherNumbering && x.FinancialYearId == FinancialYearId).ToListAsync();
                        deta.BillAmount = relatedData.Where(x => x.Id != deta.PartyBalanceId && x.DetailId == Guid.Empty).Sum(x => x.Credit - x.Debit);
                        deta.PaidAmount = relatedData.Where(x => x.Id != deta.PartyBalanceId && x.DetailId != Guid.Empty).Sum(x => x.Debit - x.Credit);
                        deta.BalanceAmount = deta.BillAmount - deta.PaidAmount;
                    }
                    details.AddRange((await partyBalanceService.GetPaymentAgainstAmount(detail.LedgerId, detail.Amount, detail.Id)).Details);
                    det.Details = details;
                    det.NewReferenceAmount = databaseData.Where(x => x.VoucherTypeId == paymentMaster.VoucherTypeId && x.VoucherNo == paymentMaster.VoucherNo && x.VoucherNumbering == paymentMaster.VoucherNumbering).Sum(x => x.Debit);
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

                result.PaymentDetails = detailsDto;
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

        public async Task<Guid> CreateOrEdit(CreateOrEditPaymentMasterDto input)
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

        [AbpAuthorize(AppPermissions.PagesPaymentMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var masterData =
                await paymentMasterRepository.FirstOrDefaultAsync(
                    x => x.TenantId == AbpSession.TenantId && x.Id == input.Id);
            var ledgerIds = (await ledgerPostingRepository.GetAll().Where(x =>
                x.VoucherNo == masterData.VoucherNo && x.VoucherNumbering == masterData.VoucherNumbering &&
                x.FinancialYearId == masterData.FinancialYearId &&
                x.VoucherTypeId == masterData.VoucherTypeId).ToListAsync()).Select(x => x.Id).ToList();

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherNo == masterData.VoucherNo && x.VoucherNumbering == masterData.VoucherNumbering &&
                x.FinancialYearId == masterData.FinancialYearId && x.VoucherTypeId == masterData.VoucherTypeId);

            await partyBalanceService.DeletePaymentTaskAsync(input.Id);
            //await partyBalanceRepository.DeleteAsync(x =>
            //    x.MasterVoucherNo == masterData.VoucherNo && x.BranchId == masterData.BranchId &&
            //    x.VoucherNumbering == masterData.VoucherNumbering && x.FinancialYearId == masterData.FinancialYearId &&
            //    x.MasterVoucherTypeId == masterData.VoucherTypeId);
            await paymentDetailRepository.DeleteAsync(x => x.PaymentMasterId == input.Id);
            await paymentMasterRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPaymentMastersToExcel(GetAllPaymentMastersInput input)
        {
            var filteredPaymentMasters = paymentMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.VoucherTypeFk).Include(x => x.AccountLedgerFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => false || e.Description.Contains(input.Filter) || e.DateMiti.Contains(input.Filter));

            var query = from o in filteredPaymentMasters
                        select new GetPaymentMasterForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Date = o.Date,
                            TotalAmount = o.TotalAmount,
                            VoucherType = o.VoucherTypeFk.Name,
                            LedgerName = o.AccountLedgerFk.Name,
                            DateMiti = o.DateMiti,
                            VoucherTypeId = o.VoucherTypeId,
                            Id = o.Id,
                        };

            var paymentMasterListDtos = await query.ToListAsync();

            return paymentMastersExcelExporter.ExportToFile(paymentMasterListDtos);
        }

        public async Task<List<PartyBalanceAdjustDto>> GetPartyBalancesOptimized(Guid ledgerId, Guid masterId,
            string masterVoucherNo, Guid masterVoucherTypeId)
        {
            // 1. Query only data we need directly from the database
            var relevantBalances = await partyBalanceRepository.GetAll()
                .Where(e => e.FinancialYearId == FinancialYearId &&
                            e.LedgerId == ledgerId &&
                            (e.Credit > 0 || e.Debit > 0))
                .Select(e => new
                {
                    e.Id,
                    e.VoucherTypeId,
                    e.VoucherNo,
                    e.ReferenceType,
                    e.LedgerId,
                    e.BranchId,
                    e.MasterId,
                    e.MasterVoucherNo,
                    e.MasterVoucherTypeId,
                    e.Date,
                    e.CreditPeriod,
                    e.Credit,
                    e.Debit
                })
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            // 2. Get voucher types in a single query
            var voucherTypes = await voucherTypeRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId)
                .Select(e => new { e.Id, e.Name })
                .AsNoTracking()
                .ToDictionaryAsync(e => e.Id, e => e.Name);

            // 3. Find settled credits (matching the receipt master)
            var settledCredits = relevantBalances
                .Where(x => x.LedgerId == ledgerId &&
                            x.MasterVoucherNo == masterVoucherNo &&
                            x.MasterVoucherTypeId == masterVoucherTypeId &&
                            x.MasterId == masterId)
                .ToList();

            var settledCreditIds = new HashSet<Guid>(settledCredits.Select(x => x.Id));

            // 4. Create lookup for payment entries (with precalculated sums)
            var creditPaymentsLookup = relevantBalances
                .Where(x => x.ReferenceType == ReferenceType.Against.ToString() &&
                            !settledCreditIds.Contains(x.Id))
                .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo })
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Debit - x.Credit)
                );

            // 5. Create lookup for adjustment entries (with precalculated sums)
            var creditAdjustmentsLookup = settledCredits
                .Where(x => x.ReferenceType == ReferenceType.Against.ToString())
                .GroupBy(x => new { x.VoucherTypeId, x.VoucherNo })
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Debit - x.Credit)
                );

            // 6. Build the result in a single pass without repeated LINQ evaluations
            var baseBalances = relevantBalances
                .Where(e => e.ReferenceType is "New" or "OnAccount")
                .Where(e => e.VoucherTypeId != null)
                .Where(e => !settledCreditIds.Contains(e.Id))
                .OrderBy(e => e.Date)
                .ToList();

            // 7. Process each balance in a fast loop 
            var results = new List<PartyBalanceAdjustDto>();
            foreach (var detail in baseBalances)
            {
                var totalCredit = detail.Credit - detail.Debit;

                // Use dictionary lookups with null safety
                var key = new { detail.VoucherTypeId, detail.VoucherNo };
                var creditPayments = creditPaymentsLookup.ContainsKey(key) ? creditPaymentsLookup[key] : 0;
                var creditAdjustments = creditAdjustmentsLookup.ContainsKey(key) ? creditAdjustmentsLookup[key] : 0;

                var netCredit = totalCredit - creditPayments;

                // Skip zero balances early to avoid unnecessary object creation
                if (Math.Abs(netCredit) <= 0.001M) continue;

                // Get voucher name with null safety
                string voucherName = null;
                if (detail.VoucherTypeId != null && voucherTypes.ContainsKey(detail.VoucherTypeId.Value))
                    voucherName = voucherTypes[detail.VoucherTypeId.Value];

                var absCredit = Math.Abs(netCredit);

                results.Add(new PartyBalanceAdjustDto
                {
                    Id = detail.Id,
                    VoucherNo = detail.VoucherNo ?? string.Empty,
                    VoucherTypeId = (Guid)detail.VoucherTypeId,
                    VoucherTypeName = voucherName,
                    Type = detail.ReferenceType,
                    PayDate = DateConverter.ConvertToNepali(detail.Date),
                    DueDate = DateConverter.ConvertToNepali(detail.Date.AddDays(detail.CreditPeriod)),
                    BalanceString = $"{absCredit:F2} {(netCredit > 0 ? "Cr" : "Dr")}",
                    Balance = netCredit,
                    BillAmt = Math.Abs(totalCredit),
                    Paid = creditPayments,
                    Adjust = creditAdjustments,
                    IsDisable = netCredit < 0 || creditAdjustments < 0
                });
            }

            return results;
        }

        public async Task<PaymentMasterForViewNewDto> GetPaymentMasterForViewNew(Guid id)
        {
            var result = new PaymentMasterForViewNewDto();
            var paymentMaster = await paymentMasterRepository.GetAll()
                .Where(x => x.Id == id).Include(x => x.AccountLedgerFk).FirstOrDefaultAsync();
            if (paymentMaster != null)
            {
                result.Id = paymentMaster.Id;
                result.VoucherNo = paymentMaster.VoucherNo;
                result.DateMiti = paymentMaster.DateMiti;
                result.TotalAmount = paymentMaster.TotalAmount;
                result.Description = paymentMaster.Description;
                result.LedgerName = paymentMaster.AccountLedgerFk.Name;
            }

            var paymentDetails = await paymentDetailRepository.GetAll()
                .Where(x => x.PaymentMasterId == id).Include(x => x.AccountLedgerFk).Select(x =>
                    new PaymentDetailForViewDetailDto
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        ChequeMiti = x.ChequeMiti,
                        ChequeNo = x.ChequeNo,
                        LedgerId = x.LedgerId,
                        LedgerName = x.AccountLedgerFk.Name,
                        IsBillByBill = x.AccountLedgerFk.IsBillByBill
                    }).AsNoTracking().ToListAsync();
            foreach (var bill in paymentDetails.Where(x => x.IsBillByBill))
                bill.PartyBalances = await partyBalanceRepository.GetAll()
                    .Where(x => x.LedgerId == bill.LedgerId && x.MasterVoucherTypeId == paymentMaster.VoucherTypeId &&
                                x.VoucherNumbering == paymentMaster.VoucherNumbering && x.MasterId == paymentMaster.Id &&
                                x.MasterVoucherNo == paymentMaster.VoucherNo &&
                                x.FinancialYearId == paymentMaster.FinancialYearId).Include(x => x.VoucherTypeFk).Select(
                        x => new PartyaBalanceForPaymentMasterDto
                        {
                            VoucherNo = /*x.IsAgainst ? x.AgainstVoucherNo :*/ x.VoucherNo,
                            VoucherTypeId = /*x.IsAgainst ? x.AgainstVoucherTypeId :*/ x.VoucherTypeId,
                            VoucherTypeName = x.VoucherTypeFk.Name,
                            ReferenceType = x.ReferenceType,
                            Amount = x.Debit
                        }).AsNoTracking().ToListAsync();
            result.Details = paymentDetails;
            return result;
        }


        public async Task<GetPdfForPaymentMaster> GetPaymentMasterForPdf(EntityDto<Guid> input)
        {
            var serial = 1;
            var paymentMaster = await paymentMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.Id == input.Id);
            var mainBranch = await branchRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId && x.IsMain);
            var paymentDetails = await paymentDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PaymentMasterId == paymentMaster.Id)
                .Include(x => x.AccountLedgerFk)
                .ToListAsync();
            var output = new GetPdfForPaymentMaster
            {
                Logo1 = mainBranch.Image1,
                BranchAddress = mainBranch.Address,
                BranchContact = mainBranch.PhoneNo1,
                VoucherNo = paymentMaster.VoucherNo,
                Date = paymentMaster.Date,
                TotalAmountInWord = CurrencyToAmount.AmountWords(paymentMaster.TotalAmount),
                TotalAmount = paymentMaster.TotalAmount,
                Narration = paymentMaster.Description,
                DateMiti = paymentMaster.DateMiti,
                LedgerId = paymentMaster.LedgerId,
                LedgerName = paymentMaster.AccountLedgerFk.Name,

                PaymentDetails = paymentDetails.Select(x => new GetPdfForPaymentDetail
                {
                    SlNo = serial++,
                    Amount = x.Amount,
                    ChequeNo = x.ChequeNo,
                    ChequeDate = x.ChequeMiti,
                    LedgerName = x.AccountLedgerFk.Name
                }).ToList()
            };

            return output;
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

        //[AbpAuthorize(AppPermissions.PagesPaymentMasters)]
        //public async Task<byte[]> GetPdfDownload(EntityDto<Guid> input)
        //{
        //    var model = await GetPaymentMasterForPdf(input);
        //    var document = new PaymentMasterPdf(model);
        //    Stream stream = new MemoryStream(document.GeneratePdf());
        //    var mailMessage = new StringBuilder();

        //    mailMessage.AppendLine("<b>" + L("Message") + "</b>: " +
        //                           L("",
        //                               " UTC") + "<br />");
        //    mailMessage.AppendLine("<br />");
        //    if (ValidationHelper.IsEmail(model.CustomerEmail))
        //    {
        //        var mail = new MailMessage
        //        {
        //            Subject = "Subject",
        //            Body = mailMessage.ToString(),
        //            IsBodyHtml = true,
        //            To = { model.CustomerEmail },
        //            From = new MailAddress("suktas@gmail.com")
        //        };
        //        mail.Attachments.Add(new Attachment(stream, "Invoice.Pdf"));
        //        await emailSender.SendAsync(mail);
        //    }

        //    //var file = new FileDto("PaymentMaster.pdf", MimeTypeNames.ApplicationPdf);
        //    return document.GeneratePdf();
        //}

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            return await paymentMasterRepository.CountAsync(x =>
                x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0;
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
                               }).Where(e => e.Balance != 0)
                .OrderBy(e => e.PayDate).ToList();

            return returnDebit;
        }


        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PaymentVoucher");
            var paymentMaster = await paymentMasterRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId &&
                x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = paymentMaster.VoucherTypeId,
                VoucherNo = paymentMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public void GetDateUpdate()
        {
            var data = paymentMasterRepository.GetAllList();
            foreach (var item in data)
            {
                item.DateMiti = DateConverter.ConvertToNepali(Convert.ToDateTime(item.Date));
                paymentMasterRepository.UpdateAsync(item);
            }
        }

        [AbpAuthorize(AppPermissions.PagesPaymentMasters)]
        public async Task<string> GetPaymentMasterVoucherNo()
        {
            var data = await paymentMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PaymentVoucher");
            if (data.Count == 0)
                return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;
            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }


        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await paymentMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PaymentVoucher");
            if (data.Count == 0) return voucherNumbering.StartIndex;

            return data.Max() + 1;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPaymentMasters)]
        public async Task<List<PaymentVoucherAccountLedgerDropDown>> GetAllAccountLedgerForTableDropdown()
        {
            var list = new List<PaymentVoucherAccountLedgerDropDown>();

            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(accountLedger => new PaymentVoucherAccountLedgerDropDown
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name.ToString(),
                    IsBillByBill = accountLedger.IsBillByBill,
                    AccountGroup = accountLedger.AccountGroupFk.Name
                }).ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPaymentMasters)]
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

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PaymentVoucher");
        }

        [AbpAuthorize(AppPermissions.PagesPaymentMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPaymentMasterDto input)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PaymentVoucher");
            var tenantId = AbpSession.TenantId;
            decimal? postingNumbering = PostingNumbering;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetPaymentMasterVoucherNo();
                if (await paymentMasterRepository.CountAsync(x =>
                        x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                {
                    var paymentnumbering = await paymentMasterRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.FinancialYearId == FinancialYearId).Select(x => x.VoucherNumbering).MaxAsync();
                    var objPaymentMaster = await paymentMasterRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.FinancialYearId == FinancialYearId && x.VoucherNumbering == paymentnumbering)
                        .LastOrDefaultAsync();
                    objPaymentMaster.VoucherNumbering = paymentnumbering + 1;
                    await paymentMasterRepository.UpdateAsync(objPaymentMaster);
                    throw new UserFriendlyException("Voucher number already exist");
                }
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await paymentMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId &&
                                                                  x.VoucherNo == input.VoucherNo) > 0)
                    throw new UserFriendlyException("PaymentVoucher VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var paymentMaster = new PaymentMaster
            {
                VoucherNumbering = voucherNumbering,
                TenantId = tenantId,
                FinancialYearId = FinancialYearId,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                TotalAmount = input.TotalAmount,
                Description = input.Description,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };

            var paymentId = await paymentMasterRepository.InsertAndGetIdAsync(paymentMaster);

            foreach (var detail in input.PaymentDetails)
            {
                var paymentDetail = new PaymentDetail
                {
                    TenantId = tenantId,
                    Amount = detail.Amount,
                    ChequeNo = detail.ChequeNo,
                    ChequeMiti = detail.ChequeMiti,
                    ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                        ? null
                        : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                    LedgerId = detail.LedgerId,
                    PaymentMasterId = paymentId
                };
                var detailId = await paymentDetailRepository.InsertAndGetIdAsync(paymentDetail);

                var postingLedger = new LedgerPosting
                {
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    Debit = detail.Amount,
                    Credit = 0,
                    InvoiceNo = voucherNo,
                    DateMiti = input.DateMiti,
                    FinancialYearId = FinancialYearId,
                    DetailId = input.LedgerId,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = detail.LedgerId,
                    MasterId = paymentId,
                    PostingNumber = postingNumbering
                };
                await ledgerPostingRepository.InsertAsync(postingLedger);

                if (!(await accountLedgerRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId)).IsBillByBill) continue;
                if (detail.PartyBalanceDetail == null) continue;
                await partyBalanceService.PostPayments(new CreateReceiptAgainstMasterDto
                {
                    MasterId = paymentId,
                    MasterVoucherNo = input.VoucherNo,
                    MasterVoucherTypeId = voucherTypeId,
                    MasterVoucherNumbering = voucherNumbering,
                    LedgerId = detail.LedgerId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    Data = detail.PartyBalanceDetail,
                    DetailId = detailId
                });

            }

            var ledgerPosting = new LedgerPosting
            {
                TenantId = tenantId,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Debit = 0,
                Credit = input.TotalAmount,
                InvoiceNo = voucherNo,
                DetailId = input.PaymentDetails.Select(x => x.LedgerId).FirstOrDefault(),
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                MasterId = paymentId,
                PostingNumber = postingNumbering
            };
            await ledgerPostingRepository.InsertAsync(ledgerPosting);
            return paymentId;
        }


        [DisableAuditing]
        public async Task<List<PaymentMasterSupplierListDto>> GetAllSuppliersForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors" || x.Name == "Cash" ||
                            x.AccountGroupFk.Name == "Sundry Debtors")
                .Select(accountLedger => new PaymentMasterSupplierListDto
                {
                    LedgerId = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    IsBillByBill = accountLedger.IsBillByBill
                }).AsNoTracking().ToListAsync();
        }


        protected virtual async Task<Guid> CreateOrUpdateDetails(Guid paymentId, CreateOrEditPaymentDetailDto input)
        {
            var tenantId = AbpSession.TenantId;

            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            if (input.Id == null || input.Id == Guid.Empty)
            {
                var paymentDetail = new PaymentDetail
                {
                    TenantId = tenantId,
                    Amount = input.Amount,
                    ChequeNo = input.ChequeNo,
                    ChequeMiti = input.ChequeMiti,
                    ChequeDate = string.IsNullOrEmpty(input.ChequeMiti)
                        ? null
                        : DateConverter.ConvertToEnglish(input.ChequeMiti),
                    LedgerId = input.LedgerId,
                    PaymentMasterId = paymentId
                };
                var paymentDetailId = await paymentDetailRepository.InsertAndGetIdAsync(paymentDetail);
                return paymentDetailId;
            }
            else
            {
                var paymentDetail =
                    await paymentDetailRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId &&
                                                                           x.Id == (Guid)input.Id &&
                                                                           x.PaymentMasterId == paymentId);
                if (paymentDetail == null) return (Guid)input.Id;
                paymentDetail.TenantId = tenantId;
                paymentDetail.Amount = input.Amount;
                paymentDetail.ChequeNo = input.ChequeNo;
                paymentDetail.ChequeMiti = input.ChequeMiti;
                paymentDetail.ChequeDate = string.IsNullOrEmpty(input.ChequeMiti)
                    ? null
                    : DateConverter.ConvertToEnglish(input.ChequeMiti);
                //        paymentDetail.Forex = input.Forex;
                paymentDetail.LedgerId = input.LedgerId;
                await paymentDetailRepository.UpdateAsync(paymentDetail);
                return (Guid)input.Id;
            }
        }


        public async Task<List<PartyBalanceOnPaymentMasterAddDto>> GetPartyBalanceByVoucherNo(string voucherNo,
            Guid ledgerId, Guid branchId)
        {
            var partyBalance = (await partyBalanceRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(
                x =>
                    x.AgainstInvoiceNo == voucherNo && x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId &&
                    x.BranchId == branchId).ToListAsync()).Select(x => new PartyBalanceOnPaymentMasterAddDto
                    {
                        VoucherNo = x.VoucherNo,
                        VoucherTypeId = x.VoucherTypeId,
                        ReferenceType = (ReferenceType)Enum.Parse(typeof(ReferenceType), x.ReferenceType, true),
                        Amount = x.Debit
                    }).ToList();
            return partyBalance;
        }

        [AbpAuthorize(AppPermissions.PagesPaymentMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPaymentMasterDto input)
        {
            var tenantId = AbpSession.TenantId;

            var paymentMaster = await paymentMasterRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (paymentMaster == null) throw new UserFriendlyException("Data not found");

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await paymentMasterRepository.CountAsync(x =>
                        x.Id != input.Id && x.FinancialYearId == FinancialYearId &&
                        x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PaymentMaster VoucherNo is Duplicate");
                paymentMaster.VoucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
                paymentMaster.VoucherNo = input.VoucherNo;
            var postingNumbering = paymentMaster.PostingNumbering;
            paymentMaster.LedgerId = input.LedgerId;
            paymentMaster.DateMiti = input.DateMiti;
            paymentMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
            paymentMaster.TotalAmount = input.TotalAmount;
            paymentMaster.Description = input.Description;
            paymentMaster.DateMiti = input.DateMiti;
            paymentMaster.UpdateUserId = AbpSession.UserId;
            await paymentMasterRepository.UpdateAsync(paymentMaster);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherNumbering == paymentMaster.VoucherNumbering &&
                x.FinancialYearId == FinancialYearId && x.VoucherNo == paymentMaster.VoucherNo &&
                x.VoucherTypeId == paymentMaster.VoucherTypeId);

            await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == paymentMaster.VoucherNo &&
                                                          x.MasterVoucherTypeId == paymentMaster.VoucherTypeId &&
                                                          x.FinancialYearId == FinancialYearId &&
                                                          x.MasterId == paymentMaster.Id);

            foreach (var detail in input.PaymentDetails)
            {
                Guid detailId;

                if (detail.Id == null || detail.Id == Guid.Empty)
                {
                    var paymentDetail = new PaymentDetail
                    {
                        TenantId = tenantId,
                        Amount = detail.Amount,
                        ChequeNo = detail.ChequeNo,
                        ChequeMiti = detail.ChequeMiti,
                        ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                            ? null
                            : DateConverter.ConvertToEnglish(detail.ChequeMiti),
                        LedgerId = detail.LedgerId,
                        PaymentMasterId = paymentMaster.Id
                    };
                    detailId = await paymentDetailRepository.InsertAndGetIdAsync(paymentDetail);
                }
                else
                {
                    var paymentDetail =
                        await paymentDetailRepository.FirstOrDefaultAsync(x => x.TenantId == AbpSession.TenantId &&
                                                                               x.Id == (Guid)detail.Id &&
                                                                               x.PaymentMasterId == paymentMaster.Id);
                    if (paymentDetail == null) return (Guid)input.Id;
                    paymentDetail.TenantId = tenantId;
                    paymentDetail.Amount = detail.Amount;
                    paymentDetail.ChequeNo = detail.ChequeNo;
                    paymentDetail.ChequeMiti = detail.ChequeMiti;
                    paymentDetail.ChequeDate = string.IsNullOrEmpty(detail.ChequeMiti)
                        ? null
                        : DateConverter.ConvertToEnglish(detail.ChequeMiti);
                    //        paymentDetail.Forex = input.Forex;
                    paymentDetail.LedgerId = detail.LedgerId;
                    await paymentDetailRepository.UpdateAsync(paymentDetail);
                    detailId = paymentDetail.Id;
                }
                var objLedgerDetail = new LedgerPosting
                {
                    VoucherNumbering = paymentMaster.VoucherNumbering,
                    TenantId = tenantId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = paymentMaster.VoucherNo,
                    Debit = detail.Amount,
                    Credit = 0,
                    InvoiceNo = input.VoucherNo,
                    DateMiti = input.DateMiti,
                    FinancialYearId = FinancialYearId,
                    DetailId = input.LedgerId,
                    VoucherTypeId = paymentMaster.VoucherTypeId,
                    LedgerId = detail.LedgerId,
                    MasterId = paymentMaster.Id,
                    PostingNumber = postingNumbering
                };
                await ledgerPostingRepository.InsertAsync(objLedgerDetail);
                //// party balance posting
                if (!(await accountLedgerRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == AbpSession.TenantId && x.Id == detail.LedgerId)).IsBillByBill) continue;
                if (detail.PartyBalanceDetail == null) continue;
                await partyBalanceService.PostPayments(new CreateReceiptAgainstMasterDto
                {
                    MasterId = paymentMaster.Id,
                    MasterVoucherNo = input.VoucherNo,
                    MasterVoucherTypeId = paymentMaster.VoucherTypeId,
                    MasterVoucherNumbering = paymentMaster.VoucherNumbering,
                    LedgerId = detail.LedgerId,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    Data = detail.PartyBalanceDetail,
                    DetailId = detailId
                });
            }

            var ledgerPosting = new LedgerPosting
            {
                VoucherNumbering = paymentMaster.VoucherNumbering,
                TenantId = tenantId,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                VoucherNo = input.VoucherNo,
                Debit = 0,
                Credit = input.PaymentDetails.Select(x => x.Amount).Sum(),
                InvoiceNo = input.VoucherNo,
                DetailId = input.PaymentDetails.Select(x => x.LedgerId).FirstOrDefault(),
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = paymentMaster.VoucherTypeId,
                LedgerId = input.LedgerId,
                MasterId = paymentMaster.Id,
                PostingNumber = postingNumbering
            };
            await ledgerPostingRepository.InsertAsync(ledgerPosting);


            return paymentMaster.Id;
        }

        public async Task<List<RemainingCreditDto>> GetAllRemainingCredits(Guid ledgerId, DrOrCr drOrCr)
        {
            var partyBalances = await partyBalanceRepository.GetAll().Include(x => x.VoucherTypeFk)
                .Select(x => new
                {
                    x.LedgerId,
                    x.FinancialYearId,
                    x.ReferenceType,
                    x.VoucherNo,
                    x.VoucherTypeFk,
                    x.Debit,
                    x.Credit,
                    x.Date,
                    x.CreditPeriod
                }).ToListAsync();
            var data = partyBalances.Where(x =>
                    x.LedgerId == ledgerId && x.FinancialYearId == FinancialYearId && x.ReferenceType == "New")
                .Select(x => new RemainingCreditDto
                {
                    Date = DateConverter.ConvertToNepali(x.Date),
                    DueDate = DateConverter.ConvertToNepali(x.Date.AddDays(x.CreditPeriod)),
                    VoucherType = x.VoucherTypeFk.Name,
                    VoucherNo = x.VoucherNo,
                    TotalAmount = x.Credit > 0 ? x.Credit : x.Debit,
                    PaidAmount = 0,
                    Balance = x.Credit > 0 ? x.Credit : x.Debit,
                    IsDisable = drOrCr == DrOrCr.Cr && x.Credit > 0 ? false :
                        drOrCr == DrOrCr.Dr && x.Debit > 0 ? false : true
                }).ToList();
            foreach (var party in data)
            {
                party.PaidAmount = partyBalances
                    .Where(x => x.VoucherNo == party.VoucherNo && x.VoucherTypeFk.Name == party.VoucherType &&
                                x.FinancialYearId == FinancialYearId && x.ReferenceType == "Against")
                    .Sum(x => x.Credit > 0 ? x.Credit : x.Debit);
                party.Balance = party.TotalAmount - party.PaidAmount;
            }

            return data;
        }


        #region ErrorFixed

        private async Task FixedErrorPayment()
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PaymentVoucher");
            var tenantId = AbpSession.GetTenantId();

            // Get payments that need fixing in a single query with all necessary data
            var paymentsToFix = await paymentMasterRepository.GetAll()
                .Include(pm => pm.AccountLedgerFk)
                .Where(pm => pm.TenantId == tenantId &&
                             pm.FinancialYearId == FinancialYearId &&
                             pm.VoucherTypeId == voucherTypeId)
                .Where(pm => !ledgerPostingRepository.GetAll()
                    .Any(lp => lp.VoucherTypeId == pm.VoucherTypeId &&
                               lp.FinancialYearId == pm.FinancialYearId &&
                               lp.VoucherNo == pm.VoucherNo &&
                               lp.TenantId == pm.TenantId))
                .Take(300)
                .ToListAsync();

            if (!paymentsToFix.Any()) return; // No payments to fix

            // Batch process payment details
            var paymentIds = paymentsToFix.Select(p => p.Id).ToList();
            var allPaymentDetails = await paymentDetailRepository.GetAll()
                .Include(pd => pd.AccountLedgerFk)
                .Where(pd => paymentIds.Contains(pd.PaymentMasterId) && pd.TenantId == tenantId)
                .ToListAsync();

            // Group payment details by master ID for efficient access
            var detailsByMasterId = allPaymentDetails.GroupBy(pd => pd.PaymentMasterId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Process each payment that needs fixing
            var updatedCount = 0;
            foreach (var payment in paymentsToFix)
                try
                {
                    var details = new List<PaymentDetail>();
                    detailsByMasterId.TryGetValue(payment.Id, out var paymentDetails);
                    if (paymentDetails != null) details = paymentDetails;

                    // Reconstruct ledger postings
                    await ReconstructLedgerPostings(payment, details);

                    // Reconstruct party balances if needed
                    //await ReconstructPartyBalances(payment, details);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error fixing payment {payment.Id}: {ex.Message}", ex);
                    // Continue with next payment even if this one fails
                }

            Logger.Info($"Fixed {updatedCount} payment vouchers without ledger postings");
        }

        private async Task ReconstructLedgerPostings(PaymentMaster payment, List<PaymentDetail> details)
        {
            var tenantId = AbpSession.GetTenantId();

            // Create detail ledger postings
            foreach (var detail in details)
            {
                var ledgerPosting = new LedgerPosting
                {
                    TenantId = tenantId,
                    Date = payment.Date,
                    VoucherNumbering = payment.VoucherNumbering,
                    VoucherNo = payment.VoucherNo,
                    Debit = detail.Amount,
                    Credit = 0,
                    InvoiceNo = payment.VoucherNo,
                    DateMiti = payment.DateMiti,
                    FinancialYearId = payment.FinancialYearId,
                    DetailId = payment.LedgerId,
                    VoucherTypeId = payment.VoucherTypeId,
                    LedgerId = detail.LedgerId,
                    MasterId = payment.Id,
                    PostingNumber = payment.PostingNumbering
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting);
            }

            // Create the main credit ledger posting
            var mainLedgerPosting = new LedgerPosting
            {
                TenantId = tenantId,
                Date = payment.Date,
                VoucherNumbering = payment.VoucherNumbering,
                VoucherNo = payment.VoucherNo,
                Debit = 0,
                Credit = payment.TotalAmount,
                InvoiceNo = payment.VoucherNo,
                DateMiti = payment.DateMiti,
                FinancialYearId = payment.FinancialYearId,
                DetailId = details.FirstOrDefault()?.LedgerId ?? Guid.Empty,
                VoucherTypeId = payment.VoucherTypeId,
                LedgerId = payment.LedgerId,
                MasterId = payment.Id,
                PostingNumber = payment.PostingNumbering
            };
            await ledgerPostingRepository.InsertAsync(mainLedgerPosting);
        }

        private async Task ReconstructPartyBalances(PaymentMaster payment, List<PaymentDetail> details)
        {
            var tenantId = AbpSession.GetTenantId();

            // Check if party balance reconstruction is needed
            var ledgersWithBillByBill = await accountLedgerRepository.GetAll()
                .Where(al => details.Select(d => d.LedgerId).Contains(al.Id) && al.IsBillByBill)
                .Select(al => al.Id)
                .ToListAsync();

            if (!ledgersWithBillByBill.Any()) return; // No bill-by-bill ledgers to process

            // Get party balances that might be relevant for reconstructing
            var existingPartyBalances = await partyBalanceRepository.GetAll()
                .Where(pb => pb.MasterId == payment.Id ||
                             (pb.MasterVoucherNo == payment.VoucherNo &&
                              pb.MasterVoucherTypeId == payment.VoucherTypeId &&
                              pb.FinancialYearId == payment.FinancialYearId))
                .ToListAsync();

            if (existingPartyBalances.Any()) return; // Party balances already exist, no need to recreate

            // Process each detail with bill-by-bill ledgers
            foreach (var detail in details.Where(d => ledgersWithBillByBill.Contains(d.LedgerId)))
            {
                // Create an on-account party balance (since we can't determine the proper adjustments without user input)
                var partyBalance = new PartyBalance
                {
                    Date = payment.Date,
                    LedgerId = detail.LedgerId,
                    FinancialYearId = payment.FinancialYearId,
                    VoucherTypeId = payment.VoucherTypeId,
                    VoucherNumbering = payment.VoucherNumbering,
                    VoucherNo = payment.VoucherNo,
                    AgainstVoucherTypeId = null,
                    AgainstVoucherNo = payment.VoucherNo,
                    InvoiceNo = payment.VoucherNo,
                    IsAgainst = false,
                    AgainstInvoiceNo = payment.VoucherNo,
                    ReferenceType = ReferenceType.OnAccount.ToString(),
                    Debit = detail.Amount,
                    Credit = 0,
                    CreditPeriod = 0,
                    MasterVoucherTypeId = payment.VoucherTypeId,
                    MasterId = payment.Id,
                    DetailId = detail.Id,
                    MasterVoucherNo = payment.VoucherNo,
                    TenantId = tenantId
                };
                await partyBalanceRepository.InsertAsync(partyBalance);
            }
        }

        #endregion

        public async Task<List<UniversalDropdownDto>> GetAllDuePaymentVouchers(Guid ledgerId, PaymentOptions paymentOptions)
        {
            return (await partyBalanceService.GetAllRemainingPayments(ledgerId, paymentOptions));
        }

        public async Task<decimal> GetAllRemainingPaymentBalance(Guid ledgerId, PaymentOptions paymentTypes,
            string voucherNo)
        {
            return await partyBalanceService.GetAllRemainingPaymentBalance(ledgerId, paymentTypes, voucherNo);
        }

        public async Task<GetReceiptAgainstMasterDto> GetPaymentAgainst(Guid ledgerId, decimal amount, Guid detailId)
        {
            return await partyBalanceService.GetPaymentAgainstAmount(ledgerId, amount, detailId);
        }
    }
}