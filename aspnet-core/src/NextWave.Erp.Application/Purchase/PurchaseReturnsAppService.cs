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
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Purchase.Exporting;
using NextWave.Erp.Purchase.Pdf;
using NextWave.Erp.SharedDtos;
using NextWave.Erp.Transaction;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace NextWave.Erp.Purchase
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
    public class PurchaseReturnsAppService(
        IRepository<PurchaseReturn, Guid> purchaseReturnRepository,
        IRepository<User, long> userRepository,
        IPurchaseReturnsExcelExporter purchaseReturnsExcelExporter,
        //  IRepository<UnitConversion, Guid> unitConversionRepository,
        //IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<Tax, Guid> taxRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<VoucherPhotos, Guid> voucherPhotosRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository,
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
        IUnitOfWorkManager unitOfWorkManager,
        PartyBalanceService partyBalanceService,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IDocumentsAppService documentsAppService,
        // IAppNotifier appNotifier,
        //UserManager userManager,
        StockManagementAppService stockManagementAppService,
        MaterialStockPostingService materialStockPostingService,
        IRepository<StockMaintain, Guid> stockMaintainRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository)
        //IRepository<UserBranch, Guid> userBranchRepository)
        : ErpAppServiceBase, IPurchaseReturnsAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetPurchaseReturnForViewDto>> GetAll(GetAllUniversalMastersInput input)
        {
            if (!string.IsNullOrEmpty(input.Filter))
                input.Filter = input.Filter.Trim();
            var filteredPurchaseReturns = purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrEmpty(input.Filter),
                    x => x.AccountLedgerFk.Name.Contains(input.Filter) || x.VoucherNo.Contains(input.Filter))
                .Select(x => new
                {
                    x.Id,
                    x.Date,
                    x.DateMiti,
                    x.VoucherNo,
                    x.TotalDiscount,
                    x.Description,
                    x.PurchaseAccount,
                    x.TotalTax,
                    x.TotalAmount,
                    x.GrandTotal,
                    x.LrNo,
                    x.TransportationCompany,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseReturns = filteredPurchaseReturns.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseReturns = filteredPurchaseReturns.Where(x => x.Date <= date);
            }

            var pagedAndFilteredPurchaseReturns = filteredPurchaseReturns
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var purchaseReturns = from o in pagedAndFilteredPurchaseReturns
                                  join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                      o.CreateUserId equals o6.Id into j6
                                  from s6 in j6.DefaultIfEmpty()
                                  join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                      o.UpdateUserId equals o5.Id into j5
                                  from s5 in j5.DefaultIfEmpty()
                                  select new GetPurchaseReturnForViewDto
                                  {
                                      VoucherNo = o.VoucherNo,
                                      Date = o.Date,
                                      DateMiti = o.DateMiti,
                                      TotalDiscount = o.TotalDiscount,
                                      Description = o.Description,
                                      PurchaseAccount = o.PurchaseAccount,
                                      TotalTax = o.TotalTax,
                                      TotalAmount = o.TotalAmount,
                                      GrandTotal = o.GrandTotal,
                                      LrNo = o.LrNo,
                                      TransportationCompany = o.TransportationCompany,
                                      Id = o.Id,
                                      CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                      UpdateUser = s5 == null || s5.Name == null ? "" : s5.Name,
                                      LedgerName = o.LedgerName
                                  };

            var totalCount = await filteredPurchaseReturns.CountAsync();
            return new PagedResultDto<GetPurchaseReturnForViewDto>(
                totalCount,
                await purchaseReturns.ToListAsync()
            );
        }


        [AbpAuthorize(AppPermissions.PagesPurchaseReturnsEdit)]
        public async Task<GetPurchaseReturnForEditOutput> GetPurchaseReturnForEdit(EntityDto<Guid> input)
        {
            var purchaseReturn = await purchaseReturnRepository.FirstOrDefaultAsync(input.Id);
            var purchaseMaster =
                await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturn.PurchaseMasterId);
            var voucherNo = "";
            Guid? masterId = Guid.Empty;
            if (purchaseMaster != null)
            {
                voucherNo = purchaseMaster.VoucherNo;
                masterId = purchaseMaster.Id;
            }

            var output = new GetPurchaseReturnForEditOutput
            {
                Id = purchaseReturn.Id,
                VoucherNo = purchaseReturn.VoucherNo,
                DateMiti = purchaseReturn.DateMiti,
                Description = purchaseReturn.Description,
                PurchaseMasterVoucherNo = voucherNo,
                PurchaseAccount = purchaseReturn.PurchaseAccount,
                TotalTax = purchaseReturn.TotalTax,
                TotalAmount = purchaseReturn.TotalAmount,
                TotalDiscount = purchaseReturn.TotalDiscount,
                NetAmount = purchaseReturn.NetAmount,
                DebitOrCreditNote = purchaseReturn.DebitOrCreditNote,
                TotalTaxableAmount = purchaseReturn.TotalTaxableAmount,
                GrandTotal = purchaseReturn.GrandTotal,
                InvoiceType = purchaseReturn.InvoiceType,
                LrNo = purchaseReturn.LrNo,
                TransportationCompany = purchaseReturn.TransportationCompany,
                VoucherTypeId = purchaseReturn.VoucherTypeId,
                PurchaseMasterId = masterId,
                LedgerId = purchaseReturn.LedgerId,
                ReturnType = purchaseReturn.ReturnType,
                ReturnTaxId = purchaseReturn.ReturnTaxId,
                ReturnAmount = purchaseReturn.TotalAmount,
                ReturnTaxAmount = purchaseReturn.TotalTax
            };

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            var purchaseReturnDetails = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PurchaseReturnId == input.Id).AsNoTracking().Include(x => x.ProductFk).ToListAsync();

            var returnDetail = new List<PurchaseReturnDetailDto>();

            foreach (var purchaseReturnDetail in purchaseReturnDetails)
            {
                List<PurchaseReturnUnitsQtyDto> getAllUnitList = new List<PurchaseReturnUnitsQtyDto>();
                if (purchaseReturnDetail.PurchaseDetailsId != null)
                    getAllUnitList = await GetAllUnitQtyForTableDropdown(purchaseReturnDetail.ProductId, (Guid)purchaseReturnDetail.PurchaseDetailsId);
                else
                {
                    getAllUnitList = await GetAllUnitQtyForTableDropdown(purchaseReturnDetail.ProductId, Guid.Empty);
                }
                var purchase = new PurchaseReturnDetailDto
                {
                    Id = purchaseReturnDetail.Id,
                    Qty = purchaseReturnDetail.Qty,
                    Rate = purchaseReturnDetail.Rate,
                    Discount = purchaseReturnDetail.Discount,
                    TaxAmount = purchaseReturnDetail.TaxAmount,
                    GrossAmount = purchaseReturnDetail.GrossAmount,
                    NetAmount = purchaseReturnDetail.NetAmount,
                    Amount = purchaseReturnDetail.Amount,
                    PurchaseDetailsId = purchaseReturnDetail.PurchaseDetailsId,
                    ProductId = purchaseReturnDetail.ProductId,
                    DiscountPer = purchaseReturnDetail.DiscountPer,
                    ProductCode = purchaseReturnDetail.ProductCode,
                    TaxId = purchaseReturnDetail.TaxId,
                    UnitId = purchaseReturnDetail.UnitId,
                    UnitsList = getAllUnitList,

                };
                returnDetail.Add(purchase);
            }

            output.PurchaseReturnDetail = returnDetail;
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPurchaseReturnDto input)
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

        [AbpAuthorize(AppPermissions.PagesPurchaseReturnsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var purchaser = await purchaseReturnRepository.FirstOrDefaultAsync(input.Id);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == purchaser.VoucherTypeId && x.FinancialYearId == purchaser.FinancialYearId &&
                x.VoucherNo == purchaser.VoucherNo);

            var stockPostings = await stockPostingRepository.GetAll()
                .Where(x => x.VoucherTypeId == purchaser.VoucherTypeId &&
                            x.FinancialYearId == purchaser.FinancialYearId &&
                            x.VoucherNo == purchaser.VoucherNo)
                .AsNoTracking()
                .ToListAsync();
            await materialStockPostingService.ReverseExistingStockPostingsAsync(stockPostings);

            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == purchaser.VoucherTypeId && x.FinancialYearId == purchaser.FinancialYearId &&
                x.VoucherNo == purchaser.VoucherNo);

            await partyBalanceService.DeletePurchaseReturnEntryAsync(input.Id);

            await purchaseReturnDetailRepository.DeleteAsync(x => x.PurchaseReturnId == input.Id);

            await purchaseReturnRepository.DeleteAsync(input.Id);

            //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
            //    new LocalizableString($"Purchase Return Bill No {purchaser.VoucherNo} is Deleted!",
            //        ERPConsts.LocalizationSourceName));
        }

        public async Task<FileDto> GetPurchaseReturnsToExcel(GetAllUniversalMastersInput input)
        {
            var filteredPurchaseReturns = purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.PurchaseMasterFk)
                .Include(e => e.AccountLedgerFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => false || e.Description.Contains(input.Filter) || e.LrNo.Contains(input.Filter) ||
                         e.TransportationCompany.Contains(input.Filter));
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseReturns = filteredPurchaseReturns.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseReturns = filteredPurchaseReturns.Where(x => x.Date <= date);
            }

            var query = from o in filteredPurchaseReturns
                        join o1 in purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on
                            o.PurchaseMasterId equals o1.Id into j1
                        from s1 in j1.DefaultIfEmpty()
                        join o3 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on o.LedgerId
                            equals o3.Id into j3
                        from s3 in j3.DefaultIfEmpty()
                        select new GetPurchaseReturnForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Date = o.Date,
                            TotalDiscount = o.TotalDiscount,
                            Description = o.Description,
                            PurchaseAccount = o.PurchaseAccount,
                            PurchaseMasterVoucherNo = o.PurchaseMasterFk.VoucherNo,
                            TotalTax = o.TotalTax,
                            TotalAmount = o.TotalAmount,
                            GrandTotal = o.GrandTotal,
                            LrNo = o.LrNo,
                            TransportationCompany = o.TransportationCompany,
                            Id = o.Id,
                            LedgerName = s3 == null || s3.Name == null ? "" : s3.Name
                        };

            var purchaseReturnListDtos = await query.AsNoTracking().ToListAsync();

            return purchaseReturnsExcelExporter.ExportToFile(purchaseReturnListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<UniversalDropdownDto>> GetAllPurchaseMasterForTableDropdown()
        {
            return await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(purchaseMaster => new UniversalDropdownDto
                {
                    Id = purchaseMaster.Id,
                    DisplayName = purchaseMaster == null || purchaseMaster.VoucherNo == null
                        ? ""
                        : purchaseMaster.VoucherNo.ToString()
                }).ToListAsync();
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<PurchaseReturnAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            var data = await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors" || x.AccountGroupFk.Name == "Sundry Debtors" ||
                            x.Name == "Cash")
                .Select(accountLedger => new PurchaseReturnAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    MobileNo = accountLedger.Phone,
                    Address = accountLedger.Address
                }).AsNoTracking().ToListAsync();
            return data;
        }


        [UnitOfWork]
        public async Task FixedPurchaseReturn()
        {
            await FixedPurchaseReturnError();
            await DeleteDuplicatePurchaseReturnLedgerPostings();
            await PostMissingPurchaseReturnLedgerData();
            await PostMissingPurchaseReturnStockData();
        }

        [UnitOfWork]
        private async Task FixedPurchaseReturnError()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .Where(lp => !purchaseReturnRepository.GetAll()
                        .Any(pr => pr.VoucherTypeId == lp.VoucherTypeId &&
                                   pr.FinancialYearId == lp.FinancialYearId &&
                                   pr.VoucherNo == lp.VoucherNo &&
                                   pr.TenantId == lp.TenantId
                        ))
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
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately
                Logger.Debug($"An error occurred: {ex.Message}");
            }
        }

        private async Task DeleteDuplicatePurchaseReturnLedgerPostings()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
                var tenantId = AbpSession.GetTenantId();

                // Find duplicate ledger postings by grouping and identifying records with count > 1
                var duplicateGroups = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == tenantId &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .GroupBy(lp => new
                    {
                        lp.VoucherNo,
                        lp.LedgerId,
                        lp.Debit,
                        lp.Credit
                    })
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        DuplicateGroup = g.Key,
                        PostingIds =
                            g.OrderBy(lp => lp.Id).Skip(1).Select(lp => lp.Id).ToList() // Keep first one, delete others
                    })
                    .ToListAsync();

                // Collect all duplicate IDs to delete
                var idsToDelete = duplicateGroups
                    .SelectMany(g => g.PostingIds)
                    .ToList();

                // Delete in batches
                if (idsToDelete.Any())
                {
                    const int batchSize = 1000;
                    for (var i = 0; i < idsToDelete.Count; i += batchSize)
                    {
                        var batchIds = idsToDelete.Skip(i).Take(batchSize).ToList();
                        await ledgerPostingRepository.DeleteAsync(p => batchIds.Contains(p.Id));
                        await UnitOfWorkManager.Current.SaveChangesAsync(); // Save each batch

                        Logger.Info(
                            $"Deleted {batchIds.Count} duplicate ledger postings, batch {i / batchSize + 1} of {(idsToDelete.Count + batchSize - 1) / batchSize}");
                    }

                    Logger.Info($"Total duplicate ledger postings deleted: {idsToDelete.Count}");
                }
                else
                {
                    Logger.Info("No duplicate ledger postings found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting duplicate purchase return ledger postings: {ex.Message}", ex);
            }
        }

        [UnitOfWork]
        public async Task PostMissingPurchaseReturnLedgerData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing ledger postings for purchase returns");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
                var activeTax = await taxRepository.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Rate > 0);

                // Retrieve purchase returns with missing ledger postings via left join
                var unpostedPurchaseReturns = await (
                        from pr in purchaseReturnRepository.GetAll().AsNoTracking()
                            // Left join on ledger postings
                        join lp in ledgerPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                pr.TenantId,
                                pr.FinancialYearId,
                                pr.VoucherTypeId,
                                pr.VoucherNo
                            }
                            equals new
                            {
                                lp.TenantId,
                                lp.FinancialYearId,
                                lp.VoucherTypeId,
                                lp.VoucherNo
                            }
                            into ledgerGrp
                        from ledger in ledgerGrp.DefaultIfEmpty()
                        where
                            pr.TenantId == tenantId &&
                            pr.FinancialYearId == FinancialYearId &&
                            pr.VoucherTypeId == voucherTypeId &&
                            ledger == null // means no existing ledger posting
                        select new
                        {
                            pr.Id,
                            pr.TenantId,
                            pr.VoucherNumbering,
                            pr.DateMiti,
                            pr.Date,
                            pr.VoucherTypeId,
                            pr.VoucherNo,
                            pr.PurchaseAccount,
                            pr.LedgerId,
                            pr.GrandTotal,
                            pr.TotalAmount,
                            pr.TotalTax,
                            pr.PostingNumber,
                            pr.FinancialYearId,
                            pr.DebitOrCreditNote,
                            pr.ReturnType
                        }
                    )
                    .ToListAsync();

                if (!unpostedPurchaseReturns.Any())
                {
                    Logger.Info("No purchase returns with missing ledger postings found");
                    return;
                }

                Logger.Info($"Found {unpostedPurchaseReturns.Count} purchase returns with missing ledger postings");

                // Prepare ledger postings
                var ledgerPostings = new List<LedgerPosting>();

                // Process each unposted purchase return
                foreach (var purchaseReturn in unpostedPurchaseReturns)
                {
                    // Supplier/Creditor posting (Debit for return - reducing liability)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = purchaseReturn.TenantId,
                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                        Date = purchaseReturn.Date,
                        DateMiti = purchaseReturn.DateMiti,
                        VoucherTypeId = purchaseReturn.VoucherTypeId,
                        VoucherNo = purchaseReturn.VoucherNo,
                        LedgerId = purchaseReturn.PurchaseAccount,
                        DetailId = purchaseReturn.LedgerId,
                        Debit = purchaseReturn.DebitOrCreditNote && purchaseReturn.ReturnType == ReturnType.RateDifference
                            ? purchaseReturn.TotalAmount
                            : 0,
                        Credit = purchaseReturn.DebitOrCreditNote && purchaseReturn.ReturnType == ReturnType.RateDifference
                            ? 0
                            : purchaseReturn.TotalAmount,
                        FinancialYearId = purchaseReturn.FinancialYearId,
                        InvoiceNo = purchaseReturn.VoucherNo,
                        PostingNumber = purchaseReturn.PostingNumber,
                        MasterId = purchaseReturn.Id
                    });

                    // Tax posting if applicable (Credit - reducing tax receivable/input tax)
                    if (purchaseReturn.TotalTax > 0 && activeTax != null)
                        ledgerPostings.Add(new LedgerPosting
                        {
                            TenantId = purchaseReturn.TenantId,
                            VoucherNumbering = purchaseReturn.VoucherNumbering,
                            Date = purchaseReturn.Date,
                            DateMiti = purchaseReturn.DateMiti,
                            VoucherTypeId = purchaseReturn.VoucherTypeId,
                            VoucherNo = purchaseReturn.VoucherNo,
                            LedgerId = activeTax.LedgerId,
                            DetailId = purchaseReturn.LedgerId,
                            Debit = purchaseReturn.ReturnType == ReturnType.RateDifference &&
                                    purchaseReturn.DebitOrCreditNote
                                ? purchaseReturn.TotalTax
                                : 0,
                            Credit = purchaseReturn.ReturnType == ReturnType.RateDifference &&
                                     purchaseReturn.DebitOrCreditNote
                                ? 0
                                : purchaseReturn.TotalTax,
                            FinancialYearId = purchaseReturn.FinancialYearId,
                            InvoiceNo = purchaseReturn.VoucherNo,
                            PostingNumber = purchaseReturn.PostingNumber,
                            MasterId = purchaseReturn.Id
                        });

                    // Purchase account posting (Credit - reducing expense/asset)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = purchaseReturn.TenantId,
                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                        Date = purchaseReturn.Date,
                        DateMiti = purchaseReturn.DateMiti,
                        VoucherTypeId = purchaseReturn.VoucherTypeId,
                        VoucherNo = purchaseReturn.VoucherNo,
                        LedgerId = purchaseReturn.LedgerId,
                        DetailId = purchaseReturn.PurchaseAccount,
                        Debit = purchaseReturn.DebitOrCreditNote && purchaseReturn.ReturnType == ReturnType.RateDifference
                            ? 0
                            : purchaseReturn.GrandTotal,
                        Credit = purchaseReturn.DebitOrCreditNote && purchaseReturn.ReturnType == ReturnType.RateDifference
                            ? purchaseReturn.GrandTotal
                            : 0,
                        FinancialYearId = purchaseReturn.FinancialYearId,
                        InvoiceNo = purchaseReturn.VoucherNo,
                        PostingNumber = purchaseReturn.PostingNumber,
                        MasterId = purchaseReturn.Id
                    });
                }

                // Bulk insert ledger postings
                if (ledgerPostings.Count > 0)
                {
                    await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);
                    await UnitOfWorkManager.Current.SaveChangesAsync();
                    Logger.Info($"Successfully created {ledgerPostings.Count} ledger postings " +
                                $"for {unpostedPurchaseReturns.Count} purchase returns");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting ledger data for purchase returns: {ex.Message}", ex);
                throw;
            }
        }

        [UnitOfWork]
        public async Task PostMissingPurchaseReturnStockData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing stock postings for purchase returns");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
                var purchaseVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

                // Find purchase returns that have details but no corresponding stock postings
                var purchaseReturnsWithMissingStockPostings = await (
                        from pr in purchaseReturnRepository.GetAll().AsNoTracking()
                        join prd in purchaseReturnDetailRepository.GetAll().AsNoTracking()
                            on pr.Id equals prd.PurchaseReturnId
                        // Left join on stock postings for this specific purchase return detail
                        join sp in stockPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                pr.TenantId,
                                pr.FinancialYearId,
                                pr.VoucherTypeId,
                                pr.VoucherNo,
                                prd.ProductId
                            }
                            equals new
                            {
                                sp.TenantId,
                                sp.FinancialYearId,
                                sp.VoucherTypeId,
                                sp.VoucherNo,
                                sp.ProductId
                            }
                            into stockGrp
                        from stock in stockGrp.DefaultIfEmpty()
                        where
                            pr.TenantId == tenantId &&
                            pr.FinancialYearId == FinancialYearId &&
                            pr.VoucherTypeId == voucherTypeId &&
                            pr.ReturnType !=
                            ReturnType.RateDifference && // Rate difference returns don't have stock movement
                            stock == null && // means no existing stock posting
                            prd.ProductFk.ProductType != ProductTypeEnum.Services // Only process non-service products
                        select new
                        {
                            PurchaseReturn = pr,
                            PurchaseReturnDetail = prd,
                            prd.ProductFk.ProductType,
                        }
                    )
                    .ToListAsync();

                if (!purchaseReturnsWithMissingStockPostings.Any())
                {
                    Logger.Info("No purchase returns with missing stock postings found");
                    return;
                }

                Logger.Info(
                    $"Found {purchaseReturnsWithMissingStockPostings.Count} purchase return details with missing stock postings");

                // Group by purchase return to process efficiently
                var groupedByPurchaseReturn = purchaseReturnsWithMissingStockPostings
                    .GroupBy(x => x.PurchaseReturn.Id)
                    .ToList();

                // Prepare collections for batch operations
                var stockPostingsToInsert = new List<StockPosting>();

                // Process each purchase return
                foreach (var purchaseReturnGroup in groupedByPurchaseReturn)
                {
                    var purchaseReturn = purchaseReturnGroup.First().PurchaseReturn;

                    // Delete any existing partial/erroneous stock data for this purchase return
                    await DeleteExistingPurchaseReturnStockData(new DeleteStockDataDto
                    {
                        VoucherTypeId = purchaseReturn.VoucherTypeId,
                        FinancialYearId = purchaseReturn.FinancialYearId,
                        VoucherNo = purchaseReturn.VoucherNo
                    });

                    // Process each purchase return detail in this return
                    foreach (var item in purchaseReturnGroup)
                    {
                        var purchaseReturnDetail = item.PurchaseReturnDetail;

                        // Create stock posting (Outward for return)
                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = purchaseReturn.VoucherNumbering,
                            Date = purchaseReturn.Date,
                            DateMiti = purchaseReturn.DateMiti,
                            LedgerId = purchaseReturn.LedgerId,
                            VoucherTypeId = purchaseReturn.VoucherTypeId,
                            VoucherNo = purchaseReturn.VoucherNo,
                            GrossAmount = purchaseReturnDetail.GrossAmount,
                            DiscountAmount = purchaseReturnDetail.Discount,
                            NetAmount = purchaseReturnDetail.NetAmount,
                            Amount = purchaseReturnDetail.Amount,
                            TaxAmount = purchaseReturnDetail.TaxAmount,
                            IsValueIncrease = false, // Purchase return decreases inventory value
                            ProductId = purchaseReturnDetail.ProductId,
                            UnitId = purchaseReturnDetail.UnitId,
                            AgainstVoucherTypeId = purchaseVoucherTypeId,
                            AgainstVoucherNo = purchaseReturn.PurchaseMasterId != null
                                ? (await purchaseMasterRepository.FirstOrDefaultAsync(purchaseReturn.PurchaseMasterId
                                    .Value))?.VoucherNo ?? ""
                                : "",
                            InWardQty = 0,
                            OutWardQty = purchaseReturnDetail.Qty, // Outward quantity for return
                            Rate = purchaseReturnDetail.Rate,
                            FinancialYearId = purchaseReturn.FinancialYearId,
                            MasterId = purchaseReturn.Id,
                            TenantId = tenantId
                        };

                        stockPostingsToInsert.Add(stockPosting);
                    }

                    // Update stock levels using the stock management service
                    foreach (var item in purchaseReturnGroup)
                    {
                        var purchaseReturnDetail = item.PurchaseReturnDetail;

                        var stockManage = new StockMaintainDto
                        {
                            DateMiti = purchaseReturn.DateMiti,
                            ProductId = purchaseReturnDetail.ProductId,
                            Qty = -purchaseReturnDetail.Qty, // Negative quantity for return (outward)
                            Rate = purchaseReturnDetail.Rate,
                            FinancialYearId = purchaseReturn.FinancialYearId,
                            Type = StockMaintainTypeEnum.Outward, // Outward for return
                            UnitId = purchaseReturnDetail.UnitId,
                        };

                        try
                        {
                            await stockManagementAppService.MaintainStock(stockManage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not update stock levels for product {purchaseReturnDetail.ProductId} " +
                                        $"in purchase return {purchaseReturn.VoucherNo}: {ex.Message}");
                        }
                    }
                }

                // Bulk insert stock postings
                if (stockPostingsToInsert.Any())
                {
                    await stockPostingRepository.InsertRangeAsync(stockPostingsToInsert);
                    await UnitOfWorkManager.Current.SaveChangesAsync();

                    Logger.Info($"Successfully created {stockPostingsToInsert.Count} stock postings");
                }

                Logger.Info(
                    $"Completed processing missing stock postings for {groupedByPurchaseReturn.Count} purchase returns");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting stock data for purchase returns: {ex.Message}", ex);
                throw;
            }
        }

        private async Task DeleteExistingPurchaseReturnStockData(DeleteStockDataDto stockDataInfo)
        {
            // Delete any existing stock postings for this purchase return
            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == stockDataInfo.VoucherTypeId &&
                x.FinancialYearId == stockDataInfo.FinancialYearId &&
                x.VoucherNo == stockDataInfo.VoucherNo);

            // Note: For purchase returns, we typically don't delete IMEI entries during error fixing
            // since they represent physical inventory returned. Only delete if absolutely necessary.
        }


        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>>
            GetAllPartyWiseAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x =>
                    x.AccountGroupFk.Name == "Sales Account" || x.AccountGroupFk.Name == "Purchase Account")
                .AsNoTracking().Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        public async Task<List<TaxDto>> GetAllTaxAccountLedgerForTableDropdown()
        {
            return await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking().Select(x =>
                new TaxDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Rate = x.Rate,
                    LedgerId = x.LedgerId
                }).ToListAsync();
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var purchaseReturnMaster = await purchaseReturnRepository.FirstOrDefaultAsync(x =>
                x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = purchaseReturnMaster.VoucherTypeId,
                VoucherNo = purchaseReturnMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public async Task<PdfForPurchaseReturnModel> GetPurchaseReturnForPdf(Guid id)
        {
            var purchaseReturn = await purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseMasterFk)
                .Where(x => x.Id == id).AsNoTracking().FirstOrDefaultAsync();

            var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);

            if (purchaseReturn != null)
            {
                var mainBranch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturn.LedgerId);
                var result = new PdfForPurchaseReturnModel
                {
                    BranchName = branch.CompanyName,
                    Logo1 = branch.Image1,
                    Date = purchaseReturn.Date,
                    Address = branch.Address,
                    BranchContact = branch.PhoneNo1 + " / " + branch.PhoneNo2,
                    BranchAddress = branch.Address,
                    Pan = mainBranch.PANumber,
                    DateMiti = DateConverter.ConvertToNepali(purchaseReturn.Date),
                    Tin = 0,
                    OrderNo = purchaseReturn.VoucherNo,
                    CustomerName = accountLedger.Name,
                    CustomerAddress = accountLedger.Address,
                    PurchaseVoucherNo = purchaseReturn.PurchaseMasterFk == null
                        ? ""
                        : purchaseReturn.PurchaseMasterFk.VoucherNo,
                    CustomerPan = accountLedger.Pan,
                    DebitOrCreditNote = purchaseReturn.DebitOrCreditNote,
                    PurchaseReturnDetail = null,
                    TotalAmountInWord = CurrencyToAmount.AmountWords(purchaseReturn.GrandTotal),
                    TotalAmount = purchaseReturn.TotalAmount,
                    TotalDiscount = purchaseReturn.TotalDiscount,
                    TaxableAmount = purchaseReturn.TotalTaxableAmount,
                    NetAmount = purchaseReturn.NetAmount,
                    TaxAmount = purchaseReturn.TotalTax,
                    DiscountAmount = purchaseReturn.TotalDiscount,
                    GrandTotal = purchaseReturn.GrandTotal,
                    Description = purchaseReturn.Description,
                    ApprovedBy = null,
                    ReceivedBy = null
                };
                var serial = 1;
                result.PurchaseReturnDetail =
                    (await purchaseReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.PurchaseReturnId == id)
                        .Include(x => x.ProductFk)
                        .Include(x => x.UnitFk)
                        .AsNoTracking().ToListAsync()).Select(x => new PdfForPurchaseReturnDetailModel
                        {
                            SlNo = serial++,
                            ProductCode = x.ProductCode,
                            HsCode = x.ProductFk.HsCode,
                            ProductName = x.ProductFk.Name,
                            Quantity = x.Qty,
                            Discount = x.Discount,
                            Unit = x.UnitFk.Name,
                            Rate = x.Rate,
                            Amount = x.NetAmount
                        }).ToList();

                return result;
            }

            throw new UserFriendlyException("Data not found");
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseReturnsPrint)]
        public async Task<byte[]> GetPdfDownload(Guid id)
        {
            var filePath = "PurchaeReturn";
            var model = await GetPurchaseReturnForPdf(id);
            var list = new List<PdfForPurchaseReturnModel>();
            list.Add(model);
            var document = new PurchaseReturnsPdf(list);
            return document.GeneratePdf();

        }

        [DisableAuditing]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnits()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Select(unit => new PurchaseReturnUnitsQtyDto
                {
                    UnitId = unit.Id,
                    UnitName = unit == null || unit.Name == null ? "" : unit.Name.ToString(),
                    Rate = 0,
                    Qty = 0
                }).ToListAsync();
        }

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            if (await purchaseReturnRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0)
                return true;
            return false;
        }

        public async Task<PurchaseReturnForViewNewDto> GetPurchaseReturnForViewNew(Guid id)
        {
            var purchaseReturn = await purchaseReturnRepository.GetAsync(id);
            var voucherNo = "";
            var purchaseMaster =
                await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturn.PurchaseMasterId);
            if (purchaseMaster != null)
                voucherNo = purchaseMaster.VoucherNo;
            var output = new PurchaseReturnForViewNewDto
            {
                Id = purchaseReturn.Id,
                VoucherNo = purchaseReturn.VoucherNo,
                DateMiti = purchaseReturn.DateMiti,
                TotalDiscount = purchaseReturn.TotalDiscount,
                NetAmount = purchaseReturn.NetAmount,
                TotalTaxableAmount = purchaseReturn.TotalTaxableAmount,
                ReturnType = purchaseReturn.ReturnType.ToString(),
                Description = purchaseReturn.Description,
                TotalTax = purchaseReturn.TotalTax,
                PurchaseMasterVoucherNo = voucherNo,
                TotalAmount = purchaseReturn.TotalAmount,
                //DebitOrCreditNote = purchaseReturn.DebitOrCreditNote,
                GrandTotal = purchaseReturn.GrandTotal,
                PurchaseMasterId = purchaseReturn.PurchaseMasterId,
                LedgerId = purchaseReturn.LedgerId
            };

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            var details = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.PurchaseReturnId == id).Include(x => x.ProductFk).Include(x => x.UnitFk)
                .AsNoTracking().Select(x => new PurchaseReturnDetailForViewDto
                {
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Discount = x.Discount,
                    DiscountPer = x.DiscountPer,
                    TaxAmount = x.TaxAmount,
                    GrossAmount = x.GrossAmount,
                    NetAmount = x.NetAmount,
                    Amount = x.Amount,
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductFk.Name,
                    UnitName = x.UnitFk.Name
                }).ToListAsync();
            output.Details = details;
            return output;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<UniversalDropdownDto>> GetAllPurchaseAccountForTableDropdown()
        {
            return await accountLedgerRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Purchase Account")
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        public async Task<string> GetPurchaseReturnVoucherNo()
        {
            var data = await purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .AsNoTracking().Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseReturn");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        public async Task<List<PurchaseReturnDetailDto>> GetPurchaseInvoiceByInvoiceNumber(Guid masterId)
        {
            var returnedProducts = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseReturnFk)
                .Where(
                    x => x.PurchaseReturnFk.PurchaseMasterId ==
                         masterId /*&& x.PurchaseReturnFk.ReturnType != ReturnType.RateDifference*/)
                .Select(x => new
                {
                    purchaseDetailsId = x.PurchaseDetailsId,
                    x.ProductId,
                    x.UnitId,
                    x.PurchaseReturnFk.VoucherNo,
                    x.PurchaseReturnFk.VoucherTypeId,
                    x.Qty,
                    x.PurchaseReturnFk.PurchaseMasterId
                }).AsNoTracking().ToListAsync();

            var purchaseDetails = await purchaseDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.ProductFk)
                .Include(x => x.TaxFk)
                .Where(x => x.PurchaseMasterId == masterId)
                .Select(x => new
                {
                    x.Id,
                    x.ProductId,
                    x.Qty,
                    x.Rate,
                    x.GrossAmount,
                    x.NetAmount,
                    x.Amount,
                    x.TaxAmount,
                    x.TaxId,
                    x.Discount,
                    x.DiscountPercent,
                    x.UnitId,
                    ProductName = x.ProductFk.Name,
                    PurchaseDetailId = x.Id,
                    x.PurchaseMasterFk.VoucherTypeId,
                    TaxRate = x.TaxFk.Rate == 0 ? 0 : x.TaxFk.Rate / 100,
                    x.PurchaseMasterFk.VoucherNo,
                    x.PurchaseMasterId
                }).AsNoTracking().ToListAsync();

            var result = new List<PurchaseReturnDetailDto>();
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            if (returnedProducts.Count == 0)
                foreach (var purchaseDetail in purchaseDetails)
                {


                    var unitLists =
                        await GetAllUnitQtyForTableDropdown(purchaseDetail.ProductId, purchaseDetail.Id);
                    var a = new PurchaseReturnDetailDto
                    {
                        Id = purchaseDetail.PurchaseMasterId,
                        PurchaseDetailsId = purchaseDetail.PurchaseDetailId,
                        Qty = purchaseDetail.Qty,
                        Rate = purchaseDetail.Rate,
                        TaxAmount = purchaseDetail.TaxAmount,
                        Discount = purchaseDetail.Discount,

                        GrossAmount = purchaseDetail.GrossAmount,
                        NetAmount = purchaseDetail.NetAmount,
                        Amount = purchaseDetail.Amount,
                        DiscountPer = purchaseDetail.DiscountPercent,
                        UnitsList = unitLists,
                        ProductId = purchaseDetail.ProductId,
                        UnitId = purchaseDetail.UnitId,
                        TaxId = purchaseDetail.TaxId
                    };
                    result.Add(a);
                }
            else
                foreach (var purchaseDetail in purchaseDetails)
                {
                    var objProduct = returnedProducts
                        .Where(x =>
                            x.PurchaseMasterId == purchaseDetail.PurchaseMasterId &&
                            x.ProductId == purchaseDetail.ProductId &&
                            x.purchaseDetailsId == purchaseDetail.PurchaseDetailId).ToList();

                    var objUnitList = objProduct
                        //  .Where(x => x.ProductId == objMaterialReceipt.ProductId && x.MaterialReceiptMasterId == objMaterialReceipt.MaterialReceiptMasterId)
                        .Select(order => new UnitConversionParamDetails
                        {
                            UnitId = order.UnitId,
                            Qty = order.Qty
                        });

                    var conversion = new UnitConversionParamDto
                    {
                        ProductId = purchaseDetail.ProductId,
                        UnitId = purchaseDetail.UnitId,
                        Qty = purchaseDetail.Qty,
                        Rate = purchaseDetail.Rate,
                        Details = objUnitList.ToList()
                    };

                    var unitConversion = await ERPCommonManager.UnitConversionMinus(conversion);
                    if (unitConversion.Qty > 0)
                    {


                        // throw new UserFriendlyException(unitConversion.Rate.ToString());
                        var unitLists =
                            await GetAllUnitQtyForTableDropdown(purchaseDetail.ProductId, purchaseDetail.Id);
                        var a = new PurchaseReturnDetailDto
                        {
                            Id = purchaseDetail.PurchaseMasterId,
                            PurchaseDetailsId = purchaseDetail.PurchaseDetailId,
                            Qty = unitConversion.Qty,
                            Rate = unitConversion.Rate,
                            UnitsList = unitLists,
                            TaxAmount = purchaseDetail.TaxRate * (unitConversion.Qty * unitConversion.Rate -
                                                                      purchaseDetail.DiscountPercent / 100 *
                                                                      unitConversion.Qty * unitConversion.Rate),
                            Discount = purchaseDetail.DiscountPercent / 100 * unitConversion.Qty * unitConversion.Rate,

                            GrossAmount = unitConversion.Qty * unitConversion.Rate,
                            NetAmount = unitConversion.Qty * unitConversion.Rate - purchaseDetail.DiscountPercent /
                                100 * unitConversion.Qty * unitConversion.Rate,
                            Amount = unitConversion.Qty * unitConversion.Rate -
                                     purchaseDetail.DiscountPercent / 100 * unitConversion.Qty * unitConversion.Rate +
                                     purchaseDetail.TaxRate * (unitConversion.Qty * unitConversion.Rate -
                                                                   purchaseDetail.DiscountPercent / 100 *
                                                                   unitConversion.Qty * unitConversion.Rate),
                            DiscountPer = purchaseDetail.DiscountPercent,
                            ProductId = purchaseDetail.ProductId,
                            UnitId = unitConversion.UnitId,
                            TaxId = purchaseDetail.TaxId
                        };
                        result.Add(a);
                    }
                }

            return result;
        }

        public async Task<List<IdAndNumberDto>> GetInvoiceNumber(Guid ledgerId)
        {
            var list = new List<IdAndNumberDto>();
            var returnedProducts = await purchaseReturnDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseReturnFk)
                .Where(x => x.PurchaseReturnFk.LedgerId == ledgerId
                            && x.PurchaseReturnFk.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.ProductId,
                    x.Qty,
                    x.PurchaseReturnFk.PurchaseMasterId,
                    purchaseDetailsId = x.PurchaseDetailsId,
                    x.UnitId
                }).AsNoTracking().ToListAsync();

            var purchaseMaster = await purchaseDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PurchaseMasterFk.LedgerId == ledgerId
                            && x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.Id,
                    x.ProductId,
                    x.Qty,
                    x.UnitId,
                    x.Rate,
                    x.PurchaseMasterFk.VoucherNo,
                    x.PurchaseMasterId
                }).AsNoTracking().ToListAsync();

            foreach (var item in purchaseMaster)
            {
                var objList = new List<UnitConversionParamDetails>();
                if (returnedProducts.Count(x => x.ProductId == item.ProductId && x.purchaseDetailsId == item.Id) > 0)
                {
                    var objOrders = returnedProducts
                        .Where(x => x.ProductId == item.ProductId && x.purchaseDetailsId == item.Id)
                        .ToList();
                    objList.AddRange(objOrders.Select(order => new UnitConversionParamDetails
                    { UnitId = order.UnitId, Qty = order.Qty }));

                    var conversion = new UnitConversionParamDto
                    {
                        ProductId = item.ProductId,
                        UnitId = item.UnitId,
                        Qty = item.Qty,
                        Rate = item.Rate,
                        Details = objList
                    };
                    var actualConversionUnit = await ERPCommonManager.UnitConversionMinus(conversion);

                    if (actualConversionUnit.Qty > 0)
                        list.Add(new IdAndNumberDto
                        {
                            Id = item.PurchaseMasterId,
                            VoucherNumber = item.VoucherNo
                        });
                }
                else
                {
                    list.Add(new IdAndNumberDto
                    {
                        Id = item.PurchaseMasterId,
                        VoucherNumber = item.VoucherNo
                    });
                }
            }

            return list.DistinctBy(x => x.Id).ToList();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<PurchaseReturnAccountLedgerTableDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => (x.AccountGroupFk.Name == "Cash-in Hand" ||
                                                       x.AccountGroupFk.Name == "Bank Account" ||
                                                       x.AccountGroupFk.Name == "Bank OD A/C"))
                .Select(pricingLevel => new PurchaseReturnAccountLedgerTableDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }



        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnitForTableDropdown(Guid productId)
        {
            var result = new List<PurchaseReturnUnitsQtyDto>();
            var product = (await productRepository.GetAll().Where(x => x.Id == productId).ToListAsync()).FirstOrDefault();
            result.Add(new PurchaseReturnUnitsQtyDto
            {
                UnitId = product.UnitId,
                UnitName = product.UnitFk.Name,
                ProductId = product.Id,
                Rate = product.PurchaseRate,
                Qty = 0
            });
            return result;
        }

        [DisableAuditing]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnitQtyForTableDropdown(Guid productId,
            Guid purchaseDetailId)
        {
            var result = new List<PurchaseReturnUnitsQtyDto>();
            if (purchaseDetailId != Guid.Empty)
            {
                var purchaseDetails = (await purchaseDetailRepository.GetAll().Include(x => x.UnitFk).Where(x =>
                    x.ProductId == productId && x.Id == purchaseDetailId).ToListAsync()).FirstOrDefault();

                result.Add(new PurchaseReturnUnitsQtyDto
                {
                    UnitId = purchaseDetails.UnitId,
                    UnitName = purchaseDetails.UnitFk.Name,
                    ProductId = purchaseDetails.ProductId,
                    Rate = purchaseDetails.Rate,
                    Qty = purchaseDetails.Qty
                });
                return result;


                //var purchaseDetails = await purchaseDetailRepository.FirstOrDefaultAsync(x =>
                //    x.ProductId == productId && x.Id == purchaseDetailId);

                //var unitConversion = await unitConversionRepository.GetAll()
                //    .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
                //    .Include(x => x.UnitFk).AsNoTracking().ToListAsync();

                //var lowestUnit = unitConversion.OrderBy(x => x.ConversionRate).First();
                //var purchaseUnitConv = unitConversion.First(x => x.UnitId == purchaseDetails.UnitId);
                //var purchaseQty = purchaseDetails.Qty * purchaseUnitConv.PrimaryQty / purchaseUnitConv.Qty *
                //    lowestUnit.Qty / lowestUnit.PrimaryQty;

                //var productRate = purchaseDetails.Rate / purchaseUnitConv.PrimaryQty * purchaseUnitConv.Qty;
                //return unitConversion.Select(unit => new PurchaseReturnUnitsQtyDto
                //{
                //    UnitId = unit.UnitId,
                //    ProductId = unit.ProductId,
                //    UnitName = unit.UnitFk.Name == null ? "" : unit.UnitFk.Name.ToString(),
                //    Rate = productRate * unit.PrimaryQty / unit.Qty,
                //    Qty = purchaseQty * unit.Qty / unit.PrimaryQty * lowestUnit.PrimaryQty / lowestUnit.Qty * unit.Qty /
                //          unit.PrimaryQty
                //}).ToList();
            }

            var product = (await productRepository.GetAll().Where(x => x.Id == productId).ToListAsync()).FirstOrDefault();
            result.Add(new PurchaseReturnUnitsQtyDto
            {
                UnitId = product.UnitId,
                UnitName = product.UnitFk.Name,
                ProductId = product.Id,
                Rate = product.PurchaseRate,
                Qty = 0
            });
            return result;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<PurchaseReturnAccountLedgerTableDto>> GetAllExpensesLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x =>
                   (x.AccountGroupFk.Name == "Misc.Expenses (ASSET)" ||
                                               x.AccountGroupFk.Name == "Direct Expenses" ||
                                               x.AccountGroupFk.Name == "Indirect Expenses"))
                .Select(pricingLevel => new PurchaseReturnAccountLedgerTableDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseReturns)]
        public async Task<List<UniversalDropdownDto>> GetAllProductForTableDropdown()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        public async Task<PurchaseOrderMasterProductListDto> GetProductById(Guid productId)
        {
            var data = await productRepository.FirstOrDefaultAsync(x => x.Id == productId);
            var unitList = await GetAllUnitForTableDropdown(productId);
            var result = new PurchaseOrderMasterProductListDto
            {
                Id = data.Id,
                ProductName = data.Name,
                Rate = data.PurchaseRate,
                TaxId = data.TaxId,
                UnitId = data.UnitId,
                UnitsList = unitList
            };

            return result;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<PurchaseMasterTaxTableDto>> GetAllTaxForTableDropdown()
        {
            return await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(tax => new PurchaseMasterTaxTableDto
                {
                    Id = tax.Id,
                    Name = tax.Name == null ? "" : tax.Name.ToString(),
                    Rate = tax.Rate
                }).AsNoTracking().ToListAsync();
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await purchaseReturnRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .AsNoTracking().Select(x => x.VoucherNumbering).ToListAsync();
            if (data.Count == 0)
            {
                var voucherNumbering =
                    await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseReturn");
                return voucherNumbering.StartIndex;
            }

            return data.Max() + 1;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PurchaseReturn");
        }

        private async Task<string> CheckValidationPurchaseMaster(CreateOrEditPurchaseReturnDto input)
        {
            var materialReceiptDetails = await purchaseDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PurchaseMasterFk.Id == input.PurchaseMasterId)
                .AsNoTracking().ToListAsync();

            var rejectionOutDetails = await purchaseReturnDetailRepository.GetAll()
                .Include(x => x.PurchaseDetailFk)
                .ThenInclude(x => x.PurchaseMasterFk)
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PurchaseDetailFk.PurchaseMasterFk.Id == input.PurchaseMasterId)
                .AsNoTracking().ToListAsync();

            var qtyMessage = string.Empty;
            var num = 0;
            foreach (var detail in input.PurchaseReturnDetail)
            {
                num++;
                var objMeterialReceipt = materialReceiptDetails.FirstOrDefault(x =>
                    x.ProductId == detail.ProductId && x.Id == detail.PurchaseDetailsId);
                var unitList = new List<UnitConversionParamDetails>();
                if (objMeterialReceipt != null)
                {
                    var objPurchaseOrders = rejectionOutDetails.Where(x => x.ProductId == detail.ProductId &&
                                                                           x.PurchaseDetailsId == detail.PurchaseDetailsId)
                        .ToList();

                    if (objPurchaseOrders.Count > 0)
                    {
                        var objOrders = materialReceiptDetails
                            .Where(x => x.ProductId == detail.ProductId && x.Id == detail.PurchaseDetailsId)
                            .Select(order => new UnitConversionParamDetails
                            {
                                UnitId = order.UnitId,
                                Qty = order.Qty
                            });
                        unitList.AddRange(objOrders);
                    }

                    unitList.Add(new UnitConversionParamDetails
                    {
                        UnitId = objMeterialReceipt.UnitId,
                        Qty = objMeterialReceipt.Qty
                    });

                    //var objMinusUnitList= 
                    var minusUnitForInput = new UnitConversionParamDto
                    {
                        ProductId = detail.ProductId,
                        UnitId = detail.UnitId,
                        Qty = detail.Qty ?? 0,
                        Rate = detail.Rate ?? 0,
                        Details = unitList
                    };

                    var minusUnitConversion = await ERPCommonManager.UnitConversionMinus(minusUnitForInput);
                    if (minusUnitConversion.Qty > 0)
                        qtyMessage +=
                            $"Row({num})  is Greater by {minusUnitConversion.Qty}  {minusUnitConversion.UnitName},";
                }
            }

            if (qtyMessage.Length > 0) qtyMessage = qtyMessage.Remove(qtyMessage.Length - 1);
            if (qtyMessage.Length > 0) return $"PurchaseMaster  {qtyMessage}";
            return qtyMessage;
        }

        private async Task ValidatePurchaseReturnStockAsync(CreateOrEditPurchaseReturnDto input)
        {
            if (input.ReturnType is not (ReturnType.ProductWise or ReturnType.NA) ||
                input.PurchaseReturnDetail == null ||
                input.PurchaseReturnDetail.Count == 0)
                return;

            var existingDetails = input.Id is null || input.Id == Guid.Empty
                ? new List<PurchaseReturnDetail>()
                : await purchaseReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.PurchaseReturnId == input.Id)
                    .AsNoTracking()
                    .ToListAsync();

            var requestedByUnit = input.PurchaseReturnDetail
                .Where(x => x.ProductId != Guid.Empty && x.UnitId != Guid.Empty)
                .GroupBy(x => new { x.ProductId, x.UnitId })
                .Select(x => new
                {
                    x.Key.ProductId,
                    x.Key.UnitId,
                    Qty = x.Sum(d => d.Qty ?? 0)
                })
                .ToList();

            foreach (var requested in requestedByUnit)
            {
                var product = await productRepository.GetAsync(requested.ProductId);
                if (product.ProductType == ProductTypeEnum.Services)
                    continue;

                var availableQty = await GetAvailableStockAsync(requested.ProductId, requested.UnitId);
                var previousQty = 0m;

                foreach (var existingDetail in existingDetails.Where(x => x.ProductId == requested.ProductId))
                {
                    previousQty += await ConvertQtyAsync(
                        existingDetail.ProductId,
                        existingDetail.UnitId,
                        requested.UnitId,
                        existingDetail.Qty);
                }

                var effectiveAvailable = availableQty + previousQty;
                if (requested.Qty > effectiveAvailable)
                    throw new UserFriendlyException("Insufficient stock",
                        $"{product.Name} return requires {requested.Qty}, available {effectiveAvailable}");
            }
        }

        private async Task<decimal> GetAvailableStockAsync(Guid productId, Guid unitId)
        {
            var stock = await stockMaintainRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
                .OrderByDescending(x => x.LastModifiedDate)
                .FirstOrDefaultAsync();

            if (stock == null)
                return 0;

            var available = stock.OpeningQty + stock.InwardQty - stock.OutwardQty;
            return await ConvertQtyAsync(productId, stock.UnitId, unitId, available);
        }

        private async Task<decimal> ConvertQtyAsync(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
        {
            if (fromUnitId == toUnitId)
                return qty;

            var conversions = await unitConversionRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .ToListAsync();

            var from = conversions.FirstOrDefault(x => x.UnitId == fromUnitId);
            var to = conversions.FirstOrDefault(x => x.UnitId == toUnitId);
            if (from == null || to == null || from.Qty == 0 || to.PrimaryQty == 0)
                return qty;

            var primaryQty = qty * from.PrimaryQty / from.Qty;
            return primaryQty * to.Qty / to.PrimaryQty;
        }


        [AbpAuthorize(AppPermissions.PagesPurchaseReturnsCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPurchaseReturnDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            var errorMessage = await CheckValidationPurchaseMaster(input);
            if (errorMessage.Length > 0) throw new UserFriendlyException(errorMessage);
            await ValidatePurchaseReturnStockAsync(input);
            var postingnumbering = PostingNumbering;
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            var purchaseMaster = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == input.PurchaseMasterId);
            if (purchaseMaster == null && input.ReturnType != ReturnType.NA)
                throw new UserFriendlyException("Purchase data not found.");
            //VoucherGeneration 
            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetPurchaseReturnVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await purchaseReturnRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PurchaseReturn VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var purchaseReturn = new PurchaseReturn
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DateMiti = input.DateMiti,
                TotalDiscount = input.TotalDiscount,
                Description = input.Description,
                PurchaseAccount = input.PurchaseAccount,
                DebitOrCreditNote = input.DebitOrCreditNote,
                TotalTax = input.TotalTax,
                NetAmount = input.NetAmount,
                TotalTaxableAmount = input.TotalTaxableAmount,
                TotalAmount = input.TotalAmount,
                //InvoiceType = input.ReturnType == ReturnType.NA ? input.InvoiceType : purchaseMaster.InvoiceTypeEnum,
                GrandTotal = input.GrandTotal,
                FinancialYearId = FinancialYearId,
                LrNo = input.LrNo,
                TransportationCompany = input.TransportationCompany,
                VoucherTypeId = voucherTypeId,
                PurchaseMasterId = input.ReturnType == ReturnType.NA ? null : input.PurchaseMasterId,
                LedgerId = input.LedgerId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                TenantId = tenantId,
                PostingNumber = postingnumbering,
                ReturnType = input.ReturnType,
                ReturnTaxId = input.ReturnTaxId
            };
            if (purchaseReturn.ReturnType == ReturnType.PartyWise)
            {
                purchaseReturn.TotalAmount = input.ReturnAmount;
                purchaseReturn.ReturnTaxId = input.ReturnTaxId;
                purchaseReturn.PurchaseMasterId = input.ReturnType != ReturnType.NA ? input.PurchaseMasterId : Guid.Empty;
                purchaseReturn.TotalTaxableAmount = input.ReturnTaxAmount > 0 ? input.ReturnAmount : 0;
                purchaseReturn.TotalTax = input.ReturnTaxAmount;
                purchaseReturn.NetAmount = input.ReturnAmount;
                purchaseReturn.GrandTotal = input.ReturnAmount + input.ReturnTaxAmount;
            }

            var masterId = await purchaseReturnRepository.InsertAndGetIdAsync(purchaseReturn);

            var ledger = new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DetailId = input.LedgerId,
                Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? input.TotalAmount : 0,
                Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? 0 : input.TotalAmount,
                InvoiceNo = input.VoucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.PurchaseAccount,
                TenantId = tenantId,
                PostingNumber = postingnumbering,
                VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                  (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                MasterId = masterId
            };
            await ledgerPostingRepository.InsertAsync(ledger);

            var ledger5 = new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DetailId = input.PurchaseAccount,
                Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? 0 : input.GrandTotal,
                Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? input.GrandTotal : 0,
                InvoiceNo = input.VoucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                PostingNumber = postingnumbering,
                MasterId = masterId,
                VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                  (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                TenantId = tenantId
            };
            await ledgerPostingRepository.InsertAsync(ledger5);
            if (input.ReturnType == ReturnType.PartyWise)
            {
                var ledger6 = new LedgerPosting
                {
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DetailId = input.PurchaseAccount,
                    Debit = 0,
                    Credit = input.ReturnTaxAmount,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.ReturnTaxId,
                    PostingNumber = postingnumbering,
                    MasterId = masterId,
                    VendorVoucherNo = purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo,
                    TenantId = tenantId
                };
                await ledgerPostingRepository.InsertAsync(ledger6);
            }

            var accountLedgerDetails = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);


            if (accountLedgerDetails != null)
                if (accountLedgerDetails.IsBillByBill)
                {
                    await partyBalanceService.CreatePurchaseReturnEntryAsync(new PartyBalanceNewEntryDto
                    {
                        Date = DateConverter.ConvertToEnglish(purchaseReturn.DateMiti),
                        DueDate = DateConverter.ConvertToEnglish(purchaseReturn.DateMiti),
                        LedgerId = purchaseReturn.LedgerId,
                        VoucherNo = purchaseReturn.VoucherNo,
                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                        Amount = purchaseReturn.GrandTotal,
                        MasterId = purchaseReturn.Id,
                        AgainstId = purchaseReturn.PurchaseMasterId ?? Guid.Empty,
                    });
                    //var partyBalanceData = new PartyBalance
                    //{
                    //    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    //    LedgerId = input.LedgerId,
                    //    FinancialYearId = FinancialYearId,
                    //    VoucherTypeId = input.ReturnType == ReturnType.NA ? voucherTypeId : againstVoucherTypeId,
                    //    VoucherNumbering = voucherNumbering,
                    //    VoucherNo = input.ReturnType == ReturnType.NA ? input.VoucherNo : purchaseMaster.VoucherNo,
                    //    AgainstVoucherTypeId = input.ReturnType == ReturnType.NA ? Guid.Empty : voucherTypeId,
                    //    AgainstVoucherNo = input.ReturnType == ReturnType.NA ? "" : input.VoucherNo,
                    //    InvoiceNo = input.VoucherNo,
                    //    AgainstInvoiceNo = purchaseMaster == null ? "" : purchaseMaster.VoucherNo,
                    //    ReferenceType = input.ReturnType == ReturnType.NA ? "New" : "Against",
                    //    Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                    //        ? 0
                    //        : input.GrandTotal,
                    //    Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                    //        ? input.GrandTotal
                    //        : 0,
                    //    CreditPeriod = 0,
                    //    BranchId = input.BranchId,
                    //    MasterVoucherTypeId = voucherTypeId,
                    //    MasterId = masterId,
                    //    DetailId = Guid.Empty,
                    //    MasterVoucherNo = voucherNo,
                    //    TenantId = tenantId
                    //};
                    //await partyBalanceRepository.InsertAsync(partyBalanceData);
                }

            if (input.TotalDiscount > 0)
            {
                var ledger1 = new LedgerPosting
                {
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DetailId = input.PurchaseAccount,
                    Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                        ? 0
                        : input.TotalDiscount,
                    Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                        ? input.TotalDiscount
                        : 0,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "Discount Received")).Id,
                    TenantId = tenantId,
                    PostingNumber = postingnumbering,
                    VendorVoucherNo = purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo,
                    MasterId = masterId
                };
                await ledgerPostingRepository.InsertAsync(ledger1);
            }

            if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                foreach (var detail in input.PurchaseReturnDetail)
                {
                    var data = new PurchaseReturnDetail
                    {
                        Id = detail.Id == Guid.Empty ? Guid.NewGuid() : detail.Id,
                        Qty = detail.Qty ?? 0,
                        Rate = detail.Rate ?? 0,
                        Discount = detail.Discount ?? 0,
                        TaxAmount = detail.TaxAmount ?? 0,
                        PurchaseDetailsId = input.ReturnType == ReturnType.NA ? null : detail.PurchaseDetailsId,
                        DiscountPer = detail.DiscountPer ?? 0,
                        GrossAmount = detail.GrossAmount ?? 0,
                        NetAmount = detail.NetAmount ?? 0,
                        Amount = detail.Amount ?? 0,
                        ProductCode = detail.ProductCode,
                        PurchaseReturnId = masterId,
                        ProductId = detail.ProductId,
                        TaxId = detail.TaxId,
                        UnitId = detail.UnitId,
                        TenantId = tenantId
                    };
                    await purchaseReturnDetailRepository.InsertAsync(data);

                    if (detail.TaxAmount > 0)
                    {
                        var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                        var ledger3 = new LedgerPosting
                        {
                            VoucherNumbering = voucherNumbering,
                            VoucherNo = voucherNo,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DetailId = input.PurchaseAccount,
                            Debit = input.ReturnType == ReturnType.RateDifference && input.DebitOrCreditNote
                                ? detail.TaxAmount ?? 0
                                : 0,
                            Credit = input.ReturnType == ReturnType.RateDifference && input.DebitOrCreditNote
                                ? 0
                                : detail.TaxAmount ?? 0,
                            InvoiceNo = input.VoucherNo,
                            FinancialYearId = FinancialYearId,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            LedgerId = tax.LedgerId,
                            TenantId = tenantId,
                            PostingNumber = postingnumbering,
                            VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                              (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                            MasterId = masterId
                        };
                        await ledgerPostingRepository.InsertAsync(ledger3);
                    }

                    if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                    {
                        await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                        {
                            VoucherNumbering = voucherNumbering,
                            VoucherNo = voucherNo,
                            DateMiti = input.DateMiti,
                            LedgerId = input.LedgerId,
                            VoucherTypeId = voucherTypeId,
                            ProductId = detail.ProductId,
                            UnitId = detail.UnitId,
                            GrossAmount = detail.GrossAmount ?? 0,
                            DiscountAmount = detail.Discount ?? 0,
                            NetAmount = detail.NetAmount ?? 0,
                            Amount = detail.Amount ?? 0,
                            TaxAmount = detail.TaxAmount ?? 0,
                            AgainstVoucherTypeId = againstVoucherTypeId,
                            AgainstVoucherNo = input.ReturnType == ReturnType.NA ? "" : purchaseMaster.VoucherNo,
                            Qty = detail.Qty ?? 0,
                            MovementType = StockMaintainTypeEnum.Outward,
                            IsValueIncrease = false,
                            Rate = detail.Rate ?? 0,
                            FinancialYearId = FinancialYearId,
                            MasterId = masterId,
                            VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                               (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                            SourceDetailId = data.Id,
                            TenantId = tenantId
                        });
                    }

                    if (input.ReturnType == ReturnType.RateDifference)
                    {
                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = voucherNumbering,
                            VoucherNo = voucherNo,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            LedgerId = input.LedgerId,
                            VoucherTypeId = voucherTypeId,
                            ProductId = detail.ProductId,
                            UnitId = detail.UnitId,
                            GrossAmount = detail.GrossAmount ?? 0,
                            DiscountAmount = detail.Discount ?? 0,
                            NetAmount = detail.NetAmount ?? 0,
                            Amount = detail.Amount ?? 0,
                            TaxAmount = detail.TaxAmount ?? 0,
                            AgainstVoucherTypeId = againstVoucherTypeId,
                            AgainstVoucherNo = purchaseMaster?.VoucherNo,
                            InWardQty = 0,
                            OutWardQty = 0,
                            IsValueIncrease = input.DebitOrCreditNote ? true : false,
                            Rate = detail.Rate ?? 0,
                            FinancialYearId = FinancialYearId,
                            MasterId = masterId,
                            VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                              (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                            TenantId = tenantId
                        };
                        await stockPostingRepository.InsertAndGetIdAsync(stockPosting);
                    }
                }

            await unitOfWork.CompleteAsync();
            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseReturnsEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPurchaseReturnDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            await ValidatePurchaseReturnStockAsync(input);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");
            var purchaseVoucherType = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

            var purchaseReturn = await purchaseReturnRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (purchaseReturn != null)
            {
                // purchaseReturn.VoucherNo = input.VoucherNo;
                if (await GetVoucherGenerateType() == "Manually")
                {
                    if (await purchaseReturnRepository.CountAsync(x =>
                            x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                        0) throw new UserFriendlyException("PurchaseReturn VoucherNo is Duplicate");
                    purchaseReturn.VoucherNo = input.VoucherNo;
                }

                if (await GetVoucherGenerateType() == "Duplicate") purchaseReturn.VoucherNo = input.VoucherNo;
                var postingNumbering = purchaseReturn.PostingNumber;
                purchaseReturn.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                purchaseReturn.DateMiti = input.DateMiti;
                purchaseReturn.Description = input.Description;
                purchaseReturn.PurchaseAccount = input.PurchaseAccount;
                purchaseReturn.TotalTax = input.TotalTax;
                purchaseReturn.TotalAmount = input.TotalAmount;
                purchaseReturn.GrandTotal = input.GrandTotal;
                purchaseReturn.LrNo = input.LrNo;
                purchaseReturn.TransportationCompany = input.TransportationCompany;
                purchaseReturn.PurchaseMasterId = input.PurchaseMasterId;
                purchaseReturn.LedgerId = input.LedgerId;
                purchaseReturn.ReturnType = input.ReturnType;
                purchaseReturn.InvoiceType =
                    input.ReturnType == ReturnType.NA ? input.InvoiceType : purchaseReturn.InvoiceType;
                purchaseReturn.ReturnTaxId = input.ReturnTaxId;
                purchaseReturn.DebitOrCreditNote = input.DebitOrCreditNote;
                purchaseReturn.UpdateUserId = AbpSession.UserId;
                purchaseReturn.TotalDiscount = input.TotalDiscount;
                purchaseReturn.NetAmount = input.NetAmount;
                purchaseReturn.TotalTaxableAmount = input.TotalTaxableAmount;
                if (purchaseReturn.ReturnType == ReturnType.PartyWise)
                {
                    purchaseReturn.TotalAmount = input.ReturnAmount;
                    purchaseReturn.ReturnTaxId = input.ReturnTaxId;
                    purchaseReturn.TotalTaxableAmount = input.ReturnTaxAmount > 0 ? input.ReturnAmount : 0;
                    purchaseReturn.TotalTax = input.ReturnTaxAmount;
                    purchaseReturn.NetAmount = input.ReturnAmount;
                    purchaseReturn.GrandTotal = input.ReturnAmount + input.ReturnTaxAmount;
                }

                await purchaseReturnRepository.UpdateAsync(purchaseReturn);

                // delete ledgerposting
                await ledgerPostingRepository.DeleteAsync(x => x.VoucherNo == input.VoucherNo &&
                                                               x.VoucherTypeId == voucherTypeId &&
                                                               x.FinancialYearId == FinancialYearId);

                var existingStockPostings = await stockPostingRepository.GetAll()
                    .Where(x => x.VoucherNo == input.VoucherNo &&
                                x.VoucherTypeId == voucherTypeId &&
                                x.FinancialYearId == FinancialYearId)
                    .AsNoTracking()
                    .ToListAsync();
                await materialStockPostingService.ReverseExistingStockPostingsAsync(existingStockPostings);

                await stockPostingRepository.DeleteAsync(x => x.VoucherNo == input.VoucherNo &&
                                                              x.VoucherTypeId == voucherTypeId &&
                                                              x.FinancialYearId == FinancialYearId);

                //delete purchasedetails
                var detailsIds = input.PurchaseReturnDetail.Select(x => x.Id).ToList();
                var detailsDatabaseIds = await purchaseReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.PurchaseReturnId == input.Id).AsNoTracking()
                    .Select(x => x.Id).ToListAsync();

                var accountLedgerDetails = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);

                var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
                var purchaseMaster =
                    await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == input.PurchaseMasterId);

                //await partyBalanceRepository.DeleteAsync(x => x.FinancialYearId == FinancialYearId &&
                //                                              x.BranchId == input.BranchId &&
                //                                              x.VoucherNo == input.VoucherNo &&
                //                                              x.VoucherTypeId == againstVoucherTypeId &&
                //                                              x.AgainstVoucherTypeId == voucherTypeId);
                if (accountLedgerDetails != null)
                    if (accountLedgerDetails.IsBillByBill)
                    {
                        await partyBalanceService.UpdatePurchaseReturnEntryAsync(new PartyBalanceNewEntryDto
                        {
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DueDate = DateConverter.ConvertToEnglish(input.DateMiti),
                            LedgerId = input.LedgerId,
                            VoucherNo = purchaseReturn.VoucherNo,
                            VoucherNumbering = purchaseReturn.VoucherNumbering,
                            Amount = input.GrandTotal,
                            MasterId = purchaseReturn.Id,
                            AgainstId = purchaseMaster.Id,
                        });
                        //var partyBalanceData = new PartyBalance
                        //{
                        //    VoucherNumbering = purchaseReturn.VoucherNumbering,
                        //    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        //    LedgerId = input.LedgerId,
                        //    FinancialYearId = FinancialYearId,
                        //    VoucherTypeId = input.ReturnType == ReturnType.NA ? voucherTypeId : againstVoucherTypeId,
                        //    VoucherNo = input.ReturnType == ReturnType.NA ? input.VoucherNo : purchaseMaster.VoucherNo,
                        //    AgainstVoucherTypeId = input.ReturnType == ReturnType.NA ? Guid.Empty : voucherTypeId,
                        //    AgainstVoucherNo = input.ReturnType == ReturnType.NA ? "" : input.VoucherNo,
                        //    InvoiceNo = input.VoucherNo,
                        //    AgainstInvoiceNo = purchaseMaster == null ? "" : purchaseMaster.VoucherNo,
                        //    ReferenceType = input.ReturnType == ReturnType.NA ? "New" : "Against",
                        //    Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                        //        ? 0
                        //        : input.GrandTotal,
                        //    Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                        //        ? input.GrandTotal
                        //        : 0,
                        //    CreditPeriod = 0,
                        //    BranchId = input.BranchId,
                        //    MasterVoucherTypeId = voucherTypeId,
                        //    MasterId = (Guid)input.Id,
                        //    DetailId = Guid.Empty,
                        //    MasterVoucherNo = purchaseMaster.VoucherNo,
                        //    TenantId = tenantId
                        //};
                        //await partyBalanceRepository.InsertAsync(partyBalanceData);
                    }

                foreach (var detailsDatabaseId in detailsDatabaseIds)
                    if (!detailsIds.Contains(detailsDatabaseId))
                        await purchaseReturnDetailRepository.DeleteAsync(detailsDatabaseId);
                //delete additonalcost


                // ledgerpostings Add
                var ledger9 = new LedgerPosting
                {
                    VoucherNumbering = purchaseReturn.VoucherNumbering,
                    VoucherNo = input.VoucherNo,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DetailId = input.LedgerId,
                    Debit =
                        input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? input.TotalAmount : 0,
                    Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                        ? 0
                        : input.TotalAmount,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.PurchaseAccount,
                    TenantId = tenantId,
                    PostingNumber = postingNumbering,
                    VendorVoucherNo = purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo,
                    MasterId = purchaseReturn.Id
                };
                await ledgerPostingRepository.InsertAsync(ledger9);

                var ledger = new LedgerPosting
                {
                    VoucherNumbering = purchaseReturn.VoucherNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = input.VoucherNo,
                    DetailId = input.PurchaseAccount,
                    Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? 0 : input.GrandTotal,
                    Credit =
                        input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference ? input.GrandTotal : 0,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.LedgerId,
                    TenantId = tenantId,
                    PostingNumber = postingNumbering,
                    VendorVoucherNo = purchaseMaster?.VendorInvoiceNo +
                                      (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                    MasterId = purchaseReturn.Id
                };
                await ledgerPostingRepository.InsertAsync(ledger);

                if (input.ReturnType == ReturnType.PartyWise)
                {
                    var ledger6 = new LedgerPosting
                    {
                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                        VoucherNo = input.VoucherNo,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DetailId = input.PurchaseAccount,
                        Debit = 0,
                        Credit = input.ReturnTaxAmount,
                        InvoiceNo = input.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = input.ReturnTaxId,
                        PostingNumber = postingNumbering,
                        MasterId = purchaseReturn.Id,
                        VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                          (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                        TenantId = tenantId
                    };
                    await ledgerPostingRepository.InsertAsync(ledger6);
                }

                if (input.TotalDiscount > 0)
                {
                    var ledger1 = new LedgerPosting
                    {
                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        VoucherNo = input.VoucherNo,
                        DetailId = input.PurchaseAccount,
                        Debit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                            ? 0
                            : input.TotalDiscount,
                        Credit = input.DebitOrCreditNote && input.ReturnType == ReturnType.RateDifference
                            ? input.TotalDiscount
                            : 0,
                        InvoiceNo = input.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "Discount Received"))
                            .Id,
                        TenantId = tenantId,
                        PostingNumber = postingNumbering,
                        VendorVoucherNo = purchaseMaster?.VendorInvoiceNo +
                                          (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                        MasterId = purchaseReturn.Id
                    };
                    await ledgerPostingRepository.InsertAsync(ledger1);
                }

                if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)

                    foreach (var detail in input.PurchaseReturnDetail)
                        if (detail.Id == Guid.Empty)
                        {
                            var data = new PurchaseReturnDetail
                            {
                                Qty = detail.Qty ?? 0,
                                Rate = detail.Rate ?? 0,
                                Discount = detail.Discount ?? 0,
                                TaxAmount = detail.TaxAmount ?? 0,
                                PurchaseDetailsId = detail.PurchaseDetailsId,
                                GrossAmount = detail.GrossAmount ?? 0,
                                NetAmount = detail.NetAmount ?? 0,
                                ProductCode = detail.ProductCode,
                                DiscountPer = detail.DiscountPer ?? 0,
                                Amount = detail.Amount ?? 0,
                                PurchaseReturnId = (Guid)input.Id,
                                ProductId = detail.ProductId,
                                TaxId = detail.TaxId,
                                UnitId = detail.UnitId,
                                TenantId = purchaseReturn.TenantId
                            };
                            var detailId = await purchaseReturnDetailRepository.InsertAndGetIdAsync(data);


                            if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                            {
                                await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                                {
                                    VoucherNumbering = purchaseReturn.VoucherNumbering,
                                    DateMiti = input.DateMiti,
                                    LedgerId = input.LedgerId,
                                    VoucherTypeId = voucherTypeId,
                                    VoucherNo = input.VoucherNo,
                                    ProductId = detail.ProductId,
                                    GrossAmount = detail.GrossAmount ?? 0,
                                    DiscountAmount = detail.Discount ?? 0,
                                    NetAmount = detail.NetAmount ?? 0,
                                    Amount = detail.Amount ?? 0,
                                    IsValueIncrease = false,
                                    TaxAmount = detail.TaxAmount ?? 0,
                                    UnitId = detail.UnitId,
                                    AgainstVoucherTypeId = purchaseVoucherType,
                                    AgainstVoucherNo = purchaseMaster.VoucherNo,
                                    Qty = detail.Qty ?? 0,
                                    MovementType = StockMaintainTypeEnum.Outward,
                                    Rate = detail.Rate ?? 0,
                                    FinancialYearId = FinancialYearId,
                                    MasterId = purchaseReturn.Id,
                                    VendorVoucherNo = purchaseMaster?.VendorInvoiceNo,
                                    SourceDetailId = detailId,
                                    TenantId = tenantId
                                });
                            }

                            if (input.ReturnType == ReturnType.RateDifference)
                            {
                                var stockPosting = new StockPosting
                                {
                                    VoucherNumbering = purchaseReturn.VoucherNumbering,
                                    VoucherNo = input.VoucherNo,
                                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                    DateMiti = input.DateMiti,
                                    LedgerId = input.LedgerId,
                                    VoucherTypeId = voucherTypeId,
                                    ProductId = detail.ProductId,
                                    UnitId = detail.UnitId,
                                    GrossAmount = detail.GrossAmount ?? 0,
                                    DiscountAmount = detail.Discount ?? 0,
                                    NetAmount = detail.NetAmount ?? 0,
                                    Amount = detail.Amount ?? 0,
                                    TaxAmount = detail.TaxAmount ?? 0,
                                    AgainstVoucherTypeId = againstVoucherTypeId,
                                    AgainstVoucherNo = purchaseMaster.VoucherNo,
                                    InWardQty = 0,
                                    OutWardQty = 0,
                                    IsValueIncrease = input.DebitOrCreditNote ? true : false,
                                    Rate = detail.Rate ?? 0,
                                    FinancialYearId = FinancialYearId,
                                    MasterId = input.Id,
                                    VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                                      (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                                    TenantId = tenantId
                                };
                                await stockPostingRepository.InsertAndGetIdAsync(stockPosting);
                            }

                            if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                                if (detail.TaxId != Guid.Empty && detail.TaxAmount != 0 && detail.TaxAmount != null)
                                {
                                    var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                                    var ledger3 = new LedgerPosting
                                    {
                                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                                        VoucherNo = purchaseReturn.VoucherNo,
                                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                        DetailId = input.PurchaseAccount,
                                        Debit = input.ReturnType == ReturnType.RateDifference && input.DebitOrCreditNote
                                            ? detail.TaxAmount ?? 0
                                            : 0,
                                        Credit = input.ReturnType == ReturnType.RateDifference && input.DebitOrCreditNote
                                            ? 0
                                            : detail.TaxAmount ?? 0,
                                        InvoiceNo = input.VoucherNo,
                                        FinancialYearId = FinancialYearId,
                                        DateMiti = input.DateMiti,
                                        VoucherTypeId = voucherTypeId,
                                        LedgerId = tax.LedgerId,
                                        TenantId = tenantId,
                                        PostingNumber = postingNumbering,
                                        VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                                          (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                                        MasterId = input.Id
                                    };
                                    await ledgerPostingRepository.InsertAsync(ledger3);
                                }
                        }
                        else
                        {
                            var data = await purchaseReturnDetailRepository.FirstOrDefaultAsync(x => x.Id == detail.Id);
                            if (data != null)
                            {
                                data.Qty = detail.Qty ?? 0;
                                data.Rate = detail.Rate ?? 0;
                                data.Discount = detail.Discount ?? 0;
                                data.TaxAmount = detail.TaxAmount ?? 0;
                                data.GrossAmount = detail.GrossAmount ?? 0;
                                data.NetAmount = detail.NetAmount ?? 0;
                                data.Amount = detail.Amount ?? 0;
                                data.DiscountPer = detail.DiscountPer ?? 0;
                                data.ProductId = detail.ProductId;
                                data.PurchaseDetailsId = detail.PurchaseDetailsId;
                                data.ProductCode = detail.ProductCode;
                                data.TaxId = detail.TaxId;
                                data.UnitId = detail.UnitId;
                                await purchaseReturnDetailRepository.UpdateAsync(data);

                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                                {
                                    await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                                    {
                                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                                        DateMiti = input.DateMiti,
                                        LedgerId = input.LedgerId,
                                        VoucherTypeId = voucherTypeId,
                                        VoucherNo = input.VoucherNo,
                                        ProductId = detail.ProductId,
                                        UnitId = detail.UnitId,
                                        GrossAmount = detail.GrossAmount ?? 0,
                                        IsValueIncrease = false,
                                        DiscountAmount = detail.Discount ?? 0,
                                        NetAmount = detail.NetAmount ?? 0,
                                        Amount = detail.Amount ?? 0,
                                        TaxAmount = detail.TaxAmount ?? 0,
                                        AgainstVoucherTypeId = Guid.Empty,
                                        AgainstVoucherNo = "",
                                        Qty = detail.Qty ?? 0,
                                        MovementType = StockMaintainTypeEnum.Outward,
                                        Rate = detail.Rate ?? 0,
                                        FinancialYearId = FinancialYearId,
                                        MasterId = purchaseReturn.Id,
                                        VendorVoucherNo = purchaseMaster?.VendorInvoiceNo +
                                                          (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                                        SourceDetailId = data.Id,
                                        TenantId = tenantId
                                    });
                                }

                                if (input.ReturnType == ReturnType.RateDifference)
                                {
                                    var stockPosting = new StockPosting
                                    {
                                        VoucherNumbering = purchaseReturn.VoucherNumbering,
                                        VoucherNo = purchaseReturn.VoucherNo,
                                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                        DateMiti = input.DateMiti,
                                        LedgerId = input.LedgerId,
                                        VoucherTypeId = voucherTypeId,
                                        ProductId = detail.ProductId,
                                        UnitId = detail.UnitId,
                                        GrossAmount = detail.GrossAmount ?? 0,
                                        DiscountAmount = detail.Discount ?? 0,
                                        NetAmount = detail.NetAmount ?? 0,
                                        Amount = detail.Amount ?? 0,
                                        TaxAmount = detail.TaxAmount ?? 0,
                                        AgainstVoucherNo = purchaseMaster.VoucherNo,
                                        InWardQty = 0,
                                        OutWardQty = 0,
                                        IsValueIncrease = input.DebitOrCreditNote ? true : false,
                                        Rate = detail.Rate ?? 0,
                                        FinancialYearId = FinancialYearId,
                                        MasterId = input.Id,
                                        VendorVoucherNo = (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                                          (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                                        TenantId = tenantId
                                    };
                                    await stockPostingRepository.InsertAndGetIdAsync(stockPosting);
                                }

                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference
                                    or ReturnType.NA)
                                    if (detail.TaxId != Guid.Empty && detail.TaxAmount != 0 && detail.TaxAmount != null)
                                    {
                                        var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                                        var ledger3 = new LedgerPosting
                                        {
                                            VoucherNumbering = purchaseReturn.VoucherNumbering,
                                            VoucherNo = purchaseReturn.VoucherNo,
                                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                            DetailId = input.PurchaseAccount,
                                            Debit = input.ReturnType == ReturnType.RateDifference && input.DebitOrCreditNote
                                                ? detail.TaxAmount ?? 0
                                                : 0,
                                            Credit = input.ReturnType == ReturnType.RateDifference &&
                                                     input.DebitOrCreditNote
                                                ? 0
                                                : detail.TaxAmount ?? 0,
                                            InvoiceNo = input.VoucherNo,
                                            FinancialYearId = FinancialYearId,
                                            DateMiti = input.DateMiti,
                                            VoucherTypeId = voucherTypeId,
                                            LedgerId = tax.LedgerId,
                                            TenantId = tenantId,
                                            PostingNumber = postingNumbering,
                                            VendorVoucherNo =
                                                (purchaseMaster == null ? "" : purchaseMaster.VendorInvoiceNo) +
                                                (input.DebitOrCreditNote ? " CreditNote" : " DebitNote"),
                                            MasterId = input.Id
                                        };
                                        await ledgerPostingRepository.InsertAsync(ledger3);
                                    }
                            }
                        }
            }

            await unitOfWork.CompleteAsync();
            return purchaseReturn.Id;
        }


        public async Task UploadImageNew(IFormFile file, Guid purchaseReturnId)
        {
            var purchaseOrder = await purchaseReturnRepository.FirstOrDefaultAsync(x => x.Id == purchaseReturnId);
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseReturn");

            var fileType = Path.GetExtension(file.FileName);
            if (fileType != ".jpg" && fileType != ".jpeg" && fileType != ".png" && fileType != ".pdf")
                throw new UserFriendlyException($"This filetype '{fileType}' is not accepted.");

            var photo = new VoucherPhotos
            {
                VoucherNo = purchaseOrder.VoucherNo,
                VoucherNumbering = purchaseOrder.VoucherNumbering,
                FileName = file.FileName,
                FileType = fileType,
                FinancialYearId = FinancialYearId,
                VoucherTypeId = voucherTypeId,
                TenantId = tenantId
            };
            var id = await voucherPhotosRepository.InsertAndGetIdAsync(photo);

            var fileDetails = await voucherPhotosRepository.FirstOrDefaultAsync(x => x.Id == id);
            if (file == null) throw new UserFriendlyException("Please select upload file");

            var changedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            long kb = 1000;
            if (file.Length > 500 * kb)
                throw new UserFriendlyException("File size is large.");
            if (file.Length > 0)
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                var fileBytes = ms.ToArray();
                fileDetails.Image = fileBytes;
                var s = Convert.ToBase64String(fileBytes);
            }

            fileDetails.ChangedFileName = changedFileName;
            await voucherPhotosRepository.UpdateAsync(fileDetails);
        }

        [DisableAuditing]
        public async Task<List<DocumentDetailsDto>> GetAllDocuments(Guid purchaseReturnId)
        {
            var result = new List<DocumentDetailsDto>();
            var purchaseOrder = await purchaseReturnRepository.FirstOrDefaultAsync(purchaseReturnId);
            if (purchaseOrder == null)
                throw new UserFriendlyException("Data not found");
            result = await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.VoucherNo == purchaseOrder.VoucherNo &&
                            x.VoucherNumbering == purchaseOrder.VoucherNumbering &&
                            x.VoucherTypeId == purchaseOrder.VoucherTypeId &&
                            x.FinancialYearId == purchaseOrder.FinancialYearId).Include(x => x.VoucherTypeFk).AsNoTracking().Select(x =>
                    new DocumentDetailsDto
                    {
                        Id = x.Id,
                        FileName = x.FileName,
                        FileType = x.FileType,
                        VoucherNo = x.VoucherNo,
                        VoucherType = x.VoucherTypeFk.Name,
                        Image = null
                    }).ToListAsync();
            return result;
        }

        public async Task<DocumentDetailsDto> GetImage(Guid id)
        {
            var data = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Id == id).Include(x => x.VoucherTypeFk).ToListAsync()).FirstOrDefault();
            if (data != null) throw new UserFriendlyException("Data Not Found");

            var result = new DocumentDetailsDto
            {
                Id = data.Id,
                FileName = data.FileName,
                FileType = data.FileType,
                VoucherNo = data.VoucherNo,
                Image = data.Image
            };
            return result;
        }

        public async Task DeleteFile(Guid id)
        {
            await voucherPhotosRepository.DeleteAsync(id);
        }
    }
}
