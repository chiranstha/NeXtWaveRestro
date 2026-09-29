using Abp.Application.Features;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Repositories;
using Abp.IO.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NepDate;
using Newtonsoft.Json;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common;
using NextWave.Erp.Configuration;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Features;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Sales.Exporting;
using NextWave.Erp.Sales.Importing;
using NextWave.Erp.Sales.Pdf;
using NextWave.Erp.SharedDtos;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static NextWave.Erp.Configuration.AppSettings;
using PaymentMethod = NextWave.Erp.Enums.PaymentMethod;


namespace NextWave.Erp.Sales
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesSalesMasters)]
    public class SalesMastersAppService(
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<ProductGroup, Guid> productGroupRepository,
        ISalesMastersNewExcelExporter salesMastersNewExcelExporter,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
    //    IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<VoucherPhotos, Guid> voucherPhotosRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<Product, Guid> productRepository,
        //IRepository<SalesProductCancelMaster, Guid> salesProductCancelRepository,
        IRepository<ReceiptMaster, Guid> receiptMasterRepository,
        //IRepository<SalesProductCancelDetail, Guid> salesProductCancelDetailRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<ReceiptDetail, Guid> receiptDetailRepository,
        IRepository<Tax, Guid> taxRepository,
        IUnitOfWorkManager unitOfWorkManager,

        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<User, long> userRepository,
        IRepository<FinancialYear, Guid> financialYearRepository,
        IRepository<SalesDetail, Guid> salesDetailRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<StockMaintain, Guid> stockMaintainRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
        IDocumentsAppService documentsAppService,
        //EmailSender emailSender,
        //IAppFolders appFolders,
        //IAppNotifier appNotifier,
        IBinaryObjectManager binaryObjectManager,
        IBackgroundJobManager backgroundJobManager,
        IFeatureChecker featureChecker,
        //UserManager userManager,
        //IRepository<UserBranch, Guid> userBranchRepository,
        //ISmsSender smsSender,
        PartyBalanceService partyBalanceService,
        StockManagementAppService stockManagementAppService,
        MaterialStockPostingService materialStockPostingService,
        //IRepository<Tenant> tenantRepository,
        IRepository<StockFifoTable, Guid> stockFifoTableRepository)
        : ErpAppServiceBase, ISalesMastersAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        protected virtual Guid FinancialYearId
        {
            get
            {
                var financialYearId = ERPCommonManager.GetCurrentFinancialYearId(AbpSession.GetUserId());
                if (financialYearId == Guid.Empty)
                    throw new UserFriendlyException(
                        "No active financial year found. Please create or select a financial year before proceeding.");
                return financialYearId;
            }
        }

        [DisableAuditing]
        public async Task<PagedResultDto<GetSalesMasterForGetAllDto>> GetAll(GetAllUniversalMastersInput input)
        {
            var isActiveIrd = true;
            //await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsCBMS,
            //AbpSession.GetTenantId());

            var filteredSalesMasters = salesMasterRepository.GetAll().AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.VoucherNo.Contains(input.Filter.Trim()) || e.DateMiti.Contains(input.Filter.Trim())
                                                                   || e.AccountLedgerFk.Name.Contains(
                                                                       input.Filter.Trim()) ||
                                                                   e.VatNo.Contains(input.Filter.Trim()))
                .Select(o => new
                {
                    o.VoucherNo,
                    o.Date,
                    IsCancle = o.IsDelete,
                    o.DateMiti,
                    o.TaxAmount,
                    NoTaxableAmount = o.NetAmount - o.TaxableAmount,
                    o.BillDiscount,
                    o.GrandTotal,
                    o.GrossAmount,
                    o.TaxableAmount,
                    o.NetAmount,
                    o.PaymentMethod,
                    o.VatRefundAmount,
                    o.Id,
                    o.CreateUserId,
                    o.UpdateUserId,
                    o.LedgerName,
                    o.VatNo,
                    //    o.BranchId,
                    o.LedgerId,
                    o.IsDelete,
                    o.VoucherNumbering
                });

            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredSalesMasters =
                filteredSalesMasters.OrderByDescending(e => e.Date.Date).ThenByDescending(e => e.VoucherNumbering)
                    .PageBy(input);

            var salesMasters = from o in pagedAndFilteredSalesMasters
                                   //join o2 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                                   //    .AsNoTracking() on o.LedgerId equals o2.Id into j2
                                   // from s2 in j2.DefaultIfEmpty()
                               join o3 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                                   on o.CreateUserId equals o3.Id into j4
                               from s3 in j4.DefaultIfEmpty()
                               join o4 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                                   on o.UpdateUserId equals o4.Id into j5
                               from s4 in j5.DefaultIfEmpty()
                               select new GetSalesMasterForGetAllDto
                               {
                                   VoucherNo = o.VoucherNo,
                                   Date = o.Date,
                                   IsCancle = o.IsDelete,
                                   DateMiti = o.DateMiti,
                                   TaxAmount = o.IsDelete ? 0 : o.TaxAmount,
                                   NoTaxableAmount = Math.Round(o.IsDelete ? 0 : o.NetAmount - o.TaxableAmount, 1),
                                   BillDiscount = o.IsDelete ? 0 : o.BillDiscount,
                                   GrandTotal = o.IsDelete ? 0 : o.GrandTotal,
                                   TotalAmount = o.IsDelete ? 0 : o.GrossAmount,
                                   TaxableAmount = o.IsDelete ? 0 : o.TaxableAmount,
                                   SubTotalAmount = o.IsDelete ? 0 : o.NetAmount,
                                   PaymentMethod = o.PaymentMethod,
                                   VatRefundAmount = o.VatRefundAmount,
                                   Id = o.Id,
                                   CreateUser = s3 == null || s3.Name == null ? "" : s3.Name,
                                   UpdateUser = s4 == null || s4.Name == null ? "" : s4.Name,
                                   LedgerName = o.IsDelete ? "[CANCEL BILL]" : o.LedgerName,
                                   PanNo = o.IsDelete ? "" : o.VatNo,
                               };

            var totalCount = await filteredSalesMasters.CountAsync();


            // Check migration status  
            //var status = await partyBalanceService.GetMigrationStatus();
            return new PagedResultDto<GetSalesMasterForGetAllDto>(
                totalCount,
                await salesMasters.ToListAsync()
            );
        }




        [AbpAuthorize(AppPermissions.PagesSalesMastersEdit)]
        public async Task<CreateOrEditSalesMasterDto> GetSalesMasterForEdit(Guid id)
        {
            //using (unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            //{
            //    var tenants = await tenantRepository.GetAll().ToListAsync();
            //    foreach (var tenant in tenants)
            //    {
            //        var result = await partyBalanceService.ExecutePartyBalanceMigrationNew(tenant);

            //    }
            //}

            //using var unitOfWork = unitOfWorkManager.Begin(new UnitOfWorkOptions
            //{
            //    IsTransactional = true,
            //    IsolationLevel = IsolationLevel.ReadCommitted
            //});

            try
            {
                // Get sales master with a single, optimized query
                var salesMaster = await salesMasterRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                x.Id == id &&
                                x.FinancialYearId == FinancialYearId &&
                                !x.IsDelete &&
                                x.LedgerId != null)
                    .Include(x => x.AccountLedgerFk)
                    .FirstOrDefaultAsync();

                if (salesMaster == null)
                    throw new UserFriendlyException("Data not found");

                // Create output DTO object
                var output = new CreateOrEditSalesMasterDto
                {
                    Id = salesMaster.Id,
                    VoucherNo = salesMaster.VoucherNo,
                    SalesAccountId = salesMaster.SalesAccountId,
                    DateMiti = salesMaster.DateMiti,
                    CreditPeriod = salesMaster.CreditPeriod,
                    Description = salesMaster.Description,
                    TaxAmount = salesMaster.TaxAmount,
                    BillDiscount = salesMaster.BillDiscount,
                    GrandTotal = salesMaster.GrandTotal,
                    TotalAmount = salesMaster.GrossAmount,
                    TaxableAmount = salesMaster.TaxableAmount,
                    CustomerAddress = salesMaster.CustomerAddress,
                    SubTotalAmount = salesMaster.NetAmount,
                    PiNumber = salesMaster.PINumber,
                    InvoiceType = salesMaster.InvoiceType,
                    VehicleNo = salesMaster.VehicleNo,
                    PaymentMethodLedgerId = salesMaster.PaymentMethodLedgerId,
                    PaymentMethod = salesMaster.PaymentMethod,
                    VatRefundAmount = salesMaster.VatRefundAmount,
                    LrNo = salesMaster.LrNo,
                    SalesModeType = salesMaster.SalesModeType,
                    LedgerId = salesMaster.LedgerId ?? Guid.Empty,
                    AgainstVoucherNo = salesMaster.AgainstVoucherNo,
                    CustomerPhoneNo = salesMaster.CustomerPhoneNo,
                    SourceModule = salesMaster.SourceModule,
                    SourceDocumentId = salesMaster.SourceDocumentId
                };

                // Get all sales details in one query with eager loading of ProductFk
                var salesDetails = await salesDetailRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.TenantId == AbpSession.TenantId && x.SalesMasterId == id)
                    .Include(x => x.ProductFk)
                    .ToListAsync();

                // Prepare for batch processing of unit conversions and IMEIs
                var productIds = salesDetails.Select(sd => sd.ProductId).Distinct().ToList();
                var serialProductIds = salesDetails.Select(sd => sd.ProductId).Distinct()
                    .ToList();

                // Map details to DTOs using the pre-fetched data
                output.SalesDetails = salesDetails.Select(detail => new SalesDetailDto
                {
                    Id = detail.Id,
                    Qty = detail.Qty,
                    Rate = detail.Rate,
                    TaxAmount = detail.TaxAmount,
                    Discount = detail.Discount,
                    GrossAmount = detail.GrossAmount,
                    NetAmount = detail.NetAmount,
                    Amount = detail.Amount,
                    DiscountPer = detail.DiscountPer,
                    ProductId = detail.ProductId,
                    UnitId = detail.UnitId,
                    SalesDetailId = detail.AgainstDetailId,
                    TaxId = detail.TaxId
                }).ToList();

                //      await unitOfWork.CompleteAsync();
                return output;
            }
            catch (Exception ex)
            {
                //     unitOfWork.Dispose();
                throw new UserFriendlyException(L("Error"), ex.Message);
            }
        }


        public async Task<Guid> CreateOrEdit(CreateOrEditSalesMasterDto input)
        {
            Guid salesMasterId;
            if (input.Id == null || input.Id == Guid.Empty)
            {
                salesMasterId = await Create(input);
                //if (await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsEmailSent,
                //        AbpSession.GetTenantId())) await SendEmail(salesMasterId);
            }
            else
            {
                if (!featureChecker.IsEnabled(AbpSession.GetTenantId(), AppFeatures.IrdBilling))
                    salesMasterId = await Update(input);
                else
                    throw new UserFriendlyException("You Can't Update Bill");
            }

            return salesMasterId;
        }

        [AbpAuthorize(AppPermissions.PagesSalesMastersDelete)]
        public async Task Delete(Guid id)
        {
            var sale = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(e => e.Id == id);
            if (sale == null) throw new UserFriendlyException("Data not found");

            if (sale.SyncwithIrd)
                throw new UserFriendlyException("Can not Update or Delete", "Data is Sync with Ird");

            var salesReturn = await salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.SalesMasterId == id)
                .ToListAsync();

            if (salesReturn.Count > 0)
            {
                var joinVoucherNo = string.Join(',', salesReturn.Select(x => x.VoucherNo));
                throw new UserFriendlyException($"Sales Return Reference VhNo:- {joinVoucherNo} Exists");
            }

            await partyBalanceService.DeleteSalesEntryAsync(id);
            await ledgerPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId &&
                x.VoucherNo == sale.VoucherNo);
            var stockPostings = await stockPostingRepository.GetAll()
                .Where(x => x.VoucherTypeId == sale.VoucherTypeId &&
                            x.FinancialYearId == sale.FinancialYearId &&
                            x.VoucherNo == sale.VoucherNo &&
                            x.VoucherNumbering == sale.VoucherNumbering)
                .AsNoTracking()
                .ToListAsync();
            await materialStockPostingService.ReverseExistingStockPostingsAsync(stockPostings);


            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId &&
                x.VoucherNo == sale.VoucherNo && x.VoucherNumbering == sale.VoucherNumbering);

            var users = await UserManager.FindByIdAsync(AbpSession.UserId.ToString());

            if (AbpSession.TenantId != null &&
                await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsTaxEnabled,
                    AbpSession.GetTenantId()))
            {
                sale.UpdateUserId = AbpSession.UserId;
                sale.ServiceDeliveryId = null;
                sale.AgainstId = null;
                sale.IsDelete = true;
                await salesMasterRepository.UpdateAsync(sale);
            }
            else
            {
                //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(), new LocalizableString(
                //    $"Sales Invoice Bill No {sale.VoucherNo} is Delete by {users?.UserName}!",
                //    ERPConsts.LocalizationSourceName));

                await salesDetailRepository.DeleteAsync(x =>
                    x.SalesMasterId == sale.Id && x.TenantId == AbpSession.GetTenantId());
                await salesMasterRepository.DeleteAsync(sale);
            }
        }

        public async Task<FileDto> GetSalesMastersToExcel(GetAllUniversalMastersInput input)
        {
            var filteredSalesMasters = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId);

            var productdetail = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                //  .Include(x => x.SalesMasterFk).Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk)
                .Where(x => x.SalesMasterFk.IsDelete == false).ToListAsync();

            //if (input.BranchId != 0)
            //    filteredSalesMasters = filteredSalesMasters.Where(x => x.BranchId == input.BranchId);
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date <= date);
            }

            var accountLedger = await accountLedgerRepository.GetAll().Where(x => !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Pan
                }).AsNoTracking().ToListAsync();
            var list = new List<GetSalesMasterExportDto>();
            foreach (var item in filteredSalesMasters.OrderBy(x => x.Date.Date).ThenBy(e => e.VoucherNumbering))
            {
                var productname = string.Empty;
                decimal qty = 0;
                foreach (var salesitem in productdetail.Where(x => x.SalesMasterId == item.Id).ToList())
                {
                    productname = productname /*+ salesitem.ProductFk.ProductGroupFk.Name + ","*/;
                    qty += salesitem.Qty;
                }

                productname = productname.Length > 0 ? productname.Remove(productname.Length - 1) : "";
                var sales = new GetSalesMasterExportDto
                {
                    DateMiti = item.DateMiti.Replace('/', '.'),
                    VoucherNo = item.VoucherNo,
                    LedgerName = item.IsDelete
                        ? "CANCELLED"
                        : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Name,
                    PanNo = item.IsDelete ? "" : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Pan,
                    ProductName = item.IsDelete ? "" : productname,
                    Quantity = item.IsDelete ? 0 : qty,
                    GrossAmount = item.IsDelete ? 0 : item.GrossAmount,
                    BillDiscount = item.IsDelete ? 0 : item.BillDiscount,
                    TaxAmount = item.IsDelete ? 0 : item.TaxAmount,
                    TotalAmount = item.IsDelete ? 0 : item.GrandTotal,
                    TaxableAmount = item.IsDelete ? 0 : item.TaxableAmount
                };
                list.Add(sales);
            }


            return salesMastersNewExcelExporter.ExportToFile(list);
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesMasters)]
        public async Task<List<SalesMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            var cashLedgers = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Name == "Cash-in Hand")
                .AsNoTracking().Select(x => x.Id).ToListAsync();


            var cashLedgerList = new List<Guid>();
            foreach (var cash in cashLedgers)
                cashLedgerList.AddRange(await FuncRecursive(cash));

            var cashledger = await accountLedgerRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => cashLedgerList.Contains(x.AccountGroupId))
                .Select(accountLedger => new SalesMasterAccountLedgerTableDto
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
                .Select(accountLedger => new SalesMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    IsCash = false,
                    MobileNo = accountLedger.Phone,
                    CreditPeriod = accountLedger.CreditPeriod ?? 0,
                    Address = accountLedger.Address
                }).ToListAsync();


            cashledger.AddRange(sundryDebtorOrCreditor);
            return cashledger;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllFinancialYearForTableDropdown()
        {
            return await financialYearRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Select(financialYear => new UniversalDropdownDto
                {
                    Id = financialYear.Id,
                    DisplayName = financialYear == null || financialYear.FromMiti == null
                        ? ""
                        : financialYear.FromMiti.ToString()
                }).ToListAsync();
        }


        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllProductGroup()
        {
            var data = new List<UniversalDropdownDto>();


            var list = await productGroupRepository.GetAll().AsNoTracking()
                .Where(e => e.GroupUnder != null)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToListAsync();
            data.AddRange(list);
            return data;
        }


        [AbpAuthorize(AppPermissions.PagesSalesMastersDelete)]
        public async Task DeleteCancle(EntityDto<Guid> input)
        {
            var isTaxEnable = await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsTaxEnabled,
                AbpSession.GetTenantId());
            var isCbms = await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsTaxEnabled,
                AbpSession.GetTenantId());

            if (isTaxEnable || isCbms)

            {
                var sale = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(e => e.Id == input.Id);
                if (sale == null) throw new UserFriendlyException("Data not found");


                var salesReturn = await salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .AsNoTracking()
                    .Where(x => x.SalesMasterId == input.Id)
                    .ToListAsync();

                if (salesReturn.Count > 0)
                {
                    var joinVoucherNo = string.Join(',', salesReturn.Select(x => x.VoucherNo));
                    throw new UserFriendlyException($"Sales Return Reference VhNo:- {joinVoucherNo} Exists");
                }


                await ledgerPostingRepository.DeleteAsync(x =>
                    x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId &&
                    x.VoucherNo == sale.VoucherNo);
                var details = await salesDetailRepository.GetAll().Where(x => x.SalesMasterId == sale.Id).ToListAsync();
                foreach (var detail in details)
                {
                    var stockManage = new StockMaintainDto
                    {
                        DateMiti = sale.DateMiti,
                        ProductId = detail.ProductId,
                        Qty = -detail.Qty,
                        Rate = detail.Rate,
                        FinancialYearId = FinancialYearId,
                        Type = StockMaintainTypeEnum.Outward,
                        UnitId = detail.UnitId
                    };
                    await stockManagementAppService.MaintainStock(stockManage);
                }
                // await _salesDetailRepository.DeleteAsync(x => x.SalesMasterId == sale.Id);

                //foreach (var objPartyBalance in await partyBalanceRepository.GetAllListAsync(x =>
                //             x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId
                //                                                   && x.VoucherNo == sale.VoucherNo &&
                //                                                   x.ReferenceType == "Against" &&
                //                                                   x.BranchId == sale.BranchId))
                //{
                //    var onAccountPosting = await partyBalanceRepository.FirstOrDefaultAsync(x =>
                //        x.VoucherTypeId == objPartyBalance.MasterVoucherTypeId && x.FinancialYearId == sale.FinancialYearId
                //                                                               && x.VoucherNo ==
                //                                                               objPartyBalance.MasterVoucherNo &&
                //                                                               x.ReferenceType ==
                //                                                               ReferenceType.OnAccount.ToString() &&
                //                                                               x.BranchId == sale.BranchId);
                //    if (onAccountPosting != null)
                //    {
                //        onAccountPosting.Debit += objPartyBalance.Debit;
                //        onAccountPosting.Credit += objPartyBalance.Credit;
                //        await partyBalanceRepository.UpdateAsync(onAccountPosting);
                //    }
                //    else
                //    {
                //        var partyBalanceData = new PartyBalance
                //        {
                //            Date = objPartyBalance.Date,
                //            LedgerId = objPartyBalance.DetailId,
                //            FinancialYearId = FinancialYearId,
                //            VoucherTypeId = objPartyBalance.MasterVoucherTypeId,
                //            VoucherNumbering = 0,
                //            VoucherNo = objPartyBalance.MasterVoucherNo,
                //            AgainstVoucherTypeId = null,
                //            AgainstVoucherNo = objPartyBalance.MasterVoucherNo,
                //            InvoiceNo = objPartyBalance.MasterVoucherNo,
                //            IsAgainst = false,
                //            AgainstInvoiceNo = objPartyBalance.MasterVoucherNo,
                //            ReferenceType = ReferenceType.OnAccount.ToString(),
                //            Debit = objPartyBalance.Debit,
                //            Credit = objPartyBalance.Credit,
                //            CreditPeriod = 0,

                //            BranchId = objPartyBalance.BranchId,
                //            MasterVoucherTypeId = objPartyBalance.MasterVoucherTypeId,
                //            MasterId = objPartyBalance.MasterId,
                //            DetailId = objPartyBalance.DetailId,
                //            MasterVoucherNo = objPartyBalance.MasterVoucherNo,
                //            TenantId = objPartyBalance.TenantId
                //        };
                //        await partyBalanceRepository.InsertAsync(partyBalanceData);
                //    }

                //    await partyBalanceRepository.DeleteAsync(objPartyBalance);
                //}


                //await partyBalanceRepository.DeleteAsync(x =>
                //    x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId &&
                //    x.VoucherNo == sale.VoucherNo && x.VoucherNumbering == sale.VoucherNumbering &&
                //    x.BranchId == sale.BranchId && x.ReferenceType == "New" && x.AgainstVoucherTypeId == Guid.Empty);

                await stockPostingRepository.DeleteAsync(x =>
                    x.VoucherTypeId == sale.VoucherTypeId && x.FinancialYearId == sale.FinancialYearId &&
                    x.VoucherNo == sale.VoucherNo && x.VoucherNumbering == sale.VoucherNumbering);

                await ReceiptDelete(sale.VoucherTypeId, sale.VoucherNo);

                var users = await UserManager.FindByIdAsync(AbpSession.UserId.ToString());

                sale.UpdateUserId = AbpSession.UserId;
                sale.ServiceDeliveryId = null;
                sale.AgainstId = null;
                sale.IsDelete = true;
                await salesMasterRepository.UpdateAsync(sale);
            }
        }

        private async Task GetAllFixedLedgerDebitCreditPosting()
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");


            var getTax = await taxRepository.FirstOrDefaultAsync(e => e.TenantId == AbpSession.TenantId && e.Rate > 0);

            var salesList = await salesMasterRepository.GetAll().AsNoTracking()
                .Where(sm => sm.TenantId == AbpSession.GetTenantId()
                             && !sm.IsDelete
                             && sm.FinancialYearId == FinancialYearId
                             && !ledgerPostingRepository.GetAll()
                                 .Include(e => e.AccountLedgerFk)
                                 .Where(lp => lp.TenantId == AbpSession.GetTenantId()
                                              && lp.FinancialYearId == FinancialYearId
                                              && lp.VoucherTypeId == voucherTypeId &&
                                              lp.AccountLedgerFk.Name != "Sales Account"
                                              && lp.AccountLedgerFk.Name != "Tax Account")
                                 .Select(lp => lp.VoucherNo)
                                 .Contains(sm.VoucherNo))
                .Select(sm => new
                {
                    sm.Id,
                    sm.TenantId,
                    sm.VoucherNumbering,
                    sm.DateMiti,
                    sm.Date,
                    sm.VoucherTypeId,
                    sm.VoucherNo,
                    sm.SalesAccountId,
                    sm.LedgerId,
                    sm.GrandTotal,
                    sm.NetAmount,
                    sm.TaxAmount,
                    sm.PostingNumbering,
                    sm.PaymentMethod,
                    sm.FinancialYearId,
                    sm.PaymentMethodLedgerId
                })
                .ToListAsync();


            var cashLedger = await accountLedgerRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.GetTenantId() && e.Name == "Cash")
                .AsNoTracking() // Add AsNoTracking()
                .FirstOrDefaultAsync();

            if (cashLedger == null) throw new UserFriendlyException("Cash not found");

            // Lists to accumulate ledger postings for bulk insertion
            var ledgerPostingsToInsert = new List<LedgerPosting>();

            foreach (var input in salesList)
            {
                // 1. Bulk Delete Related Data
                await DeleteRelatedData(input.Id);

                // 2. Create Ledger Posting for Sales Account
                var ledgerPosting1 = new LedgerPosting
                {
                    TenantId = input.TenantId,
                    VoucherNumbering = input.VoucherNumbering,
                    Date = input.Date,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = input.VoucherTypeId,
                    VoucherNo = input.VoucherNo,
                    LedgerId = input.SalesAccountId,
                    DetailId = (Guid)input.LedgerId,
                    Debit = 0,
                    FinancialYearId = input.FinancialYearId,
                    Credit = input.NetAmount,
                    InvoiceNo = input.VoucherNo,
                    PostingNumber = input.PostingNumbering,
                    MasterId = input.Id
                };
                ledgerPostingsToInsert.Add(ledgerPosting1);

                if (input.TaxAmount > 0)
                    if (getTax != null)
                    {
                        var ledgerPosting2 = new LedgerPosting
                        {
                            TenantId = input.TenantId,
                            VoucherNumbering = input.VoucherNumbering,
                            Date = input.Date,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = input.VoucherTypeId,
                            VoucherNo = input.VoucherNo,
                            LedgerId = getTax.LedgerId,
                            DetailId = (Guid)input.LedgerId,
                            Debit = 0,
                            FinancialYearId = input.FinancialYearId,
                            Credit = input.TaxAmount,
                            InvoiceNo = input.VoucherNo,
                            PostingNumber = input.PostingNumbering,
                            MasterId = input.Id
                        };
                        ledgerPostingsToInsert.Add(ledgerPosting2);
                    }

                // 3. Create Ledger Posting for Cash or Payment Method
                if (cashLedger.Id == (Guid)input.LedgerId)
                {
                    //determine ledgerPosting
                    var ledgerPosting = new LedgerPosting();
                    if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR)
                    {
                        if (input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty)
                            ledgerPosting = new LedgerPosting
                            {
                                TenantId = input.TenantId,
                                VoucherNumbering = input.VoucherNumbering,
                                Date = input.Date,
                                DateMiti = input.DateMiti,
                                VoucherTypeId = input.VoucherTypeId,
                                VoucherNo = input.VoucherNo,
                                LedgerId = (Guid)input.LedgerId,
                                DetailId = input.SalesAccountId,
                                Debit = input.GrandTotal,
                                FinancialYearId = FinancialYearId,
                                Credit = 0,
                                InvoiceNo = input.VoucherNo,
                                PostingNumber = input.PostingNumbering,
                                MasterId = input.Id
                            };
                        else
                            ledgerPosting = new LedgerPosting
                            {
                                TenantId = input.TenantId,
                                VoucherNumbering = input.VoucherNumbering,
                                Date = input.Date,
                                DateMiti = input.DateMiti,
                                VoucherTypeId = input.VoucherTypeId,
                                VoucherNo = input.VoucherNo,
                                LedgerId = (Guid)input.PaymentMethodLedgerId,
                                DetailId = input.SalesAccountId,
                                Debit = input.GrandTotal,
                                FinancialYearId = FinancialYearId,
                                Credit = 0,
                                InvoiceNo = input.VoucherNo,
                                PostingNumber = input.PostingNumbering,
                                MasterId = input.Id
                            };
                    }
                    else
                    {
                        ledgerPosting = new LedgerPosting
                        {
                            TenantId = input.TenantId,
                            VoucherNumbering = input.VoucherNumbering,
                            Date = input.Date,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = input.VoucherTypeId,
                            VoucherNo = input.VoucherNo,
                            LedgerId = (Guid)input.LedgerId,
                            DetailId = input.SalesAccountId,
                            Debit = input.GrandTotal,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = input.VoucherNo,
                            PostingNumber = input.PostingNumbering,
                            MasterId = input.Id
                        };
                    }

                    ledgerPostingsToInsert.Add(ledgerPosting);
                    var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e =>
                        e.RefVoucherTypeId == input.VoucherTypeId && e.RefVoucherNo == input.VoucherNo);

                    if (receiptMaster != null) await DeleteReceiptVoucher(input.Id, input.VoucherNo);
                }
                else
                {
                    //first posting a lederPosting

                    var ledgerPosting = new LedgerPosting
                    {
                        TenantId = input.TenantId,
                        VoucherNumbering = input.VoucherNumbering,
                        Date = input.Date,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = input.VoucherTypeId,
                        VoucherNo = input.VoucherNo,
                        LedgerId = (Guid)input.LedgerId,
                        DetailId = input.SalesAccountId,
                        Debit = input.GrandTotal,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = input.VoucherNo,
                        PostingNumber = input.PostingNumbering,
                        MasterId = input.Id
                    };
                    ledgerPostingsToInsert.Add(ledgerPosting);


                    if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR or PaymentMethod.Cash)
                    {
                        var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e =>
                            e.RefVoucherTypeId == input.VoucherTypeId && e.RefVoucherNo == input.VoucherNo);
                        if (receiptMaster == null) await ReceiptVoucherPosting(input.Id, input.VoucherNo);
                    }
                }
            }

            await ledgerPostingRepository.InsertRangeAsync(ledgerPostingsToInsert);
        }


        private async Task DeleteReceiptVoucher(Guid id, string voucherNo)
        {
            var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e =>
                e.RefVoucherTypeId == id && e.RefVoucherNo == voucherNo);

            if (receiptMaster != null)
            {
                await receiptDetailRepository.DeleteAsync(x => x.ReceiptMasterId == receiptMaster.Id);
                await ledgerPostingRepository.DeleteAsync(x =>
                    x.VoucherNo == receiptMaster.VoucherNo &&
                    x.VoucherNumbering == receiptMaster.VoucherNumbering &&
                    x.VoucherTypeId == receiptMaster.VoucherTypeId &&
                    x.FinancialYearId == receiptMaster.FinancialYearId);

                await receiptMasterRepository.DeleteAsync(id);

                //await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == receiptMaster.VoucherNo &&
                //                                              x.MasterVoucherTypeId ==
                //                                              receiptMaster.VoucherTypeId &&
                //                                              x.FinancialYearId == id &&
                //                                              x.BranchId == receiptMaster.BranchId);
            }
        }


        private async Task GetAllFixedLedgerPosting()
        {
            var cashLedger = await accountLedgerRepository
                .GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(e => e.Name == "Cash");
            if (cashLedger == null) throw new UserFriendlyException("Cash not found");


            var salesList = await salesMasterRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.GetTenantId())
                .Where(e => !e.IsDelete && !e.IsErrorFixed &&
                            (e.PaymentMethod == PaymentMethod.Card_Swipe || e.PaymentMethod == PaymentMethod.QR)
                            && e.LedgerId == cashLedger.Id)
                .OrderBy(e => e.Date).ThenBy(e => e.VoucherNumbering)
                .ToListAsync();


            //throw new UserFriendlyException(JsonConvert.SerializeObject(salesList));

            foreach (var input in salesList)
            {
                await ledgerPostingRepository.DeleteAsync(e =>
                    e.VoucherTypeId == input.VoucherTypeId && e.FinancialYearId == input.FinancialYearId &&
                    e.VoucherNo == input.VoucherNo);


                //await partyBalanceRepository.DeleteAsync(e =>
                //    e.VoucherTypeId == input.VoucherTypeId && e.FinancialYearId == input.FinancialYearId &&
                //    e.VoucherNo == input.VoucherNo && e.BranchId == input.BranchId);


                var receiptList = await receiptMasterRepository.GetAll().Where(e =>
                    e.RefVoucherTypeId == input.VoucherTypeId && e.RefVoucherNo == input.VoucherNo).ToListAsync();


                foreach (var receipt in receiptList)
                    if (receipt != null)
                    {
                        await receiptDetailRepository.DeleteAsync(x => x.ReceiptMasterId == receipt.Id);
                        await ledgerPostingRepository.DeleteAsync(x =>
                            x.VoucherNo == receipt.VoucherNo &&
                            x.VoucherNumbering == receipt.VoucherNumbering &&
                            x.VoucherTypeId == receipt.VoucherTypeId && x.FinancialYearId == receipt.FinancialYearId);

                        await receiptMasterRepository.DeleteAsync(input.Id);

                        //await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == receipt.VoucherNo &&
                        //                                              x.MasterVoucherTypeId == receipt.VoucherTypeId &&
                        //                                              x.FinancialYearId == input.FinancialYearId &&
                        //                                              x.BranchId == receipt.BranchId);
                    }

                var ledgerPosting1 = new LedgerPosting
                {
                    TenantId = input.TenantId,
                    VoucherNumbering = input.VoucherNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    VoucherTypeId = input.VoucherTypeId,
                    VoucherNo = input.VoucherNo,
                    LedgerId = input.SalesAccountId,
                    DetailId = (Guid)input.LedgerId,
                    Debit = 0,
                    FinancialYearId = input.FinancialYearId,
                    Credit = input.NetAmount,
                    InvoiceNo = input.VoucherNo,
                    PostingNumber = input.PostingNumbering,
                    MasterId = input.Id
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting1);


                if (cashLedger.Id == (Guid)input.LedgerId)
                {
                    if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR)
                    {
                        if (input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty)
                        {
                            var ledgerPosting = new LedgerPosting
                            {
                                TenantId = input.TenantId,
                                VoucherNumbering = input.VoucherNumbering,
                                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                DateMiti = input.DateMiti,
                                VoucherTypeId = input.VoucherTypeId,
                                VoucherNo = input.VoucherNo,
                                LedgerId = (Guid)input.LedgerId,
                                DetailId = input.SalesAccountId,
                                Debit = input.NetAmount,
                                FinancialYearId = FinancialYearId,
                                Credit = 0,
                                InvoiceNo = input.VoucherNo,
                                PostingNumber = input.PostingNumbering,
                                MasterId = input.Id
                            };
                            await ledgerPostingRepository.InsertAsync(ledgerPosting);
                        }
                        else
                        {
                            var ledgerPosting = new LedgerPosting
                            {
                                TenantId = input.TenantId,
                                VoucherNumbering = input.VoucherNumbering,
                                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                DateMiti = input.DateMiti,
                                VoucherTypeId = input.VoucherTypeId,
                                VoucherNo = input.VoucherNo,
                                LedgerId = (Guid)input.PaymentMethodLedgerId,
                                DetailId = input.SalesAccountId,
                                Debit = input.NetAmount,
                                FinancialYearId = FinancialYearId,
                                Credit = 0,
                                InvoiceNo = input.VoucherNo,
                                PostingNumber = input.PostingNumbering,
                                MasterId = input.Id
                            };
                            await ledgerPostingRepository.InsertAsync(ledgerPosting);
                        }
                    }
                    else
                    {
                        var ledgerPosting = new LedgerPosting
                        {
                            TenantId = input.TenantId,
                            VoucherNumbering = input.VoucherNumbering,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = input.VoucherTypeId,
                            VoucherNo = input.VoucherNo,
                            LedgerId = (Guid)input.LedgerId,
                            DetailId = input.SalesAccountId,
                            Debit = input.NetAmount,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = input.VoucherNo,
                            PostingNumber = input.PostingNumbering,
                            MasterId = input.Id
                        };
                        await ledgerPostingRepository.InsertAsync(ledgerPosting);
                    }


                    var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e =>
                        e.RefVoucherTypeId == input.VoucherTypeId && e.RefVoucherNo == input.VoucherNo);

                    if (receiptMaster != null)
                    {
                        await receiptDetailRepository.DeleteAsync(x => x.ReceiptMasterId == receiptMaster.Id);
                        await ledgerPostingRepository.DeleteAsync(x =>
                            x.VoucherNo == receiptMaster.VoucherNo &&
                            x.VoucherNumbering == receiptMaster.VoucherNumbering &&
                            x.VoucherTypeId == receiptMaster.VoucherTypeId &&
                            x.FinancialYearId == receiptMaster.FinancialYearId);

                        await receiptMasterRepository.DeleteAsync(input.Id);

                        //await partyBalanceRepository.DeleteAsync(x => x.MasterVoucherNo == receiptMaster.VoucherNo &&
                        //                                              x.MasterVoucherTypeId ==
                        //                                              receiptMaster.VoucherTypeId &&
                        //                                              x.FinancialYearId == input.FinancialYearId &&
                        //                                              x.BranchId == receiptMaster.BranchId);
                    }
                }

                if (cashLedger.Id != (Guid)input.LedgerId)

                {
                    //first posting a lederPosting
                    var ledgerPosting = new LedgerPosting
                    {
                        TenantId = input.TenantId,
                        VoucherNumbering = input.VoucherNumbering,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = input.VoucherTypeId,
                        VoucherNo = input.VoucherNo,
                        LedgerId = (Guid)input.LedgerId,
                        DetailId = input.SalesAccountId,
                        Debit = input.NetAmount,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = input.VoucherNo,
                        PostingNumber = input.PostingNumbering,
                        MasterId = input.Id
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);


                    if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR or PaymentMethod.Cash)
                    {
                        var receiptMaster = await receiptMasterRepository.FirstOrDefaultAsync(e =>
                            e.RefVoucherTypeId == input.VoucherTypeId && e.RefVoucherNo == input.VoucherNo);
                        if (receiptMaster != null) await ReceiptVoucherPostingPos(input, input.VoucherNo);
                    }
                }

                input.IsErrorFixed = true;
                await salesMasterRepository.UpdateAsync(input);
            }
        }


        private async Task DateFixed()
        {
            var salesMasters = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId)
                .ToListAsync();
            foreach (var salesMaster in salesMasters)
            {
                var date = DateConverter.ConvertToEnglish(salesMaster.DateMiti);
                if (salesMaster.Date.Date != date.Date)
                {
                    salesMaster.Date = date;
                    await salesMasterRepository.UpdateAsync(salesMaster);
                }
            }
        }

        private async Task LedgerPostingDateFixed()
        {
            var salesMasters = await ledgerPostingRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.DateMiti != "")
                .Take(8000)
                .ToListAsync();
            foreach (var salesMaster in salesMasters)
            {
                var date = DateConverter.ConvertToEnglish(salesMaster.DateMiti);
                if (salesMaster.Date.Date != date.Date) salesMaster.Date = date;

                await ledgerPostingRepository.UpdateAsync(salesMaster);
            }
        }


        public async Task<decimal> GetSalesRate(Guid productId, Guid unitId, decimal quantityToSell)
        {
            return (await productRepository.FirstOrDefaultAsync(x => x.Id == productId)).SalesRate;

            //var unitConversion = await unitConversionRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
            //    .AsNoTracking()
            //    .Include(x => x.UnitFk).Select(x => new
            //    {
            //        x.ProductId,
            //        x.UnitId,
            //        x.PrimaryQty,
            //        x.Qty,
            //        UnitName = x.UnitFk.Name,
            //        x.ConversionRate
            //    }).ToListAsync();


            //var stockQuery = await stockPostingRepository.GetAll()
            //    .AsNoTracking()
            //    .Where(sp => sp.ProductId == productId && sp.FinancialYearId == FinancialYearId && !sp.IsDeleted)
            //    .Select(x => new
            //    {
            //        x.VoucherNumbering,
            //        x.Date,
            //        x.InWardQty,
            //        x.OutWardQty,
            //        x.Amount,
            //        x.UnitId
            //    }).ToListAsync();

            //var getConversion = unitConversion.FirstOrDefault(a => a.UnitId == unitId);
            //var minUnit = unitConversion.MinBy(a => a.ConversionRate);

            //var orderedUnits = unitConversion.OrderByDescending(arg => arg.ConversionRate).ToList();

            //var remainingQuantity = quantityToSell;
            //if (getConversion != null || minUnit != null)
            //    remainingQuantity = quantityToSell * getConversion.PrimaryQty *
            //                        minUnit.Qty /
            //                        (getConversion.Qty * minUnit.PrimaryQty);


            //decimal totalCost = 0;


            //foreach (var data in stockQuery.OrderBy(sp => sp.Date).ThenBy(e => e.VoucherNumbering).ToList())
            //{
            //    var unitConversionDetail = unitConversion.FirstOrDefault(a => a.UnitId == data.UnitId);
            //    if (unitConversionDetail == null || minUnit == null) continue;
            //    var availableQuantity = (data.InWardQty - data.OutWardQty) * unitConversionDetail.PrimaryQty *
            //                            minUnit.Qty /
            //                            (unitConversionDetail.Qty * minUnit.PrimaryQty);
            //    var rate = data.Amount / Math.Abs(availableQuantity);

            //    if (availableQuantity <= 0) continue;

            //    if (availableQuantity >= remainingQuantity)
            //    {
            //        totalCost += remainingQuantity * rate;
            //        remainingQuantity = 0;
            //    }
            //    else
            //    {
            //        totalCost += availableQuantity * rate;
            //        remainingQuantity -= availableQuantity;
            //    }

            //    if (remainingQuantity <= 0) break;
            //}


            //if (remainingQuantity > 0) return 0;
            //// throw new UserFriendlyException("Not enough stock to fulfill the sale.");
            //var inputUnit = unitConversion.FirstOrDefault(e => e.UnitId == unitId);
            //if (inputUnit != null)
            //{
            //    var finalrate = totalCost / quantityToSell;
            //    //throw new UserFriendlyException((finalrate * inputUnit.ConversionRate).ToString());

            //    var multiplyQty = inputUnit.PrimaryQty * minUnit.Qty /
            //                      (inputUnit.Qty * minUnit.PrimaryQty);
            //    var a = multiplyQty;
            //    return finalrate * multiplyQty;
            //}

            //return 0;
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


                var missData = await salesMasterRepository.GetAll().AsSplitQuery()
                    .Include(x => x.AccountLedgerFk)
                    .Where(x => x.LedgerId != null)
                    .Where(x => x.TenantId == AbpSession.TenantId && !accountId.Contains((Guid)x.LedgerId))
                    .Select(x => new { x.Id, x.LedgerId, x.AccountLedgerFk.Name, x.TenantId })
                    .AsNoTracking().ToListAsync();
                foreach (var item in missData)
                {
                    var accountLedger = accountLedgers.FirstOrDefault(x => x.Name == item.Name);
                    if (accountLedger != null)
                    {
                        var salesMaster = await salesMasterRepository.GetAll()
                            .Where(e => e.TenantId == AbpSession.GetTenantId())
                            .FirstOrDefaultAsync(e => e.Id == item.Id);
                        salesMaster.LedgerId = accountLedger.Id;
                        await salesMasterRepository.UpdateAsync(salesMaster);
                    }
                }
            }
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "SalesInvoice");
        }


        public async Task<SalesMasterForViewNewDto> GetSalesMasterForViewNew(Guid id)
        {
            var result = await salesMasterRepository.GetAll()
                .Where(x => x.Id == id)
                .AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .Select(x => new SalesMasterForViewNewDto
                {
                    Id = x.Id,
                    VoucherNo = x.VoucherNo,
                    Date = x.DateMiti,
                    PaymentMethod = x.PaymentMethod.ToString(),
                    LedgerName = x.AccountLedgerFk.Name,
                    TaxAmount = x.TaxAmount,
                    GrossAmount = x.GrossAmount,
                    VoucherTypeId = x.VoucherTypeId,
                    BillDiscount = x.BillDiscount,
                    TaxableAmount = x.TaxableAmount,
                    GrandTotal = x.GrandTotal,
                    TotalAmount = x.NetAmount
                })
                .FirstOrDefaultAsync() ?? throw new UserFriendlyException("Data not found");

            result.Details = await salesDetailRepository.GetAll()
                .Where(x => x.SalesMasterId == id)
                .AsNoTracking()
                .Include(x => x.TaxFk)
                .Include(x => x.ProductFk)
                .Include(x => x.UnitFk)
                .Select(x => new SalesMasterForViewDetailDto
                {
                    Qty = x.Qty,
                    Rate = x.Rate,
                    TaxName = x.TaxFk.Name,
                    TaxAmount = x.TaxAmount,
                    GrossAmount = x.GrossAmount,
                    NetAmount = x.NetAmount,
                    Discount = x.Discount,
                    TotalAmount = x.Amount,
                    ProductId = x.ProductId,
                    ProductName = x.ProductFk.Name,
                    UnitName = x.UnitFk.Name,
                })
                .ToListAsync();

            return result;
        }


        public async Task ImportSalesMasterFromExcel(IFormFile file)
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

            await BackgroundJobManager.EnqueueAsync<ImportSalesMasterToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                });
        }

        private async Task<List<Guid>> FuncRecursive(Guid id)
        {
            var result = await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.GroupUnder == id).AsNoTracking().Select(x => x.Id).ToListAsync();
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

        protected virtual async Task StockAndLedgerFixed()
        {
            var i = 200;
            var salesdetailslist = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().ToListAsync();
            foreach (var purchaseMaster in await salesMasterRepository.GetAll()
                         .Where(x => x.TenantId == AbpSession.TenantId).Where(x => !x.IsDelete)
                         .OrderBy(x => x.FinancialYearId).ToListAsync())
            {
                var tenantId = AbpSession.TenantId;
                if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

                if (purchaseMaster != null)
                {
                    i += 1;
                    var accountLedgerDetails =
                        await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                            .FirstOrDefaultAsync(x => x.Id == purchaseMaster.LedgerId);
                    //if (accountLedgerDetails != null)
                    //    if (accountLedgerDetails.IsBillByBill)
                    //        if (purchaseMaster.LedgerId != null)
                    //        {
                    //            var partyBalanceData = new PartyBalance
                    //            {
                    //                VoucherNumbering = purchaseMaster.VoucherNumbering,
                    //                Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                    //                LedgerId = (Guid)purchaseMaster.LedgerId,
                    //                FinancialYearId = purchaseMaster.FinancialYearId,
                    //                VoucherTypeId = purchaseMaster.VoucherTypeId,
                    //                VoucherNo = purchaseMaster.VoucherNo,
                    //                AgainstVoucherTypeId = Guid.Empty,
                    //                AgainstVoucherNo = "NA",
                    //                InvoiceNo = purchaseMaster.VoucherNo,
                    //                AgainstInvoiceNo = "NA",
                    //                ReferenceType = "New",
                    //                Debit = purchaseMaster.GrandTotal,
                    //                Credit = 0,
                    //                CreditPeriod = purchaseMaster.CreditPeriod,

                    //                BranchId = purchaseMaster.BranchId,
                    //                TenantId = tenantId
                    //            };
                    //            await partyBalanceRepository.InsertAsync(partyBalanceData);
                    //        }

                    var ledgerPosting = new LedgerPosting
                    {
                        TenantId = tenantId,
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                        DateMiti = purchaseMaster.DateMiti,
                        VoucherTypeId = purchaseMaster.VoucherTypeId,
                        VoucherNo = purchaseMaster.VoucherNo,
                        LedgerId = purchaseMaster.LedgerId ?? Guid.Empty,
                        DetailId = purchaseMaster.SalesAccountId,
                        Debit = purchaseMaster.GrandTotal,
                        FinancialYearId = purchaseMaster.FinancialYearId,
                        Credit = 0,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        PostingNumber = i,
                        MasterId = purchaseMaster.Id
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);

                    var ledgerPosting1 = new LedgerPosting
                    {
                        TenantId = tenantId,
                        VoucherNumbering = purchaseMaster.VoucherNumbering,
                        Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                        DateMiti = purchaseMaster.DateMiti,
                        VoucherTypeId = purchaseMaster.VoucherTypeId,
                        VoucherNo = purchaseMaster.VoucherNo,
                        LedgerId = purchaseMaster.SalesAccountId,
                        DetailId = (Guid)purchaseMaster.LedgerId,
                        Debit = 0,
                        FinancialYearId = purchaseMaster.FinancialYearId,
                        Credit = purchaseMaster.NetAmount,
                        InvoiceNo = purchaseMaster.VoucherNo,
                        PostingNumber = i,
                        MasterId = purchaseMaster.Id
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting1);

                    await ledgerPostingRepository.InsertAsync(ledgerPosting1);
                    var taxList = new List<TaxDetailDto>();

                    foreach (var inputSalesDetailDto in salesdetailslist.Where(
                                 x => x.SalesMasterId == purchaseMaster.Id))
                    {
                        await productRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                            .FirstOrDefaultAsync(x => x.Id == inputSalesDetailDto.ProductId);

                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = purchaseMaster.VoucherNumbering,
                            Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                            DateMiti = purchaseMaster.DateMiti,
                            LedgerId = purchaseMaster.LedgerId,
                            VoucherTypeId = purchaseMaster.VoucherTypeId,
                            VoucherNo = purchaseMaster.VoucherNo,
                            Amount = inputSalesDetailDto.Amount,
                            ProductId = inputSalesDetailDto.ProductId,
                            UnitId = inputSalesDetailDto.UnitId,
                            AgainstVoucherTypeId = Guid.Empty,
                            AgainstVoucherNo = "",
                            InWardQty = 0,
                            OutWardQty = inputSalesDetailDto.Qty,
                            Rate = inputSalesDetailDto.Rate,
                            FinancialYearId = purchaseMaster.FinancialYearId,
                            MasterId = purchaseMaster.Id,
                            TenantId = tenantId
                        };
                        await stockPostingRepository.InsertAsync(stockPosting);

                        if (inputSalesDetailDto.TaxId != Guid.Empty)
                        {
                            var tax = new TaxDetailDto
                            {
                                TaxId = inputSalesDetailDto.TaxId,
                                Amount = inputSalesDetailDto.TaxAmount
                            };

                            if (taxList.Select(x => x.TaxId).Contains(tax.TaxId))
                            {
                                var obj = taxList.FirstOrDefault(x => x.TaxId == tax.TaxId);
                                if (obj != null)
                                    obj.Amount = tax.Amount + obj.Amount;
                            }
                            else
                            {
                                taxList.Add(tax);
                            }
                        }
                    }

                    foreach (var tax in taxList)
                    {
                        var taxes = await taxRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                            .FirstOrDefaultAsync(x => x.Id == tax.TaxId);
                        if (taxes.Rate > 0)
                        {
                            var ledgerPosting2 = new LedgerPosting
                            {
                                TenantId = tenantId,
                                VoucherNumbering = purchaseMaster.VoucherNumbering,
                                Date = DateConverter.ConvertToEnglish(purchaseMaster.DateMiti),
                                DateMiti = purchaseMaster.DateMiti,
                                VoucherTypeId = purchaseMaster.VoucherTypeId,
                                VoucherNo = purchaseMaster.VoucherNo,
                                LedgerId = taxes.LedgerId,
                                DetailId = purchaseMaster.SalesAccountId,
                                Debit = 0,
                                FinancialYearId = purchaseMaster.FinancialYearId,
                                Credit = tax.Amount,
                                InvoiceNo = purchaseMaster.VoucherNo,
                                PostingNumber = i,
                                MasterId = purchaseMaster.Id,
                            };
                            await ledgerPostingRepository.InsertAsync(ledgerPosting2);
                        }
                    }
                }
            }
        }

        [DisableAuditing]
        public async Task<List<SalesProductTableDto>> GetAllProduct()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(x => new SalesProductTableDto
                {
                    ProductId = x.Id,
                    Name = x.Name,
                    Rate = x.SalesRate,
                    ProductGroupId = x.ProductGroupId,
                    UnitId = x.UnitId
                }).ToListAsync();
        }

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            if (await salesMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0)
                return true;
            return false;
        }

        public async Task<ProductWithPricingLevelDto> GetProductById(Guid productId)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("Invalid product ID", nameof(productId));

            //  var date = DateConverter.ConvertToEnglish(dateMiti);
            var tenantId = AbpSession.GetTenantId();

            // Combine product and tax query into a single database call with projection
            var productWithTax = await productRepository.GetAll()
                .Where(e => e.TenantId == tenantId && e.Id == productId)
                .Select(p => new
                {
                    Product = p,
                    TaxRate = taxRepository.GetAll()
                        .Where(t => t.TenantId == tenantId && t.Id == p.TaxId)
                        .Select(t => t.Rate)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (productWithTax?.Product == null)
                throw new UserFriendlyException("Product not found");

            var stock = await stockPostingRepository.GetAll().Where(x => x.ProductId == productId).SumAsync(x => x.InWardQty - x.OutWardQty);

            // Create the DTO with the retrieved data
            return new ProductWithPricingLevelDto
            {
                Id = productWithTax.Product.Id,
                ProductName = productWithTax.Product.Name,
                UnitId = productWithTax.Product.UnitId,
                ProductType = productWithTax.Product.ProductType,
                TaxId = productWithTax.Product.TaxId,
                TaxRate = productWithTax.TaxRate,
                Quantity = stock,
                Rate = productWithTax.Product.SalesRate
            };
        }
        public async Task<PdfForSalesInvoiceModel> GetSalesInvoiceForPdf(Guid id)
        {
            var salesInvoice = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.Id == id)
                .Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync();

            if (salesInvoice == null) throw new UserFriendlyException("Data not found");

            if (salesInvoice.LedgerId == null) throw new UserFriendlyException("Ledger not found");

            var ledgerBalanceString = "";

            var cashLedgers = await accountGroupRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Name == "Cash-in Hand")
                .AsNoTracking().Select(x => x.Id).ToListAsync();

            if (!cashLedgers.Contains((Guid)salesInvoice.LedgerId))
            {
                var ledgerBalance = await ledgerPostingRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId
                                && x.FinancialYearId == FinancialYearId
                                && x.LedgerId == salesInvoice.LedgerId && x.Date.Date < salesInvoice.Date.Date)
                    .SumAsync(x => x.Debit - x.Credit);
                if (ledgerBalance > 0)
                    ledgerBalanceString = Math.Round(ledgerBalance, 2).ToString();
            }


            var isTaxEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(
                ErpSettings.IsTaxEnabled, AbpSession.GetTenantId());
            //var invoiceName = await SettingManager.GetSettingValueForTenantAsync(
            //    PrintSettings.InvoiceText, AbpSession.GetTenantId());
            var mainBranch = await branchRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId && e.IsMain)
                .FirstOrDefaultAsync();


            var userName = await GetCurrentUserAsync();
            var salesDetails = await salesDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.SalesMasterId == id)
                .AsNoTracking()
                .Include(x => x.ProductFk)
                .Include(x => x.UnitFk)
                .ToListAsync();


            var tzf = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
            var dt = TimeZoneInfo.ConvertTime(DateTime.Now, tzf);
            var branchPhone = mainBranch.PhoneNo1;
            if (!string.IsNullOrWhiteSpace(mainBranch.PhoneNo2))
                branchPhone += $"/{mainBranch.PhoneNo2}";

            var list = new List<PdfForSalesInvoiceDetailModel>();
            var serial = 1;

            // Fetch all unit conversions related to the products in the sales details
            var productIds = salesDetails.Select(sd => sd.ProductId).Distinct().ToList();
            //var unitConversions = await unitConversionRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId && productIds.Contains(x.ProductId) && x.ConversionRate != 1)
            //    .AsNoTracking()
            //    .Include(x => x.UnitFk)
            //    .ToListAsync();

            foreach (var objSalesDetail in salesDetails)
            {
                //var unitConversion =
                //    unitConversions.FirstOrDefault(x => x.ProductId == objSalesDetail.ProductId && x.ConversionRate != 1);

                decimal? altQty = null;
                var altUnit = string.Empty;

                //if (unitConversion != null)
                //{
                //    altQty = unitConversion.UnitId == objSalesDetail.UnitId
                //        ? objSalesDetail.Qty
                //        : Math.Round(objSalesDetail.Qty / unitConversion.PrimaryQty * unitConversion.Qty, 2);
                //    altUnit = unitConversion.UnitFk.Name;
                //}

                var productNameParts = objSalesDetail.ProductName.Split("=>");
                var productName = productNameParts[0];
                var packaging = productNameParts.Length > 1 ? productNameParts[1] : "";

                var sales = new PdfForSalesInvoiceDetailModel
                {
                    SlNo = serial++,
                    ProductName = productName,
                    Packaging = packaging,
                    Quantity = objSalesDetail.Qty,
                    HsCode = objSalesDetail.ProductFk.HsCode,
                    Unit = objSalesDetail.UnitFk.Name,
                    AltQty = altQty,
                    AltUnit = altUnit,
                    DiscountPer = objSalesDetail.DiscountPer,
                    Rate = objSalesDetail.Rate,
                    Amount = objSalesDetail.GrossAmount
                };
                list.Add(sales);
            }

            //customer edit infomation
            var ledgerName = salesInvoice.AccountLedgerFk.Name;
            if (ledgerName.ToLower() == "cash" && salesInvoice.LedgerName != string.Empty &&
                salesInvoice.LedgerName.ToLower() != "cash") ledgerName = $"{salesInvoice.LedgerName}";

            var result = new PdfForSalesInvoiceModel
            {
                BranchName = mainBranch.CompanyName,
                BranchCode = mainBranch.BranchCode,
                Date = salesInvoice.Date,
                LrNo = salesInvoice.LrNo,
                LedgerBalance = ledgerBalanceString,
                CompanyEmail = mainBranch.Email,
                Address = mainBranch.Address,
                Province = mainBranch.State.ToString(),
                DateMiti = DateConverter.ConvertToNepali(salesInvoice.Date),
                BranchPan = mainBranch.PANumber,
                BranchPhone = branchPhone,
                TermsOfPayment = salesInvoice.PaymentMethod.ToString(),
                PrintDate = DateConverter.ConvertToNepali(dt) + "  Time : " + dt.ToString("h:mm:ss tt"),
                Logo1 = mainBranch.Image1,
                ChalanNo = salesInvoice.AgainstVoucherNo,
                //AgainstId = GetGuids(salesInvoice.AgainstId),
                PrintedDateTime = DateTime.Today,
                PiNumber = salesInvoice.PINumber,
                PrintUser = userName.UserName,
                VoucherTypeName = "Sales Invoice",
                OrderNo = salesInvoice.VoucherNo,
                LedgerName = salesInvoice.LedgerName,
                GrossAmount = salesInvoice.GrossAmount,
                TotalAmountInWord = CurrencyToAmount.AmountWords(salesInvoice.GrandTotal),
                TotalAmount = salesInvoice.NetAmount,
                DiscountAmount = salesInvoice.BillDiscount,
                GrandTotal = salesInvoice.GrandTotal,
                TaxAmount = salesInvoice.TaxAmount,
                TaxableAmount = salesInvoice.TaxableAmount,
                CustomerAddress = salesInvoice.CustomerAddress,
                CustomerName = ledgerName,
                CustomerPhone = salesInvoice.CustomerPhoneNo,
                CustomerEmail = salesInvoice.AccountLedgerFk.Email,
                CustomerPan = salesInvoice.VatNo,
                VehicleNumber = salesInvoice.LrNo,
                Description = salesInvoice.Description,
                InvoiceName = "INVOICE",
                NoOfCopy = "",
                ApprovedBy = "",
                ReceivedBy = "",
                SalesInvoiceDetail = list
            };

            if (!salesInvoice.IsDelete)
            {
                if (isTaxEnabled)
                {
                    if (salesInvoice.NoOfPrint > 2)
                    {
                        var noOfPrint = salesInvoice.NoOfPrint - 2;
                        result.InvoiceName = "INVOICE";
                        result.NoOfCopy = "Copy of original | No.of print# :" + noOfPrint;
                    }
                    else if (salesInvoice.NoOfPrint == 2)
                    {
                        result.InvoiceName = "INVOICE";
                    }
                    else if (salesInvoice.NoOfPrint == 1)
                    {
                        result.InvoiceName = "TAX INVOICE";
                        salesInvoice.PrintedTime = DateConverter.ConvertToNepali(dt) + "  Time : " +
                                                   dt.ToString("h:mm:ss tt");
                    }
                }
                else
                {
                    if (salesInvoice.NoOfPrint > 2)
                    {
                        var noOfPrint = salesInvoice.NoOfPrint - 2;
                        result.InvoiceName = "INVOICE";
                        result.NoOfCopy = "Copy of original | No.of print# :" + noOfPrint;
                    }
                    else if (salesInvoice.NoOfPrint == 2 || salesInvoice.NoOfPrint == 1)
                    {
                        result.InvoiceName = "INVOICE";
                    }

                    salesInvoice.PrintedTime = DateConverter.ConvertToNepali(dt) + "  Time : " +
                                               dt.ToString("h:mm:ss tt");
                }
            }
            else
            {
                result.NoOfCopy = "CANCELLED";
                result.InvoiceName = "TAX INVOICE";
            }

            salesInvoice.NoOfPrint += 1;
            salesInvoice.IsPrint = true;
            await salesMasterRepository.UpdateAsync(salesInvoice);

            return result;
        }

        public async Task<PdfForSalesPosModel> GetSalesPosForPdf(Guid id)
        {
            var serial = 1;
            var list = new List<PdfForSalesPosDetailModel>();

            var salesInvoice = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync(x => x.Id == id);


            if (salesInvoice != null)
            {
                var mainbranch = await branchRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(x => x.IsMain);
                var tzf = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
                var dt = TimeZoneInfo.ConvertTime(DateTime.Now, tzf);
                var branchPhone = mainbranch.PhoneNo1;
                if (!string.IsNullOrWhiteSpace(mainbranch.PhoneNo2))
                    branchPhone += $"/{mainbranch.PhoneNo2}";
                var salesDetails = await salesDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                    .Where(x => x.SalesMasterId == id)
                    .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync();

                foreach (var objSalesDetail in salesDetails)
                {
                    var sales = new PdfForSalesPosDetailModel
                    {
                        SlNo = serial++,
                        ProductName = objSalesDetail.ProductName.Split("=>")[0],
                        Quantity = objSalesDetail.Qty,
                        HsCode = objSalesDetail.ProductFk.HsCode,
                        Rate = objSalesDetail.Rate,
                        GrossAmount = objSalesDetail.GrossAmount,
                        Amount = objSalesDetail.Amount
                    };
                    list.Add(sales);
                }

                var isInvoice = false;
                var copyString = "";
                if (salesInvoice.NoOfPrint == 2 && salesInvoice.SalesType == SalesType.TI)
                    isInvoice = true;
                if (salesInvoice.NoOfPrint > 2 && salesInvoice.SalesType == SalesType.TI)
                {
                    copyString = "Copy of Original || No of print " + (salesInvoice.NoOfPrint - 2);
                    isInvoice = true;
                }

                if (salesInvoice.NoOfPrint > 1 && salesInvoice.SalesType == SalesType.ABT)
                    copyString = "Copy of Original || No of print " + (salesInvoice.NoOfPrint - 1);
                var result = new PdfForSalesPosModel
                {
                    BranchName = mainbranch.CompanyName,
                    Address = mainbranch.Address,
                    DateMiti = DateConverter.ConvertToNepali(salesInvoice.Date),
                    BranchPan = mainbranch.PANumber,
                    BranchPhone = branchPhone,
                    TaxAmount = salesInvoice.TaxAmount,
                    TermsOfPayment = salesInvoice.PaymentMethod.ToString(),
                    PrintDate = DateConverter.ConvertToNepali(dt) + "  Time : " +
                                dt.ToString("h:mm:ss tt"),
                    OrderNo = salesInvoice.VoucherNo,
                    TotalAmountInWord = CurrencyToAmount.AmountWords(salesInvoice.GrandTotal),
                    GrandTotal = salesInvoice.GrandTotal,
                    CustomerName = salesInvoice.LedgerName,
                    CustomerPan = salesInvoice.VatNo,
                    NoOfCopy = copyString,
                    IsInvoice = isInvoice,
                    SalesInvoiceDetail = list
                };

                salesInvoice.PrintedTime =
                    DateConverter.ConvertToNepali(dt) + "  Time : " + dt.ToString("h:mm:ss tt");

                salesInvoice.NoOfPrint += 1;
                salesInvoice.IsPrint = true;
                await salesMasterRepository.UpdateAsync(salesInvoice);
                return result;
            }

            throw new UserFriendlyException("Data not found");
        }


        public async Task<PdfForSalesPosModel> GetSalesInvoiceForPdfPos(Guid id)
        {
            var serial = 1;
            var list = new List<PdfForSalesPosDetailModel>();

            var salesInvoice = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync(x => x.Id == id);

            var isTax = await SettingManager.GetSettingValueForTenantAsync<bool>(
                ErpSettings.IsTaxEnabled, AbpSession.GetTenantId());
            //var invoiceName = await SettingManager.GetSettingValueForTenantAsync(
            //    PrintSettings.InvoiceText, AbpSession.GetTenantId());

            if (salesInvoice != null)
            {
                var mainbranch = await branchRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(x => x.IsMain);

                var userName = await GetCurrentUserAsync();
                var tzf = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
                var dt = TimeZoneInfo.ConvertTime(DateTime.Now, tzf);
                var branchPhone = mainbranch.PhoneNo1;
                if (!string.IsNullOrWhiteSpace(mainbranch.PhoneNo2))
                    branchPhone += $"/{mainbranch.PhoneNo2}";
                var salesDetails = await salesDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                    .Where(x => x.SalesMasterId == id)
                    .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync();

                foreach (var objSalesDetail in salesDetails)
                {
                    var sales = new PdfForSalesPosDetailModel
                    {
                        SlNo = serial++,
                        ProductName = objSalesDetail.ProductFk.Name.Split("=>")[0],
                        Quantity = objSalesDetail.Qty,
                        Rate = objSalesDetail.Rate,
                        Amount = objSalesDetail.Amount
                    };
                    list.Add(sales);
                }

                var result = new PdfForSalesPosModel
                {
                    BranchName = mainbranch.CompanyName,
                    Address = mainbranch.Address,
                    DateMiti = DateConverter.ConvertToNepali(salesInvoice.Date),
                    BranchPan = mainbranch.PANumber,
                    BranchPhone = branchPhone,
                    TermsOfPayment = salesInvoice.PaymentMethod.ToString(),
                    PrintDate = DateConverter.ConvertToNepali(dt) + ", " + dt.ToString("h:mm:ss tt"),
                    OrderNo = salesInvoice.VoucherNo,
                    TotalAmountInWord = CurrencyToAmount.AmountWords(salesInvoice.GrandTotal),
                    GrandTotal = salesInvoice.GrandTotal,
                    CustomerName = salesInvoice.AccountLedgerFk.Name,
                    CustomerPan = salesInvoice.AccountLedgerFk.Pan,
                    NoOfCopy = "",
                    SalesInvoiceDetail = list
                };
                salesInvoice.PrintedTime =
                    DateConverter.ConvertToNepali(dt) + "  Time : " + dt.ToString("h:mm:ss tt");

                salesInvoice.NoOfPrint += 1;
                salesInvoice.IsPrint = true;
                await salesMasterRepository.UpdateAsync(salesInvoice);
                return result;
            }

            throw new UserFriendlyException("Data not found");
        }


        public async Task<List<PdfForSalesInvoiceModel>> GetSalesInvoiceForVatService()
        {
            var salesInvoiceList = await salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).ToListAsync();
            // var printSettings = (await _tenantSettingsApp.GetAllSettings()).PrintSettings;
            var isTax = await SettingManager.GetSettingValueForTenantAsync<bool>(
                ErpSettings.IsTaxEnabled, AbpSession.GetTenantId());

            var billlist = new List<PdfForSalesInvoiceModel>();
            foreach (var salesInvoice in salesInvoiceList)
                if (salesInvoice != null)
                {
                    var accountLedgerDetailss =
                        await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                            .FirstOrDefaultAsync(x => x.Id == salesInvoice.LedgerId);
                    var mainbranch = await branchRepository.GetAll()
                        .Where(e => e.TenantId == AbpSession.GetTenantId()).FirstOrDefaultAsync(x => x.IsMain);

                    var userName = await GetCurrentUserAsync();
                    var tzf = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
                    var dt = TimeZoneInfo.ConvertTime(DateTime.Now, tzf);
                    var branchPhone = mainbranch.PhoneNo1;
                    if (mainbranch.PhoneNo2.Length > 0) branchPhone += $"/{mainbranch.PhoneNo2}";

                    var invoiceName = "TAX INVOICE";
                    var noOfCopy = string.Empty;
                    if (!salesInvoice.IsDelete)
                    {
                        if (isTax)
                        {
                            switch (salesInvoice.NoOfPrint)
                            {
                                case 1:
                                    invoiceName = "TAX INVOICE";
                                    noOfCopy = "";
                                    break;
                                case 2:
                                    invoiceName = "INVOICE";
                                    noOfCopy = "";
                                    break;
                            }

                            if (salesInvoice.NoOfPrint > 2)
                            {
                                var noOfPrint = salesInvoice.NoOfPrint - 2;
                                invoiceName = "TAX INVOICE";
                                noOfCopy = "Copy of original | No.of print# :" + noOfPrint;
                            }

                            salesInvoice.NoOfPrint += 1;
                            await salesMasterRepository.UpdateAsync(salesInvoice);
                        }
                    }
                    else
                    {
                        noOfCopy = "CANCELLED";
                        invoiceName = "TAX INVOICE";
                    }

                    var result = new PdfForSalesInvoiceModel
                    {
                        BranchName = mainbranch.CompanyName,
                        Date = salesInvoice.Date,
                        CompanyEmail = mainbranch.Email,
                        Address = mainbranch.Address,
                        Province = mainbranch.State.ToString(),
                        DateMiti = DateConverter.ConvertToNepali(salesInvoice.Date),
                        BranchPan = mainbranch.PANumber,
                        BranchPhone = branchPhone,
                        TermsOfPayment = Enum.GetName(typeof(PaymentMethod), salesInvoice.PaymentMethod),
                        PrintDate = DateConverter.ConvertToNepali(dt) + "  Time : " +
                                    dt.ToString("h:mm:ss tt"),
                        Logo1 = mainbranch.Image1,
                        ChalanNo = "",
                        PrintedDateTime = DateTime.Today,
                        PrintUser = userName.UserName,
                        VoucherTypeName = "Sales Invoice",
                        OrderNo = salesInvoice.VoucherNo,
                        LedgerName = accountLedgerDetailss.Name,
                        TotalAmountInWord = CurrencyToAmount.AmountWords(salesInvoice.GrandTotal),
                        TotalAmount = salesInvoice.NetAmount,
                        DiscountAmount = salesInvoice.BillDiscount,
                        GrandTotal = salesInvoice.GrandTotal,
                        TaxAmount = salesInvoice.TaxAmount,
                        TaxableAmount = salesInvoice.TaxableAmount,
                        CustomerAddress = salesInvoice.CustomerAddress,
                        CustomerName = accountLedgerDetailss.Name,
                        CustomerPhone = accountLedgerDetailss.Phone,
                        CustomerEmail = accountLedgerDetailss.Email,
                        CustomerPan = accountLedgerDetailss.Pan,
                        VehicleNumber = salesInvoice.LrNo,
                        Description = salesInvoice.Description,
                        InvoiceName = invoiceName,
                        NoOfCopy = noOfCopy,
                        ApprovedBy = null,
                        ReceivedBy = null
                    };

                    var serial = 1;

                    var list = new List<PdfForSalesInvoiceDetailModel>();

                    var salesDetails = await salesDetailRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                        .Where(x => x.SalesMasterId == salesInvoice.Id)
                        .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync();
                    foreach (var objSalesDetail in salesDetails)
                    {
                        //var unitconversion = await unitConversionRepository.GetAll()
                        //    .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking().Include(x => x.UnitFk)
                        //    .FirstOrDefaultAsync(x => x.ProductId == objSalesDetail.ProductId && x.Qty != 1);
                        decimal altQty = 0;
                        //   if (unitconversion != null) altQty = objSalesDetail.Qty / unitconversion.PrimaryQty;


                        var sales = new PdfForSalesInvoiceDetailModel
                        {
                            SlNo = serial++,
                            ProductName = objSalesDetail.ProductFk.Name.Split("=>")[0],
                            Packaging = objSalesDetail.ProductFk.Name.Split("=>").Length > 1
                                ? objSalesDetail.ProductFk.Name.Split("=>")[1]
                                : "",
                            Quantity = objSalesDetail.Qty,
                            Unit = objSalesDetail.UnitFk.Name,
                            AltQty = altQty,
                            DiscountPer = objSalesDetail.DiscountPer,
                            Rate = objSalesDetail.Rate,
                            Amount = objSalesDetail.GrossAmount
                        };
                        list.Add(sales);
                    }

                    result.SalesInvoiceDetail = list;
                    billlist.Add(result);
                }

            return billlist;
        }



        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var salesMaster = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x =>
                    x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = salesMaster.VoucherTypeId,
                VoucherNo = salesMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesMasters)]
        public async Task<List<SalesMasterAccountLedgerTableDto>> GetAllSalesAccountForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sales Account")
                .Select(accountLedger => new SalesMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).OrderBy(x => x.Id).AsNoTracking().ToListAsync();
        }


        public void GetDateUpdate()
        {
            var data = salesMasterRepository.GetAllList();
            foreach (var item in data)
            {
                item.DateMiti = DateConverter.ConvertToNepali(Convert.ToDateTime(item.Date));
                item.CreditDate = item.Date.AddDays(item.CreditPeriod);
                salesMasterRepository.UpdateAsync(item);
            }
        }

        protected async Task<int> GetInlineVoucherNo(SalesType salesType)
        {
            var data = await salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId && x.SalesType == salesType)
                .Select(x => x.VoucherNumbering).ToListAsync();
            if (data.Count() == 0)
            {
                if (salesType == SalesType.Sales)
                {
                    var voucherNumbering =
                        await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "SalesInvoice");
                    return voucherNumbering.StartIndex;
                }

                if (salesType == SalesType.TI)
                {
                    var voucherNumbering =
                        await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "TI");
                    return voucherNumbering.StartIndex;
                }

                if (salesType == SalesType.ABT)
                {
                    var voucherNumbering =
                        await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ABT");
                    return voucherNumbering.StartIndex;
                }
            }

            return data.Max() + 1;
        }

        private async Task<string> GetReceiptMasterVoucherNo()
        {
            // Get the next inline voucher number
            var nextVoucherNumber = await GetReceiptMasterInlineVoucherNo();

            // Get voucher numbering format
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ReceiptVoucher");

            // Format and return the full voucher number
            var vhNo = $"{voucherNumbering.Prefix}{nextVoucherNumber}{voucherNumbering.Postfix}";

            if (await receiptMasterRepository.CountAsync(e => e.VoucherNo == vhNo && e.FinancialYearId == FinancialYearId) >
                0)
            {
                // If the voucher number already exists, increment it and format again
                nextVoucherNumber++;
                vhNo = $"{voucherNumbering.Prefix}{nextVoucherNumber}{voucherNumbering.Postfix}";
            }

            return vhNo;
        }

        protected async Task<int> GetReceiptMasterInlineVoucherNo()
        {
            // Get voucher numbering info (for default start index)
            var voucherNumbering =
                await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ReceiptVoucher");

            // Query executed only once with Max performed at database level
            var maxVoucherNumber = receiptMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering)
                .AsEnumerable()
                .DefaultIfEmpty(0) // Handle empty result set directly
                .Max();

            // If no existing vouchers, use the starting index from voucher numbering
            if (maxVoucherNumber == 0) return voucherNumbering.StartIndex;

            // Otherwise, increment the max value
            return maxVoucherNumber + 1;
        }

        //  [UnitOfWork]
        [AbpAuthorize(AppPermissions.PagesSalesMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditSalesMasterDto input)
        {
            // Validate date against financial year
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            if (FinancialYear.FromDate > date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            if (FinancialYear.ToDate < date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");


            // Validate date for IRD compliance
            if (
                AbpSession.TenantId != null &&
                await SettingManager.GetSettingValueForTenantAsync<bool>(ErpSettings.IsTaxEnabled,
                    AbpSession.GetTenantId()) &&
                DateConverter.ConvertToEnglish(input.DateMiti).Date != DateTime.Today.Date)
                throw new UserFriendlyException("Please enter today's date");

            using var unitOfWork = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            try
            {
                var postingNumbering = PostingNumbering;
                var tenantId = AbpSession.GetTenantId();

                // Get account ledger information
                var getAccountLedger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);
                if (getAccountLedger == null)
                    throw new UserFriendlyException("Ledger not found");

                // Determine sales type and get voucher information
                var (salesType, voucherTypeId, voucherNumbering, voucherNo) = await DetermineSalesTypeAndVoucher(input);

                // Pre-calculate totals
                var totals = CalculateSalesTotals(input);

                // Create sales master entity
                var salesMaster = new SalesMaster
                {
                    VoucherNumbering = voucherNumbering,
                    TenantId = tenantId,
                    VoucherNo = voucherNo,
                    SalesAccountId = input.SalesAccountId,
                    Date = date,
                    CreditPeriod = input.CreditPeriod,
                    Description = input.Description,
                    DateMiti = input.DateMiti,
                    CustomerAddress = input.CustomerAddress,
                    CreditDate = date.AddDays(input.CreditPeriod),
                    PINumber = input.PiNumber,
                    TaxAmount = Math.Round(totals.TaxAmount, 2),
                    TaxableAmount = Math.Round(totals.TaxableAmount, 2),
                    BillDiscount = Math.Round(totals.Discount, 2),
                    GrandTotal = Math.Round(totals.GrandTotal, 2),
                    GrossAmount = input.TotalAmount,
                    NetAmount = Math.Round(totals.NetAmount, 2),
                    IsPrint = input.IsPrint,
                    PaymentMethodLedgerId = input.PaymentMethodLedgerId == Guid.Empty ? null : input.PaymentMethodLedgerId,
                    NoOfPrint = 1,
                    SyncwithIrd = false,
                    PrintedTime = input.IsPrint ? DateTime.Now.ToString("h:mm:ss tt") : "",
                    IsRealTime = false,
                    PaymentMethod = input.PaymentMethod,
                    IsDelete = false,
                    InvoiceType = input.InvoiceType,
                    VatRefundAmount = input.VatRefundAmount,
                    LrNo = input.LrNo,
                    PrintUserId = AbpSession.TenantId,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.LedgerId,
                    VehicleNo = input.VehicleNo,
                    AgainstId = input.AgainstId != null ? string.Join(',', input.AgainstId) : null,
                    SalesModeType = input.SalesModeType,
                    AgainstVoucherNo = input.AgainstVoucherNo,
                    FinancialYearId = FinancialYearId,
                    CreateUserId = AbpSession.UserId,
                    UpdateUserId = null,
                    LedgerName = string.IsNullOrEmpty(input.CustomerName) ? getAccountLedger.Name : input.CustomerName,
                    CustomerPhoneNo = input.CustomerPhoneNo,
                    VatNo = input.CustomerVatNo,
                    PostingNumbering = postingNumbering,
                    IrdSyncDateTime = DateTime.Now,
                    SalesType = salesType,
                    ServiceDeliveryId = "",
                    SourceModule = input.SourceModule,
                    SourceDocumentId = input.SourceDocumentId
                };

                // Insert sales master and get ID
                var salesMasterId = await salesMasterRepository.InsertAndGetIdAsync(salesMaster);

                // Process all sales details - batch prepare
                var stockPostingsToInsert = new List<StockPosting>();
                var salesDetailsToInsert = new List<SalesDetail>();
                var taxList = new List<TaxDetailDto>();

                foreach (var inputSalesDetailDto in input.SalesDetails.Where(e => e.Qty > 0))
                {
                    var (detail, taxDetail) = await CreateSalesDetail(
                        inputSalesDetailDto,
                        input,
                        salesMasterId,
                        tenantId,
                        voucherTypeId,
                        voucherNo,
                        voucherNumbering,
                        stockPostingsToInsert);

                    salesDetailsToInsert.Add(detail);

                    if (taxDetail != null && taxDetail.TaxId != Guid.Empty)
                    {
                        // Aggregate tax details by tax ID
                        var existingTax = taxList.FirstOrDefault(x => x.TaxId == taxDetail.TaxId);
                        if (existingTax != null)
                            existingTax.Amount += taxDetail.Amount;
                        else
                            taxList.Add(taxDetail);
                    }
                }

                // Batch insert sales details
                if (salesDetailsToInsert.Count > 0) await salesDetailRepository.InsertRangeAsync(salesDetailsToInsert);

                // Batch insert stock postings
                if (stockPostingsToInsert.Count > 0)
                    foreach (var postingStock in stockPostingsToInsert)
                    {
                        var stockId = await stockPostingRepository.InsertAndGetIdAsync(postingStock);

                    }

                // Batch insert IMEIs


                // Create tax ledger postings
                var taxLedgerPostings = new List<LedgerPosting>();
                foreach (var tax in taxList)
                {
                    var taxes = await taxRepository.GetAll()
                        .FirstOrDefaultAsync(x => x.Id == tax.TaxId && x.TenantId == tenantId);

                    if (taxes?.Rate > 0)
                        taxLedgerPostings.Add(new LedgerPosting
                        {
                            TenantId = tenantId,
                            VoucherNumbering = voucherNumbering,
                            Date = date,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = voucherNo,
                            LedgerId = taxes.LedgerId,
                            DetailId = input.SalesAccountId,
                            Debit = 0,
                            FinancialYearId = FinancialYearId,
                            Credit = tax.Amount,
                            InvoiceNo = voucherNo,
                            PostingNumber = postingNumbering,
                            MasterId = salesMasterId
                        });
                }

                if (taxLedgerPostings.Count > 0) await ledgerPostingRepository.InsertRangeAsync(taxLedgerPostings);

                // Create party balance if applicable
                if (getAccountLedger.IsBillByBill)
                    await partyBalanceService.CreateSalesEntryAsync(new PartyBalanceNewEntryDto
                    {
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DueDate = DateConverter.ConvertToEnglish(input.DateMiti).AddDays(input.CreditPeriod),
                        LedgerId = input.LedgerId,
                        VoucherNo = salesMaster.VoucherNo,
                        VoucherNumbering = salesMaster.VoucherNumbering,
                        Amount = input.GrandTotal,
                        MasterId = salesMasterId,
                    });
                //  await CreatePartyBalance(input, voucherTypeId, voucherNumbering, voucherNo, salesMasterId, tenantId);

                // Create sales account ledger posting
                await CreateSalesAccountLedgerPosting(input, voucherTypeId, voucherNumbering, voucherNo, salesMasterId,
                    totals.NetAmount, postingNumbering, tenantId);

                // Create payment ledger posting based on payment method
                await CreatePaymentLedgerPosting(
                    input,
                    voucherTypeId,
                    voucherNumbering,
                    voucherNo,
                    salesMasterId,
                    totals.GrandTotal,
                    salesMaster,
                    getAccountLedger,
                    postingNumbering,
                    tenantId);

                // Send SMS if requested
                //if (input.IsSendSms && !string.IsNullOrEmpty(getAccountLedger?.Phone))
                //    await SendSms(getAccountLedger.Phone);

                await unitOfWork.CompleteAsync();
                return salesMasterId;
            }

            catch (Exception ex)
            {
                // Rollback transaction on error
                unitOfWork.Dispose();
                throw new UserFriendlyException(L("Error"), ex.Message);
            }
        }


        private async Task<(SalesType SalesType, Guid VoucherTypeId, int VoucherNumbering, string VoucherNo)>
            DetermineSalesTypeAndVoucher(CreateOrEditSalesMasterDto input)
        {
            var salesType = SalesType.Sales;
            Guid voucherTypeId;
            var voucherNo = input.VoucherNo;
            int voucherNumbering;

            var isAbt = false;
            //await SettingManager.GetSettingValueForTenantAsync<bool>(
            //ErpSettings.IsAbt, AbpSession.GetTenantId());
            var isIrdSoftware = true;
            //await SettingManager.GetSettingValueForTenantAsync<bool>(
            //ErpSettings.IsIRDSoftware, AbpSession.GetTenantId());

            if (isIrdSoftware)
            {
                if (isAbt)
                {
                    if (input.GrandTotal >= 10000)
                    {
                        salesType = SalesType.TI;
                        voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("TI");
                        voucherNumbering = await GetInlineVoucherNo(salesType);
                        voucherNo = await GetSalesMasterVoucherNo(salesType);
                    }
                    else
                    {
                        salesType = SalesType.ABT;
                        voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ABT");
                        voucherNumbering = await GetInlineVoucherNo(salesType);
                        voucherNo = await GetSalesMasterVoucherNo(salesType);
                    }
                }
                else
                {
                    voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                    voucherNumbering = await GetInlineVoucherNo(salesType);
                    voucherNo = await GetSalesMasterVoucherNo(salesType);
                }
            }
            else
            {
                voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                voucherNumbering = await GetInlineVoucherNo(salesType);

                var getVoucherType =
                    await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "SalesInvoice");

                switch (getVoucherType)
                {
                    case "Automatic":
                        voucherNo = await GetSalesMasterVoucherNo(salesType);
                        if (await salesMasterRepository.CountAsync(x =>
                                x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0)
                            throw new UserFriendlyException("Sales VoucherNo is Duplicate");
                        break;

                    case "Manually" when await salesMasterRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) > 0:
                        throw new UserFriendlyException("Sales VoucherNo is Duplicate");
                }
            }

            return (salesType, voucherTypeId, voucherNumbering, voucherNo);
        }

        private (decimal TaxAmount, decimal TaxableAmount, decimal NetAmount, decimal GrandTotal, decimal Discount)
            CalculateSalesTotals(CreateOrEditSalesMasterDto input)
        {
            decimal taxAbleAmount = 0, taxAmount = 0, grandTotal = 0, discount = 0, netAmount = 0;

            foreach (var detail in input.SalesDetails.Where(e => e.Qty > 0))
            {
                taxAbleAmount += detail.TaxAmount > 0 ? detail.NetAmount ?? 0 : 0;
                taxAmount += detail.TaxAmount ?? 0;
                grandTotal += detail.Amount ?? 0;
                discount += detail.Discount ?? 0;
                netAmount += detail.NetAmount ?? 0;
            }

            return (taxAmount, taxAbleAmount, netAmount, grandTotal, discount);
        }

        private (decimal TaxAmount, decimal TaxableAmount, decimal NetAmount, decimal GrandTotal, decimal Discount)
            AdjustTotalsForLoyalty(
                (decimal TaxAmount, decimal TaxableAmount, decimal NetAmount, decimal GrandTotal, decimal Discount) totals,
                decimal loyaltyAmount)
        {
            var adjusted = totals;
            adjusted.TaxableAmount -= loyaltyAmount;
            adjusted.NetAmount -= loyaltyAmount;
            adjusted.TaxAmount = adjusted.TaxableAmount * 13 / 100;
            adjusted.GrandTotal = adjusted.NetAmount + adjusted.TaxAmount;
            return adjusted;
        }



        private async Task<(SalesDetail Detail, TaxDetailDto TaxDetail)> CreateSalesDetail(
            SalesDetailDto inputSalesDetailDto,
            CreateOrEditSalesMasterDto input,
            Guid salesMasterId,
            int tenantId,
            Guid voucherTypeId,
            string voucherNo,
            int voucherNumbering,
            List<StockPosting> stockPostingsToInsert)
        {
            var againstDetailId = inputSalesDetailDto.SalesDetailId == Guid.Empty
                ? null
                : inputSalesDetailDto.SalesDetailId;



            var productData = await productRepository.GetAsync(inputSalesDetailDto.ProductId);
            // Create sales detail
            var detail = new SalesDetail
            {
                Id = inputSalesDetailDto.Id.HasValue && inputSalesDetailDto.Id.Value != Guid.Empty
                    ? inputSalesDetailDto.Id.Value
                    : Guid.NewGuid(),
                TenantId = tenantId,
                SalesMasterId = salesMasterId,
                Qty = inputSalesDetailDto.Qty,
                Rate = inputSalesDetailDto.Rate,
                Discount = inputSalesDetailDto.Discount ?? 0,
                ProductId = inputSalesDetailDto.ProductId,
                UnitId = inputSalesDetailDto.UnitId,
                ProductName = productData.Name,
                TaxId = inputSalesDetailDto.TaxId
            };

            var taxRate = ERPCommonManager.GetTaxRate(inputSalesDetailDto.TaxId);

            // Calculate detail amounts
            if (taxRate > 0)
            {
                detail.Amount = inputSalesDetailDto.Amount ?? 0;
                detail.NetAmount = detail.Amount / (1 + taxRate);
                detail.GrossAmount = detail.NetAmount + inputSalesDetailDto.Discount ?? 0;
                detail.TaxAmount = detail.NetAmount * taxRate;
                detail.DiscountPer = inputSalesDetailDto.DiscountPer ?? 0;
            }
            else
            {
                detail.GrossAmount = detail.Qty * detail.Rate;
                detail.NetAmount = detail.GrossAmount - detail.Discount;
                detail.TaxAmount = detail.NetAmount * taxRate;
                detail.Amount = detail.NetAmount + detail.TaxAmount;
                detail.DiscountPer = inputSalesDetailDto.DiscountPer ?? 0;
            }

            await ProcessSalesStock(
                input,
                inputSalesDetailDto,
                voucherTypeId,
                voucherNo,
                voucherNumbering,
                detail,
                stockPostingsToInsert,
                salesMasterId,
                tenantId);

            // Create tax detail if applicable
            TaxDetailDto taxDetail = null;
            if (inputSalesDetailDto.TaxId != Guid.Empty)
                taxDetail = new TaxDetailDto
                {
                    TaxId = inputSalesDetailDto.TaxId,
                    Amount = inputSalesDetailDto.TaxAmount ?? 0
                };

            return (detail, taxDetail);
        }

        private async Task ProcessSalesStock(
            CreateOrEditSalesMasterDto input,
            SalesDetailDto inputSalesDetailDto,
            Guid voucherTypeId,
            string voucherNo,
            int voucherNumbering,
            SalesDetail detail,
            List<StockPosting> stockPostingsToInsert,
            Guid salesMasterId,
            int tenantId)
        {
            await materialStockPostingService.ApplySalesIssueAsync(new MaterialSalesStockPostingRequest
            {
                DateMiti = input.DateMiti,
                LedgerId = input.LedgerId,
                VoucherTypeId = voucherTypeId,
                VoucherNo = voucherNo,
                VoucherNumbering = voucherNumbering,
                ProductId = detail.ProductId,
                UnitId = detail.UnitId,
                Qty = detail.Qty,
                Rate = detail.Rate,
                GrossAmount = detail.GrossAmount,
                DiscountAmount = detail.Discount,
                NetAmount = detail.NetAmount,
                Amount = detail.Amount,
                TaxAmount = detail.TaxAmount,
                FinancialYearId = FinancialYearId,
                MasterId = salesMasterId,
                SourceDetailId = detail.Id,
                TenantId = tenantId
            });
        }

        private async Task ValidateAvailableStock(Guid productId, Guid unitId, decimal requiredQty, string productName)
        {
            var availableQty = await GetAvailableStock(productId, unitId);
            if (availableQty < requiredQty)
                throw new UserFriendlyException("Insufficient stock",
                    $"{productName} required {requiredQty}, available {availableQty}");
        }

        private async Task<decimal> GetAvailableStock(Guid productId, Guid requiredUnitId)
        {
            var stock = await stockMaintainRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId)
                .OrderByDescending(x => x.LastModifiedDate)
                .FirstOrDefaultAsync();

            if (stock == null)
                return 0;

            var available = stock.OpeningQty + stock.InwardQty - stock.OutwardQty;
            return await ConvertQty(productId, stock.UnitId, requiredUnitId, available);
        }

        private async Task<decimal> ConvertQty(Guid productId, Guid fromUnitId, Guid toUnitId, decimal qty)
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


        private async Task ProcessNormalSalesDetail(
            CreateOrEditSalesMasterDto input,
            SalesDetailDto inputSalesDetailDto,
            Guid voucherTypeId,
            string voucherNo,
            int voucherNumbering,
            SalesDetail detail,
            List<StockPosting> stockPostingsToInsert,
            Guid salesMasterId,
            int tenantId)
        {
            var productData = await productRepository.GetAsync(inputSalesDetailDto.ProductId);

            if (productData.ProductType != ProductTypeEnum.Services)
            {
                var stockPosting = new StockPosting
                {
                    VoucherNumbering = voucherNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    LedgerId = input.LedgerId,
                    VoucherTypeId = voucherTypeId,
                    VoucherNo = voucherNo,
                    GrossAmount = detail.GrossAmount,
                    DiscountAmount = detail.Discount,
                    NetAmount = detail.NetAmount,
                    Amount = detail.Amount,
                    TaxAmount = detail.TaxAmount,
                    IsValueIncrease = false,
                    ProductId = inputSalesDetailDto.ProductId,
                    UnitId = inputSalesDetailDto.UnitId,
                    AgainstVoucherTypeId = Guid.Empty,
                    AgainstVoucherNo = "",
                    InWardQty = 0,
                    OutWardQty = detail.Qty,
                    Rate = inputSalesDetailDto.NetAmount.HasValue && inputSalesDetailDto.Qty > 0
                        ? (decimal)(inputSalesDetailDto.NetAmount / inputSalesDetailDto.Qty)
                        : 0,
                    FinancialYearId = FinancialYearId,
                    MasterId = salesMasterId,
                    TenantId = tenantId
                };
                stockPostingsToInsert.Add(stockPosting);
            }
        }

        //private async Task ProcessDifferentUnitConversion(
        //    StockPosting deliveryStockPosting,
        //    SalesDetailDto inputSalesDetailDto,
        //    UnitConversionResultDto netDeliveryNote,
        //    List<StockPosting> rejectionInStockPosting)
        //{
        //    var unitConversions = await unitConversionRepository.GetAll()
        //        .Where(x => x.ProductId == inputSalesDetailDto.ProductId)
        //        .AsNoTracking()
        //        .ToListAsync();

        //    var minUnit = unitConversions.MinBy(x => x.ConversionRate);
        //    var detailUnit = unitConversions.FirstOrDefault(x => x.UnitId == inputSalesDetailDto.UnitId);
        //    var netDeliveryUnit = unitConversions.FirstOrDefault(x => x.UnitId == netDeliveryNote.UnitId);

        //    if (minUnit != null && detailUnit != null && netDeliveryUnit != null)
        //    {
        //        // Calculate quantities in smallest unit
        //        var detailQtyInMinUnit = inputSalesDetailDto.Qty * detailUnit.PrimaryQty * minUnit.Qty /
        //                                 detailUnit.Qty / minUnit.PrimaryQty;

        //        var netDeliveryInMinUnit = netDeliveryNote.Qty * netDeliveryUnit.PrimaryQty * minUnit.Qty /
        //                                   netDeliveryUnit.Qty / minUnit.PrimaryQty;

        //        if (detailQtyInMinUnit >= netDeliveryInMinUnit)
        //        {
        //            // When detail qty exceeds available qty
        //            var added = new UnitConversionAddParamsDto
        //            {
        //                ProductId = inputSalesDetailDto.ProductId,
        //                Details = rejectionInStockPosting.Select(x => new UnitConversionAddParamDetails
        //                {
        //                    Qty = x.InWardQty,
        //                    UnitId = x.UnitId,
        //                    GrossAmount = x.GrossAmount,
        //                    Discount = x.DiscountAmount,
        //                    NetAmount = x.NetAmount,
        //                    TaxAmount = x.TaxAmount,
        //                    TotalAmount = x.Amount
        //                }).ToList()
        //            };

        //            var result = await ERPCommonManager.UnitConversionAdd(added);

        //            // Update delivery stock posting
        //            deliveryStockPosting.OutWardQty = result.Qty;
        //            deliveryStockPosting.Rate = result.Rate;
        //            deliveryStockPosting.GrossAmount = result.GrossAmount;
        //            deliveryStockPosting.NetAmount = result.NetAmount;
        //            deliveryStockPosting.Amount = result.TotalAmount;
        //            deliveryStockPosting.UnitId = result.UnitId;

        //            await stockPostingRepository.UpdateAsync(deliveryStockPosting);
        //        }
        //        else
        //        {
        //            // When detail qty is less than available qty
        //            var det = new List<UnitConversionParamDetails>
        //        {
        //            new()
        //            {
        //                Qty = inputSalesDetailDto.Qty,
        //                UnitId = inputSalesDetailDto.UnitId
        //            }
        //        };

        //            var delivery = new UnitConversionParamDto
        //            {
        //                ProductId = inputSalesDetailDto.ProductId,
        //                UnitId = deliveryStockPosting.UnitId,
        //                Qty = deliveryStockPosting.OutWardQty,
        //                Rate = deliveryStockPosting.Rate,
        //                Details = det
        //            };

        //            var netDeliver = await ERPCommonManager.UnitConversionMinus(delivery);

        //            deliveryStockPosting.OutWardQty = netDeliver.Qty;
        //            deliveryStockPosting.UnitId = netDeliver.UnitId;
        //            deliveryStockPosting.GrossAmount -= inputSalesDetailDto.GrossAmount ?? 0;
        //            deliveryStockPosting.NetAmount -= inputSalesDetailDto.NetAmount ?? 0;
        //            deliveryStockPosting.Rate = netDeliver.Rate;
        //            deliveryStockPosting.Amount -= inputSalesDetailDto.Amount ?? 0;

        //            await stockPostingRepository.UpdateAsync(deliveryStockPosting);
        //        }
        //    }
        //}


        private async Task CreateSalesAccountLedgerPosting(
            CreateOrEditSalesMasterDto input,
            Guid voucherTypeId,
            int voucherNumbering,
            string voucherNo,
            Guid salesMasterId,
            decimal netAmount,
            decimal postingNumbering,
            int tenantId)
        {
            await ledgerPostingRepository.InsertAsync(new LedgerPosting
            {
                TenantId = tenantId,
                VoucherNumbering = voucherNumbering,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                VoucherNo = voucherNo,
                LedgerId = input.SalesAccountId,
                DetailId = input.LedgerId,
                Debit = 0,
                FinancialYearId = FinancialYearId,
                Credit = netAmount,
                InvoiceNo = voucherNo,
                VendorVoucherNo = "",
                PostingNumber = postingNumbering,
                MasterId = salesMasterId
            });
        }

        private async Task CreatePaymentLedgerPosting(
            CreateOrEditSalesMasterDto input,
            Guid voucherTypeId,
            int voucherNumbering,
            string voucherNo,
            Guid salesMasterId,
            decimal grandTotal,
            SalesMaster salesMaster,
            AccountLedger getAccountLedger,
            decimal postingNumbering,
            int tenantId)
        {
            // Get cash ledger for reference
            var cashLedger =
                await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "Cash" && x.TenantId == tenantId);
            var allocations = input.PaymentAllocations?.Where(x => x.Amount > 0).ToList() ?? new List<SalesPaymentAllocationDto>();
            if (allocations.Count > 0)
            {
                if (allocations.Sum(x => x.Amount) != grandTotal + input.RestaurantTipAmount)
                    throw new UserFriendlyException("Restaurant payment allocations do not match the invoice and tip total");
                if (input.RestaurantTipAmount < 0 ||
                    (input.RestaurantTipAmount > 0 && !input.RestaurantTipLedgerId.HasValue))
                    throw new UserFriendlyException("A valid restaurant tip ledger is required before tips can be posted");

                if (cashLedger?.Id == input.LedgerId)
                {
                    foreach (var allocation in allocations)
                    {
                        var ledgerId = allocation.PaymentMethod == PaymentMethod.Cash
                            ? cashLedger.Id
                            : allocation.PaymentLedgerId ?? throw new UserFriendlyException("Select a ledger for every card or QR allocation");
                        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                        {
                            TenantId = tenantId,
                            VoucherNumbering = voucherNumbering,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = voucherNo,
                            LedgerId = ledgerId,
                            DetailId = input.SalesAccountId,
                            Debit = allocation.Amount,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = voucherNo,
                            PostingNumber = postingNumbering,
                            MasterId = salesMasterId
                        });
                    }
                    if (input.RestaurantTipAmount > 0)
                        await ledgerPostingRepository.InsertAsync(new LedgerPosting
                        {
                            TenantId = tenantId,
                            VoucherNumbering = voucherNumbering,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = voucherNo,
                            LedgerId = input.RestaurantTipLedgerId.Value,
                            DetailId = input.SalesAccountId,
                            Debit = 0,
                            FinancialYearId = FinancialYearId,
                            Credit = input.RestaurantTipAmount,
                            InvoiceNo = voucherNo,
                            PostingNumber = postingNumbering,
                            MasterId = salesMasterId
                        });
                }
                else
                    await CreateRestaurantReceiptVoucherPos(
                        input, salesMaster, voucherNo, allocations, grandTotal, input.RestaurantTipAmount,
                        input.RestaurantTipLedgerId);

                return;
            }

            if (cashLedger?.Id == input.LedgerId)
            {
                // Case when customer is cash ledger
                if (input.PaymentMethod is PaymentMethod.Cash or PaymentMethod.Cheque or PaymentMethod.Credit)
                {
                    // Direct cash payment
                    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    {
                        TenantId = tenantId,
                        VoucherNumbering = voucherNumbering,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        LedgerId = input.LedgerId,
                        DetailId = input.SalesAccountId,
                        Debit = grandTotal,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        VendorVoucherNo = "",
                        InvoiceNo = voucherNo,
                        PostingNumber = postingNumbering,
                        MasterId = salesMasterId
                    });
                }
                else if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR)
                {
                    // Card or QR payment
                    if (input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty)
                        throw new UserFriendlyException("Please Select Bank");

                    await ledgerPostingRepository.InsertAsync(new LedgerPosting
                    {
                        TenantId = tenantId,
                        VoucherNumbering = voucherNumbering,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        LedgerId = (Guid)input.PaymentMethodLedgerId,
                        DetailId = input.SalesAccountId,
                        Debit = grandTotal,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = voucherNo,
                        PostingNumber = postingNumbering,
                        MasterId = salesMasterId
                    });
                }
            }
            else
            {
                // Case when customer is not cash ledger - credit sale
                await ledgerPostingRepository.InsertAsync(new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    VoucherNo = voucherNo,
                    LedgerId = input.LedgerId,
                    DetailId = input.SalesAccountId,
                    Debit = grandTotal,
                    FinancialYearId = FinancialYearId,
                    Credit = 0,
                    InvoiceNo = voucherNo,
                    VendorVoucherNo = "",
                    PostingNumber = postingNumbering,
                    MasterId = salesMasterId
                });

                // Create receipt voucher if immediate payment was made
                if (input.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR or PaymentMethod.Cash)
                    await ReceiptVoucherPostingPos(salesMaster, voucherNo);
            }
        }


        public async Task<bool> SyncWithIrd()
        {
            var cbmsSetting = await SettingManager.GetSettingValueForTenantAsync(
                    ErpSettings.IsCBMS,
                    (int)AbpSession.TenantId) == "True";
            if (AbpSession.TenantId != null && await SettingManager.GetSettingValueForTenantAsync(
                    ErpSettings.IsCBMS,
                    (int)AbpSession.TenantId) == "True")
            {
                var branchInfo = branchRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefault(x => x.IsMain);
                if (branchInfo == null) throw new UserFriendlyException("Branch Information is not set");
                if (string.IsNullOrWhiteSpace(branchInfo.PANumber))
                    throw new UserFriendlyException("Pan Number is not set in branch information");

                var currentFinancialYear = new NepaliDate(DateTime.Today).FiscalYearStartDate();


                var username = await SettingManager.GetSettingValueForTenantAsync(ErpSettings.CBMSUsername,
                    (int)AbpSession.TenantId);
                var password = await SettingManager.GetSettingValueForTenantAsync(ErpSettings.CBMSPassword,
                    (int)AbpSession.TenantId);
                var sellerPan = branchInfo.PANumber;
                var salesData = await salesMasterRepository.GetAll().Where(x => !x.SyncwithIrd)
                    .Include(x => x.AccountLedgerFk)
                    .Where(e => e.Date.Date >= currentFinancialYear.EnglishDate.Date)
                    .Where(e => e.FinancialYearId == FinancialYearId)
                    .Include(x => x.FinancialYearFk).OrderBy(e => e.Date.Date).Take(10).ToListAsync();
                var syncCount = 0;
                var total = salesData.Count;
                foreach (var data in salesData.DistinctBy(e => e.VoucherNo).OrderByDescending(e => e.Date.Date)
                             .OrderByDescending(e => e.VoucherNumbering))
                {
                    var fromDate = currentFinancialYear.ToString().Split('/');
                    var objfromdate = Convert.ToInt32(fromDate[0]);

                    var toDate = objfromdate - 1999;
                    var fincialYear = objfromdate + ".0" + toDate;
                    //validate date miti
                    string npdate = "", year = "", month = "", day = "";
                    var objdateMiti = data.DateMiti.Split('/');
                    year = objdateMiti[0];
                    var objMonth = int.Parse(objdateMiti[1]);
                    var objDay = int.Parse(objdateMiti[2]);


                    if (objMonth <= 9)
                        month = $"0{objMonth}";
                    else
                        month = objMonth.ToString();

                    if (objDay <= 9)
                        day = $"0{objDay}";
                    else
                        day = objDay.ToString();

                    npdate = year + "." + month + "." + day;

                    var realDateTime = DateTime.Now;
                    var cbms = new
                    {
                        username,
                        password,
                        seller_pan = sellerPan,
                        buyer_pan = data.AccountLedgerFk.Pan,
                        fiscal_year = fincialYear,
                        buyer_name = data.AccountLedgerFk.Name,
                        invoice_number = data.VoucherNo,
                        invoice_date = npdate,
                        total_sales = (double)data.GrandTotal,
                        taxable_sales_vat = (double)data.TaxableAmount,
                        vat = (double)data.TaxAmount,
                        excisable_amount = 0.0,
                        excise = 0.0,
                        taxable_sales_hst = 0.0,
                        hst = 0.0,
                        amount_for_esf = 0.0,
                        esf = 0.0,
                        export_sales = 0.0,
                        tax_exempted_sales = (double)(data.NetAmount - data.TaxableAmount),
                        isrealtime = false,
                        datetimeclient = realDateTime
                    };

                    try
                    {
                        using (var client = new HttpClient())
                        {
                            // Set proper headers
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));

                            // Set timeout
                            client.Timeout = TimeSpan.FromSeconds(30);

                            var jsonContent = JsonConvert.SerializeObject(cbms);
                            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                            var response = await client.PostAsync("https://cbapi.ird.gov.np/api/bill", content);

                            // Read response content regardless of HTTP status
                            var responseContent = await response.Content.ReadAsStringAsync();

                            // Parse CBMS response code
                            if (int.TryParse(responseContent, out int cbmsResponseCode))
                            {
                                switch (cbmsResponseCode)
                                {
                                    case 200: // Success
                                        syncCount++;
                                        data.SyncwithIrd = true;
                                        data.IrdSyncDateTime = realDateTime;
                                        await salesMasterRepository.UpdateAsync(data);

                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} successfully synced to IRD!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //case 100: // API credentials do not match
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Invalid credentials!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //case 101: // Bill already exists
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} already exists in IRD system!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //case 102: // Exception while saving bill details
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Invalid field values!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //case 103: // Unknown exceptions
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Server error!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //case 104: // Model invalid
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Invalid data model!",
                                        //            ERPConsts.LocalizationSourceName));
                                        //    break;

                                        //default:
                                        //    await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                        //        new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Unknown response code {cbmsResponseCode}!",
                                        //            ERPConsts.LocalizationSourceName));
                                        break;
                                }
                            }
                            else
                            {
                                // Handle non-numeric response
                                //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                                //    new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Invalid response format - {responseContent}!",
                                //        ERPConsts.LocalizationSourceName));
                            }
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        // Network-related errors
                        //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                        //    new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Network error - {httpEx.Message}!",
                        //        ERPConsts.LocalizationSourceName));
                    }
                    catch (TaskCanceledException tcEx)
                    {
                        // Timeout errors
                        //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                        //    new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: Request timeout!",
                        //        ERPConsts.LocalizationSourceName));
                    }
                    catch (Exception ex)
                    {
                        // Other errors
                        //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                        //    new LocalizableString($"Sales Invoice Bill No {data.VoucherNo} sync failed: {ex.Message}!",
                        //        ERPConsts.LocalizationSourceName));
                    }
                }

                //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                //    new LocalizableString($"Sales Bills {syncCount} out of {total} sync with ird",
                //        ERPConsts.LocalizationSourceName));
            }

            return true;
        }

        //public async Task<bool> PostCbms(BillViewModelDto data)
        //{
        //    using var client = new HttpClient();
        //    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        //    var response = await client.PostAsync("https://cbapi.ird.gov.np/api/bill", content);

        //    if (response.StatusCode == HttpStatusCode.OK)
        //    {
        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        return true;
        //    }

        //    throw new UserFriendlyException("Request failed with status code: " + response.StatusCode);
        //}


        //[AbpAuthorize(AppPermissions.PagesSalesPosCreate)]
        //public async Task<Guid> CreatePos(CreateSalesPosDto input)
        //{
        //    // Use a separate unit of work to ensure isolation
        //    using var unitOfWork = unitOfWorkManager.Begin(new UnitOfWorkOptions
        //    {
        //        IsTransactional = true,
        //        //IsolationLevel = IsolationLevel.ReadCommitted
        //    });

        //    try
        //    {
        //        // Cache common values
        //        var tenantId = AbpSession.GetTenantId();
        //        var postingNumbering = PostingNumbering;

        //        // Get voucher information sequentially instead of in parallel
        //        var salesTypeVoucherInfo = await GetPosVoucherInfo(input);
        //        var salesType = salesTypeVoucherInfo.SalesType;
        //        var voucherTypeId = salesTypeVoucherInfo.VoucherTypeId;
        //        var voucherNumbering = salesTypeVoucherInfo.VoucherNumbering;
        //        var voucherNo = salesTypeVoucherInfo.VoucherNo;

        //        // Get required entities sequentially to avoid DbContext concurrency issues
        //        var getAccountLedger = await accountLedgerRepository.GetAsync(input.LedgerId);
        //        var salesLedger = await accountLedgerRepository.FirstOrDefaultAsync(x =>
        //            x.Name == "Sales Account" && x.TenantId == tenantId);

        //        if (salesLedger == null)
        //            throw new UserFriendlyException("Sales Account not found");

        //        var getTaxList = await taxRepository.GetAll()
        //            .Where(e => e.TenantId == tenantId)
        //            .ToListAsync();

        //        // Calculate totals
        //        var totalsResult = CalculateSalePosTotals(input, getTaxList);
        //        var taxAbleAmount = totalsResult.TaxableAmount;
        //        var taxAmount = totalsResult.TaxAmount;
        //        var grandTotal = totalsResult.GrandTotal;
        //        var discount = totalsResult.Discount;
        //        var netAmount = totalsResult.NetAmount;



        //        if (isLoyaltyPoint)
        //        {
        //            var adjusted = AdjustForLoyalty(taxAbleAmount, netAmount, input.LoyaltyAmount);
        //            taxAbleAmount = adjusted.TaxableAmount;
        //            netAmount = adjusted.NetAmount;
        //            taxAmount = adjusted.TaxAmount;
        //            grandTotal = adjusted.GrandTotal;
        //        }

        //        // Create and insert sales master entity
        //        var salesMaster = new SalesMaster
        //        {
        //            VoucherNumbering = voucherNumbering,
        //            TenantId = tenantId,
        //            VoucherNo = voucherNo,
        //            SalesAccountId = salesLedger.Id,
        //            Date = DateConverter.ConvertToEnglish(input.DateMiti),
        //            CreditPeriod = 0,
        //            Description = input.Description,
        //            DateMiti = input.DateMiti,
        //            CreditDate = DateConverter.ConvertToEnglish(input.DateMiti)
        //                .AddDays(getAccountLedger.CreditPeriod ?? 0),
        //            TaxAmount = taxAmount,
        //            LoyaltyAmount = input.LoyaltyAmount,
        //            LoyaltyPointUsed = input.LoyaltyPointUsed,
        //            AdditionalCost = 0,
        //            CreatedDate = DateTime.Now,
        //            AddTime = input.AddTime,
        //            CustomerPhoneNo = input.CustomerPhoneNo,
        //            LedgerName = input.CustomerName,
        //            VatNo = input.CustomerVatNo,
        //            CustomerAddress = input.CustomerAddress,
        //            BillDiscount = discount,
        //            GrandTotal = grandTotal,
        //            GrossAmount = netAmount,
        //            TaxableAmount = taxAbleAmount,
        //            SalesType = salesType,
        //            PaidAmount = 0,
        //            IsPrint = input.IsPrint,
        //            PaymentMethodLedgerId = input.PaymentMethodLedgerId,
        //            NoOfPrint = input.IsPrint ? 1 : 0,
        //            NetAmount = input.NetAmount,
        //            SyncwithIrd = false,
        //            PrintedTime = input.IsPrint ? DateTime.Now.ToString("h:mm:ss tt") : "",
        //            IsRealTime = false,
        //            PaymentMethod = input.PaymentMethod,
        //            IsDelete = false,
        //            InvoiceType = InvoiceTypeEnum.LocalInvoice,
        //            VatRefundAmount = 0,
        //            LrNo = "",
        //            TransportationCompany = "",
        //            PrintUserId = (int)AbpSession.GetUserId(),
        //            VoucherTypeId = voucherTypeId,
        //            LedgerId = input.LedgerId,
        //            TransporterId = null,
        //            VehicleNo = "",
        //            FreightTerm = FreightTerm.None,
        //            FreightAmount = 0,
        //            ServiceDeliveryId = null,
        //            AgainstId = null,
        //            SalesModeType = SalesModeType.Na,
        //            AgainstVoucherNo = "",
        //            FinancialYearId = FinancialYearId,
        //            CreateUserId = AbpSession.UserId,
        //            UpdateUserId = null,
        //            PostingNumbering = postingNumbering
        //        };

        //        var salesMasterId = await salesMasterRepository.InsertAndGetIdAsync(salesMaster);
        //        await unitOfWorkManager.Current.SaveChangesAsync(); // Save before continuing to ensure ID is generated


        //        // Prepare collections for batch operations
        //        var salesDetails = new List<SalesDetail>();
        //        var stockPostings = new List<StockPosting>();
        //        var taxEntriesMap = new Dictionary<Guid, decimal>();

        //        // Process each sales detail
        //        foreach (var detail in input.SalesDetails)
        //        {
        //            if (detail.Qty <= 0) continue;

        //            var detailResult = CreateSalesDetailAndStockPosting(
        //                detail, salesMasterId, tenantId, voucherTypeId,
        //                voucherNo, voucherNumbering, input.BranchId,
        //                input.DateMiti, input.LedgerId);

        //            salesDetails.Add(detailResult.Item1);
        //            stockPostings.Add(detailResult.Item2);

        //            // Add IMEIs if applicable
        //            if (detail.IsAllowSerialNo && detail.ImeiList?.Count > 0)
        //                imeis.AddRange(CreateImeiEntries(detail.ImeiList, detail.ProductId, tenantId,
        //                    voucherTypeId, voucherNo, voucherNumbering, input.BranchId, input.DateMiti,
        //                    salesMasterId));

        //            // Track tax amounts by tax ID
        //            if (detail.TaxId != Guid.Empty && detail.TaxAmount > 0)
        //            {
        //                if (taxEntriesMap.ContainsKey(detail.TaxId))
        //                    taxEntriesMap[detail.TaxId] += detail.TaxAmount ?? 0;
        //                else
        //                    taxEntriesMap[detail.TaxId] = detail.TaxAmount ?? 0;
        //            }
        //        }

        //        // Batch insert all entities with explicit save after each batch
        //        if (salesDetails.Any())
        //        {
        //            await salesDetailRepository.InsertRangeAsync(salesDetails);
        //            await unitOfWorkManager.Current.SaveChangesAsync();
        //        }

        //        if (stockPostings.Any())
        //        {
        //            await stockPostingRepository.InsertRangeAsync(stockPostings);
        //            await unitOfWorkManager.Current.SaveChangesAsync();
        //        }


        //        // Insert tax ledger postings
        //        if (taxEntriesMap.Any())
        //        {
        //            await CreateTaxLedgerPostings(taxEntriesMap, tenantId, voucherTypeId, voucherNo,
        //                voucherNumbering, input.BranchId, input.DateMiti, salesMaster.SalesAccountId,
        //                salesMasterId, postingNumbering);
        //            await unitOfWorkManager.Current.SaveChangesAsync();
        //        }

        //        // Create party balance if applicable
        //        if (getAccountLedger.IsBillByBill)
        //        {
        //            await CreatePartyBalance(tenantId, voucherNumbering, input.DateMiti, input.LedgerId,
        //                voucherTypeId, voucherNo, input.BranchId, salesMasterId, grandTotal);
        //            await unitOfWorkManager.Current.SaveChangesAsync();
        //        }

        //        // Create sales account and payment ledger postings
        //        await CreateLedgerPostings(tenantId, voucherNumbering, input.DateMiti, voucherTypeId,
        //            voucherNo, input.BranchId, salesMaster, getAccountLedger, postingNumbering,
        //            salesMasterId, input.PaymentMethod);

        //        await unitOfWork.CompleteAsync();
        //        return salesMasterId;
        //    }
        //    catch (Exception ex)
        //    {
        //        //Ensure the unit of work is disposed in case of exceptions
        //        unitOfWork.Dispose();
        //        throw new UserFriendlyException(L("Error"), ex.Message);
        //    }
        //}


        //private async Task<(SalesType SalesType, Guid VoucherTypeId, int VoucherNumbering, string VoucherNo)>
        //    GetPosVoucherInfo(CreateSalesPosDto input)
        //{
        //    var isAbt = await SettingManager.GetSettingValueForTenantAsync<bool>(
        //        ErpSettings.IsAbt, AbpSession.GetTenantId());

        //    var isIrdSoftware = await SettingManager.GetSettingValueForTenantAsync<bool>(
        //        ErpSettings.IsIRDSoftware, AbpSession.GetTenantId());

        //    SalesType salesType;
        //    Guid voucherTypeId;
        //    int voucherNumbering;
        //    string voucherNo;

        //    if (isIrdSoftware)
        //    {
        //        if (isAbt)
        //        {
        //            if (input.GrandTotal >= 10000)
        //            {
        //                salesType = SalesType.TI;
        //                voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("TI");
        //            }
        //            else
        //            {
        //                salesType = SalesType.ABT;
        //                voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ABT");
        //            }
        //        }
        //        else
        //        {
        //            salesType = SalesType.Sales;
        //            voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
        //        }
        //    }
        //    else
        //    {
        //        salesType = SalesType.Sales;
        //        voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
        //    }

        //    voucherNumbering = await GetInlineVoucherNo(input.BranchId, salesType);
        //    voucherNo = await GetSalesMasterVoucherNo(input.BranchId, salesType);

        //    return (salesType, voucherTypeId, voucherNumbering, voucherNo);
        //}

        //private (decimal TaxableAmount, decimal TaxAmount, decimal GrandTotal, decimal Discount, decimal NetAmount)
        //    CalculateSalePosTotals(CreateSalesPosDto input, List<Tax> taxList)
        //{
        //    decimal taxableAmount = 0, taxAmount = 0, grandTotal = 0, discount = 0, netAmount = 0;

        //    foreach (var detail in input.SalesDetails.Where(d => d.Qty > 0))
        //    {
        //        grandTotal += detail.Amount ?? 0;
        //        discount += detail.Discount ?? 0;
        //        netAmount += detail.NetAmount ?? 0;

        //        var tax = taxList.FirstOrDefault(x => x.Id == detail.TaxId);

        //        if (tax != null && tax.Rate > 0)
        //        {
        //            var taxRate = tax.Rate / 100;
        //            var netAmt = detail.NetAmount ?? 0;
        //            taxAmount += netAmt * taxRate;
        //            taxableAmount += detail.NetAmount ?? 0;
        //        }
        //    }

        //    return (taxableAmount, taxAmount, grandTotal, discount, netAmount);
        //}


        //private (SalesDetail, StockPosting) CreateSalesDetailAndStockPosting(
        //    SalesPosDetailDto detail, Guid salesMasterId, int tenantId, Guid voucherTypeId,
        //    string voucherNo, int voucherNumbering, Guid branchId, string dateMiti, Guid ledgerId)
        //{
        //    var taxRate = ErpCommonManager.GetTaxRate(detail.TaxId);
        //    decimal grossAmount, netAmount, amount, taxAmount;

        //    var productData = productRepository.FirstOrDefault(x => x.Id == detail.ProductId && x.TenantId == tenantId);
        //    // Create sales detail
        //    var salesDetail = new SalesDetail
        //    {
        //        TenantId = tenantId,
        //        SalesMasterId = salesMasterId,
        //        Qty = detail.Qty,
        //        Rate = detail.Rate,
        //        Discount = detail.Discount ?? 0,
        //        ProductId = detail.ProductId,
        //        UnitId = detail.UnitId,
        //        AgainstDetailId = null,
        //        ProductName = productData.Name,
        //        TaxId = detail.TaxId
        //    };

        //    // Calculate amounts based on tax rate
        //    if (taxRate > 0)
        //    {
        //        amount = detail.Amount ?? 0;
        //        netAmount = amount / (1 + taxRate);
        //        grossAmount = netAmount + (detail.Discount ?? 0);
        //        taxAmount = netAmount * taxRate;
        //    }
        //    else
        //    {
        //        grossAmount = detail.Qty * detail.Rate;
        //        netAmount = grossAmount - (detail.Discount ?? 0);
        //        taxAmount = netAmount * taxRate;
        //        amount = netAmount + taxAmount;
        //    }

        //    salesDetail.Amount = amount;
        //    salesDetail.NetAmount = netAmount;
        //    salesDetail.GrossAmount = grossAmount;
        //    salesDetail.TaxAmount = taxAmount;
        //    salesDetail.DiscountPer = detail.DiscountPer ?? 0;

        //    // Create stock posting
        //    var stockPosting = new StockPosting
        //    {
        //        VoucherNumbering = voucherNumbering,
        //        Date = DateConverter.ConvertToEnglish(dateMiti),
        //        DateMiti = dateMiti,
        //        LedgerId = ledgerId,
        //        VoucherTypeId = voucherTypeId,
        //        VoucherNo = voucherNo,
        //        GrossAmount = grossAmount,
        //        DiscountAmount = detail.Discount ?? 0,
        //        NetAmount = netAmount,
        //        Amount = amount,
        //        TaxAmount = taxAmount,
        //        IsValueIncrease = false,
        //        ProductId = detail.ProductId,
        //        UnitId = detail.UnitId,
        //        AgainstVoucherTypeId = Guid.Empty,
        //        AgainstVoucherNo = "",
        //        InWardQty = 0,
        //        OutWardQty = detail.Qty,
        //        Rate = detail.Rate,
        //        FinancialYearId = FinancialYearId,
        //        MasterId = salesMasterId,
        //        TenantId = tenantId
        //    };

        //    return (salesDetail, stockPosting);
        //}

        private async Task CreateTaxLedgerPostings(Dictionary<Guid, decimal> taxEntries, int tenantId,
            Guid voucherTypeId, string voucherNo, int voucherNumbering,
            string dateMiti, Guid salesAccountId, Guid salesMasterId, decimal postingNumbering)
        {
            var ledgerPostings = new List<LedgerPosting>();

            foreach (var entry in taxEntries)
            {
                var tax = await taxRepository.GetAsync(entry.Key);

                if (tax.Rate <= 0)
                    continue;

                ledgerPostings.Add(new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumbering,
                    Date = DateConverter.ConvertToEnglish(dateMiti),
                    DateMiti = dateMiti,
                    VoucherTypeId = voucherTypeId,
                    VoucherNo = voucherNo,
                    LedgerId = tax.LedgerId,
                    DetailId = salesAccountId,
                    Debit = 0,
                    FinancialYearId = FinancialYearId,
                    Credit = entry.Value,
                    InvoiceNo = voucherNo,
                    PostingNumber = postingNumbering,
                    MasterId = salesMasterId
                });
            }

            if (ledgerPostings.Count > 0)
                await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);
        }

        //private async Task CreatePartyBalance(int tenantId, int voucherNumbering, string dateMiti,
        //    Guid ledgerId, Guid voucherTypeId, string voucherNo, Guid branchId,
        //    Guid salesMasterId, decimal grandTotal)
        //{
        //    await partyBalanceRepository.InsertAsync(new PartyBalance
        //    {
        //        VoucherNumbering = voucherNumbering,
        //        Date = DateConverter.ConvertToEnglish(dateMiti),
        //        LedgerId = ledgerId,
        //        FinancialYearId = FinancialYearId,
        //        VoucherTypeId = voucherTypeId,
        //        VoucherNo = voucherNo,
        //        AgainstVoucherTypeId = Guid.Empty,
        //        AgainstVoucherNo = "NA",
        //        InvoiceNo = voucherNo,
        //        AgainstInvoiceNo = "NA",
        //        ReferenceType = "New",
        //        Debit = grandTotal,
        //        Credit = 0,
        //        CreditPeriod = 0,

        //        BranchId = branchId,
        //        MasterVoucherTypeId = voucherTypeId,
        //        MasterId = salesMasterId,
        //        DetailId = Guid.Empty,
        //        MasterVoucherNo = voucherNo,
        //        TenantId = tenantId
        //    });
        //}

        private async Task CreateLedgerPostings(int tenantId, int voucherNumbering, string dateMiti,
            Guid voucherTypeId, string voucherNo, SalesMaster salesMaster,
            AccountLedger accountLedger, decimal postingNumbering, Guid salesMasterId,
            PaymentMethod paymentMethod)
        {
            var ledgerPostings = new List<LedgerPosting>();
            var date = DateConverter.ConvertToEnglish(dateMiti);

            // Sales account credit posting
            ledgerPostings.Add(new LedgerPosting
            {
                TenantId = tenantId,
                VoucherNumbering = voucherNumbering,
                Date = date,
                DateMiti = dateMiti,
                VoucherTypeId = voucherTypeId,
                VoucherNo = voucherNo,
                LedgerId = salesMaster.SalesAccountId,
                DetailId = (Guid)salesMaster.LedgerId,
                Debit = 0,
                FinancialYearId = FinancialYearId,
                Credit = salesMaster.NetAmount,
                InvoiceNo = voucherNo,
                PostingNumber = postingNumbering,
                MasterId = salesMasterId
            });

            // Determine payment posting ledger ID based on payment method
            var isCashLedger = await IsCashLedger(accountLedger.Id);
            Guid paymentLedgerId;

            if (isCashLedger)
            {
                if (paymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR)
                {
                    if (salesMaster.PaymentMethodLedgerId == null || salesMaster.PaymentMethodLedgerId == Guid.Empty)
                        throw new UserFriendlyException("Please Select Bank");

                    paymentLedgerId = (Guid)salesMaster.PaymentMethodLedgerId;
                }
                else
                {
                    paymentLedgerId = (Guid)salesMaster.LedgerId;
                }
            }
            else
            {
                paymentLedgerId = (Guid)salesMaster.LedgerId;
            }

            // Payment debit posting
            ledgerPostings.Add(new LedgerPosting
            {
                TenantId = tenantId,
                VoucherNumbering = voucherNumbering,
                Date = date,
                DateMiti = dateMiti,
                VoucherTypeId = voucherTypeId,
                VoucherNo = voucherNo,
                LedgerId = paymentLedgerId,
                DetailId = salesMaster.SalesAccountId,
                Debit = salesMaster.GrandTotal,
                FinancialYearId = FinancialYearId,
                Credit = 0,
                InvoiceNo = voucherNo,
                PostingNumber = postingNumbering,
                MasterId = salesMasterId
            });

            await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);

            // Create receipt voucher if needed
            if (!isCashLedger && paymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR or PaymentMethod.Cash)
                await ReceiptVoucherPostingPos(salesMaster, voucherNo);
        }

        private async Task<bool> IsCashLedger(Guid ledgerId)
        {
            var cashLedger = await accountLedgerRepository.FirstOrDefaultAsync(
                e => e.TenantId == AbpSession.GetTenantId() && e.Name == "Cash");

            return cashLedger?.Id == ledgerId;
        }

        public async Task<List<UniversalDropdownDto>> GetAllUnits()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(unit => new UniversalDropdownDto
                {
                    Id = unit.Id,
                    DisplayName = unit == null || unit.Name == null ? "" : unit.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        //[DisableAuditing]
        //public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnitsMaster()
        //{
        //    return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
        //        .Select(unit => new PurchaseReturnUnitsQtyDto
        //        {
        //            UnitId = unit.Id,
        //            UnitName = unit == null || unit.Name == null ? "" : unit.Name.ToString(),
        //            Rate = 0,
        //            Qty = 0
        //        }).AsNoTracking().ToListAsync();
        //}


        private async Task ReceiptVoucherPosting(Guid salesId, string refVoucherNo)
        {
            var input = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == salesId);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var voucherTypeIdReceiptMaster = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var tenantId = AbpSession.TenantId;
            var postingNo = PostingNumbering + 1;
            var receiptVhNumbering = await GetReceiptMasterInlineVoucherNo();
            var receiptVhNo = await GetReceiptMasterVoucherNo();
            var ledger = await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == input.LedgerId);

            if (input.PaymentMethod is PaymentMethod.Cash or PaymentMethod.QR or PaymentMethod.Card_Swipe)
            {
                var cashLedger = await accountLedgerRepository.GetAll()
                    .Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(x => x.Name == "Cash");
                if (cashLedger == null)
                    throw new UserFriendlyException("Ledger Name cash not found");
                var receiptMaster = new ReceiptMaster
                {
                    VoucherNumbering = receiptVhNumbering,
                    VoucherNo = receiptVhNo,
                    PostingNumbering = postingNo,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    RefVoucherNo = refVoucherNo,
                    RefVoucherTypeId = voucherTypeId,
                    TotalAmount = input.GrandTotal,
                    Description = "Receipt Payment form SalesInvoice Bill No" + input.VoucherNo,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    FinancialYearId = FinancialYearId,
                    CreateUserId = AbpSession.UserId,
                    TenantId = tenantId
                };
                switch (input.PaymentMethod)
                {
                    case PaymentMethod.Cash:
                        receiptMaster.LedgerId = cashLedger.Id;
                        break;
                    case PaymentMethod.QR
                        when input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty:
                        throw new UserFriendlyException("Please Select a Bank");
                    case PaymentMethod.QR:
                        receiptMaster.LedgerId = (Guid)input.PaymentMethodLedgerId;
                        break;
                    case PaymentMethod.Card_Swipe
                        when input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty:
                        throw new UserFriendlyException("Please Select a Bank");
                    case PaymentMethod.Card_Swipe:
                        receiptMaster.LedgerId = (Guid)input.PaymentMethodLedgerId;
                        break;
                }

                var receiptMasterId = await receiptMasterRepository.InsertAndGetIdAsync(receiptMaster);

                var ledgerPosting = new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = receiptVhNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = receiptVhNo,
                    Debit = input.GrandTotal,
                    Credit = 0,
                    InvoiceNo = receiptVhNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = (Guid)input.LedgerId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    LedgerId = receiptMaster.LedgerId,
                    MasterId = receiptMasterId,
                    PostingNumber = postingNo
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting);

                var receiptDetail = new ReceiptDetail
                {
                    Amount = input.GrandTotal,
                    ChequeNo = "",
                    ChequeDate = null,
                    LedgerId = (Guid)input.LedgerId,
                    ReceiptMasterId = receiptMasterId,
                    TenantId = tenantId
                };
                await receiptDetailRepository.InsertAsync(receiptDetail);

                var ledgerPostingrd = new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = receiptVhNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = receiptVhNo,
                    Debit = 0,
                    Credit = input.GrandTotal,
                    InvoiceNo = receiptVhNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = receiptMaster.LedgerId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    LedgerId = (Guid)input.LedgerId,
                    MasterId = receiptMasterId,
                    PostingNumber = postingNo
                };
                await ledgerPostingRepository.InsertAsync(ledgerPostingrd);

                if (ledger.IsBillByBill)
                {
                    //var partyBalanceData = new PartyBalance
                    //{
                    //    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    //    LedgerId = (Guid)input.LedgerId,
                    //    FinancialYearId = FinancialYearId,
                    //    VoucherTypeId = voucherTypeId,
                    //    VoucherNumbering = receiptVhNumbering,
                    //    IsAgainst = false,
                    //    AgainstVoucherTypeId = voucherTypeIdReceiptMaster,
                    //    AgainstVoucherNo = receiptVhNo, // input.VoucherNo,
                    //    AgainstInvoiceNo = receiptVhNo,
                    //    VoucherNo = refVoucherNo,
                    //    InvoiceNo = refVoucherNo,
                    //    ReferenceType = "Against",
                    //    Debit = 0,
                    //    Credit = input.GrandTotal,
                    //    CreditPeriod = 0,

                    //    BranchId = input.BranchId,
                    //    MasterVoucherTypeId = voucherTypeIdReceiptMaster,
                    //    MasterId = receiptMasterId,
                    //    DetailId = Guid.Empty,
                    //    MasterVoucherNo = receiptVhNo,
                    //    TenantId = tenantId
                    //};
                    //await partyBalanceRepository.InsertAsync(partyBalanceData);
                }
            }
        }


        private async Task ReceiptVoucherPostingPos(SalesMaster input, string refVoucherNo)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var voucherTypeIdReceiptMaster = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var tenantId = AbpSession.TenantId;
            var postingNo = PostingNumbering + 1;
            var receiptVhNumbering = await GetReceiptMasterInlineVoucherNo();
            var receiptVhNo = await GetReceiptMasterVoucherNo();
            var ledger = await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == input.LedgerId);

            if (input.PaymentMethod is PaymentMethod.Cash or PaymentMethod.QR or PaymentMethod.Card_Swipe)
            {
                var cashLedger = await accountLedgerRepository.GetAll()
                    .Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(x => x.Name == "Cash");
                if (cashLedger == null)
                    throw new UserFriendlyException("Ledger Name cash not found");
                var receiptMaster = new ReceiptMaster
                {
                    VoucherNumbering = receiptVhNumbering,
                    VoucherNo = receiptVhNo,
                    PostingNumbering = postingNo,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    DateMiti = input.DateMiti,
                    RefVoucherNo = refVoucherNo,
                    RefVoucherTypeId = voucherTypeId,
                    TotalAmount = input.GrandTotal,
                    Description = input.Description,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    FinancialYearId = FinancialYearId,
                    CreateUserId = AbpSession.UserId,
                    TenantId = tenantId
                };
                switch (input.PaymentMethod)
                {
                    case PaymentMethod.Cash:
                        receiptMaster.LedgerId = cashLedger.Id;
                        break;
                    case PaymentMethod.QR
                        when input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty:
                        throw new UserFriendlyException("Please Select a Bank");
                    case PaymentMethod.QR:
                        receiptMaster.LedgerId = (Guid)input.PaymentMethodLedgerId;
                        break;
                    case PaymentMethod.Card_Swipe
                        when input.PaymentMethodLedgerId == null || input.PaymentMethodLedgerId == Guid.Empty:
                        throw new UserFriendlyException("Please Select a Bank");
                    case PaymentMethod.Card_Swipe:
                        receiptMaster.LedgerId = (Guid)input.PaymentMethodLedgerId;
                        break;
                }

                var receiptMasterId = await receiptMasterRepository.InsertAndGetIdAsync(receiptMaster);

                var ledgerPosting = new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = receiptVhNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = receiptVhNo,
                    Debit = input.NetAmount,
                    Credit = 0,
                    InvoiceNo = receiptVhNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = (Guid)input.LedgerId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    LedgerId = receiptMaster.LedgerId,
                    DetailIds = "",
                    VendorVoucherNo = "",
                    MasterId = receiptMasterId,
                    PostingNumber = postingNo
                };
                await ledgerPostingRepository.InsertAsync(ledgerPosting);

                var receiptDetail = new ReceiptDetail
                {
                    ChequeMiti = "",
                    Forex = 0,
                    Amount = input.NetAmount,
                    ChequeNo = "",
                    ChequeDate = null,
                    LedgerId = (Guid)input.LedgerId,
                    ReceiptMasterId = receiptMasterId,
                    TenantId = tenantId
                };
                var detailId = await receiptDetailRepository.InsertAndGetIdAsync(receiptDetail);

                var ledgerPostingrd = new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = receiptVhNumbering,
                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    VoucherNo = receiptVhNo,
                    Debit = 0,
                    Credit = input.NetAmount,
                    InvoiceNo = receiptVhNo,
                    FinancialYearId = FinancialYearId,
                    DetailId = receiptMaster.LedgerId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeIdReceiptMaster,
                    LedgerId = (Guid)input.LedgerId,
                    MasterId = receiptMasterId,
                    PostingNumber = postingNo
                };
                await ledgerPostingRepository.InsertAsync(ledgerPostingrd);

                if (ledger.IsBillByBill)
                {
                    await partyBalanceService.CreateReceiptTaskAsync(new AdjustmentEntryDto
                    {
                        MasterId = input.Id,
                        MasterVoucherNo = input.VoucherNo,
                        MasterVoucherNumbering = input.VoucherNumbering,
                        LedgerId = (Guid)input.LedgerId,
                        Date = input.Date,
                        OnAccountPaid = input.GrandTotal,
                        Amount = input.GrandTotal,
                        VoucherNo = receiptVhNo,
                        VoucherNumbering = receiptVhNumbering,
                        DetailId = detailId,
                        VoucherTypeId = voucherTypeIdReceiptMaster,
                    });


                    //    var partyBalanceData = new PartyBalance
                    //    {
                    //        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                    //        LedgerId = (Guid)input.LedgerId,
                    //        FinancialYearId = FinancialYearId,
                    //        VoucherTypeId = voucherTypeId,
                    //        VoucherNumbering = receiptVhNumbering,
                    //        IsAgainst = false,
                    //        AgainstVoucherTypeId = voucherTypeIdReceiptMaster,
                    //        AgainstVoucherNo = receiptVhNo, // input.VoucherNo,
                    //        AgainstInvoiceNo = receiptVhNo,
                    //        VoucherNo = refVoucherNo,
                    //        InvoiceNo = refVoucherNo,
                    //        ReferenceType = "Against",
                    //        Debit = 0,

                    //        Credit = input.NetAmount,
                    //        CreditPeriod = 0,
                    //        IsProcessed = true,
                    //        BranchId = input.BranchId,
                    //        MasterVoucherTypeId = voucherTypeIdReceiptMaster,
                    //        MasterId = receiptMasterId,
                    //        DetailId = Guid.Empty,
                    //        MasterVoucherNo = receiptVhNo,
                    //        TenantId = tenantId
                    //    };
                    //    await partyBalanceRepository.InsertAsync(partyBalanceData);
                    //    int x = 100;
                    //    float asd = 2343f;
                }
            }
        }

        private async Task CreateRestaurantReceiptVoucherPos(
            CreateOrEditSalesMasterDto input,
            SalesMaster salesMaster,
            string invoiceVoucherNo,
            List<SalesPaymentAllocationDto> allocations,
            decimal invoiceAmount,
            decimal tipAmount,
            Guid? tipLedgerId)
        {
            var tenantId = AbpSession.TenantId;
            var receiptVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var invoiceVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            var voucherNumbering = await GetReceiptMasterInlineVoucherNo();
            var voucherNo = await GetReceiptMasterVoucherNo();
            var postingNumbering = PostingNumbering + 1;
            var total = allocations.Sum(x => x.Amount);
            var receipt = new ReceiptMaster
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                PostingNumbering = postingNumbering,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DateMiti = input.DateMiti,
                RefVoucherNo = invoiceVoucherNo,
                RefVoucherTypeId = invoiceVoucherTypeId,
                TotalAmount = total,
                Description = "Restaurant settlement for " + invoiceVoucherNo,
                VoucherTypeId = receiptVoucherTypeId,
                FinancialYearId = FinancialYearId,
                CreateUserId = AbpSession.UserId,
                TenantId = tenantId,
                LedgerId = allocations[0].PaymentLedgerId ??
                    await accountLedgerRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId && x.Name == "Cash")
                        .Select(x => x.Id).FirstOrDefaultAsync()
            };
            if (receipt.LedgerId == Guid.Empty)
                throw new UserFriendlyException("Configure a valid restaurant payment ledger before billing");

            var receiptId = await receiptMasterRepository.InsertAndGetIdAsync(receipt);
            foreach (var allocation in allocations)
            {
                var ledgerId = allocation.PaymentLedgerId ?? throw new UserFriendlyException(
                    "Select a ledger for every card, QR, or cash payment");
                await ledgerPostingRepository.InsertAsync(new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumbering,
                    Date = receipt.Date,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = receiptVoucherTypeId,
                    VoucherNo = voucherNo,
                    LedgerId = ledgerId,
                    DetailId = input.LedgerId,
                    Debit = allocation.Amount,
                    Credit = 0,
                    InvoiceNo = invoiceVoucherNo,
                    FinancialYearId = FinancialYearId,
                    MasterId = receiptId,
                    PostingNumber = postingNumbering
                });
            }

            var appliedInvoiceAmount = 0m;
            if (invoiceAmount > 0)
            {
                var detailId = await receiptDetailRepository.InsertAndGetIdAsync(new ReceiptDetail
                {
                    Amount = invoiceAmount,
                    ChequeNo = string.Empty,
                    ChequeMiti = string.Empty,
                    LedgerId = input.LedgerId,
                    ReceiptMasterId = receiptId,
                    TenantId = tenantId
                });
                await ledgerPostingRepository.InsertAsync(new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumbering,
                    Date = receipt.Date,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = receiptVoucherTypeId,
                    VoucherNo = voucherNo,
                    LedgerId = input.LedgerId,
                    DetailId = receipt.LedgerId,
                    Debit = 0,
                    Credit = invoiceAmount,
                    InvoiceNo = invoiceVoucherNo,
                    FinancialYearId = FinancialYearId,
                    MasterId = receiptId,
                    PostingNumber = postingNumbering
                });
                appliedInvoiceAmount = invoiceAmount;

                var customerLedger = await accountLedgerRepository.FirstOrDefaultAsync(
                    x => x.Id == input.LedgerId && x.TenantId == AbpSession.TenantId);
                if (customerLedger?.IsBillByBill == true)
                    await partyBalanceService.CreateReceiptTaskAsync(new AdjustmentEntryDto
                    {
                        MasterId = salesMaster.Id,
                        MasterVoucherNo = invoiceVoucherNo,
                        MasterVoucherNumbering = salesMaster.VoucherNumbering,
                        LedgerId = input.LedgerId,
                        Date = receipt.Date,
                        OnAccountPaid = appliedInvoiceAmount,
                        Amount = appliedInvoiceAmount,
                        VoucherNo = voucherNo,
                        VoucherNumbering = voucherNumbering,
                        DetailId = detailId,
                        VoucherTypeId = receiptVoucherTypeId
                    });
            }

            if (tipAmount > 0)
            {
                if (!tipLedgerId.HasValue)
                    throw new UserFriendlyException("Configure a restaurant tip ledger before collecting tips");
                await receiptDetailRepository.InsertAsync(new ReceiptDetail
                {
                    Amount = tipAmount,
                    ChequeNo = "Restaurant tip",
                    ChequeMiti = string.Empty,
                    LedgerId = tipLedgerId.Value,
                    ReceiptMasterId = receiptId,
                    TenantId = tenantId
                });
                await ledgerPostingRepository.InsertAsync(new LedgerPosting
                {
                    TenantId = tenantId,
                    VoucherNumbering = voucherNumbering,
                    Date = receipt.Date,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = receiptVoucherTypeId,
                    VoucherNo = voucherNo,
                    LedgerId = tipLedgerId.Value,
                    DetailId = receipt.LedgerId,
                    Debit = 0,
                    Credit = tipAmount,
                    InvoiceNo = invoiceVoucherNo,
                    FinancialYearId = FinancialYearId,
                    MasterId = receiptId,
                    PostingNumber = postingNumbering
                });
            }
        }



        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllBankAccount()
        {
            return (await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking().Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Bank Account")
                .ToListAsync()).Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToList();
        }


        [AbpAuthorize(AppPermissions.PagesSalesMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditSalesMasterDto input)
        {
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                if (input.Id != null)
                {
                    var salesMaster = await salesMasterRepository.GetAll()
                        .Where(e => e.TenantId == AbpSession.GetTenantId()).FirstOrDefaultAsync(x => x.Id == input.Id);
                    if (salesMaster != null)
                    {
                        var oldVoucherNo = salesMaster.VoucherNo;

                        var getVoucherType =
                            await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "SalesInvoice");

                        if (getVoucherType == "Manually")
                        {
                            if (await salesMasterRepository.CountAsync(x =>
                                    x.Id != input.Id && x.FinancialYearId == FinancialYearId &&
                                    x.VoucherNo == input.VoucherNo) >
                                0) throw new UserFriendlyException("Sales VoucherNo is Duplicate");
                            salesMaster.VoucherNo = input.VoucherNo;
                        }

                        if (salesMaster.SyncwithIrd)
                            throw new UserFriendlyException("Can not Update or Delete", "Data is Sync with Ird");

                        var getAccountLedger =
                            await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                                .FirstOrDefaultAsync(x => x.Id == input.LedgerId);
                        var salesType = SalesType.Sales;
                        var voucherNumbering = await GetInlineVoucherNo(salesType);
                        var voucherNo = input.VoucherNo;

                        if (getVoucherType == "Duplicate")
                            salesMaster.VoucherNo = input.VoucherNo;

                        var postingNumbering = salesMaster.PostingNumbering;
                        var voucherTypeId = salesMaster.VoucherTypeId;
                        var tenantId = salesMaster.TenantId;
                        salesMaster.CreditPeriod = input.CreditPeriod;
                        salesMaster.Description = input.Description;
                        salesMaster.DateMiti = input.DateMiti;
                        salesMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                        salesMaster.PINumber = input.PiNumber;
                        salesMaster.CustomerAddress = input.CustomerAddress;
                        salesMaster.CreditDate = DateConverter.ConvertToEnglish(input.DateMiti)
                            .AddDays(input.CreditPeriod);


                        decimal taxAbleAmount = 0, taxAmount = 0, grandTotal = 0, discount = 0, netAmount = 0;
                        foreach (var detail in input.SalesDetails)
                        {
                            taxAbleAmount += detail.TaxAmount > 0 ? detail.NetAmount ?? 0 : 0;
                            taxAmount += detail.TaxAmount ?? 0;
                            grandTotal += detail.Amount ?? 0;
                            discount += detail.Discount ?? 0;
                            netAmount += detail.NetAmount ?? 0;
                        }

                        //updating sales master tax amount fixed
                        salesMaster.TaxAmount = Math.Round(taxAmount, 2);
                        salesMaster.TaxableAmount = Math.Round(taxAbleAmount, 2);
                        salesMaster.BillDiscount = Math.Round(discount, 2);
                        salesMaster.GrandTotal = Math.Round(grandTotal, 2);
                        salesMaster.GrossAmount = input.TotalAmount;
                        salesMaster.NetAmount = Math.Round(netAmount, 2);
                        salesMaster.PaymentMethodLedgerId =
                            input.PaymentMethodLedgerId == Guid.Empty ? null : input.PaymentMethodLedgerId;

                        salesMaster.PaymentMethod = input.PaymentMethod;
                        salesMaster.InvoiceType = input.InvoiceType;
                        salesMaster.VatRefundAmount = input.VatRefundAmount;
                        salesMaster.LrNo = input.LrNo;
                        salesMaster.PrintUserId = AbpSession.TenantId;
                        salesMaster.VoucherTypeId = voucherTypeId;
                        salesMaster.LedgerId = input.LedgerId;
                        salesMaster.VehicleNo = input.VehicleNo;

                        salesMaster.AgainstId = input.AgainstId != null ? string.Join(',', input.AgainstId) : "";
                        salesMaster.SalesModeType = input.SalesModeType;
                        salesMaster.AgainstVoucherNo = input.AgainstVoucherNo;
                        salesMaster.FinancialYearId = FinancialYearId;
                        salesMaster.CreateUserId = AbpSession.UserId;
                        salesMaster.LedgerName = string.IsNullOrEmpty(input.CustomerName)
                            ? getAccountLedger?.Name
                            : input.CustomerName;
                        salesMaster.CustomerPhoneNo = input.CustomerPhoneNo;
                        salesMaster.VatNo = input.CustomerVatNo;
                        salesMaster.PostingNumbering = postingNumbering;
                        salesMaster.SourceModule = input.SourceModule;
                        salesMaster.SourceDocumentId = input.SourceDocumentId;
                        salesMaster.UpdateUserId = AbpSession.UserId;
                        await salesMasterRepository.UpdateAsync(salesMaster);

                        var salesReturns = await salesReturnMasterRepository.GetAll()
                            .Where(x => x.SalesMasterId == salesMaster.Id).ToListAsync();
                        if (salesReturns.Count > 0)
                        {
                            var message = string.Join(',', salesReturns.Select(x => x.VoucherNo));
                            throw new UserFriendlyException(
                                $"SalesReturn reference Exists in VoucherNo {message}, so this invoice can't be edited.");
                        }

                        var existingStockPostings = await stockPostingRepository.GetAll()
                            .Where(x => x.VoucherNo == oldVoucherNo &&
                                        x.VoucherTypeId == voucherTypeId &&
                                        x.FinancialYearId == FinancialYearId)
                            .AsNoTracking()
                            .ToListAsync();
                        await materialStockPostingService.ReverseExistingStockPostingsAsync(existingStockPostings);

                        await salesDetailRepository.DeleteAsync(x => x.SalesMasterId == salesMaster.Id);

                        await stockPostingRepository.DeleteAsync(x => x.VoucherNo == oldVoucherNo &&
                                                                      x.VoucherTypeId == voucherTypeId &&
                                                                      x.FinancialYearId == FinancialYearId);

                        await ledgerPostingRepository.DeleteAsync(x =>
                            x.VoucherNumbering == salesMaster.VoucherNumbering &&
                            x.VoucherNo == oldVoucherNo && x.VoucherTypeId == voucherTypeId &&
                            x.FinancialYearId == FinancialYearId);

                        //foreach (var objPartyBalance in await partyBalanceRepository.GetAllListAsync(x =>
                        //             x.VoucherTypeId == salesMaster.VoucherTypeId &&
                        //             x.FinancialYearId == salesMaster.FinancialYearId
                        //             && x.VoucherNo == salesMaster.VoucherNo && x.ReferenceType == "Against" &&
                        //             x.BranchId == salesMaster.BranchId))
                        //{
                        //    objPartyBalance.InvoiceNo = "";
                        //    objPartyBalance.VoucherNo = "";
                        //    objPartyBalance.VoucherTypeId = null;
                        //    objPartyBalance.ReferenceType = "OnAccount";
                        //    await partyBalanceRepository.UpdateAsync(objPartyBalance);
                        //}

                        //await partyBalanceRepository.DeleteAsync(x =>
                        //    x.VoucherTypeId == salesMaster.VoucherTypeId &&
                        //    x.FinancialYearId == salesMaster.FinancialYearId &&
                        //    x.VoucherNo == oldVoucherNo &&
                        //    x.VoucherNumbering == salesMaster.VoucherNumbering &&
                        //    x.BranchId == salesMaster.BranchId && x.ReferenceType == "New" &&
                        //    x.AgainstVoucherTypeId == Guid.Empty);

                        var taxList = new List<TaxDetailDto>();


                        foreach (var inputSalesDetailDto in input.SalesDetails)
                        {
                            var againstdetailId =
                                inputSalesDetailDto.SalesDetailId == Guid.Empty
                                    ? null
                                    : inputSalesDetailDto.SalesDetailId;
                            var detail = new SalesDetail
                            {
                                Id = inputSalesDetailDto.Id.HasValue && inputSalesDetailDto.Id.Value != Guid.Empty
                                    ? inputSalesDetailDto.Id.Value
                                    : Guid.NewGuid(),
                                TenantId = tenantId,
                                Qty = inputSalesDetailDto.Qty,
                                Rate = inputSalesDetailDto.Rate,
                                Discount = inputSalesDetailDto.Discount ?? 0
                            };

                            if (ERPCommonManager.GetTaxRate(inputSalesDetailDto.TaxId) > 0)
                            {
                                detail.Amount = inputSalesDetailDto.Amount ?? 0;
                                detail.NetAmount = detail.Amount /
                                                   (1 + ERPCommonManager.GetTaxRate(inputSalesDetailDto.TaxId));
                                detail.GrossAmount = detail.NetAmount + inputSalesDetailDto.Discount ?? 0;
                                detail.TaxAmount = detail.NetAmount *
                                                   ERPCommonManager.GetTaxRate(inputSalesDetailDto.TaxId);
                                detail.DiscountPer = inputSalesDetailDto.DiscountPer ?? 0;
                            }
                            else
                            {
                                detail.GrossAmount = detail.Qty * detail.Rate;
                                detail.NetAmount = detail.GrossAmount - detail.Discount;
                                detail.TaxAmount = detail.NetAmount *
                                                   ERPCommonManager.GetTaxRate(inputSalesDetailDto.TaxId);
                                detail.Amount = detail.NetAmount + detail.TaxAmount;
                                detail.DiscountPer = inputSalesDetailDto.DiscountPer ?? 0;
                            }

                            detail.SalesMasterId = salesMaster.Id;
                            var productData = await productRepository.FirstOrDefaultAsync(x =>
                                x.TenantId == AbpSession.GetTenantId() && x.Id == inputSalesDetailDto.ProductId);
                            detail.ProductName = productData?.Name;
                            detail.ProductId = inputSalesDetailDto.ProductId;
                            detail.UnitId = inputSalesDetailDto.UnitId;
                            detail.AgainstDetailId = againstdetailId;
                            detail.TaxId = inputSalesDetailDto.TaxId;
                            await salesDetailRepository.InsertAsync(detail);


                            await materialStockPostingService.ApplySalesIssueAsync(new MaterialSalesStockPostingRequest
                            {
                                DateMiti = input.DateMiti,
                                LedgerId = input.LedgerId,
                                VoucherTypeId = voucherTypeId,
                                VoucherNo = salesMaster.VoucherNo,
                                VoucherNumbering = salesMaster.VoucherNumbering,
                                ProductId = inputSalesDetailDto.ProductId,
                                UnitId = inputSalesDetailDto.UnitId,
                                Qty = detail.Qty,
                                Rate = detail.Rate,
                                GrossAmount = detail.GrossAmount,
                                DiscountAmount = detail.Discount,
                                NetAmount = detail.NetAmount,
                                Amount = detail.Amount,
                                TaxAmount = detail.TaxAmount,
                                FinancialYearId = FinancialYearId,
                                MasterId = salesMaster.Id,
                                SourceDetailId = detail.Id,
                                TenantId = tenantId
                            });


                            if (inputSalesDetailDto.TaxId != Guid.Empty)
                            {
                                var tax = new TaxDetailDto
                                {
                                    TaxId = inputSalesDetailDto.TaxId,
                                    Amount = inputSalesDetailDto.TaxAmount ?? 0
                                };

                                if (taxList.Select(x => x.TaxId).Contains(tax.TaxId))
                                {
                                    var obj = taxList.FirstOrDefault(x => x.TaxId == tax.TaxId);
                                    if (obj != null)
                                        obj.Amount = tax.Amount + obj.Amount;
                                }
                                else
                                {
                                    taxList.Add(tax);
                                }
                            }
                        }

                        foreach (var tax in taxList)
                        {
                            var taxes = await taxRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                                .FirstOrDefaultAsync(x => x.Id == tax.TaxId);
                            if (taxes.Rate > 0)
                            {
                                var ledgerPosting2 = new LedgerPosting
                                {
                                    TenantId = tenantId,
                                    VoucherNumbering = salesMaster.VoucherNumbering,
                                    Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                    DateMiti = input.DateMiti,
                                    VoucherTypeId = voucherTypeId,
                                    VoucherNo = salesMaster.VoucherNo,
                                    LedgerId = taxes.LedgerId,
                                    DetailId = input.SalesAccountId,
                                    Debit = 0,
                                    FinancialYearId = FinancialYearId,
                                    Credit = tax.Amount,
                                    InvoiceNo = salesMaster.VoucherNo,
                                    PostingNumber = postingNumbering,
                                    MasterId = salesMaster.Id
                                };
                                await ledgerPostingRepository.InsertAsync(ledgerPosting2);
                            }
                        }



                        var ledger =
                            await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                                .FirstOrDefaultAsync(x => x.Id == input.LedgerId);

                        //if (ledger.IsBillByBill)
                        //{
                        //    var partyBalanceData = new PartyBalance
                        //    {
                        //        VoucherNumbering = salesMaster.VoucherNumbering,
                        //        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        //        LedgerId = input.LedgerId,
                        //        FinancialYearId = FinancialYearId,
                        //        VoucherTypeId = voucherTypeId,
                        //        VoucherNo = salesMaster.VoucherNo,
                        //        AgainstVoucherTypeId = Guid.Empty,
                        //        AgainstVoucherNo = "NA",
                        //        InvoiceNo = salesMaster.VoucherNo,
                        //        AgainstInvoiceNo = "NA",
                        //        ReferenceType = "New",
                        //        Debit = salesMaster.GrandTotal,
                        //        Credit = 0,
                        //        CreditPeriod = input.CreditPeriod,

                        //        BranchId = input.BranchId,
                        //        MasterVoucherTypeId = voucherTypeId,
                        //        MasterId = (Guid)input.Id,
                        //        DetailId = Guid.Empty,
                        //        MasterVoucherNo = salesMaster.VoucherNo,
                        //        TenantId = tenantId
                        //    };
                        //    await partyBalanceRepository.InsertAsync(partyBalanceData);
                        //}
                        await partyBalanceService.UpdateSalesEntryAsync(new PartyBalanceNewEntryDto
                        {
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DueDate = DateConverter.ConvertToEnglish(input.DateMiti).AddDays(input.CreditPeriod),
                            LedgerId = input.LedgerId,
                            VoucherNo = salesMaster.VoucherNo,
                            VoucherNumbering = salesMaster.VoucherNumbering,
                            Amount = input.GrandTotal,
                            MasterId = salesMaster.Id,
                        });

                        var ledgerPosting = new LedgerPosting
                        {
                            TenantId = tenantId,
                            VoucherNumbering = salesMaster.VoucherNumbering,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = salesMaster.VoucherNo,
                            LedgerId = input.LedgerId,
                            DetailId = input.SalesAccountId,
                            Debit = grandTotal,
                            FinancialYearId = FinancialYearId,
                            Credit = 0,
                            InvoiceNo = salesMaster.VoucherNo,
                            PostingNumber = postingNumbering,
                            MasterId = salesMaster.Id
                        };
                        await ledgerPostingRepository.InsertAsync(ledgerPosting);

                        var ledgerPosting1 = new LedgerPosting
                        {
                            TenantId = tenantId,
                            VoucherNumbering = salesMaster.VoucherNumbering,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            VoucherNo = salesMaster.VoucherNo,
                            LedgerId = input.SalesAccountId,
                            DetailId = input.LedgerId,
                            Debit = 0,
                            FinancialYearId = FinancialYearId,
                            Credit = netAmount,
                            InvoiceNo = salesMaster.VoucherNo,
                            PostingNumber = postingNumbering,
                            MasterId = salesMaster.Id
                        };
                        await ledgerPostingRepository.InsertAsync(ledgerPosting1);
                    }
                    else
                    {
                        throw new UserFriendlyException("Data for VoucherNo : " + input.VoucherNo + " not found.");
                    }
                }

                await unitOfWork.CompleteAsync();
            }

            return (Guid)input.Id;
        }

        protected async Task ReceiptDelete(Guid vouchertypeId, string voucherNo)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("ReceiptVoucher");
            var master = await receiptMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x =>
                    x.RefVoucherTypeId == vouchertypeId && x.RefVoucherNo == voucherNo &&
                    x.FinancialYearId == FinancialYearId);
            if (master != null)
            {
                await receiptDetailRepository.DeleteAsync(x => x.ReceiptMasterId == master.Id);
                await ledgerPostingRepository.DeleteAsync(x =>
                    x.VoucherNo == master.VoucherNo &&
                    x.VoucherNumbering == master.VoucherNumbering &&
                    x.VoucherTypeId == voucherTypeId && x.FinancialYearId == master.FinancialYearId);

                await receiptMasterRepository.DeleteAsync(master.Id);

                //await partyBalanceRepository.DeleteAsync(x => x.AgainstVoucherNo == master.VoucherNo &&
                //                                              x.AgainstVoucherTypeId == master.VoucherTypeId &&
                //                                              x.FinancialYearId == FinancialYearId &&
                //                                              x.BranchId == master.BranchId);
                //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
                //    new LocalizableString($"Receipt Master of Voucher No {master.VoucherNo} is Deleted!",
                //        ERPConsts.LocalizationSourceName));
            }
        }


        //public async Task<FileDto> GetSalesMastersToExcel(GetAllSalesMastersForExcelInput input)
        //{
        //    var filteredSalesMasters = _salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
        //        .Include(e => e.AccountLedgerFk)
        //        .Include(e => e.UserFk)
        //        .Include(e => e.PricingLevelFk)
        //        .Include(e => e.DeliveryNoteMasterFk)
        //        .Include(e => e.SalesOrderMasterFk)
        //        .Include(e => e.SalesQuotationMasterFk)
        //        .Include(e => e.FinancialYearFk)
        //        .Include(e => e.BranchFk)
        //        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
        //            e => false || e.Description.Contains(input.Filter) ||
        //                 e.LrNo.Contains(input.Filter) || e.TransportationCompany.Contains(input.Filter));
        //    if (input.BranchId != 0)
        //    {
        //        filteredSalesMasters = filteredSalesMasters.Where(x => x.BranchId == input.BranchId);
        //    }
        //    if (input.FromMiti != null)
        //    {
        //        var date = DateConverter.ConvertToEnglish(input.FromMiti);
        //        filteredSalesMasters = filteredSalesMasters.Where(x => x.Date >= date);
        //    }
        //    if (input.ToMiti != null)
        //    {
        //        var date = DateConverter.ConvertToEnglish(input.ToMiti);
        //        filteredSalesMasters = filteredSalesMasters.Where(x => x.Date <= date);
        //    }
        //    var query = (from o in filteredSalesMasters
        //                 join o1 in _AccountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.LedgerId equals o1.Id into j1
        //                 from s1 in j1.DefaultIfEmpty()
        //                 join o2 in _UserRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.UserId equals o2.Id into j2
        //                 from s2 in j2.DefaultIfEmpty()
        //                 join o3 in _PricingLevelRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.PricingLevelId equals o3.Id into
        //                     j3
        //                 from s3 in j3.DefaultIfEmpty()
        //                 join o4 in _DeliveryNoteMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.DeliveryNoteMasterId equals
        //                     o4.Id into j4
        //                 from s4 in j4.DefaultIfEmpty()
        //                 join o5 in _SalesOrderMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.SalesOrderMasterId equals
        //                     o5.Id into j5
        //                 from s5 in j5.DefaultIfEmpty()
        //                 join o6 in _SalesQuotationMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.SalesQuotationMasterId
        //                     equals o6.Id into
        //                     j6
        //                 from s6 in j6.DefaultIfEmpty()
        //                 join o7 in _FinancialYearRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.FinancialYearId equals o7.Id into
        //                     j7
        //                 from s7 in j7.DefaultIfEmpty()
        //                 join o8 in _BrandRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on o.BranchId equals o8.Id into j8
        //                 from s8 in j8.DefaultIfEmpty()
        //                 select new GetSalesMasterForViewDto()
        //                 {
        //                     VoucherNo = o.VoucherNo,
        //                     SalesAccountId = o.SalesAccountId,
        //                     Date = o.Date,
        //                     CreditPeriod = o.CreditPeriod,
        //                     Description = o.Description,
        //                     TaxAmount = o.TaxAmount,
        //                     AdditionalCost = o.AdditionalCost,
        //                     BillDiscount = o.BillDiscount,
        //                     GrandTotal = o.GrandTotal,
        //                     TotalAmount = o.TotalAmount,
        //                     TaxableAmount = o.TaxableAmount,
        //                     NoOfPrint = o.NoOfPrint,
        //                     SubTotalAmount = o.SubTotalAmount,
        //                     SyncwithIRD = o.SyncwithIrd,
        //                     PrintedTime = o.PrintedTime,
        //                     IsRealTime = o.IsRealTime,
        //                     PaymentMethod = o.PaymentMethod,
        //                     IsDelete = o.IsDelete,
        //                     VatRefundAmount = o.VatRefundAmount,
        //                     lrNo = o.LrNo,
        //                     TransportationCompany = o.TransportationCompany,

        //                     VoucherTypeId = o.VoucherTypeId,
        //                     Id = o.Id,
        //                     LedgerName = s1 == null || s1.Name == null ? "" : s1.Name,
        //                     UserName = s2 == null || s2.Name == null ? "" : s2.Name,

        //                     BrandName = s8 == null || s8.Name == null ? "" : s8.Name.ToString()
        //                 });

        //    var salesMasterListDtos = await query.ToListAsync();

        //    return _salesMastersExcelExporter.ExportToFile(salesMasterListDtos);
        //}


        public async Task<SalesAccountLedgerDto> GetAccountLedger(Guid ledgerId)
        {
            var ledger = await accountLedgerRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(e => e.Id == ledgerId);
            if (ledger == null)
                throw new UserFriendlyException("Ledger Can not Found");

            return new SalesAccountLedgerDto
            {
                Name = ledger.Name,
                CreditLimit = ledger.CreditLimit,
                CreditPeriod = ledger.CreditPeriod,
                Address = ledger.Address
            };
        }

        public async Task<string> GetSalesMasterVoucherNo(SalesType salesType)
        {
            var data = await salesMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.FinancialYearId == FinancialYearId && x.SalesType == salesType)
                .Select(x => x.VoucherNumbering).ToListAsync();

            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "SalesInvoice");
            if (salesType == SalesType.TI)
                voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "TI");
            if (salesType == SalesType.ABT)
                voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "ABT");

            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;
            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }


        // Extract common stock calculation logic to reduce code duplication
        //private async Task CalculateStockQuantities(List<SalesDetailDto> items, bool isDeliveryNote = false)
        //{
        //    foreach (var item in items)
        //    {
        //        var stockPosting = await stockPostingRepository.GetAll()
        //            .Where(x => x.TenantId == AbpSession.TenantId &&
        //                        x.ProductId == item.ProductId &&
        //                        x.FinancialYearId == FinancialYearId)
        //            .AsNoTracking()
        //            .ToListAsync();

        //        var unitConversion = await unitConversionRepository.GetAll()
        //            .Where(x => x.TenantId == AbpSession.TenantId &&
        //                        x.ProductId == item.ProductId)
        //            .Include(x => x.UnitFk)
        //            .AsNoTracking()
        //            .ToListAsync();

        //        if (!unitConversion.Any())
        //            continue;

        //        var minUnit = unitConversion.OrderBy(x => x.ConversionRate).First();

        //        // Group and calculate stock
        //        var stockQty = stockPosting
        //            .GroupBy(x => x.UnitId)
        //            .OrderBy(x => x.Key);

        //        decimal minUnitQty = 0;

        //        foreach (var stock in stockQty)
        //        {
        //            var thisUnit = unitConversion.FirstOrDefault(x => x.UnitId == stock.Key);
        //            if (thisUnit == null)
        //                continue;

        //            var netQty = stock.Sum(x => x.InWardQty - x.OutWardQty);
        //            minUnitQty += netQty * thisUnit.PrimaryQty / thisUnit.Qty * minUnit.Qty / minUnit.PrimaryQty;
        //        }

        //        var productUnitconversion = unitConversion.FirstOrDefault(x => x.UnitId == item.UnitId);
        //        if (productUnitconversion != null)
        //        {
        //            var qty = minUnitQty * productUnitconversion.Qty / productUnitconversion.PrimaryQty *
        //                minUnit.PrimaryQty / minUnit.Qty;
        //            item.StockQty = isDeliveryNote ? qty + item.Qty : qty;
        //        }
        //    }
        //}

        /////end


        [DisableAuditing]
        public async Task<List<TaxesDetailDto>> GetAllTaxes()
        {
            return (await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                    .ToListAsync())
                .Select(x =>
                    new TaxesDetailDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Rate = x.Rate,
                        Description = x.Description,
                        LedgerId = x.LedgerId
                    }).ToList();
        }

        public async Task UploadImageNew(IFormFile file, Guid salesInvoiceId)
        {
            var purchaseOrder = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == salesInvoiceId);
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");

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

            var fileDetails = await voucherPhotosRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == id);
            if (file == null) throw new UserFriendlyException("Please select upload file");

            var changedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            long kb = 1000;
            if (file.Length > 500 * kb)
                throw new UserFriendlyException("File size is large.");
            if (file.Length > 0)
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    fileDetails.Image = fileBytes;
                    //  string s = Convert.ToBase64String(fileBytes);
                }

            fileDetails.ChangedFileName = changedFileName;
            await voucherPhotosRepository.UpdateAsync(fileDetails);
        }

        public async Task<List<DocumentDetailsDto>> GetAllDocuments(Guid salesMasterId)
        {
            var purchaseOrder = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(e => e.Id == salesMasterId);
            if (purchaseOrder == null)
                throw new UserFriendlyException("Data not found");
            var result = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.VoucherNo == purchaseOrder.VoucherNo &&
                            x.VoucherNumbering == purchaseOrder.VoucherNumbering &&
                            x.VoucherTypeId == purchaseOrder.VoucherTypeId &&
                            x.FinancialYearId == purchaseOrder.FinancialYearId).Include(x => x.VoucherTypeFk).AsNoTracking()
                .ToListAsync()).Select(x => new DocumentDetailsDto
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
                .Where(x => x.Id == id).Include(x => x.VoucherTypeFk).AsNoTracking().ToListAsync()).FirstOrDefault();
            if (data == null) throw new UserFriendlyException("Data not Found");

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

        public async Task<FileDto> ExportWithDetails(GetAllUniversalMastersInput input)
        {
            var filteredSalesMasters = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId);

            var productdetail = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(x => x.SalesMasterFk).Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk)
                .Where(x => x.SalesMasterFk.IsDelete == false).ToListAsync();


            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date <= date);
            }

            var accountLedger = await accountLedgerRepository.GetAll().Where(x => !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Pan
                }).AsNoTracking().ToListAsync();
            var list = new List<GetSalesMasterExportWithProductDto>();
            foreach (var item in filteredSalesMasters.OrderBy(x => x.Id))
            {
                var productname = string.Empty;
                decimal qty = 0;
                foreach (var salesitem in productdetail.Where(x => x.SalesMasterId == item.Id).ToList())
                {
                    productname = productname + salesitem.ProductFk.ProductGroupFk.Name + ",";
                    qty += salesitem.Qty;
                }

                productname = productname.Length > 0 ? productname.Remove(productname.Length - 1) : "";
                var sales = new GetSalesMasterExportWithProductDto
                {
                    DateMiti = item.DateMiti.Replace('/', '.'),
                    VoucherNo = item.VoucherNo,
                    LedgerName = item.IsDelete
                        ? "CANCELLED"
                        : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Name,
                    PanNo = item.IsDelete ? "" : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Pan,
                    ProductName = item.IsDelete ? "" : productname,
                    Quantity = item.IsDelete ? 0 : qty,
                    GrandTotal = item.IsDelete ? 0 : item.GrandTotal,
                    BillDiscount = item.IsDelete ? 0 : item.BillDiscount,
                    TaxAmount = item.IsDelete ? 0 : item.TaxAmount,
                    TotalAmount = item.IsDelete ? 0 : item.GrossAmount,
                    TaxableAmount = item.IsDelete ? 0 : item.TaxableAmount
                };
                list.Add(sales);
            }

            return salesMastersNewExcelExporter.ExportToFileWithDetails(list);
        }

        //public async Task<SalesDtoForSwastik> ExportForSwastik(string fromMiti, string toMiti)
        //{
        //    var result = new SalesDtoForSwastik
        //    {
        //        FromMiti = fromMiti,
        //        ToMiti = toMiti,
        //        Pan = "455533",
        //        FinancialYear = FinancialYear.Name
        //    };
        //    var salesQuery = salesMasterRepository.GetAll();
        //    if (!string.IsNullOrWhiteSpace(fromMiti))
        //    {
        //        var date = DateConverter.ConvertToEnglish(fromMiti);
        //        salesQuery = salesQuery.Where(x => x.Date >= date);
        //    }

        //    if (!string.IsNullOrWhiteSpace(toMiti))
        //    {
        //        var date = DateConverter.ConvertToEnglish(toMiti);
        //        salesQuery = salesQuery.Where(x => x.Date <= date);
        //    }

        //    var sales = await salesQuery.Where(x => x.FinancialYearId == FinancialYearId).AsNoTracking().ToListAsync();
        //    var salesIds = sales.Select(x => x.Id).ToList();
        //    var salesDetails = await salesDetailRepository.GetAll().Where(x => salesIds.Contains(x.SalesMasterId))
        //        .Include(x => x.SalesMasterFk).ThenInclude(x => x.AccountLedgerFk).Include(x => x.ProductFk)
        //        .AsNoTracking().ToListAsync();
        //    //var salesReturn = await _salesReturnMasterRepository.GetAll()
        //    //    .Where(x => salesIds.Contains((Guid)x.SalesMasterId)).AsNoTracking().ToListAsync();
        //    //   var salesReturnIds = salesReturn.Select(x => x.Id).ToList();
        //    //var salesReturnDetails = await _salesReturnDetailsRepository.GetAll()
        //    //    .Where(x => salesReturnIds.Contains(x.SalesReturnMasterId)).AsNoTracking().ToListAsync();

        //    var details = new List<SalesDetailDtoForSwastik>();

        //    foreach (var salesDetail in salesDetails)
        //    {
        //        var detail = new SalesDetailDtoForSwastik
        //        {
        //            BillMiti = salesDetail.SalesMasterFk.DateMiti,
        //            BillNo = salesDetail.SalesMasterFk.VoucherNo,
        //            PartyName = salesDetail.SalesMasterFk.AccountLedgerFk.Name,
        //            Pan = salesDetail.SalesMasterFk.AccountLedgerFk.Pan,
        //            Goods = salesDetail.ProductFk.Name,
        //            Qty = salesDetail.Qty,
        //            TotalSales = salesDetail.GrossAmount,
        //            TaxExempted = salesDetail.GrossAmount - salesDetail.TaxAmount,
        //            ExportedValue = salesDetail.GrossAmount,
        //            TaxableValue = salesDetail.TaxAmount,
        //            Vat = salesDetail.TaxAmount,
        //            CountryOfExport = "",
        //            ExportedPpno = "",
        //            ExportedPpMiti = ""
        //        };
        //        details.Add(detail);
        //    }

        //    result.Details = details;
        //    return result;
        //}

        //public async Task<FileDto> ExportFileToSwastik(string fromMiti, string toMiti)
        //{
        //    var data = await ExportForSwastik(fromMiti, toMiti);
        //    return salesMastersNewExcelExporter.ExportToFileSwastik(data);
        //}


        public async Task<FileDto> GetSalesMastersToExcelNew(GetAllUniversalMastersInput input)
        {
            var filteredSalesMasters = salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId);

            var productdetail = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Include(x => x.SalesMasterFk).Include(x => x.ProductFk).ThenInclude(x => x.ProductGroupFk)
                .Include(x => x.UnitFk)
                .Where(x => x.SalesMasterFk.IsDelete == false).ToListAsync();


            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesMasters = filteredSalesMasters.Where(x => x.Date <= date);
            }

            var accountLedger = await accountLedgerRepository.GetAll().Where(x => !x.IsDelete)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Pan
                }).AsNoTracking().ToListAsync();
            var list = new List<GetSalesMasterExportDto>();
            var branch = await branchRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.IsMain);

            foreach (var item in filteredSalesMasters.OrderBy(x => x.Id))
            {
                var productname = string.Empty;
                decimal qty = 0;
                foreach (var salesitem in productdetail.Where(x => x.SalesMasterId == item.Id).ToList())
                {
                    productname = productname + salesitem.ProductFk.ProductGroupFk.Name + ",";
                    qty += salesitem.Qty;
                }

                var details = productdetail.Where(x => x.SalesMasterId == item.Id).Select(x =>
                    new ProductDetailsForExcelExport
                    {
                        Name = x.ProductFk.Name,
                        Unit = x.UnitFk.Name,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Amount = x.Amount
                    }).ToList();
                productname = productname.Length > 0 ? productname.Remove(productname.Length - 1) : "";
                var sales = new GetSalesMasterExportDto
                {
                    DateMiti = item.DateMiti.Replace('/', '.'),
                    VoucherNo = item.VoucherNo,
                    LedgerName = item.IsDelete
                        ? "CANCELLED"
                        : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Name,
                    PanNo = item.IsDelete ? "" : accountLedger.FirstOrDefault(x => x.Id == item.LedgerId)?.Pan,
                    ProductName = item.IsDelete ? "" : productname,
                    Quantity = item.IsDelete ? 0 : qty,
                    GrossAmount = item.IsDelete ? 0 : item.GrossAmount,
                    BillDiscount = item.IsDelete ? 0 : item.BillDiscount,
                    TaxAmount = item.IsDelete ? 0 : item.TaxAmount,
                    TotalAmount = item.IsDelete ? 0 : item.GrandTotal,
                    TaxableAmount = item.IsDelete ? 0 : item.TaxableAmount,
                    Details = details
                };
                list.Add(sales);
            }

            var data = new GetSalesMasterExportMasterDto
            {
                Company = branch.CompanyName,
                Address = branch.Address,
                Phone = branch.PhoneNo1,
                Details = list
            };

            return salesMastersNewExcelExporter.ExportToFileDetails(data);
        }

        public async Task<List<TableLongDto>> GetAllUsers()
        {
            var data = new List<TableLongDto>
        {
            new()
            {
                Id = 0,
                Name = "All"
            }
        };
            data.AddRange((await UserManager.GetAllUserAsync()).Select(x => new TableLongDto
            {
                Id = x.Id,
                Name = x.UserName
            }).ToList());
            return data;
        }


        #region errrocheckcode

        public async Task FixedSales()
        {
            await FixedSalesInvoiceError();
            await DeleteDuplicateSalesLedgerPostings();
            await PostMissingSalesInvoiceLedgerData();
            await PostMissingSalesInvoiceStockData();
        }

        [UnitOfWork]
        private async Task FixedSalesInvoiceError()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .Where(lp => !salesMasterRepository.GetAll()
                        .Any(sm => sm.VoucherTypeId == lp.VoucherTypeId &&
                                   sm.FinancialYearId == lp.FinancialYearId &&
                                   sm.VoucherNo == lp.VoucherNo &&
                                   sm.TenantId == lp.TenantId &&
                                   !sm.IsDelete))
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


        private async Task DeleteDuplicateSalesLedgerPostings()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
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
                Logger.Error($"Error deleting duplicate sales ledger postings: {ex.Message}", ex);
            }
        }

        [UnitOfWork]
        public async Task PostMissingSalesInvoiceLedgerData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing ledger postings for sales invoices");

                // Retrieve required data in parallel
                var activeTax = await taxRepository.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Rate > 0);
                var cashLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Name == "Cash");


                var cashLedgerId = cashLedger?.Id;

                // Retrieve sales with missing ledger postings via left join (improves query performance vs. subquery .Any())
                var unpostedSales = await (
                        from sm in salesMasterRepository.GetAll().AsNoTracking()
                            // Left join on ledger postings
                        join lp in ledgerPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                sm.TenantId,
                                sm.FinancialYearId,
                                sm.VoucherTypeId,
                                sm.VoucherNo,
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
                            sm.TenantId == tenantId &&
                            sm.FinancialYearId == FinancialYearId &&
                            !sm.IsDelete &&
                            ledger == null // means no existing ledger posting
                        select new
                        {
                            sm.Id,
                            sm.TenantId,
                            sm.VoucherNumbering,
                            sm.DateMiti,
                            sm.Date,
                            sm.VoucherTypeId,
                            sm.VoucherNo,
                            sm.SalesAccountId,
                            sm.LedgerId,
                            sm.GrandTotal,
                            sm.NetAmount,
                            sm.TaxAmount,
                            sm.PostingNumbering,
                            sm.PaymentMethod,
                            sm.FinancialYearId,
                            sm.PaymentMethodLedgerId
                        }
                    )
                    .ToListAsync();

                if (!unpostedSales.Any())
                {
                    Logger.Info("No sales invoices with missing ledger postings found");
                    return;
                }

                Logger.Info($"Found {unpostedSales.Count} sales invoices with missing ledger postings");

                // Prepare ledger postings
                var ledgerPostings = new List<LedgerPosting>();

                // Process each unposted sale
                foreach (var sale in unpostedSales)
                {
                    // Delete possibly partial or erroneous data related to this sale
                    // Minimizes duplication if we re-run to fix incomplete postings


                    // Sales account posting
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = sale.TenantId,
                        VoucherNumbering = sale.VoucherNumbering,
                        Date = sale.Date,
                        DateMiti = sale.DateMiti,
                        VoucherTypeId = sale.VoucherTypeId,
                        VoucherNo = sale.VoucherNo,
                        LedgerId = sale.SalesAccountId,
                        DetailId = sale.LedgerId ?? Guid.Empty,
                        Debit = 0,
                        Credit = sale.NetAmount,
                        FinancialYearId = sale.FinancialYearId,
                        InvoiceNo = sale.VoucherNo,
                        PostingNumber = sale.PostingNumbering,
                        MasterId = sale.Id
                    });

                    // Tax posting if applicable
                    if (sale.TaxAmount > 0 && activeTax != null)
                        ledgerPostings.Add(new LedgerPosting
                        {
                            TenantId = sale.TenantId,
                            VoucherNumbering = sale.VoucherNumbering,
                            Date = sale.Date,
                            DateMiti = sale.DateMiti,
                            VoucherTypeId = sale.VoucherTypeId,
                            VoucherNo = sale.VoucherNo,
                            LedgerId = activeTax.LedgerId,
                            DetailId = sale.LedgerId ?? Guid.Empty,
                            Debit = 0,
                            Credit = sale.TaxAmount,
                            FinancialYearId = sale.FinancialYearId,
                            InvoiceNo = sale.VoucherNo,
                            PostingNumber = sale.PostingNumbering,
                            MasterId = sale.Id
                        });


                    // Payment ledger posting (cash-based)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = sale.TenantId,
                        VoucherNumbering = sale.VoucherNumbering,
                        Date = sale.Date,
                        DateMiti = sale.DateMiti,
                        VoucherTypeId = sale.VoucherTypeId,
                        VoucherNo = sale.VoucherNo,
                        LedgerId = sale.LedgerId.Value,
                        DetailId = sale.SalesAccountId,
                        Debit = sale.GrandTotal,
                        Credit = 0,
                        FinancialYearId = sale.FinancialYearId,
                        InvoiceNo = sale.VoucherNo,
                        PostingNumber = sale.PostingNumbering,
                        MasterId = sale.Id
                    });
                }

                // Bulk insert ledger postings
                if (ledgerPostings.Count > 0)
                {
                    await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);
                    await UnitOfWorkManager.Current.SaveChangesAsync();
                    Logger.Info($"Successfully created {ledgerPostings.Count} ledger postings " +
                                $"for {unpostedSales.Count} sales invoices");
                }


                foreach (var sale in unpostedSales.Where(e => e.PaymentMethodLedgerId != null).ToList())
                    if (sale.LedgerId != cashLedgerId &&
                        sale.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR or PaymentMethod.Cash &&
                        sale.PaymentMethodLedgerId.HasValue &&
                        sale.PaymentMethodLedgerId.Value != Guid.Empty)
                        using (var receiptUow = unitOfWorkManager.Begin())
                        {
                            try
                            {
                                await DeleteRelatedData(sale.Id);
                                await ReceiptVoucherPosting(sale.Id, sale.VoucherNo);
                                await receiptUow.CompleteAsync();
                            }
                            catch (Exception ex)
                            {
                                // Log but continue with other receipt vouchers
                                Logger.Error($"Error creating receipt voucher for sale {sale.VoucherNo}: {ex.Message}", ex);
                                await receiptUow.CompleteAsync();
                            }
                        }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting ledger data for sales invoices: {ex.Message}", ex);
                throw;
            }
        }


        [UnitOfWork]
        public async Task PostMissingSalesInvoiceStockData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing stock postings for sales invoices");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");

                // Find sales invoices that have details but no corresponding stock postings
                var salesWithMissingStockPostings = await (
                        from sm in salesMasterRepository.GetAll().AsNoTracking()
                        join sd in salesDetailRepository.GetAll().AsNoTracking()
                            on sm.Id equals sd.SalesMasterId
                        // Left join on stock postings for this specific sales detail
                        join sp in stockPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                sm.TenantId,
                                sm.FinancialYearId,
                                sm.VoucherTypeId,
                                sm.VoucherNo,
                                sd.ProductId
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
                            sm.TenantId == tenantId &&
                            sm.FinancialYearId == FinancialYearId &&
                            sm.VoucherTypeId == voucherTypeId &&
                            !sm.IsDelete &&
                            stock == null && // means no existing stock posting
                            sd.ProductFk.ProductType != ProductTypeEnum.Services // Only process non-service products
                        select new
                        {
                            SalesMaster = sm,
                            SalesDetail = sd,
                            sd.ProductFk.ProductType,
                        }
                    )
                    .ToListAsync();

                if (!salesWithMissingStockPostings.Any())
                {
                    Logger.Info("No sales invoices with missing stock postings found");
                    return;
                }

                Logger.Info($"Found {salesWithMissingStockPostings.Count} sales details with missing stock postings");

                // Group by sales master to process efficiently
                var groupedBySales = salesWithMissingStockPostings
                    .GroupBy(x => x.SalesMaster.Id)
                    .ToList();

                // Prepare collections for batch operations
                var stockPostingsToInsert = new List<StockPosting>();

                // Process each sales invoice
                foreach (var salesGroup in groupedBySales)
                {
                    var salesMaster = salesGroup.First().SalesMaster;

                    // Delete any existing partial/erroneous stock data for this sales invoice
                    await DeleteExistingStockData(new DeleteStockDataDto
                    {
                        VoucherTypeId = salesMaster.VoucherTypeId,
                        FinancialYearId = salesMaster.FinancialYearId,
                        VoucherNo = salesMaster.VoucherNo,
                    });

                    // Process each sales detail in this invoice
                    foreach (var item in salesGroup)
                    {
                        var salesDetail = item.SalesDetail;

                        // Create stock posting
                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = salesMaster.VoucherNumbering,
                            Date = salesMaster.Date,
                            DateMiti = salesMaster.DateMiti,
                            LedgerId = salesMaster.LedgerId,
                            VoucherTypeId = salesMaster.VoucherTypeId,
                            VoucherNo = salesMaster.VoucherNo,
                            GrossAmount = salesDetail.GrossAmount,
                            DiscountAmount = salesDetail.Discount,
                            NetAmount = salesDetail.NetAmount,
                            Amount = salesDetail.Amount,
                            TaxAmount = salesDetail.TaxAmount,
                            IsValueIncrease = false,
                            ProductId = salesDetail.ProductId,
                            UnitId = salesDetail.UnitId,
                            AgainstVoucherTypeId = Guid.Empty,
                            AgainstVoucherNo = "",
                            InWardQty = 0,
                            OutWardQty = salesDetail.Qty,
                            Rate = salesDetail.Rate,
                            FinancialYearId = salesMaster.FinancialYearId,
                            MasterId = salesMaster.Id,
                            TenantId = tenantId
                        };

                        stockPostingsToInsert.Add(stockPosting);

                        // Handle IMEI entries if the product allows serial numbers

                    }

                    // Update stock levels using the stock management service
                    foreach (var item in salesGroup)
                    {
                        var salesDetail = item.SalesDetail;

                        var stockManage = new StockMaintainDto
                        {
                            DateMiti = salesMaster.DateMiti,
                            ProductId = salesDetail.ProductId,
                            Qty = salesDetail.Qty,
                            Rate = salesDetail.Rate,
                            FinancialYearId = salesMaster.FinancialYearId,
                            Type = StockMaintainTypeEnum.Outward,
                            UnitId = salesDetail.UnitId,
                        };

                        try
                        {
                            await stockManagementAppService.MaintainStock(stockManage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not update stock levels for product {salesDetail.ProductId} " +
                                        $"in sales {salesMaster.VoucherNo}: {ex.Message}");
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

            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting stock data for sales invoices: {ex.Message}", ex);
                throw;
            }
        }


        private async Task DeleteExistingStockData(DeleteStockDataDto stockDataInfo)
        {
            // Delete any existing stock postings for this sales invoice
            await stockPostingRepository.DeleteAsync(x =>
                x.VoucherTypeId == stockDataInfo.VoucherTypeId &&
                x.FinancialYearId == stockDataInfo.FinancialYearId &&
                x.VoucherNo == stockDataInfo.VoucherNo);

        }


        // Helper methods to create specific types of ledger postings
        private LedgerPosting CreateSalesAccountPosting(dynamic sale)
        {
            return new LedgerPosting
            {
                TenantId = sale.TenantId,
                VoucherNumbering = sale.VoucherNumbering,
                Date = sale.Date,
                DateMiti = sale.DateMiti,
                VoucherTypeId = sale.VoucherTypeId,
                VoucherNo = sale.VoucherNo,
                LedgerId = sale.SalesAccountId,
                DetailId = (Guid)sale.LedgerId,
                Debit = 0,
                FinancialYearId = sale.FinancialYearId,
                Credit = sale.NetAmount,
                InvoiceNo = sale.VoucherNo,
                PostingNumber = sale.PostingNumbering,
                MasterId = sale.Id
            };
        }

        private LedgerPosting CreateTaxPosting(dynamic sale, Guid taxLedgerId)
        {
            return new LedgerPosting
            {
                TenantId = sale.TenantId,
                VoucherNumbering = sale.VoucherNumbering,
                Date = sale.Date,
                DateMiti = sale.DateMiti,
                VoucherTypeId = sale.VoucherTypeId,
                VoucherNo = sale.VoucherNo,
                LedgerId = taxLedgerId,
                DetailId = (Guid)sale.LedgerId,
                Debit = 0,
                FinancialYearId = sale.FinancialYearId,
                Credit = sale.TaxAmount,
                InvoiceNo = sale.VoucherNo,
                PostingNumber = sale.PostingNumbering,
                MasterId = sale.Id
            };
        }

        private LedgerPosting CreateCashPaymentPosting(dynamic sale)
        {
            if (sale.PaymentMethod is PaymentMethod.Card_Swipe or PaymentMethod.QR)
            {
                // For card or QR payments
                if (sale.PaymentMethodLedgerId == null || sale.PaymentMethodLedgerId == Guid.Empty)
                    return CreatePaymentPosting(sale, (Guid)sale.LedgerId);

                return CreatePaymentPosting(sale, (Guid)sale.PaymentMethodLedgerId);
            }

            // Default cash payment
            return CreatePaymentPosting(sale, (Guid)sale.LedgerId);
        }

        private LedgerPosting CreateNonCashPaymentPosting(dynamic sale)
        {
            return CreatePaymentPosting(sale, (Guid)sale.LedgerId);
        }

        private LedgerPosting CreatePaymentPosting(dynamic sale, Guid ledgerId)
        {
            return new LedgerPosting
            {
                TenantId = sale.TenantId,
                VoucherNumbering = sale.VoucherNumbering,
                Date = sale.Date,
                DateMiti = sale.DateMiti,
                VoucherTypeId = sale.VoucherTypeId,
                VoucherNo = sale.VoucherNo,
                LedgerId = ledgerId,
                DetailId = sale.SalesAccountId,
                Debit = sale.GrandTotal,
                FinancialYearId = sale.FinancialYearId,
                Credit = 0,
                InvoiceNo = sale.VoucherNo,
                PostingNumber = sale.PostingNumbering,
                MasterId = sale.Id
            };
        }


        private async Task DeleteRelatedData(Guid salesId)
        {
            // Get the full sales master entity from the database
            var salesMaster = await salesMasterRepository.GetAsync(salesId);
            if (salesMaster == null) return; // Nothing to delete if sales record doesn't exist

            // Create a reusable filter for voucher-related deletions
            var voucherFilter = new
            {
                salesMaster.VoucherTypeId,
                salesMaster.FinancialYearId,
                salesMaster.VoucherNo
            };

            // Delete ledger postings related to the sales voucher
            await ledgerPostingRepository.DeleteAsync(e =>
                e.VoucherTypeId == voucherFilter.VoucherTypeId &&
                e.FinancialYearId == voucherFilter.FinancialYearId &&
                e.VoucherNo == voucherFilter.VoucherNo);

            // Delete party balance records related to the sales voucher
            //await partyBalanceRepository.DeleteAsync(e =>
            //    e.VoucherTypeId == voucherFilter.VoucherTypeId &&
            //    e.FinancialYearId == voucherFilter.FinancialYearId &&
            //    e.VoucherNo == voucherFilter.VoucherNo &&
            //    e.BranchId == voucherFilter.BranchId);

            // Get all receipt masters related to this sales invoice - optimize the query
            var receiptList = await receiptMasterRepository.GetAll()
                .Where(e => e.RefVoucherTypeId == voucherFilter.VoucherTypeId &&
                            e.RefVoucherNo == voucherFilter.VoucherNo)
                .ToListAsync();

            // Process each receipt
            foreach (var receipt in receiptList)
            {
                if (receipt == null) continue;

                // Delete receipt details
                await receiptDetailRepository.DeleteAsync(x =>
                    x.ReceiptMasterId == receipt.Id);

                // Delete ledger postings for this receipt
                await ledgerPostingRepository.DeleteAsync(x =>
                    x.VoucherNo == receipt.VoucherNo &&
                    x.VoucherNumbering == receipt.VoucherNumbering &&
                    x.VoucherTypeId == receipt.VoucherTypeId &&
                    x.FinancialYearId == receipt.FinancialYearId);

                // Delete party balance entries for this receipt
                //await partyBalanceRepository.DeleteAsync(x =>
                //    x.MasterVoucherNo == receipt.VoucherNo &&
                //    x.MasterVoucherTypeId == receipt.VoucherTypeId &&
                //    x.FinancialYearId == voucherFilter.FinancialYearId &&
                //    x.BranchId == receipt.BranchId);

                // Delete the receipt master itself
                await receiptMasterRepository.DeleteAsync(receipt.Id);
            }
        }

        // Optimized base rate calculation with caching and minimal database calls
        private async Task<decimal> GetOptimizedBaseRateAsync(
            PriceDetailDto priceDetail, UnitProductDto product, int salesRateSetting,
            Guid productId, List<UnitConversionDto> unitConversions)
        {
            // Priority 1: Use price detail if available
            if (priceDetail?.FinalRate > 0)
                return priceDetail.FinalRate;

            // Priority 2: Handle sales rate settings
            switch (salesRateSetting)
            {
                case 1:
                    return await GetLatestSalesRateAsync(productId, product.SalesRate);
                case 2:
                    return await GetStockBasedRateAsync(productId, product, unitConversions);
                default:
                    return product.SalesRate;
            }
        }

        // Cached latest sales rate lookup
        private async Task<decimal> GetLatestSalesRateAsync(Guid productId, decimal fallbackRate)
        {
            var latestRate = await salesDetailRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.SalesMasterFk.Date)
                .Select(x => x.Rate)
                .FirstOrDefaultAsync();

            return latestRate > 0 ? latestRate : fallbackRate;
        }

        // Optimized stock-based rate calculation
        private async Task<decimal> GetStockBasedRateAsync(Guid productId, UnitProductDto product,
            List<UnitConversionDto> unitConversions)
        {
            // Get stock calculation setting (cached if possible)
            var stockCalc = await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId());

            switch (stockCalc)
            {
                case "average":
                    return await CalculateAverageRateAsync(productId, product);
                case "fifo":
                    return await CalculateFifoRateAsync(productId, product, unitConversions);
                default:
                    return product.SalesRate;
            }
        }

        // Optimized average rate calculation
        private async Task<decimal> CalculateAverageRateAsync(Guid productId, UnitProductDto product)
        {
            var stockData = await stockFifoTableRepository.GetAll()
                .Where(x => x.ProductId == productId && x.Qty > 0) // Filter zero quantities at DB level
                .Select(x => new { x.Qty, x.Rate })
                .ToListAsync();

            if (!stockData.Any()) return product.SalesRate;

            var totalQty = stockData.Sum(x => x.Qty);
            if (totalQty <= 0) return product.SalesRate;

            var weightedAverage = stockData.Sum(x => x.Rate * x.Qty) / totalQty;
            return weightedAverage * (1 + product.Margin / 100);
        }

        // Optimized FIFO rate calculation
        private async Task<decimal> CalculateFifoRateAsync(Guid productId, UnitProductDto product,
            List<UnitConversionDto> unitConversions)
        {
            var fifoRate = await stockFifoTableRepository.GetAll()
                .Where(x => x.ProductId == productId)
                .OrderBy(e => e.Date)
                .Select(x => new { x.Rate, x.UnitId })
                .FirstOrDefaultAsync();

            if (fifoRate == null) return product.SalesRate;

            var conversionDetail = unitConversions.FirstOrDefault(a => a.UnitId == fifoRate.UnitId);
            return conversionDetail != null
                ? fifoRate.Rate * conversionDetail.Qty * (1 + product.Margin / 100)
                : product.SalesRate;
        }

        // Efficient unit rate calculation
        private static decimal CalculateUnitRate(decimal baseRate, PriceDetailDto priceDetail, UnitConversionDto unit)
        {
            var effectiveRate = priceDetail?.FinalRate > 0 ? priceDetail.FinalRate : baseRate;
            return Math.Round(effectiveRate * unit.PrimaryQty / unit.Qty, 3);
        }


        [AbpAuthorize(AppPermissions.PagesSalesMastersPrint)]
        public async Task<byte[]> GetPdfDownload(Guid id)
        {
            var list = new List<PdfForSalesInvoiceModel>();
            var getSalesMaster = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(e => e.Id == id);
            if (getSalesMaster == null) throw new UserFriendlyException("Data not found");


            if (getSalesMaster.NoOfPrint > 1)
            {
                list.Add(await GetSalesInvoiceForPdf(id));
            }
            else
            {
                var printCount = 1; // await SettingManager.GetSettingValueForTenantAsync<int>(
                //    PrintSettings.SalesNoOfPrint,
                //    AbpSession.GetTenantId());
                for (var i = 1; i <= printCount; i++) list.Add(await GetSalesInvoiceForPdf(id));
            }

            var billFormat = (await SettingManager.GetSettingValueForTenantAsync(
                ErpSettings.SalesBillFormat, AbpSession.GetTenantId())).ToLower();

            if (billFormat == "sample-1")
            {
                var document = new SalesMasterPdf11(list);
                return document.GeneratePdf();
            }


            //if (billFormat == "Pos")
            //{
            //    var isTi = false;
            //    var data = await GetSalesPosForPdf(id);
            //    if (data.GrandTotal >= 10000) isTi = true;
            //    var document = new SalesPosPdf1(data, isTi);
            //    return document.GeneratePdf();
            //}

            //if (billFormat == "ticket")
            //{
            //    var isTi = false;
            //    var data = await GetSalesPosForPdf(id);
            //    if (data.GrandTotal >= 10000) isTi = true;
            //    var document = new SalesPosTicketPdf(data, isTi);
            //    return document.GeneratePdf();
            //}

            //if (billFormat == "sample-2")
            //{
            //    var document = new SalesMasterPdf22(list);
            //    return document.GeneratePdf();
            //}
            else
            {
                var document = new SalesMasterPdf11(list);
                return document.GeneratePdf();
            }
        }

        [AbpAuthorize(AppPermissions.PagesSalesPosPrint)]
        public async Task<byte[]> GetPosBillGetAll(Guid id)
        {
            var data = await GetSalesPosForPdf(id);
            var sales = await salesMasterRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == id);
            var isTi = false;
            if (sales.SalesType == SalesType.TI)
                isTi = true;
            if (data.GrandTotal >= 10000)
                isTi = true;

            var billFormat = (await SettingManager.GetSettingValueForTenantAsync(
                ErpSettings.SalesBillFormat, AbpSession.GetTenantId())).ToLower();


            if (billFormat == "ticket")
            {
                var document = new SalesPosTicketPdf(data, isTi);
                return document.GeneratePdf();
            }
            else
            {
                var document = new SalesPosPdf1(data, isTi);
                return document.GeneratePdf();
            }

            //var printCount = await SettingManager.GetSettingValueForTenantAsync<int>(
            //    AppSettings.ErpSettings.NoOfPrint, AbpSession.GetTenantId());
            //var list = new List<PdfForSalesPOSModel>();
            //for (var i = 1; i <= printCount; i++) list.Add(await GetSalesInvoiceForPdfPOS(id));

            //var document = new SalesMasterPos1(list);
            //return document.GeneratePdf();
        }
        #endregion
    }
}
