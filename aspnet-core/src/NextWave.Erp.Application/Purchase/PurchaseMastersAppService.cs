using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.IO.Extensions;
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
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Purchase.Exporting;
using NextWave.Erp.Purchase.Importing;
using NextWave.Erp.Purchase.Pdf;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;


namespace NextWave.Erp.Purchase
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
    public class PurchaseMastersAppService(
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        IRepository<User, long> userRepository,
        IPurchaseMastersExcelExporter purchaseMastersExcelExporter,
        IRepository<NewPartyBalance, Guid> partyBalanceRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<PurchaseProductCancelMaster, Guid> purchaseProductCancelRepository,
        ProductAppService productAppService,
        StockManagementAppService stockManagementAppService,
        MaterialStockPostingService materialStockPostingService,
        IRepository<PurchaseProductCancelDetail, Guid> purchaseProoductCancelDetailRepository,
        IRepository<Tax, Guid> taxRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<VoucherPhotos, Guid> voucherPhotosRepository,
        IRepository<ImportTaxMaster, Guid> importTaxMasterRepository,
        PartyBalanceService partyBalanceService,
        IRepository<AdditionalCost, Guid> additionalCostRepository,
    //    IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<PurchaseOrderDetails, Guid> purchaseOrderDetailRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<PurchaseOrderMaster, Guid> purchaseOrderMasterRepository,
        IRepository<ImportTaxDetails, Guid> importTaxDetailsRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<PurchaseReturn, Guid> purchaserReturnRepository,
        IDocumentsAppService documentService,
        //IAppNotifier appNotifier,
        IBinaryObjectManager binaryObjectManager,
        IBackgroundJobManager backgroundJobManager,
        IRepository<UserBranch, Guid> userBranchRepository)
        : ErpAppServiceBase, IPurchaseMastersAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        [DisableAuditing]
        public async Task<PagedResultDto<PurchaseMastersForViewDto>> GetAll(GetAllUniversalMastersInput input)
        {
            if (!string.IsNullOrEmpty(input.Filter))
                input.Filter = input.Filter.Trim();
            var filteredPurchaseMasters = purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrEmpty(input.Filter),
                    x => x.AccountLedgerFk.Name.Contains(input.Filter) || x.VoucherNo.Contains(input.Filter))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.Date,
                    x.DateMiti,
                    x.VendorInvoiceNo,
                    //x.VendorInvoiceDate,
                    x.TotalTax,
                    x.GrandTotal,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name,
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredPurchaseMasters = filteredPurchaseMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var purchaseMasters = from o in pagedAndFilteredPurchaseMasters
                                  join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                      o.CreateUserId equals o6.Id into j6
                                  from s6 in j6.DefaultIfEmpty()
                                  join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                      o.UpdateUserId equals o5.Id into j5
                                  from s5 in j5.DefaultIfEmpty()
                                  select new PurchaseMastersForViewDto
                                  {
                                      VoucherNo = o.VoucherNo,
                                      DateMiti = o.DateMiti,
                                      VendorInvoiceNo = o.VendorInvoiceNo,
                                      //VendorInvoiceDate = o.VendorInvoiceDate,
                                      TotalTax = o.TotalTax,
                                      GrandTotal = o.GrandTotal,
                                      Id = o.Id,
                                      CreateUser = s6.Name ?? "",
                                      UpdateUser = s5.Name ?? "",
                                      LedgerName = o.LedgerName,
                                  };



            var totalCount = await filteredPurchaseMasters.CountAsync();
            return new PagedResultDto<PurchaseMastersForViewDto>(
                totalCount,
                await purchaseMasters.ToListAsync()
            );
        }



        [AbpAuthorize(AppPermissions.PagesPurchaseMastersEdit)]
        public async Task<CreateOrEditPurchaseMasterDto> GetPurchaseMasterForEdit(EntityDto<Guid> input)
        {
            // 1. Optimize main query with projections to reduce data transfer
            var purchaseMaster = await purchaseMasterRepository.GetAll()
                .Where(x => x.Id == input.Id && x.FinancialYearId == FinancialYearId)
                .Select(pm => new
                {
                    pm.Id,
                    pm.VoucherNo,
                    pm.DateMiti,
                    pm.VendorInvoiceNo,
                    //pm.VendorInvoiceDate,
                    pm.CreditPeriod,
                    pm.Narration,
                    pm.TotalTax,
                    //pm.InvoiceTypeEnum,
                    pm.AgainstId,
                    //pm.PpdNo,
                    pm.TotalAmount,
                    pm.TotalTaxableAmount,
                    pm.BillDiscount,
                    pm.GrandTotal,
                    pm.LrNo,
                    //pm.TotalCustomAmount,
                    //pm.CustomLedgerId,
                    //pm.TransportationCompany,
                    pm.PurchaseAccountId,
                    pm.LedgerId,
                    pm.PurchaseOrderMasterId,
                    pm.VoucherTypeId
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (purchaseMaster == null)
                throw new UserFriendlyException("Data not Found");


            // 3. Optimize purchase details query with projection and include fewer related entities
            var purchaseDetails = await purchaseDetailRepository.GetAll()
                .Include(e => e.ProductFk)
                .Where(x => x.PurchaseMasterId == input.Id && x.TenantId == AbpSession.TenantId)
                .Select(pd => new
                {
                    pd.Id,
                    pd.Qty,
                    pd.Rate,
                    pd.Discount,
                    //pd.CustomAmount,
                    pd.DiscountPercent,
                    pd.TaxAmount,
                    //pd.IsAllowSerailNo,
                    pd.GrossAmount,
                    //pd.ImportRate,
                    pd.NetAmount,
                    pd.Amount,
                    pd.PurchaseOrderDetailsId,
                    pd.ProductId,
                    pd.UnitId,
                    pd.AgainstDetalId,
                    pd.TaxId,
                })
                .AsNoTracking()
                .ToListAsync();


            // 5. Create output object without waiting for details processing
            var output = new CreateOrEditPurchaseMasterDto
            {
                Id = purchaseMaster.Id,
                VoucherNo = purchaseMaster.VoucherNo,
                DateMiti = purchaseMaster.DateMiti,
                VendorInvoiceNo = purchaseMaster.VendorInvoiceNo,
                //VendorInvoiceMiti = purchaseMaster.VendorInvoiceDate == null
                //    ? " "
                //    : DateConverter.ConvertToNepali((DateTime)purchaseMaster.VendorInvoiceDate),
                CreditPeriod = purchaseMaster.CreditPeriod,
                Narration = purchaseMaster.Narration,
                TotalTax = purchaseMaster.TotalTax,
                //InvoiceTypeEnum = purchaseMaster.InvoiceTypeEnum,
                AgainstId = purchaseMaster.AgainstId,
                //PpdNo = purchaseMaster.PpdNo,
                TotalAmount = purchaseMaster.TotalAmount,
                TaxableAmount = purchaseMaster.TotalTaxableAmount,
                BillDiscount = purchaseMaster.BillDiscount,
                GrandTotal = purchaseMaster.GrandTotal,
                LrNo = purchaseMaster.LrNo,
                //TotalCustomAmount = purchaseMaster.TotalCustomAmount,
                //CustomLedgerId = purchaseMaster.CustomLedgerId,
                //TransportationCompany = purchaseMaster.TransportationCompany,
                PurchaseAccountId = purchaseMaster.PurchaseAccountId,
                LedgerId = purchaseMaster.LedgerId,
                OrderOrReceiptId = purchaseMaster.AgainstId == PurchaseModeType.PurchaseOrder
                    ? purchaseMaster.PurchaseOrderMasterId : Guid.Empty,
            };

            // 6. Process purchase details in batches to reduce memory pressure
            var outputPurchaseDetail = new List<PurchaseDetailDto>();
            var voucherTypeId = purchaseMaster.VoucherTypeId;
            var voucherNo = purchaseMaster.VoucherNo;

            // 7. Preload common data to avoid per-item queries
            var productIds = purchaseDetails.Select(pd => pd.ProductId).Distinct().ToList();
            var taxIds = purchaseDetails.Select(pd => pd.TaxId).Distinct().ToList();
            var purchaseDetailIds = purchaseDetails.Select(pd => pd.Id).ToList();

            // 8. Batch load related data
            var taxes = await taxRepository.GetAll()
                .Where(t => taxIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Rate);

            var importTaxDetails = await importTaxDetailsRepository.GetAll()
                .Where(x => purchaseDetailIds.Contains(x.PurchaseDetailId))
                .Select(x => new
                {
                    x.Id,
                    x.PurchaseDetailId,
                    x.AccountLedgerId,
                    x.Amount,
                    x.Percent,
                    x.EffectLedgerId
                })
                .AsNoTracking()
                .ToListAsync();

            // 9. Execute unit list queries for each product in parallel
            var unitListTasks = new Dictionary<Guid, List<PurchaseReturnUnitsQtyDto>>();
            foreach (var productId in productIds)
                unitListTasks[productId] = await GetAllUnitQtyForTableDropdown(productId);


            // 11. Process each purchase detail with pre-loaded data
            foreach (var purchaseDetail in purchaseDetails)
            {
                var data1 = new PurchaseDetailDto();

                // Use pre-loaded unit list or wait for its completion
                var unitList = unitListTasks[purchaseDetail.ProductId];

                // Get tax rate from pre-loaded collection
                var taxRate = 0m;
                if (taxes.TryGetValue(purchaseDetail.TaxId, out var rate)) taxRate = rate / 100;

                data1.Id = purchaseDetail.Id;
                data1.Qty = purchaseDetail.Qty;
                //data1.ProductCode = purchaseDetail.ProductCode;
                data1.Rate = purchaseDetail.Rate;
                data1.Discount = purchaseDetail.Discount;
                //data1.CustomAmount = purchaseDetail.CustomAmount;
                data1.DiscountPercent = purchaseDetail.DiscountPercent;
                data1.TaxAmount = purchaseDetail.TaxAmount;
                data1.TaxValue = taxRate;
                data1.GrossAmount = purchaseDetail.GrossAmount;
                //data1.ImportRate = purchaseDetail.ImportRate;
                data1.NetAmount = purchaseDetail.NetAmount;
                data1.Amount = purchaseDetail.Amount;
                data1.OrderDetailId = output.AgainstId == PurchaseModeType.PurchaseOrder
                    ? purchaseDetail.PurchaseOrderDetailsId
                    : Guid.Empty;
                data1.ProductId = purchaseDetail.ProductId;
                data1.UnitId = purchaseDetail.UnitId;
                data1.OrderDetailId = purchaseDetail.AgainstDetalId;
                data1.TaxId = purchaseDetail.TaxId;
                //data1.UnitsList = unitList;

                // Get custom ledger list from pre-loaded collection
                //data1.CustomLedgerList = importTaxDetails
                //    .Where(x => x.PurchaseDetailId == purchaseDetail.Id)
                //    .Select(x => new CustomLedgerList
                //    {
                //        Id = x.Id,
                //        LedgerId = x.AccountLedgerId,
                //        Amount = x.Amount,
                //        Percent = x.Percent,
                //        EffectLedgerId = x.EffectLedgerId
                //    })
                //    .ToList();

                outputPurchaseDetail.Add(data1);
            }

            output.PurchaseDetail = outputPurchaseDetail;

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPurchaseMasterDto input)
        {
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            if (FinancialYear.FromDate > date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            if (FinancialYear.ToDate < date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");
            Guid masterId;
            if (input.Id != null && input.Id != Guid.Empty)
            {
                masterId = await Update(input);
                //if (await SettingManager.GetSettingValueForTenantAsync<bool>(AppSettings.ErpSettings.IsEmailSent,
                //        AbpSession.GetTenantId())) await SendEmail(masterId);
            }
            else
            {
                masterId = await Create(input);
                //if (await SettingManager.GetSettingValueForTenantAsync<bool>(AppSettings.ErpSettings.IsEmailSent,
                //        AbpSession.GetTenantId())) await SendEmail(masterId);
            }

            return masterId;
        }


        [AbpAuthorize(AppPermissions.PagesPurchaseMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var purchaser = await purchaseMasterRepository.FirstOrDefaultAsync(input.Id);
            var purchaseReturn = await purchaserReturnRepository.GetAllListAsync(x => x.PurchaseMasterId == input.Id);
            if (purchaseReturn.Count > 0)
            {
                var message = string.Join(',', purchaseReturn.Select(x => x.VoucherNo));
                throw new UserFriendlyException($"Purchase return reference Exists in VoucherNo {message}");
            }

            await partyBalanceService.DeletePurchaseEntryAsync(input.Id);
            await additionalCostRepository.DeleteAsync(x =>
                x.VoucherTypeId == purchaser.VoucherTypeId && x.VoucherNo == purchaser.VoucherNo &&
                x.FinancialYearId == purchaser.FinancialYearId && x.VoucherNumbering == purchaser.VoucherNumbering);

            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == purchaser.VoucherTypeId && x.FinancialYearId == purchaser.FinancialYearId &&
                x.VoucherNo == purchaser.VoucherNo && x.VoucherNumbering == purchaser.VoucherNumbering);

            var stockPostings = await stockPostingRepository.GetAll()
                .Where(x => x.VoucherTypeId == purchaser.VoucherTypeId &&
                            x.FinancialYearId == purchaser.FinancialYearId &&
                            x.VoucherNo == purchaser.VoucherNo &&
                            x.VoucherNumbering == purchaser.VoucherNumbering)
                .AsNoTracking()
                .ToListAsync();
            await materialStockPostingService.ReverseExistingStockPostingsAsync(stockPostings);

            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == purchaser.VoucherTypeId && x.FinancialYearId == purchaser.FinancialYearId &&
                x.VoucherNo == purchaser.VoucherNo && x.VoucherNumbering == purchaser.VoucherNumbering);

            var allPurchaseDetails = await purchaseDetailRepository.GetAll().Where(x => x.PurchaseMasterId == input.Id)
                .AsNoTracking().ToListAsync();

            var allPurchaseDetailIds = allPurchaseDetails.Select(x => x.Id).ToList();

            await importTaxDetailsRepository.DeleteAsync(x => allPurchaseDetailIds.Contains(x.PurchaseDetailId));
            await importTaxMasterRepository.DeleteAsync(x => x.PurchaseMasterId == input.Id);
            await purchaseDetailRepository.DeleteAsync(x => x.PurchaseMasterId == input.Id);
            await purchaseMasterRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPurchaseMastersToExcel(GetAllUniversalMastersInput input)
        {
            var filteredPurchaseMasters = purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk).Where(x => x.FinancialYearId == FinancialYearId);
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date <= date);
            }

            var query = from o in filteredPurchaseMasters.OrderBy(x => x.VoucherNumbering)
                        select new GetPurchaseMasterForViewDto
                        {
                            Date = o.Date,
                            VoucherNo = o.VoucherNo,
                            DateMiti = o.DateMiti,
                            VendorInvoiceNo = o.VendorInvoiceNo,
                            TotalAmount = o.TotalAmount,
                            //VendorInvoiceDate = o.VendorInvoiceDate,
                            TotalTax = o.TotalTax,
                            GrandTotal = o.GrandTotal,
                            BillDiscount = o.BillDiscount,
                            Narration = o.Narration,
                            Id = o.Id,
                            LedgerName = o.AccountLedgerFk.Name,
                        };

            var purchaseMasterListDtos = await query.ToListAsync();

            return purchaseMastersExcelExporter.ExportToFile(purchaseMasterListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllPurchaseOrderMasterForTableDropdown()
        {
            return await purchaseOrderMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(purchaseOrderMaster => new UniversalDropdownDto
                {
                    Id = purchaseOrderMaster.Id,
                    DisplayName = purchaseOrderMaster.Description == null
                        ? ""
                        : purchaseOrderMaster.Description.ToString()
                }).AsNoTracking().ToListAsync();
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            var cashLedgers = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Name == "Cash-in Hand")
                .AsNoTracking().Select(x => x.Id).ToListAsync();


            var cashLedgerIdList = new List<Guid>();
            foreach (var cash in cashLedgers)
                cashLedgerIdList.AddRange(await FuncRecursive(cash));

            var cashLedgerList = await accountLedgerRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => cashLedgerIdList.Contains(x.AccountGroupId))
                .Select(accountLedger => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    IsCash = true,
                    MobileNo = accountLedger.Phone,
                    CreditPeriod = accountLedger.CreditPeriod ?? 0,
                    Address = accountLedger.Address
                }).ToListAsync();


            var sundryCreditors = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Name == "Sundry Creditors" || x.Name == "Sundry Debtors")
                .AsNoTracking().Select(x => x.Id).ToListAsync();
            var sundryCreditorList = new List<Guid>();
            foreach (var sundryCreditor in sundryCreditors)
                sundryCreditorList.AddRange(await FuncRecursive(sundryCreditor));

            var sundryDebtorOrCreditor = await accountLedgerRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => sundryCreditorList.Contains(x.AccountGroupId))
                .Select(accountLedger => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    IsCash = false,
                    MobileNo = accountLedger.Phone,
                    CreditPeriod = accountLedger.CreditPeriod ?? 0,
                    Address = accountLedger.Address
                }).ToListAsync();


            cashLedgerList.AddRange(sundryDebtorOrCreditor);
            return cashLedgerList;
        }


        public async Task FixedPurchases()
        {
            await FixedPurchaseInvoiceError();
            await DeleteDuplicatePurchaseLedgerPostings();
            await PostMissingPurchaseInvoiceLedgerData();
            await PostMissingPurchaseInvoiceStockData();
        }

        [UnitOfWork]
        private async Task FixedPurchaseInvoiceError()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .Where(lp => !purchaseMasterRepository.GetAll()
                        .Any(pm => pm.VoucherTypeId == lp.VoucherTypeId &&
                                   pm.FinancialYearId == lp.FinancialYearId &&
                                   pm.VoucherNo == lp.VoucherNo &&
                                   pm.TenantId == lp.TenantId
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

        private async Task DeleteDuplicatePurchaseLedgerPostings()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
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
                Logger.Error($"Error deleting duplicate purchase ledger postings: {ex.Message}", ex);
            }
        }

        [UnitOfWork]
        public async Task PostMissingPurchaseInvoiceLedgerData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing ledger postings for purchase invoices");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
                var activeTax = await taxRepository.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Rate > 0);

                // Retrieve purchases with missing ledger postings via left join
                var unpostedPurchases = await (
                        from pm in purchaseMasterRepository.GetAll().AsNoTracking()
                            // Left join on ledger postings
                        join lp in ledgerPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                pm.TenantId,
                                pm.FinancialYearId,
                                pm.VoucherTypeId,
                                pm.VoucherNo,
                            }
                            equals new
                            {
                                lp.TenantId,
                                lp.FinancialYearId,
                                lp.VoucherTypeId,
                                lp.VoucherNo,
                            }
                            into ledgerGrp
                        from ledger in ledgerGrp.DefaultIfEmpty()
                        where
                            pm.TenantId == tenantId &&
                            pm.FinancialYearId == FinancialYearId &&
                            pm.VoucherTypeId == voucherTypeId &&
                            ledger == null // means no existing ledger posting
                        select new
                        {
                            pm.Id,
                            pm.TenantId,
                            pm.VoucherNumbering,
                            pm.DateMiti,
                            pm.Date,
                            pm.VoucherTypeId,
                            pm.VoucherNo,
                            pm.PurchaseAccountId,
                            pm.LedgerId,
                            pm.GrandTotal,
                            pm.TotalAmount,
                            pm.TotalTax,
                            pm.PostingNumbering,
                            pm.FinancialYearId
                        }
                    )
                    .ToListAsync();

                if (!unpostedPurchases.Any())
                {
                    Logger.Info("No purchase invoices with missing ledger postings found");
                    return;
                }

                Logger.Info($"Found {unpostedPurchases.Count} purchase invoices with missing ledger postings");

                // Prepare ledger postings
                var ledgerPostings = new List<LedgerPosting>();

                // Process each unposted purchase
                foreach (var purchase in unpostedPurchases)
                {
                    // Purchase account posting (Debit - increase in expense/asset)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = purchase.TenantId,
                        VoucherNumbering = purchase.VoucherNumbering,
                        Date = purchase.Date,
                        DateMiti = purchase.DateMiti,
                        VoucherTypeId = purchase.VoucherTypeId,
                        VoucherNo = purchase.VoucherNo,
                        LedgerId = purchase.PurchaseAccountId,
                        DetailId = purchase.LedgerId,
                        Debit = purchase.GrandTotal,
                        Credit = 0,
                        FinancialYearId = purchase.FinancialYearId,
                        InvoiceNo = purchase.VoucherNo,
                        PostingNumber = purchase.PostingNumbering,
                        MasterId = purchase.Id
                    });

                    // Tax posting if applicable (Debit - tax receivable/input tax)
                    if (purchase.TotalTax > 0 && activeTax != null)
                        ledgerPostings.Add(new LedgerPosting
                        {
                            TenantId = purchase.TenantId,
                            VoucherNumbering = purchase.VoucherNumbering,
                            Date = purchase.Date,
                            DateMiti = purchase.DateMiti,
                            VoucherTypeId = purchase.VoucherTypeId,
                            VoucherNo = purchase.VoucherNo,
                            LedgerId = activeTax.LedgerId,
                            DetailId = purchase.LedgerId,
                            Debit = purchase.TotalTax,
                            Credit = 0,
                            FinancialYearId = purchase.FinancialYearId,
                            InvoiceNo = purchase.VoucherNo,
                            PostingNumber = purchase.PostingNumbering,
                            MasterId = purchase.Id
                        });

                    // Supplier/Creditor posting (Credit - increase in liability)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = purchase.TenantId,
                        VoucherNumbering = purchase.VoucherNumbering,
                        Date = purchase.Date,
                        DateMiti = purchase.DateMiti,
                        VoucherTypeId = purchase.VoucherTypeId,
                        VoucherNo = purchase.VoucherNo,
                        LedgerId = purchase.LedgerId,
                        DetailId = purchase.PurchaseAccountId,
                        Debit = 0,
                        Credit = purchase.GrandTotal,
                        FinancialYearId = purchase.FinancialYearId,
                        InvoiceNo = purchase.VoucherNo,
                        PostingNumber = purchase.PostingNumbering,
                        MasterId = purchase.Id
                    });
                }

                // Bulk insert ledger postings
                if (ledgerPostings.Count > 0)
                {
                    await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);
                    await UnitOfWorkManager.Current.SaveChangesAsync();
                    //Logger.Info($"Successfully created {ledgerPostings.Count} ledger postings " +
                    //            $"for {unpostedPurchases.Count} purchase invoices");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting ledger data for purchase invoices: {ex.Message}", ex);
                throw;
            }
        }

        [UnitOfWork]
        public async Task PostMissingPurchaseInvoiceStockData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing stock postings for purchase invoices");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

                // Find purchase invoices that have details but no corresponding stock postings
                var purchasesWithMissingStockPostings = await (
                        from pm in purchaseMasterRepository.GetAll().AsNoTracking()
                        join pd in purchaseDetailRepository.GetAll().AsNoTracking()
                            on pm.Id equals pd.PurchaseMasterId
                        // Left join on stock postings for this specific purchase detail
                        join sp in stockPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                pm.TenantId,
                                pm.FinancialYearId,
                                pm.VoucherTypeId,
                                pm.VoucherNo,
                                pd.ProductId
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
                            pm.TenantId == tenantId &&
                            pm.FinancialYearId == FinancialYearId &&
                            pm.VoucherTypeId == voucherTypeId &&
                            stock == null && // means no existing stock posting
                            pd.ProductFk.ProductType != ProductTypeEnum.Services // Only process non-service products
                        select new
                        {
                            PurchaseMaster = pm,
                            PurchaseDetail = pd,
                            pd.ProductFk.ProductType,
                        }
                    )
                    .ToListAsync();

                if (!purchasesWithMissingStockPostings.Any())
                {
                    Logger.Info("No purchase invoices with missing stock postings found");
                    return;
                }

                Logger.Info(
                    $"Found {purchasesWithMissingStockPostings.Count} purchase details with missing stock postings");

                // Group by purchase master to process efficiently
                var groupedByPurchase = purchasesWithMissingStockPostings
                    .GroupBy(x => x.PurchaseMaster.Id)
                    .ToList();

                // Prepare collections for batch operations
                var stockPostingsToInsert = new List<StockPosting>();

                // Process each purchase invoice
                foreach (var purchaseGroup in groupedByPurchase)
                {
                    var purchaseMaster = purchaseGroup.First().PurchaseMaster;

                    // Delete any existing partial/erroneous stock data for this purchase invoice
                    await DeleteExistingPurchaseStockData(new DeleteStockDataDto
                    {
                        VoucherTypeId = purchaseMaster.VoucherTypeId,
                        FinancialYearId = purchaseMaster.FinancialYearId,
                        VoucherNo = purchaseMaster.VoucherNo,
                    });

                    // Process each purchase detail in this invoice
                    foreach (var item in purchaseGroup)
                    {
                        var purchaseDetail = item.PurchaseDetail;

                        // Create stock posting (Inward for purchase)
                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = purchaseMaster.VoucherNumbering,
                            Date = purchaseMaster.Date,
                            DateMiti = purchaseMaster.DateMiti,
                            LedgerId = purchaseMaster.LedgerId,
                            VoucherTypeId = purchaseMaster.VoucherTypeId,
                            VoucherNo = purchaseMaster.VoucherNo,
                            GrossAmount = purchaseDetail.GrossAmount,
                            DiscountAmount = purchaseDetail.Discount,
                            NetAmount = purchaseDetail.NetAmount,
                            Amount = purchaseDetail.Amount,
                            TaxAmount = purchaseDetail.TaxAmount,
                            IsValueIncrease = true, // Purchase increases inventory value
                            ProductId = purchaseDetail.ProductId,
                            UnitId = purchaseDetail.UnitId,
                            AgainstVoucherTypeId = Guid.Empty,
                            AgainstVoucherNo = "",
                            InWardQty = purchaseDetail.Qty, // Inward quantity for purchase
                            OutWardQty = 0,
                            Rate = purchaseDetail.Rate,
                            FinancialYearId = purchaseMaster.FinancialYearId,
                            MasterId = purchaseMaster.Id,
                            TenantId = tenantId
                        };

                        stockPostingsToInsert.Add(stockPosting);

                    }

                    // Update stock levels using the stock management service
                    foreach (var item in purchaseGroup)
                    {
                        var purchaseDetail = item.PurchaseDetail;

                        var stockManage = new StockMaintainDto
                        {
                            DateMiti = purchaseMaster.DateMiti,
                            ProductId = purchaseDetail.ProductId,
                            Qty = purchaseDetail.Qty,
                            Rate = purchaseDetail.Rate,
                            FinancialYearId = purchaseMaster.FinancialYearId,
                            Type = StockMaintainTypeEnum.Inward, // Inward for purchase
                            UnitId = purchaseDetail.UnitId,
                        };

                        try
                        {
                            await stockManagementAppService.MaintainStock(stockManage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not update stock levels for product {purchaseDetail.ProductId} " +
                                        $"in purchase {purchaseMaster.VoucherNo}: {ex.Message}");
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

                //Logger.Info($"Completed processing missing stock postings for {groupedByPurchase.Count} purchase invoices");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting stock data for purchase invoices: {ex.Message}", ex);
                throw;
            }
        }

        private async Task DeleteExistingPurchaseStockData(DeleteStockDataDto stockDataInfo)
        {
            // Delete any existing stock postings for this purchase invoice
            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == stockDataInfo.VoucherTypeId &&
                x.FinancialYearId == stockDataInfo.FinancialYearId &&
                x.VoucherNo == stockDataInfo.VoucherNo);

            // Note: For purchases, we typically don't delete IMEI entries during error fixing
            // since they represent physical inventory received. Only delete if absolutely necessary.
        }


        //protected async Task SendEmail(Guid id)
        //{
        //    var sales = await purchaseMasterRepository.GetAll()
        //        .Include(e => e.AccountLedgerFk)
        //        .FirstOrDefaultAsync(e => e.Id == id);
        //    var branchInfo = await branchRepository.FirstOrDefaultAsync(e => e.IsMain);

        //    try
        //    {
        //        if (sales != null)
        //        {
        //            var pdfBytes = await GetPdfdownload(id);
        //            if (ValidationHelper.IsEmail(sales.AccountLedgerFk.Email))
        //            {
        //                Stream stream = new MemoryStream(pdfBytes);
        //                var mailMessage = new StringBuilder();

        //                mailMessage.AppendLine("<b> Dear  " + sales.AccountLedgerFk.Name + ". </b>: " +
        //                                       "<p> Thank you so much for your Sales with us." +
        //                                       " We value your patronage and appreciate your trust in us. " +
        //                                       "We'll do our best to continue to give you the kind of service & " +
        //                                       "the product you deserve. We would love your feedback.</p>");
        //                mailMessage.AppendLine("<br />");


        //                var mail = new MailMessage
        //                {
        //                    Subject = "Purchase From" + sales.AccountLedgerFk.Name,
        //                    Body = mailMessage.ToString(),
        //                    IsBodyHtml = true,
        //                    To = { sales.AccountLedgerFk.Email },
        //                    From = new MailAddress(branchInfo.Email)
        //                };
        //                mail.Attachments.Add(new Attachment(stream, "Invoice.Pdf"));
        //                await emailSender.SendAsync(mail);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await emailSender.SendErrorAsync("Purchase Sendmail", ex.Message);
        //    }
        //}


        public async Task<bool> IsPaymentMade(string voucherNo)
        {
            var paymentVoucher = await voucherTypeRepository.FirstOrDefaultAsync(x => x.Name == "PaymentVoucher");
            var data = await partyBalanceRepository.GetAll().AsNoTracking().Where(x =>
                x.AgainstVoucherTypeId == paymentVoucher.Id && x.VoucherTypeFk.Name == "PurchaseInvoice" &&
                x.VoucherNo == voucherNo).ToListAsync();
            if (data.Count > 0)
                return true;
            return false;
        }

        public async Task<List<VoucherDublicateDto>> GetPurchaseVoucherDublicate()
        {
            var list = new List<VoucherDublicateDto>();
            await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.PurchaseMasterFk).ToListAsync();
            foreach (var item in await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                         .Where(x => x.FinancialYearId == FinancialYearId).ToListAsync())
            {
                var stockposting = await ledgerPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x =>
                        x.VoucherTypeId == item.VoucherTypeId && x.VoucherNo == item.VoucherNo &&
                        x.FinancialYearId == item.FinancialYearId).AsNoTracking().ToListAsync();
                if (stockposting.Any())
                    if (stockposting.Sum(x => x.Debit) != stockposting.Sum(x => x.Credit))
                        list.Add(new VoucherDublicateDto
                        {
                            Id = item.Id,
                            Debit = stockposting.Sum(x => x.Debit),
                            Credit = stockposting.Sum(x => x.Credit),
                            VoucherNo = item.VendorInvoiceNo,
                            GrandTotal = item.GrandTotal
                        });
            }

            return list;
        }

        protected virtual async Task Updatecode()
        {
            var i = 0;
            foreach (var purchaseMaster in await purchaseMasterRepository.GetAll()
                         .Where(x => x.TenantId == AbpSession.TenantId).OrderBy(x => x.Date.Date).ToListAsync())
            {
                var tenantId = AbpSession.TenantId;
                if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

                if (purchaseMaster != null)
                {
                    i += 1;
                    var accountLedgerDetails =
                        await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseMaster.LedgerId);


                    var ledger = new LedgerPosting
                    {
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        Date = purchaseMaster.Date,
                        VoucherNo = purchaseMaster.VoucherNo,
                        DetailId = purchaseMaster.PurchaseAccountId,
                        Debit = purchaseMaster.TotalAmount,
                        Credit = 0,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        FinancialYearId = purchaseMaster.FinancialYearId,
                        DateMiti = purchaseMaster.DateMiti,
                        VoucherTypeId = purchaseMaster.VoucherTypeId,
                        LedgerId = purchaseMaster.PurchaseAccountId,
                        TenantId = tenantId,
                        VendorVoucherNo = purchaseMaster.VendorInvoiceNo,
                        MasterId = purchaseMaster.Id,
                        PostingNumber = i
                    };
                    await ledgerPostingRepository.InsertAsync(ledger);

                    var ledger1 = new LedgerPosting
                    {
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        Date = purchaseMaster.Date,
                        VoucherNo = purchaseMaster.VoucherNo,
                        DetailId = purchaseMaster.PurchaseAccountId,
                        Debit = 0,
                        Credit = purchaseMaster.GrandTotal,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        FinancialYearId = purchaseMaster.FinancialYearId,
                        DateMiti = purchaseMaster.DateMiti,
                        VoucherTypeId = purchaseMaster.VoucherTypeId,
                        LedgerId = purchaseMaster.LedgerId,
                        TenantId = tenantId,
                        VendorVoucherNo = purchaseMaster.VendorInvoiceNo,
                        MasterId = purchaseMaster.Id,
                        PostingNumber = i
                    };
                    await ledgerPostingRepository.InsertAsync(ledger1);

                    foreach (var detail in await purchaseDetailRepository.GetAllListAsync(x =>
                                 x.PurchaseMasterId == purchaseMaster.Id))
                    {
                        if (detail.TaxId != Guid.Empty)
                        {
                            var taxLedgerId = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                            var ledgerDetail1 = new LedgerPosting
                            {
                                Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                                VoucherNo = purchaseMaster.VoucherNo,
                                DetailId = purchaseMaster.PurchaseAccountId,
                                Debit = detail.TaxAmount,
                                Credit = 0,
                                InvoiceNo = purchaseMaster.VoucherNo,
                                DateMiti = purchaseMaster.DateMiti,
                                FinancialYearId = purchaseMaster.FinancialYearId,
                                VoucherTypeId = purchaseMaster.VoucherTypeId,
                                LedgerId = taxLedgerId.LedgerId,
                                TenantId = tenantId,
                                VendorVoucherNo = purchaseMaster.VendorInvoiceNo,
                                MasterId = purchaseMaster.Id,
                                PostingNumber = i
                            };
                            await ledgerPostingRepository.InsertAsync(ledgerDetail1);
                        }

                        var productData = await productRepository.FirstOrDefaultAsync(x => x.Id == detail.ProductId);
                        if (productData.ProductType != ProductTypeEnum.Services)
                        {
                            var stockPosting = new StockPosting
                            {
                                Date = purchaseMaster.Date,
                                VoucherTypeId = purchaseMaster.VoucherTypeId,
                                VoucherNo = purchaseMaster.VoucherNo,
                                ProductId = detail.ProductId,
                                GrossAmount = detail.GrossAmount,
                                DiscountAmount = detail.Discount,
                                NetAmount = detail.NetAmount,
                                Amount = detail.Amount,
                                UnitId = detail.UnitId,
                                AgainstVoucherTypeId = Guid.Empty,
                                AgainstVoucherNo = "",
                                InWardQty = detail.Qty,
                                OutWardQty = 0,
                                Rate = detail.Rate,
                                TaxAmount = detail.TaxAmount,
                                FinancialYearId = purchaseMaster.FinancialYearId,
                                TenantId = tenantId,
                                MasterId = purchaseMaster.Id,
                                DateMiti = purchaseMaster.DateMiti,
                                VoucherNumbering = purchaseMaster.VoucherNumbering,
                                VendorVoucherNo = ""
                            };
                            await stockPostingRepository.InsertAsync(stockPosting);
                        }
                    }
                }
            }
        }


        public async Task CreateOrUpdateFile(string voucherNo)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
            var purchaseMaster = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == voucherNo &&
                x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = purchaseMaster.VoucherTypeId,
                VoucherNo = purchaseMaster.VoucherNo
            };
            await documentService.Create(input);
        }

        public async Task<GetPurchaseMasterForViewNewDto> GetPurchaseMasterForViewNew(Guid id)
        {
            var purchase = (await purchaseMasterRepository.GetAll().Where(x => x.Id == id)
                .Include(x => x.AccountLedgerFk).AsNoTracking().ToListAsync()).FirstOrDefault();

            if (purchase == null) throw new UserFriendlyException("Data not found");

            var result = new GetPurchaseMasterForViewNewDto
            {
                Id = purchase.Id,
                VoucherNo = purchase.VoucherNo,
                Date = purchase.DateMiti,
                GrossAmount = purchase.TotalAmount,
                DiscountAmount = purchase.BillDiscount,
                NetAmount = purchase.TotalAmount - purchase.BillDiscount,
                VendorInvoiceNo = purchase.VendorInvoiceNo,
                LedgerName = purchase.AccountLedgerFk.Name,
                TaxAmount = purchase.TotalTax,
                TaxableAmount = purchase.TotalTaxableAmount,
                TotalAmount = purchase.GrandTotal,
                Details = await purchaseDetailRepository.GetAll().Where(x => x.PurchaseMasterId == id)
                    .Include(x => x.TaxFk).Include(x => x.ProductFk).Include(x => x.UnitFk)
                    .Select(x => new GetPurchaseInvoiceForViewDetailDto
                    {
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Discount = x.Discount,
                        TaxName = x.TaxFk.Name,
                        TaxAmount = x.TaxAmount,
                        GrossAmount = x.GrossAmount,
                        NetAmount = x.NetAmount,
                        ProductId = x.ProductId,
                        TotalAmount = x.Amount,
                        ProductName = x.ProductFk.Name,
                        UnitName = x.UnitFk.Name
                    }).AsNoTracking().ToListAsync()
            };

            return result;
        }

        public async Task<PdfForPurchaseMasterModel> GetPurchaseMasterForPdf(Guid id)
        {
            var purchaseMaster = await purchaseMasterRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (purchaseMaster != null)
            {
                var branchData =
                    await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
                var result = new PdfForPurchaseMasterModel
                {
                    BranchName = branchData.CompanyName,
                    Date = purchaseMaster.Date,
                    Logo1 = branchData.Image1,
                    Address = branchData.Address,
                    Province = branchData.State.ToString(),
                    DateMiti = DateConverter.ConvertToNepali(purchaseMaster.Date),
                    BranchPan = branchData.PANumber,
                    BranchPhone = branchData.PhoneNo1,
                    Tin = 0,
                    BranchContact = branchData.PhoneNo1 + " /" + branchData.PhoneNo2,
                    Pan = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseMaster.LedgerId))
                        .Pan,
                    BranchAddress = branchData.Address,
                    VoucherTypeName =
                        (await voucherTypeRepository.FirstOrDefaultAsync(x => x.Id == purchaseMaster.VoucherTypeId)).Name,
                    OrderNo = purchaseMaster.VoucherNo,
                    VendorInvoiceNo = purchaseMaster.VendorInvoiceNo,
                    LedgerName =
                        (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseMaster.LedgerId))
                        .Name,
                    PurchaseDetail = null,
                    TotalAmountInWord = CurrencyToAmount.AmountWords(purchaseMaster.GrandTotal),
                    TotalAmount = purchaseMaster.TotalAmount,
                    BillDiscount = purchaseMaster.BillDiscount,
                    TaxableAmount = purchaseMaster.TotalTaxableAmount,
                    VatAmount = purchaseMaster.TotalTax,
                    GrandTotal = purchaseMaster.GrandTotal,
                    Description = purchaseMaster.Narration,
                    ApprovedBy = null,
                    ReceivedBy = null
                };
                var serial = 1;
                result.PurchaseDetail =
                    (await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.PurchaseMasterId == id)
                        .Include(x => x.ProductFk).Include(x => x.UnitFk)
                        .AsNoTracking().ToListAsync()).Select(x =>
                        new PdfForPurchaseMasterDetailModel
                        {
                            SlNo = serial++,
                            HsCode = x.ProductFk.HsCode,
                            ProductName = x.ProductFk.Name,
                            Quantity = x.Qty,
                            Unit = x.UnitFk.Name,
                            Rate = x.Rate,
                            GrossAmount = x.GrossAmount
                        }).ToList();
                return result;
            }

            throw new UserFriendlyException("Data not found");
        }


        //[AbpAuthorize(AppPermissions.PagesPurchaseMastersPrint)]
        //public async Task<byte[]> GetPdfdownload(Guid id)
        //{
        //    //var filePath = "PurchaseMaster.pdf";
        //    var model = await GetPurchaseMasterForPdf(id);
        //    var list = new List<PdfForPurchaseMasterModel> { model };
        //    var document = new PurchaseMasterPdf(list);
        //    return document.GeneratePdf();
        //}

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            if (await purchaseMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0)
                return true;
            return false;
        }

        [DisableAuditing]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnitQtyForTableDropdown(Guid productId)
        {
            var result = new List<PurchaseReturnUnitsQtyDto>();
            // 1. Use combined query for better performance
            var stock = await stockPostingRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
                .SumAsync(x => x.InWardQty - x.OutWardQty);

            var product = (await productRepository.GetAll().Include(x => x.UnitFk).Where(x => x.Id == productId).ToListAsync()).FirstOrDefault();
            result.Add(new PurchaseReturnUnitsQtyDto
            {
                ProductId = productId,
                Qty = stock,
                Rate = product.PurchaseRate,
                UnitId = product.UnitId,
                UnitName = product.UnitFk.Name
            });
            return result;
            //var unitConversion = await unitConversionRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
            //    .Include(x => x.UnitFk)
            //    .AsNoTracking()
            //    .ToListAsync();

            // 2. Handle edge case - if no unit conversions found
            //if (!unitConversion.Any())
            //    return [];

            //// 3. Get minimum conversion rate unit once
            //var minUnit = unitConversion.OrderBy(x => x.ConversionRate).First();

            // 4. Process stock quantities more efficiently
            //decimal minUnitQty = 0;
            //var stockByUnit = stockPosting.GroupBy(x => x.UnitId)
            //    .OrderBy(x => x.Key)
            //    .ToList();

            // 5. Precompute conversion factors in a single pass
          //  var unitConversionDict = unitConversion.ToDictionary(x => x.UnitId);

            // 6. Calculate total quantity in minimum unit
            //foreach (var stockGroup in stockByUnit)
            //{
            //    if (!unitConversionDict.TryGetValue(stockGroup.Key, out var thisUnit)) continue;
            //    var netQty = stockGroup.Sum(x => x.InWardQty - x.OutWardQty);
            //    // Convert to minimum unit using conversion factors
            //    minUnitQty += netQty * thisUnit.PrimaryQty / thisUnit.Qty * minUnit.Qty / minUnit.PrimaryQty;
            //}

            // 7. Get product in the same query to avoid additional database round-trip
            //var product = await productRepository.FirstOrDefaultAsync(x => x.Id == productId);
            //if (product == null)
            //    return [];

            //// 8. Create and return the result
            //return unitConversion.Select(unit => new PurchaseReturnUnitsQtyDto
            //{
            //    UnitId = unit.UnitId,
            //    UnitName = unit.UnitFk?.Name ?? string.Empty,
            //    Rate = product.PurchaseRate * unit.PrimaryQty / unit.Qty,
            //    Qty = minUnitQty * unit.Qty / unit.PrimaryQty * minUnit.PrimaryQty / minUnit.Qty
            //}).ToList();
        }


        [DisableAuditing]
        public async Task<List<PurchaseMasterUnitTableDto>> GetAllUnits()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(unit => new PurchaseMasterUnitTableDto
                {
                    Id = unit.Id,
                    DisplayName = unit.Name == null ? "" : unit.Name.ToString(),
                    Rate = 0
                }).AsNoTracking().ToListAsync();
        }


        public async Task ImportPurchaseMasterFromExcel(IFormFile file)
        {
            if (file == null) throw new UserFriendlyException(L("File_Empty_Error"));
            if (file.Length > 1048576 * 100)
                throw new UserFriendlyException(L("File_Empty_Error"));

            byte[] fileBytes;
            await using (var stream = file.OpenReadStream())
            {
                fileBytes = stream.GetAllBytes();
            }

            var tenantId = AbpSession.TenantId;
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.UtcNow} import from excel file.");
            await BinaryObjectManager.SaveAsync(fileObject);

            await BackgroundJobManager.EnqueueAsync<ImportPurchaseMasterToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                });
        }

        private async Task<List<Guid>> FuncRecursive(Guid id)
        {
            var result = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.GroupUnder == id).Select(x => x.Id).ToListAsync();
            var groupList = new List<Guid>();
            foreach (var i in result)
            {
                var groupListnew = await FuncRecursive(i);
                groupList.AddRange(groupListnew);
            }

            groupList.Add(id);
            groupList.Sort();
            return groupList;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllPurchaseAccountForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Purchase Account")
                .Select(accountLedger => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllProductForTableDropdown()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }


        public async Task<GetProductForViewDto> GetProductForView(Guid id)
        {
            return await productAppService.GetProductForView(id);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllUnitForTableDropdown()
        {
            return (await unitRepository.GetAll().Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = x.Name
            }).AsNoTracking().ToListAsync());
            //var product = await productRepository.FirstOrDefaultAsync(x => x.Id == productId);
            //return await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Include(x => x.UnitFk).Where(x => x.ProductId == productId)
            //    .Select(unit => new UniversalDropdownDto
            //    {
            //        Id = unit.UnitId,
            //        DisplayName = unit.UnitFk.Name == null ? "" : unit.UnitFk.Name.ToString(),
            //    //    Rate = product.PurchaseRate * unit.PrimaryQty / unit.Qty
            //    }).AsNoTracking().ToListAsync();
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<PurchaseMasterTaxTableDto>> GetAllTaxForTableDropdown()
        {
            return await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(godOwn => new PurchaseMasterTaxTableDto
                {
                    Id = godOwn.Id,
                    Name = godOwn.Name == null ? "" : godOwn.Name.ToString(),
                    Rate = godOwn.Rate
                }).AsNoTracking().ToListAsync();
        }



        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(x => x.AccountGroupFk).Where(x =>
                    x.AccountGroupFk.Name == "Cash-in Hand" || x.AccountGroupFk.Name == "Bank Account" ||
                    x.AccountGroupFk.Name == "Bank OD A/C")
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllExpensesLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Misc.Expenses (ASSET)" ||
                            x.AccountGroupFk.Name == "Direct Expenses" || x.AccountGroupFk.Name == "Indirect Expenses")
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }


        public async Task<string> GetPurchaseInvoiceVoucherNo()
        {
            var data = await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseInvoice");
            if (data.Count == 0)
                return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;
            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        private decimal GetTaxRate(Guid taxId)
        {
            var tax = taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .FirstOrDefault(x => x.Id == taxId);
            if (tax == null || tax.Rate == 0)
                return 0;
            return tax.Rate / 100;
        }



        protected async Task<List<PurchaseDetailDto>> MissingProductLists(Guid receiptNumber,
            PurchaseModeType purchaseModeType)
        {
            var purchaseMasters = await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.PurchaseOrderMasterId == receiptNumber).ToListAsync();

            var existingProduct = new List<ExistingProductList>();
            foreach (var purchaseMaster in purchaseMasters)
            {
                var purchaseDetails = (await purchaseDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                    .Where(x => x.PurchaseMasterId == purchaseMaster.Id).ToListAsync()).Select(x => new ExistingProductList
                    {
                        ProductId = x.ProductId,
                        Qty = x.Qty,
                        Amount = x.Amount
                    }).ToList();
                if (purchaseDetails.Count > 0)
                    existingProduct.AddRange(purchaseDetails);
            }

            var cancelMasters = await purchaseProductCancelRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Where(x => x.PurchaseOrderId == receiptNumber).ToListAsync();

            foreach (var cancelMaster in cancelMasters)
            {
                var cancelDetails = (await purchaseProoductCancelDetailRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                        .Where(x => x.PurchaseProductCancelMasterId == cancelMaster.Id).ToListAsync())
                    .Select(x => new ExistingProductList
                    {
                        ProductId = x.ProductId,
                        Qty = x.Qty,
                        Amount = 0
                    }).ToList();
                if (cancelDetails.Count > 0)
                    existingProduct.AddRange(cancelDetails);
            }

            await VoucherTypeManager.GetVoucherTypeId("PurchaseOrder");
            var result = new List<PurchaseDetailDto>();
            var details = await purchaseOrderDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(x => x.PurchaseOrderMasterFk).Include(x => x.ProductFk)
                .Where(x => x.PurchaseOrderMasterId == receiptNumber).ToListAsync();
            foreach (var x in details)
            {
                var data = new PurchaseDetailDto
                {
                    Id = x.Id,
                    Qty = x.Qty - existingProduct.Where(y => y.ProductId == x.ProductId).Select(z => z.Qty).Sum(),
                    Discount = 0,
                    TaxAmount = 0,
                    GrossAmount = 0,
                    NetAmount = 0,
                    Amount = x.Amount - existingProduct.Where(y => y.ProductId == x.ProductId).Select(z => z.Amount).Sum(),
                    OrderDetailId = x.Id,
                    ProductId = x.ProductId,
                    //ProductCode = x.ProductCode,
                    UnitId = x.UnitId,
                    TaxId = x.ProductFk.TaxId,
                    TaxValue = GetTaxRate(x.ProductFk.TaxId),
                    Rate = x.Rate
                };
                if (data.Qty > 0)
                    result.Add(data);
            }

            return result;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            if (data.Count == 0)
            {
                var voucherNumbering =
                    await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseInvoice");
                return voucherNumbering.StartIndex;
            }

            return data.Max() + 1;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PurchaseInvoice");
        }


        [AbpAuthorize(AppPermissions.PagesPurchaseMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPurchaseMasterDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            //if (input.AgainstId == PurchaseModeType.MaterialReceipt)
            //{
            //    var errorMessage = await CheckValidationMaterialReceipt(input);
            //    if (errorMessage.Length > 0) throw new UserFriendlyException("Qty Validation ", errorMessage);
            //}

            //if (input.InvoiceTypeEnum == InvoiceTypeEnum.ImportInvoice &&
            //    (input.CustomLedgerId == null || input.CustomLedgerId == Guid.Empty))
            //    throw new UserFriendlyException("Custom/Import Ledger not Selected");

            var objNumbering = PostingNumbering;
            //if (id != Guid.Empty)
            //{
            //    var purchaseMasterData = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == id);
            //    if (purchaseMasterData != null)
            //        switch (upDown)
            //        {
            //            case UpDownType.Up:
            //                {
            //                    if (purchaseMasterData.PostingNumbering != null)
            //                        objNumbering = (decimal)purchaseMasterData.PostingNumbering - (decimal)0.001;
            //                    break;
            //                }
            //            case UpDownType.Down:
            //                {
            //                    if (purchaseMasterData.PostingNumbering != null)
            //                        objNumbering = (decimal)purchaseMasterData.PostingNumbering + (decimal)0.001;
            //                    break;
            //                }
            //        }
            //}

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
            var tenantId = AbpSession.GetTenantId();
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetPurchaseInvoiceVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await purchaseMasterRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PurchaseInvoice VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var purchaseMaster = new PurchaseMaster
            {
                VoucherNo = voucherNo,
                VoucherNumbering = voucherNumbering,
                Date = date,
                DateMiti = input.DateMiti,
                CreditDate = input.CreditPeriod == null
                    ? DateConverter.ConvertToEnglish(input.DateMiti)
                    : DateConverter.ConvertToEnglish(input.DateMiti).AddDays((double)input.CreditPeriod),
                VendorInvoiceNo = input.VendorInvoiceNo,
                //VendorInvoiceDate = DateConverter.ConvertToEnglish(input.VendorInvoiceMiti),
                CreditPeriod = input.CreditPeriod ?? 0,
                //InvoiceTypeEnum = input.InvoiceTypeEnum,
                Narration = input.Narration,
                //TotalCustomAmount = input.TotalCustomAmount,
                TotalTax = input.TotalTax,
                TotalTaxableAmount = input.PurchaseDetail.Where(x => x.TaxAmount > 0).Sum(x => x.NetAmount ?? 0),
                //PpdNo = input.PpdNo,
                //CustomLedgerId = input.CustomLedgerId,
                TotalAmount = input.TotalAmount,
                BillDiscount = input.BillDiscount,
                GrandTotal = input.GrandTotal,
                AgainstId = input.AgainstId,
                LrNo = input.LrNo,
                //TransportationCompany = input.TransportationCompany,
                VoucherTypeId = voucherTypeId,
                PurchaseAccountId = input.PurchaseAccountId,
                FinancialYearId = FinancialYearId,
                LedgerId = input.LedgerId,
                PurchaseOrderMasterId =
                    input.AgainstId == PurchaseModeType.PurchaseOrder ? input.OrderOrReceiptId : null,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                TenantId = tenantId,
                PostingNumbering = objNumbering
            };
            var masterId = await purchaseMasterRepository.InsertAndGetIdAsync(purchaseMaster);

            var importTaxId = Guid.Empty;
            //if (input.InvoiceTypeEnum == InvoiceTypeEnum.ImportInvoice)
            //{
            //    var taxDet = input.PurchaseDetail;
            //    var taxCustomAmt = taxDet.Sum(t => t.CustomLedgerList.Sum(x => x.Amount));
            //    var importTax = new ImportTaxMaster
            //    {
            //        DateMiti = input.DateMiti,
            //        Date = DateConverter.ConvertToEnglish(input.DateMiti),
            //        TotalImportTax = taxCustomAmt,
            //        PurchaseVoucherNo = voucherNo,
            //        BranchId = input.BranchId,
            //        PurchaseMasterId = masterId,
            //        FinancialYearId = FinancialYear.Id,
            //        TenantId = tenantId
            //    };
            //    importTaxId = await importTaxMasterRepository.InsertAndGetIdAsync(importTax);
            //    var objCustomLedgerLists = new List<CustomLedgerList>();
            //    var customLedgers = input.PurchaseDetail.Select(x => x.CustomLedgerList);
            //    foreach (var objDetail in customLedgers) objCustomLedgerLists.AddRange(objDetail);

            //    foreach (var ledgerId in objCustomLedgerLists.Select(x => x.LedgerId).Distinct())
            //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
            //        {
            //            VoucherNumbering = voucherNumbering,
            //            TenantId = tenantId,
            //            Date = date,
            //            DateMiti = input.DateMiti,
            //            VoucherTypeId = voucherTypeId,
            //            VoucherNo = voucherNo,
            //            VendorVoucherNo = input.VendorInvoiceNo,
            //            LedgerId = ledgerId,
            //            DetailId = input.PurchaseAccountId,
            //            MasterId = masterId,
            //            Debit = objCustomLedgerLists.Where(x => x.LedgerId == ledgerId).Sum(x => x.Amount),
            //            FinancialYearId = FinancialYearId,
            //            Credit = 0,
            //            InvoiceNo = voucherNo,
            //            PostingNumber = objNumbering
            //        });

            //    foreach (var ledgerId in objCustomLedgerLists.Select(x => x.EffectLedgerId).Distinct())
            //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
            //        {
            //            VoucherNumbering = voucherNumbering,
            //            VoucherNo = voucherNo,
            //            Date = date,
            //            DetailId = input.PurchaseAccountId,
            //            Debit = 0,
            //            Credit = objCustomLedgerLists.Where(x => x.EffectLedgerId == ledgerId).Sum(x => x.Amount),
            //            InvoiceNo = voucherNo,
            //            FinancialYearId = FinancialYearId,
            //            DateMiti = input.DateMiti,
            //            VoucherTypeId = voucherTypeId,
            //            LedgerId = ledgerId,
            //            TenantId = tenantId,
            //            PostingNumber = objNumbering,
            //            MasterId = masterId,
            //            VendorVoucherNo = input.VendorInvoiceNo
            //        });

            //    //Vat posting for customLedger
            //    if (input.CustomLedgerId != null && input.CustomLedgerId != Guid.Empty)
            //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
            //        {
            //            VoucherNumbering = voucherNumbering,
            //            VoucherNo = voucherNo,
            //            Date = date,
            //            DetailId = input.PurchaseAccountId,
            //            Debit = 0,
            //            Credit = input.TotalTax,
            //            InvoiceNo = voucherNo,
            //            FinancialYearId = FinancialYearId,
            //            DateMiti = input.DateMiti,
            //            VoucherTypeId = voucherTypeId,
            //            LedgerId = input.CustomLedgerId ?? Guid.Empty,
            //            TenantId = tenantId,
            //            PostingNumber = objNumbering,
            //            VendorVoucherNo = input.VendorInvoiceNo,
            //            MasterId = masterId
            //        });
            //}


            var objAccountLedger =
                await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);
            if (objAccountLedger != null)
                if (objAccountLedger.IsBillByBill)
                    await partyBalanceService.CreatePurchaseEntryAsync(new PartyBalanceNewEntryDto
                    {
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DueDate = DateConverter.ConvertToEnglish(input.DateMiti).AddDays((int)input.CreditPeriod),
                        LedgerId = input.LedgerId,
                        VoucherNo = purchaseMaster.VoucherNo,
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        Amount = input.GrandTotal,
                        MasterId = masterId
                    });
            //if (input.InvoiceTypeEnum == InvoiceTypeEnum.ImportInvoice)
            //{
            //    var ledgerAmt = input.PurchaseDetail.Sum(x => x.ImportRate * x.Qty ?? 0);
            //    await ledgerPostingRepository.InsertAsync(new LedgerPosting
            //    {
            //        VoucherNumbering = voucherNumbering,
            //        VoucherNo = voucherNo,
            //        Date = date,
            //        DetailId = input.LedgerId,
            //        Debit = ledgerAmt,
            //        Credit = 0,
            //        InvoiceNo = voucherNo,
            //        FinancialYearId = FinancialYearId,
            //        DateMiti = input.DateMiti,
            //        VoucherTypeId = voucherTypeId,
            //        LedgerId = input.PurchaseAccountId,
            //        TenantId = tenantId,
            //        PostingNumber = objNumbering,
            //        MasterId = masterId,
            //        VendorVoucherNo = input.VendorInvoiceNo
            //    });

            //    //PartyLedger Posting
            //    await ledgerPostingRepository.InsertAsync(new LedgerPosting
            //    {
            //        VoucherNumbering = voucherNumbering,
            //        VoucherNo = voucherNo,
            //        Date = date,
            //        DetailId = input.PurchaseAccountId,
            //        Debit = 0,
            //        Credit = ledgerAmt,
            //        InvoiceNo = voucherNo,
            //        FinancialYearId = FinancialYearId,
            //        DateMiti = input.DateMiti,
            //        VoucherTypeId = voucherTypeId,
            //        LedgerId = input.LedgerId,
            //        TenantId = tenantId,
            //        PostingNumber = objNumbering,
            //        VendorVoucherNo = input.VendorInvoiceNo,
            //        MasterId = masterId
            //    });
            //}
            //else
            //{
            var partyAmt = input.PurchaseDetail.Sum(x => x.Rate * x.Qty ?? 0) +
                           input.PurchaseDetail.Sum(x => x.TaxAmount ?? 0) -
                           input.PurchaseDetail.Sum(x => x.Discount ?? 0);

            var purchaseAmt = input.PurchaseDetail.Sum(x => x.Rate * x.Qty ?? 0);

            var discountAmt = input.PurchaseDetail.Sum(x => x.Discount ?? 0);
            if (discountAmt > 0)
            {
                var discountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "Discount Received");
                await ledgerPostingRepository.InsertAsync(new LedgerPosting
                {
                    VoucherNumbering = voucherNumbering,
                    VoucherNo = voucherNo,
                    Date = date,
                    DetailId = input.LedgerId,
                    Debit = 0,
                    Credit = discountAmt,
                    InvoiceNo = voucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = discountLedger.Id,
                    TenantId = tenantId,
                    PostingNumber = objNumbering,
                    MasterId = masterId,
                    VendorVoucherNo = input.VendorInvoiceNo
                });
            }

            await ledgerPostingRepository.InsertAsync(new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = date,
                DetailId = input.LedgerId,
                Debit = purchaseAmt,
                Credit = 0,
                InvoiceNo = voucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.PurchaseAccountId,
                TenantId = tenantId,
                PostingNumber = objNumbering,
                MasterId = masterId,
                VendorVoucherNo = input.VendorInvoiceNo
            });

            //PartyLedger Posting
            await ledgerPostingRepository.InsertAsync(new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = date,
                DetailId = input.PurchaseAccountId,
                Debit = 0,
                Credit = partyAmt,
                InvoiceNo = voucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                TenantId = tenantId,
                PostingNumber = objNumbering,
                VendorVoucherNo = input.VendorInvoiceNo,
                MasterId = masterId
            });
            //}

            foreach (var detail in input.PurchaseDetail)
            {
                await materialStockPostingService.EnsurePurchaseLineAllowedAsync(detail.ProductId);

                var purchaseDetailId = await purchaseDetailRepository.InsertAndGetIdAsync(new PurchaseDetail
                {
                    Qty = detail.Qty ?? 0,
                    Rate = detail.Rate ?? 0,
                    Discount = detail.Discount ?? 0,
                    TaxAmount = detail.TaxAmount ?? 0,
                    GrossAmount = detail.GrossAmount ?? 0,
                    //CustomAmount = detail.CustomAmount,
                    DiscountPercent = detail.DiscountPercent ?? 0,
                    NetAmount = detail.NetAmount ?? 0,
                    Amount = detail.Amount ?? 0,
                    PurchaseMasterId = masterId,
                    PurchaseOrderDetailsId = input.AgainstId == PurchaseModeType.PurchaseOrder
                        ? detail.OrderDetailId == Guid.Empty ? null : detail.OrderDetailId
                        : null,
                    AgainstDetalId = detail.OrderDetailId,
                    ProductId = detail.ProductId,
                    UnitId = detail.UnitId,
                    //ImportRate = detail.ImportRate,
                    TaxId = detail.TaxId ?? ERPCommonManager.GetTaxDefault(),
                    TenantId = tenantId
                });

                //foreach (var taxDetail in detail.CustomLedgerList.Select(taxDetails => new ImportTaxDetails
                //{
                //    AccountLedgerId = taxDetails.LedgerId,
                //    Amount = taxDetails.Amount,
                //    EffectLedgerId = taxDetails.EffectLedgerId,
                //    ImportTaxMasterId = importTaxId,
                //    PurchaseDetailId = purchaseDetailId,
                //    Percent = taxDetails.Percent,
                //    TenantId = tenantId
                //})) await importTaxDetailsRepository.InsertAsync(taxDetail);

                if (detail.TaxAmount > 0)
                {
                    var taxes = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                    var ledgerDetail1 = new LedgerPosting
                    {
                        VoucherNumbering = voucherNumbering,
                        VoucherNo = voucherNo,
                        Date = date,
                        DetailId = input.PurchaseAccountId,
                        Debit = detail.TaxAmount ?? 0,
                        Credit = 0,
                        InvoiceNo = voucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = taxes.LedgerId,
                        TenantId = tenantId,
                        PostingNumber = objNumbering,
                        VendorVoucherNo = input.VendorInvoiceNo,
                        MasterId = masterId
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerDetail1);
                }


                await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                {
                    DateMiti = input.DateMiti,
                    LedgerId = input.LedgerId,
                    VoucherTypeId = voucherTypeId,
                    VoucherNo = voucherNo,
                    VoucherNumbering = voucherNumbering,
                    ProductId = detail.ProductId,
                    UnitId = detail.UnitId,
                    Qty = detail.Qty ?? 0,
                    Rate = detail.Rate ?? 0,
                    GrossAmount = detail.GrossAmount ?? 0,
                    DiscountAmount = detail.Discount ?? 0,
                    NetAmount = detail.NetAmount ?? 0,
                    Amount = detail.Amount ?? 0,
                    TaxAmount = detail.TaxAmount ?? 0,
                    MovementType = StockMaintainTypeEnum.Inward,
                    IsValueIncrease = true,
                    FinancialYearId = FinancialYearId,
                    MasterId = masterId,
                    VendorVoucherNo = input.VendorInvoiceNo,
                    SourceDetailId = purchaseDetailId,
                    TenantId = tenantId
                });
            }

            await unitOfWork.CompleteAsync();

            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPurchaseMasterDto input)
        {
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");
                var tenantId = AbpSession.TenantId;
                var purchaseMaster = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
                if (purchaseMaster == null)
                    throw new UserFriendlyException($"Data for purchase voucher no. {input.VoucherNo} not found.");
                {
                    var date = DateConverter.ConvertToEnglish(input.DateMiti);
                    var imeList = new List<string>();


                    if (await GetVoucherGenerateType() == "Manually")
                    {
                        if (await purchaseMasterRepository.CountAsync(x =>
                                x.Id != input.Id && x.FinancialYearId == FinancialYearId &&
                                x.VoucherNo == input.VoucherNo) > 0)
                            throw new UserFriendlyException("PurchaseMaster VoucherNo is Duplicate");
                        purchaseMaster.VoucherNo = input.VoucherNo;
                    }

                    if (await GetVoucherGenerateType() == "Duplicate")
                        purchaseMaster.VoucherNo = input.VoucherNo;

                    purchaseMaster.DateMiti = input.DateMiti;
                    purchaseMaster.Date = date;
                    purchaseMaster.VendorInvoiceNo = input.VendorInvoiceNo;
                    //purchaseMaster.VendorInvoiceDate = DateConverter.ConvertToEnglish(input.VendorInvoiceMiti);
                    purchaseMaster.CreditPeriod = input.CreditPeriod ?? 0;
                    purchaseMaster.Narration = input.Narration;
                    purchaseMaster.CreditDate = input.CreditPeriod == null
                        ? DateConverter.ConvertToEnglish(input.DateMiti)
                        : DateConverter.ConvertToEnglish(input.DateMiti).AddDays((double)input.CreditPeriod);
                    purchaseMaster.TotalTax = input.TotalTax;
                    purchaseMaster.TotalTaxableAmount =
                        input.PurchaseDetail.Where(x => x.TaxAmount > 0).Sum(x => x.NetAmount ?? 0);
                    purchaseMaster.TotalAmount = input.TotalAmount;
                    purchaseMaster.BillDiscount = input.BillDiscount;
                    purchaseMaster.GrandTotal = input.GrandTotal;
                    //purchaseMaster.PpdNo = input.PpdNo;
                    //purchaseMaster.InvoiceTypeEnum = input.InvoiceTypeEnum;
                    purchaseMaster.LrNo = input.LrNo;
                    //purchaseMaster.TransportationCompany = input.TransportationCompany;
                    purchaseMaster.PurchaseAccountId = input.PurchaseAccountId;
                    purchaseMaster.AgainstId = input.AgainstId;
                    purchaseMaster.FinancialYearId = FinancialYearId;
                    //purchaseMaster.CustomLedgerId = input.CustomLedgerId;
                    //purchaseMaster.TotalCustomAmount = input.TotalCustomAmount;
                    purchaseMaster.LedgerId = input.LedgerId;
                    purchaseMaster.PurchaseOrderMasterId = input.AgainstId == PurchaseModeType.PurchaseOrder
                        ? input.OrderOrReceiptId
                        : null;
                    purchaseMaster.UpdateUserId = AbpSession.UserId;
                    await purchaseMasterRepository.UpdateAsync(purchaseMaster);

                    await ledgerPostingRepository.DeleteAsync(x =>
                        x.VoucherNo == purchaseMaster.VoucherNo && x.VoucherTypeId == voucherTypeId &&
                        x.FinancialYearId == FinancialYearId);

                    var existingStockPostings = await stockPostingRepository.GetAll()
                        .Where(x => x.VoucherTypeId == purchaseMaster.VoucherTypeId &&
                                    x.VoucherNo == purchaseMaster.VoucherNo &&
                                    x.FinancialYearId == purchaseMaster.FinancialYearId)
                        .AsNoTracking()
                        .ToListAsync();
                    await materialStockPostingService.ReverseExistingStockPostingsAsync(existingStockPostings);

                    var purchaseReturns = await purchaserReturnRepository.GetAll()
                        .Where(x => x.PurchaseMasterId == purchaseMaster.Id).ToListAsync();
                    if (purchaseReturns.Count > 0)
                    {
                        var message = string.Join(',', purchaseReturns.Select(x => x.VoucherNo));
                        throw new UserFriendlyException(
                            $"Purchase Return reference Exists in VoucherNo {message}, so this invoice can't be edited.");
                    }

                    await purchaseDetailRepository.DeleteAsync(x => x.PurchaseMasterId == purchaseMaster.Id);

                    await additionalCostRepository.DeleteAsync(x =>
                        x.VoucherNo == purchaseMaster.VoucherNo && x.VoucherTypeId == voucherTypeId &&
                        x.FinancialYearId == FinancialYearId);

                    foreach (var objPartyBalance in await partyBalanceRepository.GetAllListAsync(x =>
                                 x.VoucherTypeId == purchaseMaster.VoucherTypeId &&
                                 x.FinancialYearId == purchaseMaster.FinancialYearId
                                 && x.VoucherNo == purchaseMaster.VoucherNo))
                    {
                        objPartyBalance.VoucherNo = "";
                        objPartyBalance.VoucherTypeId = Guid.Empty;
                        await partyBalanceRepository.UpdateAsync(objPartyBalance);
                    }

                    //await partyBalanceRepository.DeleteAsync(x =>
                    //    x.VoucherTypeId == purchaseMaster.VoucherTypeId &&
                    //    x.FinancialYearId == purchaseMaster.FinancialYearId &&
                    //    x.VoucherNo == purchaseMaster.VoucherNo &&
                    //    x.VoucherNumbering == purchaseMaster.VoucherNumbering &&
                    //    x.BranchId == purchaseMaster.BranchId && x.ReferenceType == "New" &&
                    //    x.AgainstVoucherTypeId == Guid.Empty);

                    await stockPostingRepository.DeleteAsync(x =>
                        x.VoucherTypeId == purchaseMaster.VoucherTypeId && x.VoucherNo == purchaseMaster.VoucherNo &&
                         x.FinancialYearId == purchaseMaster.FinancialYearId);

                    var objImportTax =
                        await importTaxMasterRepository.FirstOrDefaultAsync(x => x.PurchaseMasterId == input.Id);
                    if (objImportTax != null)
                    {
                        await importTaxDetailsRepository.DeleteAsync(x => x.ImportTaxMasterId == objImportTax.Id);
                        await importTaxMasterRepository.DeleteAsync(objImportTax);
                    }

                    var importTaxId = Guid.Empty;
                    //if (input.InvoiceTypeEnum == InvoiceTypeEnum.ImportInvoice)
                    //{
                    //    var taxDet = input.PurchaseDetail;
                    //    var taxCustomAmt = taxDet.Sum(t => t.CustomLedgerList.Sum(x => x.Amount));
                    //    var importTax = new ImportTaxMaster
                    //    {
                    //        DateMiti = input.DateMiti,
                    //        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    //        TotalImportTax = taxCustomAmt,
                    //        PurchaseVoucherNo = purchaseMaster.VoucherNo,
                    //        BranchId = input.BranchId,
                    //        PurchaseMasterId = purchaseMaster.Id,
                    //        FinancialYearId = FinancialYear.Id,
                    //        TenantId = tenantId
                    //    };
                    //    importTaxId = await importTaxMasterRepository.InsertAndGetIdAsync(importTax);
                    //    var objCustomLedgerLists = new List<CustomLedgerList>();
                    //    var customLedgers = input.PurchaseDetail.Select(x => x.CustomLedgerList);
                    //    foreach (var objDetail in customLedgers) objCustomLedgerLists.AddRange(objDetail);

                    //    foreach (var ledgerId in objCustomLedgerLists.Select(x => x.LedgerId).Distinct())
                    //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    //        {
                    //            VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //            VoucherNo = purchaseMaster.VoucherNo,
                    //            Date = date,
                    //            DetailId = input.PurchaseAccountId,
                    //            Debit = objCustomLedgerLists.Where(x => x.LedgerId == ledgerId).Sum(x => x.Amount),
                    //            Credit = 0,
                    //            InvoiceNo = purchaseMaster.VoucherNo,
                    //            FinancialYearId = FinancialYearId,
                    //            DateMiti = input.DateMiti,
                    //            VoucherTypeId = voucherTypeId,
                    //            LedgerId = ledgerId,
                    //            TenantId = tenantId,
                    //            PostingNumber = purchaseMaster.PostingNumbering,
                    //            MasterId = purchaseMaster.Id,
                    //            VendorVoucherNo = input.VendorInvoiceNo
                    //        });

                    //    foreach (var ledgerId in objCustomLedgerLists.Select(x => x.EffectLedgerId).Distinct())
                    //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    //        {
                    //            VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //            VoucherNo = purchaseMaster.VoucherNo,
                    //            Date = date,
                    //            DetailId = input.PurchaseAccountId,
                    //            Debit = 0,
                    //            Credit = objCustomLedgerLists.Where(x => x.EffectLedgerId == ledgerId)
                    //                .Sum(x => x.Amount),
                    //            InvoiceNo = purchaseMaster.VoucherNo,
                    //            FinancialYearId = FinancialYearId,
                    //            DateMiti = input.DateMiti,
                    //            VoucherTypeId = voucherTypeId,
                    //            LedgerId = ledgerId,
                    //            TenantId = tenantId,
                    //            PostingNumber = purchaseMaster.PostingNumbering,
                    //            MasterId = purchaseMaster.Id,
                    //            VendorVoucherNo = input.VendorInvoiceNo
                    //        });

                    //    //Vat posting for customLedger
                    //    if (input.CustomLedgerId != null || input.CustomLedgerId != Guid.Empty)
                    //        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    //        {
                    //            VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //            VoucherNo = purchaseMaster.VoucherNo,
                    //            Date = date,
                    //            DetailId = input.PurchaseAccountId,
                    //            Debit = 0,
                    //            Credit = input.TotalTax,
                    //            InvoiceNo = purchaseMaster.VoucherNo,
                    //            FinancialYearId = FinancialYearId,
                    //            DateMiti = input.DateMiti,
                    //            VoucherTypeId = voucherTypeId,
                    //            LedgerId = input.CustomLedgerId ?? Guid.Empty,
                    //            TenantId = tenantId,
                    //            PostingNumber = purchaseMaster.PostingNumbering,
                    //            VendorVoucherNo = input.VendorInvoiceNo,
                    //            MasterId = purchaseMaster.Id
                    //        });
                    //}

                    var objAccountLedger =
                        await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);
                    if (objAccountLedger != null)
                        if (objAccountLedger.IsBillByBill)
                            await partyBalanceService.UpdatePurchaseEntryAsync(new PartyBalanceNewEntryDto
                            {
                                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                DueDate = DateConverter.ConvertToEnglish(input.DateMiti).AddDays((int)input.CreditPeriod),
                                LedgerId = input.LedgerId,
                                VoucherNo = purchaseMaster.VoucherNo,
                                VoucherNumbering = purchaseMaster.VoucherNumbering,
                                Amount = input.GrandTotal,
                                MasterId = purchaseMaster.Id,
                            });
                    //await partyBalanceRepository.InsertAsync(new PartyBalance
                    //{
                    //    VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //    VoucherNo = purchaseMaster.VoucherNo,
                    //    Date = date,
                    //    LedgerId = input.LedgerId,
                    //    FinancialYearId = FinancialYearId,
                    //    VoucherTypeId = voucherTypeId,
                    //    AgainstVoucherTypeId = Guid.Empty,
                    //    AgainstVoucherNo = "NA",
                    //    InvoiceNo = purchaseMaster.VoucherNo,
                    //    IsAgainst = false,
                    //    AgainstInvoiceNo = "NA",
                    //    ReferenceType = "New",
                    //    Debit = 0,
                    //    Credit = input.GrandTotal,
                    //    CreditPeriod = input.CreditPeriod ?? 0,

                    //    BranchId = input.BranchId,
                    //    MasterVoucherTypeId = voucherTypeId,
                    //    MasterId = (Guid)input.Id,
                    //    DetailId = Guid.Empty,
                    //    MasterVoucherNo = purchaseMaster.VoucherNo,
                    //    TenantId = tenantId
                    //});

                    //decimal ledgerAmt;
                    //if (input.InvoiceTypeEnum == InvoiceTypeEnum.ImportInvoice)
                    //{
                    //    //Purchase Ledger Posting
                    //    var ledgerAmt = input.PurchaseDetail.Sum(x => x.ImportRate * x.Qty ?? 0);
                    //    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    //    {
                    //        VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //        VoucherNo = purchaseMaster.VoucherNo,
                    //        Date = date,
                    //        DetailId = input.LedgerId,
                    //        Debit = ledgerAmt,
                    //        Credit = 0,
                    //        InvoiceNo = purchaseMaster.VoucherNo,
                    //        FinancialYearId = FinancialYearId,
                    //        DateMiti = input.DateMiti,
                    //        VoucherTypeId = voucherTypeId,
                    //        LedgerId = input.PurchaseAccountId,
                    //        TenantId = tenantId,
                    //        PostingNumber = purchaseMaster.PostingNumbering,
                    //        MasterId = purchaseMaster.Id,
                    //        VendorVoucherNo = input.VendorInvoiceNo
                    //    });

                    //    //PartyLedger Posting
                    //    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    //    {
                    //        VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //        VoucherNo = purchaseMaster.VoucherNo,
                    //        Date = date,
                    //        DetailId = input.PurchaseAccountId,
                    //        Debit = 0,
                    //        Credit = ledgerAmt,
                    //        InvoiceNo = purchaseMaster.VoucherNo,
                    //        FinancialYearId = FinancialYearId,
                    //        DateMiti = input.DateMiti,
                    //        VoucherTypeId = voucherTypeId,
                    //        LedgerId = input.LedgerId,
                    //        TenantId = tenantId,
                    //        PostingNumber = purchaseMaster.PostingNumbering,
                    //        VendorVoucherNo = input.VendorInvoiceNo,
                    //        MasterId = purchaseMaster.Id
                    //    });
                    //}
                    //else
                    //{
                    var partyAmt = input.PurchaseDetail.Sum(x => x.Rate * x.Qty ?? 0) +
                                   input.PurchaseDetail.Sum(x => x.TaxAmount ?? 0) -
                                   input.PurchaseDetail.Sum(x => x.Discount ?? 0);

                    var purchaseAmt = input.PurchaseDetail.Sum(x => x.Rate * x.Qty ?? 0);
                    var discountAmt = input.PurchaseDetail.Sum(x => x.Discount ?? 0);
                    if (discountAmt > 0)
                    {
                        var discountLedger =
                            await accountLedgerRepository.FirstOrDefaultAsync(x =>
                                x.Name == "Discount Received");
                        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                        {
                            VoucherNumbering = purchaseMaster.VoucherNumbering,
                            VoucherNo = purchaseMaster.VoucherNo,
                            Date = date,
                            DetailId = input.LedgerId,
                            Debit = 0,
                            Credit = discountAmt,
                            InvoiceNo = purchaseMaster.VoucherNo,
                            FinancialYearId = FinancialYearId,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            LedgerId = discountLedger.Id,
                            TenantId = tenantId,
                            PostingNumber = purchaseMaster.PostingNumbering,
                            MasterId = input.Id,
                            VendorVoucherNo = input.VendorInvoiceNo
                        });
                    }

                    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    {
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        VoucherNo = purchaseMaster.VoucherNo,
                        Date = date,
                        DetailId = input.LedgerId,
                        Debit = purchaseAmt,
                        Credit = 0,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = input.PurchaseAccountId,
                        TenantId = tenantId,
                        PostingNumber = purchaseMaster.PostingNumbering,
                        MasterId = purchaseMaster.Id,
                        VendorVoucherNo = input.VendorInvoiceNo
                    });

                    //PartyLedger Posting
                    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    {
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        VoucherNo = purchaseMaster.VoucherNo,
                        Date = date,
                        DetailId = input.PurchaseAccountId,
                        Debit = 0,
                        Credit = partyAmt,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = input.LedgerId,
                        TenantId = tenantId,
                        PostingNumber = purchaseMaster.PostingNumbering,
                        VendorVoucherNo = input.VendorInvoiceNo,
                        MasterId = purchaseMaster.Id
                    });
                    //}

                    foreach (var detail in input.PurchaseDetail)
                    {
                        await materialStockPostingService.EnsurePurchaseLineAllowedAsync(detail.ProductId);

                        var purchaseDetailId = await purchaseDetailRepository.InsertAndGetIdAsync(new PurchaseDetail
                        {
                            Qty = detail.Qty ?? 0,
                            Rate = detail.Rate ?? 0,
                            Discount = detail.Discount ?? 0,
                            TaxAmount = detail.TaxAmount ?? 0,
                            GrossAmount = detail.GrossAmount ?? 0,
                            DiscountPercent = detail.DiscountPercent ?? 0,
                            NetAmount = detail.NetAmount ?? 0,
                            Amount = detail.Amount ?? 0,
                            PurchaseMasterId = purchaseMaster.Id,
                            PurchaseOrderDetailsId = input.AgainstId == PurchaseModeType.PurchaseOrder
                                ? detail.OrderDetailId == Guid.Empty ? null : detail.OrderDetailId
                                : null,
                            AgainstDetalId = detail.OrderDetailId,
                            ProductId = detail.ProductId,
                            UnitId = detail.UnitId,
                            //CustomAmount = detail.CustomAmount,
                            //ImportRate = detail.ImportRate,
                            TaxId = detail.TaxId ?? ERPCommonManager.GetTaxDefault(),
                            TenantId = tenantId
                        });

                        //foreach (var taxDetail in detail.CustomLedgerList.Select(taxDetails => new ImportTaxDetails
                        //{
                        //    AccountLedgerId = taxDetails.LedgerId,
                        //    Amount = taxDetails.Amount,
                        //    Percent = taxDetails.Percent,
                        //    EffectLedgerId = taxDetails.EffectLedgerId,
                        //    ImportTaxMasterId = importTaxId,
                        //    PurchaseDetailId = purchaseDetailId,
                        //    TenantId = tenantId
                        //})) await importTaxDetailsRepository.InsertAsync(taxDetail);

                        if (detail.TaxAmount > 0)
                        {
                            var taxes = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                            var ledgerDetail1 = new LedgerPosting
                            {
                                VoucherNumbering = purchaseMaster.VoucherNumbering,
                                VoucherNo = purchaseMaster.VoucherNo,
                                Date = date,
                                DetailId = input.PurchaseAccountId,
                                Debit = detail.TaxAmount ?? 0,
                                Credit = 0,
                                InvoiceNo = purchaseMaster.VendorInvoiceNo,
                                FinancialYearId = FinancialYearId,
                                DateMiti = input.DateMiti,
                                VoucherTypeId = voucherTypeId,
                                LedgerId = taxes.LedgerId,
                                TenantId = tenantId,
                                PostingNumber = purchaseMaster.PostingNumbering,
                                VendorVoucherNo = input.VendorInvoiceNo,
                                MasterId = input.Id
                            };
                            await ledgerPostingRepository.InsertAsync(ledgerDetail1);
                        }

                        await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                        {
                            DateMiti = input.DateMiti,
                            LedgerId = input.LedgerId,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = purchaseMaster.VoucherNo,
                            VoucherNumbering = purchaseMaster.VoucherNumbering,
                            ProductId = detail.ProductId,
                            UnitId = detail.UnitId,
                            Qty = detail.Qty ?? 0,
                            Rate = detail.Rate ?? 0,
                            GrossAmount = detail.GrossAmount ?? 0,
                            DiscountAmount = detail.Discount ?? 0,
                            NetAmount = detail.NetAmount ?? 0,
                            Amount = detail.Amount ?? 0,
                            TaxAmount = detail.TaxAmount ?? 0,
                            MovementType = StockMaintainTypeEnum.Inward,
                            IsValueIncrease = true,
                            FinancialYearId = FinancialYearId,
                            MasterId = purchaseMaster.Id,
                            VendorVoucherNo = input.VendorInvoiceNo,
                            SourceDetailId = purchaseDetailId,
                            TenantId = tenantId
                        });
                    }

                    if (input.OrderOrReceiptId != null && input.OrderOrReceiptId != Guid.Empty &&
                        input.AgainstId == PurchaseModeType.PurchaseOrder)
                    {
                        var flag = 0;
                        var missingProducts = await MissingProductLists((Guid)input.OrderOrReceiptId,
                            PurchaseModeType.PurchaseOrder);
                        foreach (var missingProduct in missingProducts)
                        {
                            var inputProduct =
                                input.PurchaseDetail.FirstOrDefault(x => x.ProductId == missingProduct.ProductId);
                            if (inputProduct.Qty != missingProduct.Qty)
                                flag = 1;
                        }

                        if (flag == 0)
                        {
                            var orderData = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x =>
                                x.Id == input.OrderOrReceiptId && input.AgainstId == PurchaseModeType.PurchaseOrder);
                            orderData.IsCompleted = true;
                            await purchaseOrderMasterRepository.UpdateAsync(orderData);
                        }

                        if (flag == 1)
                        {
                            var orderData = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x =>
                                x.Id == input.OrderOrReceiptId && input.AgainstId == PurchaseModeType.PurchaseOrder);
                            orderData.IsCompleted = false;
                            await purchaseOrderMasterRepository.UpdateAsync(orderData);
                        }
                    }
                }

                await unitOfWork.CompleteAsync();
            }

            return (Guid)input.Id;
        }

        //     [AbpAuthorize(AppPermissions.PagesPurchaseAdditionalCost)]
        //public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllCustomLedgerDropdown()
        //{
        //    return await accountLedgerRepository.GetAll()
        //        .Where(x => (x.TenantId == AbpSession.TenantId &&
        //                     x.AccountGroupFk.Name == "Sundry Creditors") ||
        //                    // x.AccountGroupFk.Name == "Sundry Debtors" ||
        //                    x.AccountGroupFk.Name == "Cash-in Hand" ||
        //                    x.AccountGroupFk.Name == "Bank Account" ||
        //                    x.AccountGroupFk.Name == "Indirect Expenses" ||
        //                    x.AccountGroupFk.Name == "Direct Expenses")
        //        .Select(accountLedger => new PurchaseMasterAccountLedgerTableDto
        //        {
        //            Id = accountLedger.Id,
        //            DisplayName = accountLedger.Name == null ? "" : accountLedger.Name.ToString()
        //        }).AsNoTracking().ToListAsync();
        //}

        //     [AbpAuthorize(AppPermissions.PagesPurchaseAdditionalCost)]
        public async Task<List<PurchaseMasterAccountLedgerTableDto>> GetAllAdditionalLedgerDropdown()
        {
            return await accountLedgerRepository.GetAll()
                .Where(x => (x.TenantId == AbpSession.TenantId &&
                             x.AccountGroupFk.Name == "Duties & Taxes") ||
                            x.AccountGroupFk.Name == "Indirect Expenses" ||
                            x.AccountGroupFk.Name == "Direct Expenses")
                .Select(accountLedger => new PurchaseMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name == null ? "" : accountLedger.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        public async Task UploadImageNew(IFormFile file, Guid purchaseMasterId)
        {
            var purchaseOrder = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseMasterId);
            var tenantId = AbpSession.TenantId;


            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseInvoice");

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
            }

            fileDetails.ChangedFileName = changedFileName;
            await voucherPhotosRepository.UpdateAsync(fileDetails);
        }

        public async Task<List<DocumentDetailsDto>> GetAllDocuments(Guid purchaseMasterId)
        {
            var purchaseOrder = await purchaseMasterRepository.FirstOrDefaultAsync(purchaseMasterId);
            if (purchaseOrder == null)
                throw new UserFriendlyException("Data not found");
            var result = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.VoucherNo == purchaseOrder.VoucherNo &&
                            x.VoucherNumbering == purchaseOrder.VoucherNumbering &&
                            x.VoucherTypeId == purchaseOrder.VoucherTypeId &&
                            x.FinancialYearId == purchaseOrder.FinancialYearId).Include(x => x.VoucherTypeFk).ToListAsync()).Select(x =>
                new DocumentDetailsDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    FileType = x.FileType,
                    VoucherNo = x.VoucherNo,
                    VoucherType = x.VoucherTypeFk.Name,
                    Image = null /*x.Image*/
                }).ToList();
            return result;
        }

        public async Task<DocumentDetailsDto> GetImage(Guid id)
        {
            var data = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Id == id).Include(x => x.VoucherTypeFk).ToListAsync()).FirstOrDefault();
            if (data == null) return null;
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


        public async Task<FileDto> GetPurchaseMastersWithDetailsToExcel(GetAllUniversalMastersInput input)
        {
            var filteredPurchaseMasters = purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk).Where(x => x.FinancialYearId == FinancialYearId);

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseMasters = filteredPurchaseMasters.Where(x => x.Date <= date);
            }

            var purchaseDetails = await purchaseDetailRepository.GetAll()
                .Include(x => x.PurchaseMasterFk).Include(x => x.UnitFk).Include(x => x.ProductFk)
                .Where(x => x.PurchaseMasterFk.FinancialYearId == FinancialYearId)
                .Select(x => new
                {
                    x.ProductFk.Name,
                    UnitName = x.UnitFk.Name,
                    x.Qty,
                    x.Rate,
                    x.Amount,
                    x.PurchaseMasterId
                }).AsNoTracking().ToListAsync();
            var query = from o in filteredPurchaseMasters.OrderBy(x => x.VoucherNumbering)
                        select new GetPurchaseMasterExcelExportDetailDto
                        {
                            Date = o.Date,
                            VoucherNo = o.VoucherNo,
                            DateMiti = o.DateMiti,
                            VendorInvoiceNo = o.VendorInvoiceNo,
                            TotalAmount = o.TotalAmount,
                            //VendorInvoiceDate = o.VendorInvoiceDate,
                            TotalTax = o.TotalTax,
                            GrandTotal = o.GrandTotal,
                            BillDiscount = o.BillDiscount,
                            Narration = o.Narration,
                            Id = o.Id,
                            LedgerName = o.AccountLedgerFk.Name
                        };

            var purchaseMasterListDtos = await query.ToListAsync();
            var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
            foreach (var data in purchaseMasterListDtos)
                data.Details = purchaseDetails.Where(x => x.PurchaseMasterId == data.Id)
                    .Select(x => new ProductDetailsForExcelExport
                    {
                        Name = x.Name,
                        Unit = x.UnitName,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Amount = x.Amount
                    }).ToList();
            var data1 = new GetPurchaseMasterExcelExportMasterDto
            {
                Branch = branch.Name,
                PhoneNo = branch.PhoneNo1,
                Address = branch.Address,
                Details = purchaseMasterListDtos
            };
            return purchaseMastersExcelExporter.ExportToFileDetails(data1);
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseMastersPrint)]
        public async Task<byte[]> GetPdfdownload(Guid id)
        {
            //var filePath = "PurchaseMaster.pdf";
            var model = await GetPurchaseMasterForPdf(id);
            var list = new List<PdfForPurchaseMasterModel> { model };
            var document = new PurchaseMasterPdf(list);
            return document.GeneratePdf();
        }
    }
}
