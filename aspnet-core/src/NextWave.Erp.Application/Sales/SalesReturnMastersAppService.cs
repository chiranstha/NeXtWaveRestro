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
using NextWave.Erp.Configuration;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Sales.Exporting;
using NextWave.Erp.Sales.Pdf;
using NextWave.Erp.Transaction;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static NextWave.Erp.Configuration.AppSettings;


namespace NextWave.Erp.Sales
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
    public class SalesReturnMastersAppService(
        IRepository<SalesReturnMaster, Guid> salesReturnMasterRepository,
        IRepository<User, long> userRepository,
        ISalesReturnMastersExcelExporter salesReturnMastersExcelExporter,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<SalesDetail, Guid> salesDetailRepository,
        IRepository<Unit, Guid> unitRepository,
        //   IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Tax, Guid> taxRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<VoucherPhotos, Guid> voucherPhotosRepository,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<Branch, Guid> branchRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<SalesReturnDetail, Guid> salesReturnDetailRepository,
        IRepository<FinancialYear, Guid> financialYearRepository,
        AppFolders appFolders,
        IHttpContextAccessor httpContextAccessor,
        IDocumentsAppService documentsAppService,
        IRepository<VoucherNumbering, Guid> voucherNumberingRepository,
        PartyBalanceService partyBalanceService,
        UserManager userManager,
        StockManagementAppService stockManagementAppService,
        MaterialStockPostingService materialStockPostingService,
        IRepository<UserBranch, Guid> userBranchRepository,
        IRepository<StockMaintain, Guid> stockMaintainRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Bom, Guid> bomRepository)
        : ErpAppServiceBase, ISalesReturnMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetSalesReturnMasterForViewDto>> GetAll(GetAllUniversalMastersInput input)
        {
            var filteredSalesReturnMasters = salesReturnMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Include(e => e.AccountLedgerFk)
                .Include(e => e.FinancialYearFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrEmpty(input.Filter),
                    x => x.AccountLedgerFk.Name.Contains(input.Filter.Trim()) || x.VoucherNo.Contains(input.Filter.Trim()))
                .Select(x => new
                {
                    x.Id,
                    x.Date,
                    x.DateMiti,
                    x.VoucherNo,
                    x.SalesAccountId,
                    x.TaxAmount,
                    x.BillDiscount,
                    x.GrandTotal,
                    x.LedgerId,
                    x.NetAmount,
                    x.LrNo,
                    x.TransportationCompany,
                    x.VoucherTypeId,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name,
                });
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesReturnMasters = filteredSalesReturnMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesReturnMasters = filteredSalesReturnMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredSalesReturnMasters = filteredSalesReturnMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);

            var salesReturnMasters = from o in pagedAndFilteredSalesReturnMasters
                                     join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                         o.CreateUserId equals o6.Id into j6
                                     from s6 in j6.DefaultIfEmpty()
                                     join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                         o.UpdateUserId equals o5.Id into j5
                                     from s5 in j5.DefaultIfEmpty()
                                     select new GetSalesReturnMasterForViewDto
                                     {
                                         VoucherNo = o.VoucherNo,
                                         TaxAmount = o.TaxAmount,
                                         LrNo = o.LrNo,
                                         TransportationCompany = o.TransportationCompany,
                                         LedgerId = o.LedgerId,
                                         DateMiti = o.Date == null ? " " : DateConverter.ConvertToNepali(o.Date),
                                         TotalAmount = o.NetAmount,
                                         BillDiscount = o.BillDiscount,
                                         GrandTotal = o.GrandTotal,
                                         Id = o.Id,
                                         CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                         UpdateUser = s5 == null || s5.Name == null ? "" : s5.Name,
                                         LedgerName = o.LedgerName,
                                     };

            var totalCount = await filteredSalesReturnMasters.CountAsync();
            return new PagedResultDto<GetSalesReturnMasterForViewDto>(
                totalCount,
                await salesReturnMasters.ToListAsync()
            );
        }

        public async Task<GetSalesReturnMasterForViewNewDto> GetSalesReturnMasterForView(Guid id)
        {
            var salesReturnMaster =
                (await salesReturnMasterRepository.GetAll().Where(x => x.Id == id).Include(x => x.SalesMasterFk)
                    .ToListAsync()).FirstOrDefault();

            var output = new GetSalesReturnMasterForViewNewDto
            {
                Id = salesReturnMaster.Id,
                VoucherNo = salesReturnMaster.VoucherNo,
                TaxAmount = salesReturnMaster.TaxAmount,
                LrNo = salesReturnMaster.LrNo,
                AgainstVoucherNo = salesReturnMaster.SalesMasterFk == null ? "" : salesReturnMaster.SalesMasterFk.VoucherNo,
                TransportationCompany = salesReturnMaster.TransportationCompany,
                NetAmount = salesReturnMaster.NetAmount,
                GrossAmount = salesReturnMaster.TotalAmount,
                BillDiscount = salesReturnMaster.BillDiscount,
                DateMiti = salesReturnMaster.DateMiti,
                GrandTotal = salesReturnMaster.GrandTotal,
                ReturnType = salesReturnMaster.ReturnType == ReturnType.RateDifference
                    ? salesReturnMaster.ReturnType +
                      (salesReturnMaster.DebitOrCreditNote ? " (DebitNote)" : " (CreditNote)")
                    : salesReturnMaster.ReturnType.ToString(),
                LedgerId = salesReturnMaster.LedgerId,
            };

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            var details = (await salesReturnDetailRepository.GetAll()
                    .Where(x => x.SalesReturnMasterId == id).Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync())
                .Select(x => new GetSalesReturnDetailsForViewNewDto
                {
                    Qty = x.Qty,
                    Rate = x.Rate,
                    TaxAmount = x.TaxAmount,
                    ProductName = x.ProductFk.Name,
                    TaxValue = x.TaxValue,
                    Discount = x.Discount,
                    DiscountPer = x.DiscountPer,
                    GrossAmount = x.GrossAmount,
                    UnitName = x.UnitFk.Name,
                    NetAmount = x.NetAmount,
                    Amount = x.Amount,
                    ProductId = x.ProductId,
                    UnitId = x.UnitId,
                    TaxId = x.TaxId
                }).ToList();
            output.Details = details;
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesSalesReturnMastersEdit)]
        public async Task<GetSalesReturnMasterForEditOutput> GetSalesReturnMasterForEdit(EntityDto<Guid> input)
        {
            var salesReturnMaster = await salesReturnMasterRepository.FirstOrDefaultAsync(input.Id);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            var output = new GetSalesReturnMasterForEditOutput
            {
                Id = salesReturnMaster.Id,
                VoucherNo = salesReturnMaster.VoucherNo,
                SalesAccountId = salesReturnMaster.SalesAccountId,
                Description = salesReturnMaster.Description,
                TaxAmount = salesReturnMaster.TaxAmount,
                LrNo = salesReturnMaster.LrNo,
                TransportationCompany = salesReturnMaster.TransportationCompany,
                Date = salesReturnMaster.Date,
                DateMiti = salesReturnMaster.DateMiti,
                TotalAmount = salesReturnMaster.TotalAmount,
                ReturnAmount = salesReturnMaster.TotalAmount,
                TaxableAmount = salesReturnMaster.TotalTaxableAmount,
                BillDiscount = salesReturnMaster.BillDiscount,
                ReturnType = salesReturnMaster.ReturnType,
                NetAmount = salesReturnMaster.NetAmount,
                InvoiceType = salesReturnMaster.InvoiceType,
                ReturnTaxAmount = salesReturnMaster.TaxAmount,
                SalesMasterId = salesReturnMaster.SalesMasterId,
                GrandTotal = salesReturnMaster.GrandTotal,
                SalesMasterVoucherNo = salesReturnMaster.SalesMasterId != null
                    ? (await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == salesReturnMaster.SalesMasterId))
                    .VoucherNo
                    : "",
                DebitOrCreditNote = salesReturnMaster.DebitOrCreditNote,
                LedgerId = salesReturnMaster.LedgerId,
                ReturnTaxId = salesReturnMaster.ReturnTaxId
            };

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            var salesReturnData = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.ProductFk).Where(x => x.SalesReturnMasterId == input.Id)
                .AsNoTracking().ToListAsync();
            var aaa = new List<SalesReturnDetailDto>();
            foreach (var x in salesReturnData)
            {
                List<PurchaseReturnUnitsQtyDto> getAllUnitList = new List<PurchaseReturnUnitsQtyDto>();
                if (x.SalesDetailId != null)
                    getAllUnitList = await GetAllUnitQtyForTableDropdown(x.ProductId, (Guid)x.SalesDetailId);
                else
                {
                    getAllUnitList = await GetAllUnitQtyForTableDropdown(x.ProductId, Guid.Empty);
                }
                var salesDto = new SalesReturnDetailDto
                {
                    Id = x.Id,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    ProductCode = x.ProductFk.ProductCode,
                    TaxAmount = x.TaxAmount,
                    Discount = x.Discount,
                    DiscountPer = x.DiscountPer,
                    GrossAmount = x.GrossAmount,
                    NetAmount = x.NetAmount,
                    Amount = x.Amount,
                    SalesDetailId = x.SalesDetailId,
                    ProductId = x.ProductId,
                    UnitsList = getAllUnitList,
                    UnitId = x.UnitId,
                    TaxId = x.TaxId
                };
                aaa.Add(salesDto);
            }

            output.SalesReturnDetail = aaa;
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditSalesReturnMasterDto input)
        {
            //DateTime date = DateConverter.ConvertToEnglish(input.DateMiti);
            //if (FinancialYear.FromDate > date)
            //    throw new UserFriendlyException($"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            //if (FinancialYear.ToDate < date)
            //    throw new UserFriendlyException($"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesSalesReturnMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var salesReturn = await salesReturnMasterRepository.FirstOrDefaultAsync(input.Id);
            await ledgerPostingRepository.DeleteAsync(x => x.VoucherTypeId == salesReturn.VoucherTypeId &&
                                                           x.FinancialYearId == salesReturn.FinancialYearId &&
                                                           x.VoucherNo == salesReturn.VoucherNo);

            if (salesReturn.ReturnType == ReturnType.ProductWise)
            {
                //  var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                var salesMaster = await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == salesReturn.SalesMasterId);

                salesMaster.VatRefundAmount -= salesReturn.TaxAmount;
                await salesMasterRepository.UpdateAsync(salesMaster);
                await partyBalanceService.DeleteSalesReturnEntryAsync(input.Id);

                //await partyBalanceRepository.DeleteAsync(x =>
                //    x.VoucherTypeId == salesMaster.VoucherTypeId && x.VoucherNo == salesMaster.VoucherNo &&
                //    x.AgainstVoucherTypeId == salesReturn.VoucherTypeId
                //    && x.AgainstVoucherNo == salesReturn.VoucherNo && x.ReferenceType == "Against" &&
                //    x.FinancialYearId == FinancialYearId && x.VoucherNumbering == salesReturn.VoucherNumbering);
            }

            var salesReturnDetails = await salesReturnDetailRepository.GetAll()
                .Where(x => x.SalesReturnMasterId == input.Id).ToListAsync();
            foreach (var detail in salesReturnDetails)
            {
                if (salesReturn.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                    await ReverseSalesReturnStockAsync(salesReturn, detail);
            }

            await stockPostingRepository.DeleteAsync(x => x.VoucherTypeId == salesReturn.VoucherTypeId &&
                                                          x.FinancialYearId == salesReturn.FinancialYearId &&
                                                          x.VoucherNo == salesReturn.VoucherNo);

            await salesReturnDetailRepository.DeleteAsync(x => x.SalesReturnMasterId == input.Id);
            await salesReturnMasterRepository.DeleteAsync(input.Id);
            //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
            //    new LocalizableString($"Sales Return Bill No {salesReturn.VoucherNo} is Deleted!",
            //        ERPConsts.LocalizationSourceName));
        }

        private async Task ApplySalesReturnStockAsync(
            string dateMiti,
            Guid ledgerId,
            Guid voucherTypeId,
            string voucherNo,
            int voucherNumbering,
            Guid againstVoucherTypeId,
            string againstVoucherNo,
            Guid masterId,
            int? tenantId,
            SalesReturnDetail detail,
            string vendorVoucherNo)
        {
            await materialStockPostingService.ApplySalesReturnAsync(new MaterialSalesReturnStockPostingRequest
            {
                DateMiti = dateMiti,
                LedgerId = ledgerId,
                VoucherTypeId = voucherTypeId,
                VoucherNo = voucherNo,
                VoucherNumbering = voucherNumbering,
                AgainstVoucherTypeId = againstVoucherTypeId,
                AgainstVoucherNo = againstVoucherNo,
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
                MasterId = masterId,
                VendorVoucherNo = vendorVoucherNo,
                SalesDetailId = detail.SalesDetailId,
                SourceDetailId = detail.Id,
                TenantId = tenantId
            });
        }

        private async Task ReverseSalesReturnStockAsync(SalesReturnMaster salesReturn, SalesReturnDetail detail)
        {
            var existingPostings = await stockPostingRepository.GetAll()
                .Where(x => x.MasterId == salesReturn.Id && x.SourceDetailId == detail.Id)
                .AsNoTracking()
                .ToListAsync();
            if (existingPostings.Count > 0)
            {
                await materialStockPostingService.ReverseExistingStockPostingsAsync(existingPostings);
                return;
            }

            var product = await productRepository.GetAsync(detail.ProductId);
            if (product.ProductType == ProductTypeEnum.Services)
                return;

            var activeRecipe = await bomRepository.GetAll()
                .Where(x => x.TenantId == salesReturn.TenantId &&
                            x.ProductId == detail.ProductId &&
                            !x.IsDeleted &&
                            x.IsActive)
                .ToListAsync();

            if (activeRecipe.Count == 0)
            {
                await stockManagementAppService.MaintainStock(new StockMaintainDto
                {
                    DateMiti = salesReturn.DateMiti,
                    ProductId = detail.ProductId,
                    Qty = -detail.Qty,
                    Rate = detail.Rate,
                    FinancialYearId = salesReturn.FinancialYearId,
                    Type = StockMaintainTypeEnum.Inward,
                    UnitId = detail.UnitId
                });
                return;
            }

            foreach (var recipeLine in activeRecipe)
            {
                var requiredQty = detail.Qty * recipeLine.Quantity * (1 + recipeLine.WastagePercentage / 100);
                var rawMaterial = await productRepository.GetAsync(recipeLine.RawMaterialId);
                var rate = recipeLine.CostRate > 0 ? recipeLine.CostRate : rawMaterial.PurchaseRate;

                await stockManagementAppService.MaintainStock(new StockMaintainDto
                {
                    DateMiti = salesReturn.DateMiti,
                    ProductId = recipeLine.RawMaterialId,
                    Qty = -requiredQty,
                    Rate = rate,
                    FinancialYearId = salesReturn.FinancialYearId,
                    Type = StockMaintainTypeEnum.Inward,
                    UnitId = recipeLine.UnitId
                });
            }
        }

        private async Task InsertSalesReturnStockPostingAsync(
            string dateMiti,
            Guid ledgerId,
            Guid voucherTypeId,
            string voucherNo,
            int voucherNumbering,
            Guid againstVoucherTypeId,
            string againstVoucherNo,
            Guid masterId,
            int? tenantId,
            Guid productId,
            Guid unitId,
            decimal qty,
            decimal rate,
            decimal grossAmount,
            decimal discount,
            decimal netAmount,
            decimal amount,
            decimal taxAmount,
            string vendorVoucherNo)
        {
            await stockPostingRepository.InsertAsync(new StockPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(dateMiti),
                DateMiti = dateMiti,
                LedgerId = ledgerId,
                VoucherTypeId = voucherTypeId,
                ProductId = productId,
                UnitId = unitId,
                GrossAmount = grossAmount,
                DiscountAmount = discount,
                NetAmount = netAmount,
                Amount = amount,
                TaxAmount = taxAmount,
                AgainstVoucherTypeId = againstVoucherTypeId,
                AgainstVoucherNo = againstVoucherNo,
                InWardQty = qty,
                OutWardQty = 0,
                Rate = rate,
                IsValueIncrease = true,
                FinancialYearId = FinancialYearId,
                MasterId = masterId,
                VendorVoucherNo = vendorVoucherNo,
                TenantId = tenantId
            });
        }

        public async Task<FileDto> GetSalesReturnMastersToExcel(GetAllUniversalMastersInput input)
        {
            var filteredSalesReturnMasters = salesReturnMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk)
                .Include(e => e.FinancialYearFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.Description.Contains(input.Filter) ||
                         e.LrNo.Contains(input.Filter) || e.TransportationCompany.Contains(input.Filter));
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredSalesReturnMasters = filteredSalesReturnMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredSalesReturnMasters = filteredSalesReturnMasters.Where(x => x.Date <= date);
            }

            var query = from o in filteredSalesReturnMasters
                        join o1 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on
                            o.LedgerId equals o1.Id into j1
                        from s1 in j1.DefaultIfEmpty()
                        join o4 in financialYearRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on
                            o.FinancialYearId equals o4.Id into j4
                        from s4 in j4.DefaultIfEmpty()
                        select new GetSalesReturnMasterForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            TaxAmount = o.TaxAmount,
                            LrNo = o.LrNo,
                            DateMiti = o.DateMiti,
                            TransportationCompany = o.TransportationCompany,
                            TotalAmount = o.TotalAmount,
                            BillDiscount = o.BillDiscount,
                            GrandTotal = o.GrandTotal,
                            Id = o.Id,
                            Description = o.Description,
                            LedgerName = s1 == null || s1.Name == null ? "" : s1.Name,
                        };

            var salesReturnMasterListDtos = await query.ToListAsync();

            return salesReturnMastersExcelExporter.ExportToFile(salesReturnMasterListDtos);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
        public async Task<List<SalesReturnMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            var data = await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Debtors" || x.AccountGroupFk.Name == "Sundry Creditors" ||
                            x.Name == "Cash")
                .Select(accountLedger => new SalesReturnMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    MobileNo = accountLedger.Phone,
                    Address = accountLedger.Address
                }).ToListAsync();
            return data;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
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

        private async Task UpdatemasterId()
        {
            foreach (var item in await salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                         .ToListAsync())
            {
                var ledgerPosting = await ledgerPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x =>
                        x.VoucherTypeId == item.VoucherTypeId && x.VoucherNo == item.VoucherNo &&
                        x.FinancialYearId == item.FinancialYearId).ToListAsync();
                foreach (var posting in ledgerPosting)
                {
                    posting.MasterId = item.Id;
                    await ledgerPostingRepository.UpdateAsync(posting);
                }
            }
        }
        //  private void OpeningStockCalc()
        //  {
        //      var products = _ProductRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).ToList();
        //      foreach (var item in products)
        //      {
        //          var imeilist = _imeiRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.VoucherTypeFk).AsNoTracking()
        //.Where(x => x.FinancialYearId == FinancialYearId && x.ProductId == item.Id);

        //          var imenum = imeilist
        //              .Where(x => x.VoucherTypeFk.Name == "SalesInvoice" || x.VoucherTypeFk.Name == "DeliveryNote"
        //                     || x.VoucherTypeFk.Name == "PurchaseReturn" || (x.VoucherTypeFk.Name == "StockJournal" && x.ImeiGenerationType == ImeiGenerationType.Consume))
        //              .Select(x => x.ImeiNumber).ToList();
        //          var imeinumber = imeilist
        //              .Where(x => !x.IsSold && !imenum.Contains(x.ImeiNumber))
        //              .Where(x => x.VoucherTypeFk.Name == "OpeningStock" || x.VoucherTypeFk.Name == "PurchaseInvoice" ||
        //                          x.VoucherTypeFk.Name == "SalesReturn" || (x.VoucherTypeFk.Name == "StockJournal" && x.ImeiGenerationType == ImeiGenerationType.Production))
        //              .ToList();

        //          foreach (var imei in imeinumber)
        //          {
        //              if (_imeiRepository.Count(x => x.ProductId == item.Id && x.ImeiNumber == imei.ImeiNumber && x.VoucherTypeId == 2 && x.FinancialYearId == 4) == 0)
        //              {
        //                  var data = new Imei
        //                  {
        //                      IsDeleted = false,
        //                      ProductId = imei.ProductId,
        //                      ImeiNumber = imei.ImeiNumber,
        //                      DateMiti = imei.DateMiti,
        //                      Date = DateConverter.ConvertToEnglish(imei.DateMiti),
        //                      IsSold = false,
        //                      VoucherNo = imei.ProductId.ToString(),
        //                      VoucherTypeId = Guid.Empty,
        //                      AgainstVoucherNo = "NA",
        //                      AgainstVoucherTypeId = Guid.Empty,
        //                      StockId = null,
        //                      ImeiGenerationType = ImeiGenerationType.BuyOrSell,
        //                      BranchId = 1,
        //                      FinancialYearId = 4,
        //                      TenantId = AbpSession.TenantId
        //                  };
        //                  _imeiRepository.Insert(data);
        //              }
        //          }
        //      }
        //  }

        private void Purchaseposting()
        {
            var sales = salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).ToList();
            var ledgerposting = ledgerPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).ToList();
            foreach (var item in sales)
            {
                var salesdetails = salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.SalesReturnMasterId == item.Id).ToList();
                if (ledgerposting.Count(x =>
                        x.VoucherTypeId == item.VoucherTypeId && x.VoucherNo == item.VoucherNo &&
                        x.FinancialYearId == item.FinancialYearId && x.LedgerId == item.LedgerId) == 0)
                {
                    var ledgerPosting = new LedgerPosting
                    {
                        TenantId = item.TenantId,
                        VoucherNumbering = item.VoucherNumbering,
                        Date = item.Date,
                        DateMiti = item.DateMiti,
                        VoucherTypeId = item.VoucherTypeId,
                        VoucherNo = item.VoucherNo,
                        LedgerId = item.LedgerId,
                        DetailId = Guid.Empty,
                        Credit = item.TotalAmount,
                        FinancialYearId = item.FinancialYearId,
                        Debit = 0,
                        InvoiceNo = item.VoucherNo,
                    };
                    ledgerPostingRepository.Insert(ledgerPosting);

                    var ledgerPosting1 = new LedgerPosting
                    {
                        TenantId = item.TenantId,
                        VoucherNumbering = item.VoucherNumbering,
                        Date = item.Date,
                        DateMiti = item.DateMiti,
                        VoucherTypeId = item.VoucherTypeId,
                        VoucherNo = item.VoucherNo,
                        LedgerId = item.SalesAccountId,
                        DetailId = Guid.Empty,
                        FinancialYearId = item.FinancialYearId,
                        Debit = item.GrandTotal,
                        Credit = 0,
                        InvoiceNo = item.VoucherNo,
                    };
                    ledgerPostingRepository.Insert(ledgerPosting1);
                    var taxList = new List<TaxDetailDto>();

                    foreach (var inputSalesDetailDto in salesdetails)
                    {
                        if (inputSalesDetailDto.TaxId != Guid.Empty)
                        {
                            var tax = new TaxDetailDto
                            {
                                TaxId = (Guid)inputSalesDetailDto.TaxId,
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

                        var productData =
                            productRepository.FirstOrDefault(x => x.Id == inputSalesDetailDto.ProductId);
                        if (productData.ProductType != ProductTypeEnum.Services)
                        {
                            var stockPosting = new StockPosting
                            {
                                VoucherNumbering = item.VoucherNumbering,
                                Date = Convert.ToDateTime(item.Date),
                                VoucherTypeId = item.VoucherTypeId,
                                VoucherNo = item.VoucherNo,
                                ProductId = inputSalesDetailDto.ProductId,
                                UnitId = inputSalesDetailDto.UnitId,
                                AgainstVoucherTypeId = Guid.Empty,
                                AgainstVoucherNo = "",
                                InWardQty = inputSalesDetailDto.Qty,
                                OutWardQty = 0,
                                Rate = inputSalesDetailDto.Rate,
                                FinancialYearId = item.FinancialYearId,
                                TenantId = item.TenantId
                            };
                            var detailId = stockPostingRepository.InsertAndGetId(stockPosting);
                        }
                    }

                    foreach (var tax in taxList)
                    {
                        var taxes = taxRepository.FirstOrDefault(x => x.Id == tax.TaxId);
                        if (taxes.Rate > 0)
                        {
                            var ledgerPosting2 = new LedgerPosting
                            {
                                TenantId = item.TenantId,
                                VoucherNumbering = item.VoucherNumbering,
                                Date = item.Date,
                                DateMiti = item.DateMiti,
                                VoucherTypeId = item.VoucherTypeId,
                                VoucherNo = item.VoucherNo,
                                LedgerId =
                                    taxRepository.FirstOrDefault(x => x.Id == tax.TaxId).LedgerId,
                                DetailId = Guid.Empty,
                                Debit = tax.Amount,
                                FinancialYearId = item.FinancialYearId,
                                Credit = 0,
                                InvoiceNo = item.VoucherNo,
                            };

                            ledgerPostingRepository.InsertAndGetId(ledgerPosting2);
                        }
                    }
                }
            }
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllProductsForSalesReturn()
        {
            return (await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .ToListAsync()).Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToList();
        }

        [DisableAuditing]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnitQtyForTableDropdown(Guid productId, Guid detailId)
        {
            var returnList = new List<PurchaseReturnUnitsQtyDto>();

            if (productId == Guid.Empty) return returnList;

            if (detailId == Guid.Empty)
            {
                var product = await productRepository.GetAll().Where(x => x.Id == productId)
                    .Include(x => x.UnitFk).Select(x => new PurchaseReturnUnitsQtyDto
                    {
                        UnitId = x.UnitId,
                        ProductId = x.Id,
                        UnitName = x.UnitFk.Name,
                        Qty = 0,
                        Rate = x.SalesRate
                    }).AsNoTracking().ToListAsync();
                return product;
            }


            var salesDetails =
                await salesDetailRepository.FirstOrDefaultAsync(x => x.Id == detailId && x.ProductId == productId);
            if (salesDetails == null) return returnList;
            {
                //var unitConversions = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                //    .Include(x => x.UnitFk).Where(x => x.ProductId == productId).AsNoTracking().ToListAsync();
                //var lowestUnit = unitConversions.OrderBy(x => x.ConversionRate).First();
                var returnedQty = await salesReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.SalesReturnMasterFk)
                    .Where(x => x.SalesDetailId == detailId && x.ProductId == productId)
                    .AsNoTracking().SumAsync(x => x.Qty);

                returnList.Add(new PurchaseReturnUnitsQtyDto
                {
                    UnitId = salesDetails.UnitId,
                    ProductId = salesDetails.ProductId,
                    UnitName = (await unitRepository.FirstOrDefaultAsync(x => x.Id == salesDetails.UnitId)).Name,
                    Rate = salesDetails.Rate,
                    Qty = salesDetails.Qty - returnedQty
                });
            }
            return returnList;
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllProduct()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).AsNoTracking().ToListAsync();
        }

        [DisableAuditing]
        public async Task<List<PurchaseReturnUnitsQtyDto>> GetAllUnits()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(unit => new PurchaseReturnUnitsQtyDto
                {
                    UnitId = unit.Id,
                    UnitName = unit.Name == null ? "" : unit.Name.ToString(),
                    Rate = 0,
                    Qty = 0
                }).AsNoTracking().ToListAsync();
        }

        [DisableAuditing]
        public async Task<List<UnitConversionServiceDto>> GetAllUnitsByProductId(Guid productId)
        {
            var product = await productRepository.FirstOrDefaultAsync(x => x.Id == productId);
            if (product == null)
                throw new UserFriendlyException("Product of id : " + productId + " not found");
            var result = UnitConversionManager.GetAllUnitConversionsAll(productId, product.SalesRate);
            return result;
        }

        public async Task<List<TaxDto>> GetAllTaxAccountLedgerForTableDropdown()
        {
            return (await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .ToListAsync()).Select(x => new TaxDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Rate = x.Rate,
                    LedgerId = x.LedgerId
                }).ToList();
        }

        public async Task<PdfForSalesReturnModel> GetSalesReturnForPdf(Guid id)
        {
            var salesReturn = await salesReturnMasterRepository.GetAll()
                .Include(e => e.VoucherTypeFk)
                .Include(x => x.SalesMasterFk)
                .Where(x => x.Id == id).FirstOrDefaultAsync();
            if (salesReturn != null)
            {
                var branchDetails =
                    await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
                var customerDetails =
                    await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == salesReturn.LedgerId);
                var result = new PdfForSalesReturnModel
                {
                    BranchName = branchDetails.CompanyName,
                    BranchContactNo = (branchDetails.PhoneNo2 == null || branchDetails.PhoneNo2 == string.Empty)
                        ? branchDetails.PhoneNo1
                        : branchDetails.PhoneNo1 + " / " + branchDetails.PhoneNo2,
                    Address = branchDetails.Address,
                    Pan = branchDetails.PANumber,
                    DateMiti = DateConverter.ConvertToNepali(salesReturn.Date),
                    Logo1 = branchDetails.Image1!,
                    SalesType = salesReturn.SalesMasterFk?.SalesType ?? SalesType.Sales,
                    SalesVoucherNo = salesReturn.SalesMasterFk?.VoucherNo,
                    DebitOrCreditNote = salesReturn.DebitOrCreditNote,
                    ReturnType = salesReturn.ReturnType,
                    CustomerName = customerDetails.Name,
                    CustomerAddress = customerDetails.Address,
                    CustomerPan = customerDetails.Pan,
                    OrderNo = salesReturn.VoucherNo,
                    LedgerName =
                        (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == salesReturn.LedgerId)).Name,
                    TotalAmountInWord = CurrencyToAmount.NumberToText((int)salesReturn.GrandTotal),
                    TotalAmount = salesReturn.TotalAmount,
                    Discount = salesReturn.BillDiscount,
                    NetAmount = salesReturn.NetAmount,
                    TaxableAmount = salesReturn.TotalTaxableAmount,
                    TaxAmount = salesReturn.TaxAmount,
                    GrandTotal = salesReturn.GrandTotal,
                    Description = salesReturn.Description,
                    ApprovedBy = null,
                    ReceivedBy = null
                };
                var serial = 1;
                result.SalesReturnDetail = (await salesReturnDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.SalesReturnMasterId == id)
                    .Include(x => x.ProductFk)
                    .Include(x => x.UnitFk)
                    .AsNoTracking().ToListAsync()).Select(x =>
                    new PdfForSalesReturnDetailModel
                    {
                        SlNo = serial++,
                        ProductName = x.ProductFk.Name.Split("=>")[0],
                        Quantity = x.Qty,
                        HsCode = x.ProductFk.HsCode,
                        Discount = x.Discount,
                        Unit = x.UnitFk.Name,
                        Rate = x.Rate,
                        Amount = x.NetAmount
                    }).ToList();

                return result;
            }

            throw new UserFriendlyException("Data not found");
        }


        public async Task<ProductWithPricingLevelDto> GetProductById(Guid productId)
        {
            var product = await productRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == productId);
            if (product == null) throw new UserFriendlyException("Product not found");
            //   var unitList = await GetAllUnitForTableDropdown(productId);

            var unitConversionDetail = new List<PurchaseReturnUnitsQtyDto>()
            {
                new PurchaseReturnUnitsQtyDto
                {
                    UnitId = product.UnitId,
                    UnitName = (await unitRepository.FirstOrDefaultAsync(x => x.Id == product.UnitId)).Name,
                    Rate = product.SalesRate,
                    Qty = 0,
                    ProductId = productId
                }
            };

            var data = new ProductWithPricingLevelDto();
            var tax = await taxRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                .FirstOrDefaultAsync(x => x.Id == product.TaxId);

            data.Id = product.Id;
            data.ProductName = product.Name;
            data.Mrp = product.Mrp;
            data.UnitId = product.UnitId;
            data.ProductType = product.ProductType;
            data.TaxId = product.TaxId;
            data.TaxRate = tax?.Rate ?? 0;
            data.Quantity = 0;
            data.Rate = product.SalesRate;
            data.UnitsList = unitConversionDetail;

            return data;
        }


        //[AbpAuthorize(AppPermissions.PagesSalesReturnMastersPrint)]
        //public async Task<byte[]> GetPdfDownload(Guid id)
        //{
        //    var model = await GetSalesReturnForPdf(id);
        //    var list = new List<PdfForSalesReturnModel> { model };
        //    if (model.SalesType == SalesType.Sales)
        //    {
        //        if (model.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
        //        {
        //            var document = new SalesReturnMasterPdf(list);
        //            return document.GeneratePdf();
        //        }
        //        else
        //        {
        //            var document = new SalesReturnPartyPdf(list);
        //            return document.GeneratePdf();
        //        }
        //    }
        //    {
        //        var document = new SalesReturnPosPdf(model);
        //        return document.GeneratePdf();
        //    }

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

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            if (await salesReturnMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0)
                return true;
            return false;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            var salesReturnMaster = await salesReturnMasterRepository.FirstOrDefaultAsync(x =>
                x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = salesReturnMaster.VoucherTypeId,
                VoucherNo = salesReturnMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        public async Task<string> GetSalesReturnVoucherNo()
        {
            var data = await salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "SalesReturn");
            if (data.Count == 0)
                return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;
            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x => (x.AccountGroupFk.Name == "Cash-in Hand" ||
                                               x.AccountGroupFk.Name == "Bank Account" ||
                                               x.AccountGroupFk.Name == "Bank OD A/C"))
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel == null || pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
        public async Task<List<UniversalDropdownDto>>
            GetAllExpensesLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x => (x.AccountGroupFk.Name == "Misc.Expenses (ASSET)" ||
                                               x.AccountGroupFk.Name == "Direct Expenses" ||
                                               x.AccountGroupFk.Name == "Indirect Expenses"))
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel == null || pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await salesReturnMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            if (data.Count == 0)
            {
                var voucherNumbering =
                    await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "SalesReturn");
                return voucherNumbering.StartIndex;
            }

            return data.Max() + 1;
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>>
            GetAllPartyWiseAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x =>
                    x.AccountGroupFk.Name == "Sales Account" || x.AccountGroupFk.Name == "Purchase Account")
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel == null || pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesSalesReturnMasters)]
        public async Task<List<UniversalDropdownDto>> GetSalesAccountForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.AccountGroupFk).Where(x => x.AccountGroupFk.Name == "Sales Account")
                .Select(pricingLevel => new UniversalDropdownDto
                {
                    Id = pricingLevel.Id,
                    DisplayName = pricingLevel == null || pricingLevel.Name == null ? "" : pricingLevel.Name.ToString()
                }).AsNoTracking().ToListAsync();
        }


        //public async Task<List<ImeiNumberListDetailDto>> GetImeiListByProductId(Guid productId)
        //{
        //    var data = await imeiRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).Where(x =>
        //            x.ProductId == productId && !x.IsSold && x.FinancialYearId == FinancialYearId)
        //        .Select(branch => new ImeiNumberListDetailDto
        //        {
        //            Id = branch.Id,
        //            ImeiNumber = branch == null || branch.ImeiNumber == null ? "" : branch.ImeiNumber.ToString(),
        //            IsSold = branch.IsSold
        //        }).AsNoTracking().ToListAsync();
        //    var data1 = data.DistinctBy(x => x.ImeiNumber).ToList();
        //    return data1;
        //}

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "SalesReturn");
        }

        [AbpAuthorize(AppPermissions.PagesSalesReturnMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditSalesReturnMasterDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            decimal? postingNumbering = PostingNumbering;
            var tenantId = AbpSession.TenantId;
            if (await salesReturnMasterRepository.CountAsync(x =>
                    x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                throw new UserFriendlyException("Voucher number already exist");
            var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
            //  var againstVoucherNo = new SalesMaster();
            var salesMaster =
                await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == input.SalesMasterId);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetSalesReturnVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await salesReturnMasterRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("SalesQuotation VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var salesReturnMaster = new SalesReturnMaster
            {
                TenantId = tenantId,
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                SalesAccountId = input.SalesAccountId,
                Description = input.Description,
                TaxAmount = input.TaxAmount,
                DebitOrCreditNote = input.DebitOrCreditNote,
                TotalTaxableAmount = input.ReturnType != ReturnType.PartyWise
                    ? input.SalesReturnDetail.Where(x => x.TaxAmount > 0).Sum(x => x.NetAmount ?? 0)
                    : 0,
                BillDiscount = input.BillDiscount,
                ValueAddedTax = input.ValueAddedTax,
                LrNo = input.LrNo,
                TransportationCompany = input.TransportationCompany,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DateMiti = input.DateMiti,
                TotalAmount = input.TotalAmount,
                GrandTotal = input.GrandTotal,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                NetAmount = input.NetAmount,
                SalesMasterId = input.SalesMasterId == Guid.Empty ? null : input.SalesMasterId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                FinancialYearId = FinancialYearId,
                PostingNumbering = postingNumbering,
                InvoiceType = salesMaster?.InvoiceType ?? input.InvoiceType,
                ReturnTaxId = input.ReturnTaxId,
                ReturnType = input.ReturnType
            };

            if (salesReturnMaster.ReturnType == ReturnType.PartyWise)
            {
                salesReturnMaster.TotalAmount = input.ReturnAmount;
                salesReturnMaster.ReturnTaxId = input.ReturnTaxId;
                salesReturnMaster.SalesMasterId = input.SalesMasterId;
                salesReturnMaster.TotalTaxableAmount = input.ReturnTaxAmount > 0 ? input.ReturnAmount : 0;
                salesReturnMaster.TaxAmount = input.ReturnTaxAmount;
                salesReturnMaster.NetAmount = input.ReturnAmount;
                salesReturnMaster.GrandTotal = input.ReturnAmount + input.ReturnTaxAmount;
            }

            var masterId = await salesReturnMasterRepository.InsertAndGetIdAsync(salesReturnMaster);

            var ledger = new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DetailId = input.LedgerId,
                Credit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference } ? input.NetAmount : 0,
                Debit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference } ? 0 : input.NetAmount,
                InvoiceNo = input.VoucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.SalesAccountId,
                TenantId = tenantId,
                PostingNumber = postingNumbering,
                VendorVoucherNo = input.DebitOrCreditNote ? "DebitNote" : "CreditNote",
                MasterId = masterId
            };
            await ledgerPostingRepository.InsertAsync(ledger);

            var ledger5 = new LedgerPosting
            {
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DetailId = input.SalesAccountId,
                Credit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference } ? 0 : input.GrandTotal,
                Debit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference } ? input.GrandTotal : 0,
                InvoiceNo = input.VoucherNo,
                FinancialYearId = FinancialYearId,
                DateMiti = input.DateMiti,
                VoucherTypeId = voucherTypeId,
                LedgerId = input.LedgerId,
                PostingNumber = postingNumbering,
                MasterId = masterId,
                VendorVoucherNo = input.DebitOrCreditNote ? "DebitNote" : "CreditNote",
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
                    DetailId = input.SalesAccountId,
                    Credit = 0,
                    Debit = input.ReturnTaxAmount,
                    InvoiceNo = input.VoucherNo,
                    FinancialYearId = FinancialYearId,
                    DateMiti = input.DateMiti,
                    VoucherTypeId = voucherTypeId,
                    LedgerId = input.ReturnTaxId,
                    PostingNumber = postingNumbering,
                    MasterId = masterId,
                    VendorVoucherNo = input.DebitOrCreditNote ? "DebitNote" : "CreditNote",
                    TenantId = tenantId
                };
                await ledgerPostingRepository.InsertAsync(ledger6);
            }

            var accountLedgerDetails =
                await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);

            await partyBalanceService.CreateSalesReturnEntryAsync(new PartyBalanceNewEntryDto
            {
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DueDate = DateConverter.ConvertToEnglish(input.DateMiti),
                LedgerId = input.LedgerId,
                VoucherNo = input.VoucherNo,
                VoucherNumbering = salesReturnMaster.VoucherNumbering,
                Amount = input.GrandTotal,
                MasterId = masterId,
                AgainstId = salesReturnMaster.SalesMasterId == null ? Guid.Empty : (Guid)salesReturnMaster.SalesMasterId
            });
            //if (accountLedgerDetails is { IsBillByBill: true })
            //{
            //    var partyBalanceData = new PartyBalance
            //    {
            //        Date = DateConverter.ConvertToEnglish(input.DateMiti),
            //        LedgerId = input.LedgerId,
            //        FinancialYearId = FinancialYearId,
            //        VoucherTypeId = againstVoucherTypeId,
            //        VoucherNo = salesMaster == null ? " " : salesMaster.VoucherNo,
            //        VoucherNumbering = voucherNumbering,
            //        AgainstVoucherTypeId = voucherTypeId,
            //        IsAgainst = true,
            //        AgainstVoucherNo = input.VoucherNo,
            //        InvoiceNo = salesMaster == null ? " " : salesMaster.VoucherNo,
            //        AgainstInvoiceNo = input.VoucherNo,
            //        ReferenceType = "Against",
            //        Debit = input is { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
            //            ? input.GrandTotal
            //            : 0,
            //        Credit = input is { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
            //            ? 0
            //            : input.GrandTotal,
            //        CreditPeriod = 0,

            //        BranchId = input.BranchId,
            //        MasterVoucherTypeId = voucherTypeId,
            //        MasterId = masterId,
            //        DetailId = Guid.Empty,
            //        MasterVoucherNo = voucherNo,
            //        TenantId = tenantId
            //    };
            //    await partyBalanceRepository.InsertAsync(partyBalanceData);
            //}

            if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                foreach (var salesReturnDetailDto in input.SalesReturnDetail)
                {
                    var detail = new SalesReturnDetail
                    {
                        Id = salesReturnDetailDto.Id == Guid.Empty ? Guid.NewGuid() : salesReturnDetailDto.Id,
                        TenantId = tenantId,
                        Qty = salesReturnDetailDto.Qty ?? 0,
                        Rate = salesReturnDetailDto.Rate ?? 0,
                        TaxAmount = salesReturnDetailDto.TaxAmount ?? 0,
                        Discount = salesReturnDetailDto.Discount ?? 0,
                        GrossAmount = salesReturnDetailDto.GrossAmount ?? 0,
                        NetAmount = salesReturnDetailDto.NetAmount ?? 0,
                        TaxValue = salesReturnDetailDto.TaxValue ?? 0,
                        SalesDetailId = salesReturnDetailDto.SalesDetailId == Guid.Empty
                            ? null
                            : salesReturnDetailDto.SalesDetailId,
                        Amount = salesReturnDetailDto.Amount ?? 0,
                        SalesReturnMasterId = masterId,
                        DiscountPer = salesReturnDetailDto.DiscountPer ?? 0,
                        ProductId = salesReturnDetailDto.ProductId,
                        UnitId = salesReturnDetailDto.UnitId,
                        TaxId = salesReturnDetailDto.TaxId
                    };
                    await salesReturnDetailRepository.InsertAsync(detail);
                    var returnDetailId = detail.Id;


                    if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                    {
                        await ApplySalesReturnStockAsync(
                            input.DateMiti,
                            input.LedgerId,
                            voucherTypeId,
                            voucherNo,
                            voucherNumbering,
                            againstVoucherTypeId,
                            salesMaster?.VoucherNo ?? "",
                            masterId,
                            tenantId,
                            detail,
                            input.DebitOrCreditNote ? "DebitNote" : "CreditNote");
                    }

                    if (input.ReturnType == ReturnType.RateDifference)
                    {
                        var stockPosting9 = new StockPosting
                        {
                            VoucherNumbering = voucherNumbering,
                            VoucherNo = voucherNo,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DateMiti = input.DateMiti,
                            LedgerId = input.LedgerId,
                            VoucherTypeId = voucherTypeId,
                            ProductId = detail.ProductId,
                            UnitId = detail.UnitId,
                            GrossAmount = detail.GrossAmount,
                            DiscountAmount = detail.Discount,
                            NetAmount = detail.NetAmount,
                            Amount = detail.Amount,
                            TaxAmount = detail.TaxAmount,
                            AgainstVoucherTypeId = againstVoucherTypeId,
                            AgainstVoucherNo = salesMaster?.VoucherNo,
                            InWardQty = 0,
                            OutWardQty = 0,
                            Rate = detail.NetAmount / detail.Qty,
                            FinancialYearId = FinancialYearId,
                            MasterId = masterId,
                            VendorVoucherNo = input.DebitOrCreditNote ? "DebitNote" : "CreditNote",
                            TenantId = tenantId
                        };
                        var stockId9 = await stockPostingRepository.InsertAndGetIdAsync(stockPosting9);
                    }

                    if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                        if (detail.TaxId != Guid.Empty && detail.TaxAmount != 0)
                        {
                            var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                            var ledger3 = new LedgerPosting
                            {
                                VoucherNumbering = voucherNumbering,
                                VoucherNo = voucherNo,
                                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                DetailId = input.SalesAccountId,
                                Credit = input is { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                    ? detail.TaxAmount
                                    : 0,
                                Debit = input is { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                    ? 0
                                    : detail.TaxAmount,
                                InvoiceNo = input.VoucherNo,
                                FinancialYearId = FinancialYearId,
                                DateMiti = input.DateMiti,
                                VoucherTypeId = voucherTypeId,
                                LedgerId = tax.LedgerId,
                                TenantId = tenantId,
                                PostingNumber = postingNumbering,
                                VendorVoucherNo = input.DebitOrCreditNote ? "DebitNote" : "CreditNote",
                                MasterId = masterId
                            };
                            await ledgerPostingRepository.InsertAsync(ledger3);
                        }
                }

            //if (AbpSession.TenantId != null &&
            //    await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.IsCBMS,
            //        (int)AbpSession.TenantId) == "True")
            if (false)
            {
                var username = await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.CBMSUsername,
                    (int)AbpSession.TenantId);
                var password = await SettingManager.GetSettingValueForTenantAsync(AppSettings.ErpSettings.CBMSPassword,
                    (int)AbpSession.TenantId);
                var cbms = new BillReturnViewModelDto
                {
                    Username = username,
                    Password = password,
                    SellerPan = (await branchRepository.FirstOrDefaultAsync(x => x.IsMain)).PANumber,
                    BuyerPan = accountLedgerDetails.Pan,
                    FiscalYear = FinancialYear.Name,
                    BuyerName = accountLedgerDetails.Name,
                    RefInvoiceNumber = salesMaster?.VoucherNo,
                    CreditNoteNumber = input.VoucherNo,
                    CreditNoteDate = input.DateMiti,
                    ReasonForReturn = input.Description,
                    TotalSales = input.GrandTotal,
                    TaxableSalesVat = input.TaxableAmount,
                    Vat = input.TaxAmount,
                    ExcisableAmount = 0,
                    Excise = 0,
                    TaxableSalesHst = 0,
                    Hst = 0,
                    AmountForEsf = 0,
                    Esf = 0,
                    ExportSales = 0,
                    TaxExemptedSales = 0,
                    Isrealtime = true,
                    DatetimeClient = DateTime.Now
                };
                var res = await PostCbms(cbms);

                //var salesReturnData = await _salesReturnMasterRepository.FirstOrDefaultAsync(x => x.Id == masterId);
                //salesReturnData.SyncwithIrd = res;
                //await _salesReturnMasterRepository.UpdateAsync(salesReturnData);
            }

            await unitOfWork.CompleteAsync();
            return masterId;
        }

        public async Task<bool> PostCbms(BillReturnViewModelDto p)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri("https://cbapi.ird.gov.np");
                var response = client.PostAsJsonAsync("api/billreturn", p).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseCode = response.Content.ReadAsStringAsync();
                    if (responseCode.Result == "200")
                        return true;
                    return false;
                }

                return false;
            }
        }

        public async Task<List<UniversalDropdownDto>> GetSalesInvoiceIdByLedgerId(Guid ledgerId)
        {
            var list = new List<UniversalDropdownDto>();

            var returnedProducts = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesReturnMasterFk)
                .Where(x => x.SalesReturnMasterFk.LedgerId == ledgerId)
                .Select(x => new
                {
                    x.ProductId,
                    x.Qty,
                    x.SalesReturnMasterFk.SalesMasterId
                }).AsNoTracking().ToListAsync();

            var salesMaster = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.SalesMasterFk.LedgerId == ledgerId && !x.SalesMasterFk.IsDelete)
                .Select(x => new
                {
                    x.ProductId,
                    x.Qty,
                    x.SalesMasterFk.VoucherNo,
                    SalesMasterId = x.SalesMasterFk.Id
                }).AsNoTracking().ToListAsync();

            foreach (var objSalesMasterId in salesMaster.Select(x => x.SalesMasterId).Distinct())
                foreach (var objSalesProduct in salesMaster.Where(x => x.SalesMasterId == objSalesMasterId))
                {
                    var objProduct = returnedProducts.Where(x =>
                        x.SalesMasterId == objSalesMasterId && x.ProductId == objSalesProduct.ProductId).ToList();
                    if (objSalesProduct.Qty <= objProduct.Sum(x => x.Qty)) continue;
                    {
                        if (list.Count(x => x.Id == objSalesMasterId) == 0)
                            list.Add(new UniversalDropdownDto
                            {
                                Id = objSalesProduct.SalesMasterId,
                                DisplayName = objSalesProduct.VoucherNo
                            });
                    }
                }

            return list;
        }

        public async Task<List<SalesDetailDto>> GetSalesDetailsByMasterId(Guid masterId)
        {
            var returnedProducts = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.SalesReturnMasterFk).Where(x => x.SalesReturnMasterFk.SalesMasterId == masterId)
                .Select(x => new
                {
                    x.ProductId,
                    x.SalesReturnMasterFk.VoucherNo,
                    x.SalesReturnMasterFk.VoucherTypeId,
                    x.Qty,
                    x.Discount,
                    x.GrossAmount,
                    x.NetAmount,
                    x.Amount,
                    x.TaxAmount,
                    x.UnitId,
                    x.SalesReturnMasterFk.SalesMasterId
                }).AsNoTracking().ToListAsync();

            var salesDetails = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.ProductFk).Include(x => x.SalesMasterFk)
                .Where(x => x.SalesMasterId == masterId && !x.SalesMasterFk.IsDelete)
                .Select(x => new
                {
                    x.ProductId,
                    x.Qty,
                    x.Rate,
                    x.TaxId,
                    x.UnitId,
                    x.Discount,
                    x.DiscountPer,
                    x.Id,
                    x.GrossAmount,
                    x.NetAmount,
                    x.Amount,
                    x.TaxAmount,
                    x.ProductFk.ProductCode,
                    x.SalesMasterFk.VoucherTypeId,
                    TaxRate = x.TaxAmount / x.Qty,
                    x.SalesMasterFk.VoucherNo,
                    x.SalesMasterId
                }).AsNoTracking().ToListAsync();

            var result = new List<SalesDetailDto>();
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
            foreach (var objSalesProduct in salesDetails)
            {
                var returnProduct = returnedProducts.Where(x =>
                    x.SalesMasterId == objSalesProduct.SalesMasterId && x.ProductId == objSalesProduct.ProductId).ToList();

                var det = returnProduct.Select(x => new UnitConversionParamDetails
                {
                    UnitId = x.UnitId,
                    Qty = x.Qty
                }).ToList();

                var returns = new UnitConversionParamDto
                {
                    ProductId = objSalesProduct.ProductId,
                    UnitId = objSalesProduct.UnitId,
                    Rate = objSalesProduct.Rate,
                    Qty = objSalesProduct.Qty,
                    Details = det
                };
                var netSales = await ERPCommonManager.UnitConversionMinus(returns);

           //     var getUnitList = await GetAllUnitQtyForTableDropdown(objSalesProduct.ProductId, objSalesProduct.Id);

                if (netSales.Qty > 0)
                {

                    var a = new SalesDetailDto
                    {
                        Id = objSalesProduct.SalesMasterId,
                        Qty = netSales.Qty,
                        Rate = netSales.Rate,
                        TaxAmount = objSalesProduct.TaxAmount - returnProduct.Sum(x => x.TaxAmount),
                        Discount = objSalesProduct.Discount - returnProduct.Sum(x => x.Discount),
                        GrossAmount = objSalesProduct.GrossAmount - returnProduct.Sum(x => x.GrossAmount),
                        NetAmount = objSalesProduct.NetAmount - returnProduct.Sum(x => x.NetAmount),
                        Amount = objSalesProduct.Amount - returnProduct.Sum(x => x.Amount),
                        ProductCode = objSalesProduct.ProductCode,
                        DiscountPer = objSalesProduct.DiscountPer,
                        SalesDetailId = objSalesProduct.Id,
                        ProductId = objSalesProduct.ProductId,
                        UnitId = netSales.UnitId,
                        TaxId = objSalesProduct.TaxId
                    };
                    result.Add(a);
                }
            }

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesSalesReturnMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditSalesReturnMasterDto input)
        {
            using (var unitOfWork = unitOfWorkManager.Begin())
            {
                var tenantId = AbpSession.TenantId;
                if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;

                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
                var salesReturnMaster = await salesReturnMasterRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
                if (salesReturnMaster != null)
                {
                    var previousReturnType = salesReturnMaster.ReturnType;
                    var previousDetails = await salesReturnDetailRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.SalesReturnMasterId == input.Id)
                        .AsNoTracking()
                        .ToListAsync();
                    var beforeTotalTaxAmt = salesReturnMaster.TotalTaxableAmount;
                    if (await GetVoucherGenerateType() == "Manually")
                    {
                        if (await salesReturnMasterRepository.CountAsync(x =>
                                x.Id != input.Id && x.FinancialYearId == FinancialYearId &&
                                x.VoucherNo == input.VoucherNo) >
                            0) throw new UserFriendlyException("SalesReturn VoucherNo is Duplicate");
                        salesReturnMaster.VoucherNo = input.VoucherNo;
                    }

                    if (await GetVoucherGenerateType() == "Duplicate")
                        salesReturnMaster.VoucherNo = input.VoucherNo;
                    var postingNumbering = salesReturnMaster.PostingNumbering;
                    salesReturnMaster.SalesAccountId = input.SalesAccountId;
                    salesReturnMaster.Description = input.Description;
                    salesReturnMaster.TaxAmount = input.TaxAmount;
                    salesReturnMaster.TotalTaxableAmount = input.SalesReturnDetail.Where(x => x.TaxAmount > 0)
                        .Sum(x => x.NetAmount ?? 0);
                    salesReturnMaster.BillDiscount = input.BillDiscount;
                    salesReturnMaster.ValueAddedTax = input.ValueAddedTax;
                    salesReturnMaster.LrNo = input.LrNo;
                    salesReturnMaster.TransportationCompany = input.TransportationCompany;
                    salesReturnMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
                    salesReturnMaster.DateMiti = input.DateMiti;
                    salesReturnMaster.TotalAmount = input.TotalAmount;
                    salesReturnMaster.BillDiscount = input.BillDiscount;
                    salesReturnMaster.NetAmount = input.NetAmount;
                    salesReturnMaster.GrandTotal = input.GrandTotal;
                    salesReturnMaster.ReturnType = input.ReturnType;
                    salesReturnMaster.LedgerId = input.LedgerId;
                    salesReturnMaster.DebitOrCreditNote = input.DebitOrCreditNote;
                    salesReturnMaster.ReturnTaxId = input.ReturnTaxId;
                    salesReturnMaster.UpdateUserId = AbpSession.UserId;
                    if (salesReturnMaster.ReturnType == ReturnType.PartyWise)
                    {
                        salesReturnMaster.TotalAmount = input.ReturnAmount;
                        salesReturnMaster.ReturnTaxId = input.ReturnTaxId;
                        salesReturnMaster.SalesMasterId = input.SalesMasterId;
                        salesReturnMaster.TotalTaxableAmount = input.ReturnTaxAmount > 0 ? input.ReturnAmount : 0;
                        salesReturnMaster.TaxAmount = input.ReturnTaxAmount;
                        salesReturnMaster.NetAmount = input.ReturnAmount;
                        salesReturnMaster.GrandTotal = input.ReturnAmount + input.ReturnTaxAmount;
                    }

                    await salesReturnMasterRepository.UpdateAsync(salesReturnMaster);

                    await ledgerPostingRepository.DeleteAsync(x => x.VoucherNo == input.VoucherNo &&
                                                                   x.FinancialYearId == FinancialYearId &&
                                                                   x.VoucherTypeId == voucherTypeId);

                    if (previousReturnType is ReturnType.ProductWise or ReturnType.NA)
                    {
                        foreach (var previousDetail in previousDetails)
                            await ReverseSalesReturnStockAsync(salesReturnMaster, previousDetail);
                    }

                    await stockPostingRepository.DeleteAsync(x => x.VoucherNo == input.VoucherNo &&
                                                                  x.VoucherTypeId == voucherTypeId &&
                                                                  x.FinancialYearId == FinancialYearId);

                    var accountLedgerDetails =
                        await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == input.LedgerId);

                    var againstVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");
                    var againstVoucherNo =
                        await salesMasterRepository.FirstOrDefaultAsync(x => x.Id == input.SalesMasterId);

                    var ledger = new LedgerPosting
                    {
                        VoucherNumbering = salesReturnMaster.VoucherNumbering,
                        VoucherNo = input.VoucherNo,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DetailId = input.LedgerId,
                        Credit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? input.TotalAmount
                            : 0,
                        Debit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? 0
                            : input.TotalAmount,
                        InvoiceNo = input.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = input.SalesAccountId,
                        TenantId = tenantId,
                        PostingNumber = postingNumbering,
                        VendorVoucherNo = "",
                        MasterId = input.Id
                    };
                    await ledgerPostingRepository.InsertAsync(ledger);

                    var ledger5 = new LedgerPosting
                    {
                        VoucherNumbering = salesReturnMaster.VoucherNumbering,
                        VoucherNo = salesReturnMaster.VoucherNo,
                        Date = DateConverter.ConvertToEnglish(input.DateMiti),
                        DetailId = input.SalesAccountId,
                        Credit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? 0
                            : input.GrandTotal,
                        Debit = input is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? input.GrandTotal
                            : 0,
                        InvoiceNo = input.VoucherNo,
                        FinancialYearId = FinancialYearId,
                        DateMiti = input.DateMiti,
                        VoucherTypeId = voucherTypeId,
                        LedgerId = input.LedgerId,
                        PostingNumber = postingNumbering,
                        MasterId = input.Id,
                        VendorVoucherNo = null,
                        TenantId = tenantId
                    };
                    await ledgerPostingRepository.InsertAsync(ledger5);

                    if (input.ReturnType is ReturnType.PartyWise or ReturnType.NA)
                    {
                        var ledger6 = new LedgerPosting
                        {
                            VoucherNumbering = salesReturnMaster.VoucherNumbering,
                            VoucherNo = input.VoucherNo,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            DetailId = input.SalesAccountId,
                            Credit = 0,
                            Debit = input.ReturnTaxAmount,
                            InvoiceNo = input.VoucherNo,
                            FinancialYearId = FinancialYearId,
                            DateMiti = input.DateMiti,
                            VoucherTypeId = voucherTypeId,
                            LedgerId = input.ReturnTaxId,
                            PostingNumber = postingNumbering,
                            MasterId = input.Id,
                            VendorVoucherNo = "",
                            TenantId = tenantId
                        };
                        await ledgerPostingRepository.InsertAsync(ledger6);
                    }

                    if (accountLedgerDetails is { IsBillByBill: true })
                    {
                        await partyBalanceService.UpdateSalesReturnEntryAsync(new PartyBalanceNewEntryDto
                        {
                            Amount = input.GrandTotal,
                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                            LedgerId = input.LedgerId,
                            DueDate = DateConverter.ConvertToEnglish((input.DateMiti)),
                            MasterId = salesReturnMaster.Id,
                            AgainstId = (salesReturnMaster.SalesMasterId == null || salesReturnMaster.SalesMasterId == Guid.Empty) ? Guid.Empty : (Guid)salesReturnMaster.SalesMasterId,
                            VoucherNo = input.VoucherNo,
                            VoucherNumbering = salesReturnMaster.VoucherNumbering
                        }
                        );
                    }

                    var detailsIds = input.SalesReturnDetail.Select(x => x.Id).ToList();
                    var detailsDataBaseIds = await salesReturnDetailRepository.GetAll()
                        .Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.SalesReturnMasterId == input.Id).Select(x => x.Id).ToListAsync();

                    foreach (var detailsDataBaseId in detailsDataBaseIds)
                        if (!detailsIds.Contains(detailsDataBaseId))
                            await salesReturnDetailRepository.DeleteAsync(detailsDataBaseId);

                    if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                        foreach (var salesReturnDetail in input.SalesReturnDetail)
                            if (salesReturnDetail.Id == Guid.Empty)
                            {
                                var detail = new SalesReturnDetail
                                {
                                    Id = Guid.NewGuid(),
                                    TenantId = tenantId,
                                    Qty = salesReturnDetail.Qty ?? 0,
                                    Rate = salesReturnDetail.Rate ?? 0,
                                    TaxAmount = salesReturnDetail.TaxAmount ?? 0,
                                    Discount = salesReturnDetail.Discount ?? 0,
                                    GrossAmount = salesReturnDetail.GrossAmount ?? 0,
                                    NetAmount = salesReturnDetail.NetAmount ?? 0,
                                    TaxValue = salesReturnDetail.TaxValue ?? 0,
                                    DiscountPer = salesReturnDetail.DiscountPer ?? 0,
                                    Amount = salesReturnDetail.Amount ?? 0,
                                    SalesReturnMasterId = (Guid)input.Id,
                                    ProductId = salesReturnDetail.ProductId,
                                    SalesDetailId = salesReturnDetail.SalesDetailId == Guid.Empty
                                        ? null
                                        : salesReturnDetail.SalesDetailId,
                                    UnitId = salesReturnDetail.UnitId,
                                    TaxId = salesReturnDetail.TaxId
                                };
                                await salesReturnDetailRepository.InsertAsync(detail);
                                var returnDetailId = detail.Id;
                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                                {
                                    await ApplySalesReturnStockAsync(
                                        input.DateMiti,
                                        input.LedgerId,
                                        voucherTypeId,
                                        input.VoucherNo,
                                        salesReturnMaster.VoucherNumbering,
                                        againstVoucherTypeId,
                                        " ",
                                        (Guid)input.Id,
                                        tenantId,
                                        detail,
                                        " ");

                                }

                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference
                                    or ReturnType.NA)
                                    if (detail.TaxId != Guid.Empty && detail.TaxAmount != 0)
                                    {
                                        var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == detail.TaxId);
                                        var ledger3 = new LedgerPosting
                                        {
                                            VoucherNumbering = salesReturnMaster.VoucherNumbering,
                                            VoucherNo = input.VoucherNo,
                                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                            DetailId = input.SalesAccountId,
                                            Credit = input is
                                            { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                                ? detail.TaxAmount
                                                : 0,
                                            Debit = input is
                                            { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                                ? 0
                                                : detail.TaxAmount,
                                            InvoiceNo = input.VoucherNo,
                                            FinancialYearId = FinancialYearId,
                                            DateMiti = input.DateMiti,
                                            VoucherTypeId = voucherTypeId,
                                            LedgerId = tax.LedgerId,
                                            TenantId = tenantId,
                                            PostingNumber = postingNumbering,
                                            VendorVoucherNo = "",
                                            MasterId = (Guid)input.Id
                                        };
                                        await ledgerPostingRepository.InsertAsync(ledger3);
                                    }
                            }
                            else
                            {
                                var salesReturnData =
                                    await salesReturnDetailRepository.FirstOrDefaultAsync(
                                        x => x.Id == salesReturnDetail.Id);

                                if (salesReturnData != null)
                                {
                                    salesReturnData.Qty = salesReturnDetail.Qty ?? 0;
                                    salesReturnData.Rate = salesReturnDetail.Rate ?? 0;
                                    salesReturnData.TaxAmount = salesReturnDetail.TaxAmount ?? 0;
                                    salesReturnData.Discount = salesReturnDetail.Discount ?? 0;
                                    salesReturnData.GrossAmount = salesReturnDetail.GrossAmount ?? 0;
                                    salesReturnData.NetAmount = salesReturnDetail.NetAmount ?? 0;
                                    salesReturnData.TaxValue = salesReturnDetail.TaxValue ?? 0;
                                    salesReturnData.SalesDetailId = salesReturnDetail.SalesDetailId == Guid.Empty
                                        ? null
                                        : salesReturnDetail.SalesDetailId;
                                    salesReturnData.Amount = salesReturnDetail.Amount ?? 0;
                                    salesReturnData.SalesReturnMasterId = (Guid)input.Id;
                                    salesReturnData.DiscountPer = salesReturnDetail.DiscountPer ?? 0;
                                    salesReturnData.ProductId = salesReturnDetail.ProductId;
                                    salesReturnData.UnitId = salesReturnDetail.UnitId;
                                    salesReturnData.TaxId = salesReturnDetail.TaxId;
                                    await salesReturnDetailRepository.UpdateAsync(salesReturnData);
                                }

                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.NA)
                                {
                                    var detailForStock = new SalesReturnDetail
                                    {
                                        Id = salesReturnData.Id,
                                        TenantId = tenantId,
                                        Qty = salesReturnDetail.Qty ?? 0,
                                        Rate = salesReturnDetail.Rate ?? 0,
                                        TaxAmount = salesReturnDetail.TaxAmount ?? 0,
                                        Discount = salesReturnDetail.Discount ?? 0,
                                        GrossAmount = salesReturnDetail.GrossAmount ?? 0,
                                        NetAmount = salesReturnDetail.NetAmount ?? 0,
                                        Amount = salesReturnDetail.Amount ?? 0,
                                        ProductId = salesReturnDetail.ProductId,
                                        UnitId = salesReturnDetail.UnitId,
                                        SalesDetailId = salesReturnDetail.SalesDetailId == Guid.Empty
                                            ? null
                                            : salesReturnDetail.SalesDetailId,
                                        TaxId = salesReturnDetail.TaxId
                                    };

                                    await ApplySalesReturnStockAsync(
                                        input.DateMiti,
                                        input.LedgerId,
                                        voucherTypeId,
                                        input.VoucherNo,
                                        salesReturnMaster.VoucherNumbering,
                                        againstVoucherTypeId,
                                        " ",
                                        (Guid)input.Id,
                                        tenantId,
                                        detailForStock,
                                        " ");
                                }

                                if (input.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference
                                    or ReturnType.NA)
                                    if (salesReturnDetail.TaxId != Guid.Empty && salesReturnDetail.TaxAmount != 0)
                                    {
                                        var tax = await taxRepository.FirstOrDefaultAsync(x =>
                                            x.Id == salesReturnDetail.TaxId);
                                        var ledger3 = new LedgerPosting
                                        {
                                            VoucherNumbering = salesReturnMaster.VoucherNumbering,
                                            VoucherNo = input.VoucherNo,
                                            Date = DateConverter.ConvertToEnglish(input.DateMiti),
                                            DetailId = input.SalesAccountId,
                                            Credit = input is
                                            { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                                ? (decimal)salesReturnDetail.TaxAmount
                                                : 0,
                                            Debit = input is
                                            { ReturnType: ReturnType.RateDifference, DebitOrCreditNote: true }
                                                ? 0
                                                : (decimal)salesReturnDetail.TaxAmount,
                                            InvoiceNo = input.VoucherNo,
                                            FinancialYearId = FinancialYearId,
                                            DateMiti = input.DateMiti,
                                            VoucherTypeId = voucherTypeId,
                                            LedgerId = tax.LedgerId,
                                            TenantId = tenantId,
                                            PostingNumber = postingNumbering,
                                            VendorVoucherNo = "",
                                            MasterId = (Guid)input.Id
                                        };
                                        await ledgerPostingRepository.InsertAsync(ledger3);
                                    }
                            }
                    if (againstVoucherNo != null)
                    {
                        againstVoucherNo.VatRefundAmount = againstVoucherNo.VatRefundAmount - beforeTotalTaxAmt +
                                                           salesReturnMaster.TotalTaxableAmount;
                        await salesMasterRepository.UpdateAsync(againstVoucherNo);
                    }
                }

                await unitOfWork.CompleteAsync();
                return salesReturnMaster.Id;
            }
        }


        public async Task UploadImageNew(IFormFile file, Guid salesReturnId)
        {
            var purchaseOrder = await salesReturnMasterRepository.FirstOrDefaultAsync(x => x.Id == salesReturnId);
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");

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
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    fileDetails.Image = fileBytes;
                    var s = Convert.ToBase64String(fileBytes);
                }

            fileDetails.ChangedFileName = changedFileName;
            await voucherPhotosRepository.UpdateAsync(fileDetails);
        }

        [DisableAuditing]
        public async Task<List<DocumentDetailsDto>> GetAllDocuments(Guid salesReturnId)
        {
            var result = new List<DocumentDetailsDto>();
            var purchaseOrder = await salesReturnMasterRepository.FirstOrDefaultAsync(salesReturnId);
            if (purchaseOrder == null)
                throw new UserFriendlyException("Data not found");
            result = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
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


        #region FixedSalesReturn

        public async Task FixedSalesReturn()
        {
            await FixedSalesReturnInvoiceError();
            await DeleteDuplicateSalesReturnLedgerPostings();
            await PostMissingSalesReturnLedgerData();
            await PostMissingSalesReturnStockData();
        }

        [UnitOfWork]
        private async Task FixedSalesReturnInvoiceError()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");

                // Execute everything at the database level in a single query
                var orphanedLedgerPostingIds = await ledgerPostingRepository.GetAll()
                    .Where(lp => lp.TenantId == AbpSession.GetTenantId() &&
                                 lp.FinancialYearId == FinancialYearId &&
                                 lp.VoucherTypeId == voucherTypeId)
                    .Where(lp => !salesReturnMasterRepository.GetAll()
                        .Any(srm => srm.VoucherTypeId == lp.VoucherTypeId &&
                                    srm.FinancialYearId == lp.FinancialYearId &&
                                    srm.VoucherNo == lp.VoucherNo &&
                                    srm.TenantId == lp.TenantId))
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
                Logger.Debug($"An error occurred in FixedSalesReturnInvoiceError: {ex.Message}");
            }
        }

        private async Task DeleteDuplicateSalesReturnLedgerPostings()
        {
            try
            {
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");
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
                            $"Deleted {batchIds.Count} duplicate sales return ledger postings, batch {i / batchSize + 1} of {(idsToDelete.Count + batchSize - 1) / batchSize}");
                    }

                    Logger.Info($"Total duplicate sales return ledger postings deleted: {idsToDelete.Count}");
                }
                else
                {
                    Logger.Info("No duplicate sales return ledger postings found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting duplicate sales return ledger postings: {ex.Message}", ex);
            }
        }

        [UnitOfWork]
        private async Task PostMissingSalesReturnLedgerData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing ledger postings for sales returns");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");

                // Retrieve sales returns with missing ledger postings via left join
                var unpostedSalesReturns = await (
                        from srm in salesReturnMasterRepository.GetAll().AsNoTracking()
                            // Left join on ledger postings
                        join lp in ledgerPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                srm.TenantId,
                                srm.FinancialYearId,
                                srm.VoucherTypeId,
                                srm.VoucherNo,
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
                            srm.TenantId == tenantId &&
                            srm.FinancialYearId == FinancialYearId &&
                            srm.VoucherTypeId == voucherTypeId &&
                            ledger == null // means no existing ledger posting
                        select new
                        {
                            srm.Id,
                            srm.TenantId,
                            srm.VoucherNumbering,
                            srm.DateMiti,
                            srm.Date,
                            srm.VoucherTypeId,
                            srm.VoucherNo,
                            srm.SalesAccountId,
                            srm.LedgerId,
                            srm.GrandTotal,
                            srm.NetAmount,
                            srm.TaxAmount,
                            srm.PostingNumbering,
                            srm.FinancialYearId,
                            srm.ReturnType,
                            srm.DebitOrCreditNote,
                            srm.TotalAmount,
                            srm.ReturnTaxId
                        }
                    )
                    .ToListAsync();

                if (!unpostedSalesReturns.Any())
                {
                    Logger.Info("No sales returns with missing ledger postings found");
                    return;
                }

                Logger.Info($"Found {unpostedSalesReturns.Count} sales returns with missing ledger postings");

                // Prepare ledger postings
                var ledgerPostings = new List<LedgerPosting>();

                // Process each unposted sales return
                foreach (var salesReturn in unpostedSalesReturns)
                {
                    // Sales account posting (credit for returns)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = salesReturn.TenantId,
                        VoucherNumbering = salesReturn.VoucherNumbering,
                        Date = salesReturn.Date,
                        DateMiti = salesReturn.DateMiti,
                        VoucherTypeId = salesReturn.VoucherTypeId,
                        VoucherNo = salesReturn.VoucherNo,
                        LedgerId = salesReturn.SalesAccountId,
                        DetailId = salesReturn.LedgerId,
                        // For returns: Credit sales account if normal return, debit if debit note
                        Credit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? salesReturn.TotalAmount
                            : 0,
                        Debit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? 0
                            : salesReturn.TotalAmount,
                        FinancialYearId = salesReturn.FinancialYearId,
                        InvoiceNo = salesReturn.VoucherNo,
                        PostingNumber = salesReturn.PostingNumbering,
                        MasterId = salesReturn.Id
                    });

                    // Customer/Party posting (debit for returns)
                    ledgerPostings.Add(new LedgerPosting
                    {
                        TenantId = salesReturn.TenantId,
                        VoucherNumbering = salesReturn.VoucherNumbering,
                        Date = salesReturn.Date,
                        DateMiti = salesReturn.DateMiti,
                        VoucherTypeId = salesReturn.VoucherTypeId,
                        VoucherNo = salesReturn.VoucherNo,
                        LedgerId = salesReturn.LedgerId,
                        DetailId = salesReturn.SalesAccountId,
                        // For returns: Debit customer if normal return, credit if debit note
                        Credit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? 0
                            : salesReturn.GrandTotal,
                        Debit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                            ? salesReturn.GrandTotal
                            : 0,
                        FinancialYearId = salesReturn.FinancialYearId,
                        InvoiceNo = salesReturn.VoucherNo,
                        PostingNumber = salesReturn.PostingNumbering,
                        MasterId = salesReturn.Id
                    });

                    // Tax posting if applicable (for party-wise returns or rate difference)
                    if (salesReturn.TaxAmount > 0 &&
                        (salesReturn.ReturnType == ReturnType.PartyWise ||
                         salesReturn.ReturnType == ReturnType.RateDifference))
                        if (salesReturn.ReturnTaxId != Guid.Empty)
                            ledgerPostings.Add(new LedgerPosting
                            {
                                TenantId = salesReturn.TenantId,
                                VoucherNumbering = salesReturn.VoucherNumbering,
                                Date = salesReturn.Date,
                                DateMiti = salesReturn.DateMiti,
                                VoucherTypeId = salesReturn.VoucherTypeId,
                                VoucherNo = salesReturn.VoucherNo,
                                LedgerId = salesReturn.ReturnTaxId,
                                DetailId = salesReturn.SalesAccountId,
                                Credit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                                    ? salesReturn.TaxAmount
                                    : 0,
                                Debit = salesReturn is { DebitOrCreditNote: true, ReturnType: ReturnType.RateDifference }
                                    ? 0
                                    : salesReturn.TaxAmount,
                                FinancialYearId = salesReturn.FinancialYearId,
                                InvoiceNo = salesReturn.VoucherNo,
                                PostingNumber = salesReturn.PostingNumbering,
                                MasterId = salesReturn.Id
                            });
                }

                // Bulk insert ledger postings
                if (ledgerPostings.Count > 0)
                {
                    await ledgerPostingRepository.InsertRangeAsync(ledgerPostings);
                    await UnitOfWorkManager.Current.SaveChangesAsync();
                    Logger.Info($"Successfully created {ledgerPostings.Count} ledger postings " +
                                $"for {unpostedSalesReturns.Count} sales returns");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting ledger data for sales returns: {ex.Message}", ex);
                throw;
            }
        }

        [UnitOfWork]
        private async Task PostMissingSalesReturnStockData()
        {
            try
            {
                // Use local variables to reduce repeated session calls
                var tenantId = AbpSession.GetTenantId();
                Logger.Info("Starting to process missing stock postings for sales returns");

                // Retrieve required data
                var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesReturn");

                // Find sales returns that have details but no corresponding stock postings
                var salesReturnsWithMissingStockPostings = await (
                        from srm in salesReturnMasterRepository.GetAll().AsNoTracking()
                        join srd in salesReturnDetailRepository.GetAll().AsNoTracking()
                            on srm.Id equals srd.SalesReturnMasterId
                        // Left join on stock postings for this specific sales return detail
                        join sp in stockPostingRepository.GetAll().AsNoTracking()
                            on new
                            {
                                srm.TenantId,
                                srm.FinancialYearId,
                                srm.VoucherTypeId,
                                srm.VoucherNo,
                                srd.ProductId
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
                            srm.TenantId == tenantId &&
                            srm.FinancialYearId == FinancialYearId &&
                            srm.VoucherTypeId == voucherTypeId &&
                            stock == null && // means no existing stock posting
                            srd.ProductFk.ProductType != ProductTypeEnum.Services // Only process non-service products
                        select new
                        {
                            SalesReturnMaster = srm,
                            SalesReturnDetail = srd,
                            srd.ProductFk.ProductType,
                        }
                    )
                    .ToListAsync();

                if (!salesReturnsWithMissingStockPostings.Any())
                {
                    Logger.Info("No sales returns with missing stock postings found");
                    return;
                }

                Logger.Info(
                    $"Found {salesReturnsWithMissingStockPostings.Count} sales return details with missing stock postings");

                // Group by sales return master to process efficiently
                var groupedBySalesReturn = salesReturnsWithMissingStockPostings
                    .GroupBy(x => x.SalesReturnMaster.Id)
                    .ToList();

                // Prepare collections for batch operations
                var stockPostingsToInsert = new List<StockPosting>();

                // Get sales invoice voucher type for against voucher references
                var salesInvoiceVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("SalesInvoice");

                // Process each sales return
                foreach (var salesReturnGroup in groupedBySalesReturn)
                {
                    var salesReturnMaster = salesReturnGroup.First().SalesReturnMaster;

                    // Get the original sales master if available
                    SalesMaster originalSalesMaster = null;
                    if (salesReturnMaster.SalesMasterId.HasValue)
                        originalSalesMaster = await salesMasterRepository
                            .FirstOrDefaultAsync(x => x.Id == salesReturnMaster.SalesMasterId.Value);

                    // Process each sales return detail in this return
                    foreach (var item in salesReturnGroup)
                    {
                        var salesReturnDetail = item.SalesReturnDetail;

                        // Create stock posting for return (inward movement for returns)
                        var stockPosting = new StockPosting
                        {
                            VoucherNumbering = salesReturnMaster.VoucherNumbering,
                            Date = salesReturnMaster.Date,
                            DateMiti = salesReturnMaster.DateMiti,
                            LedgerId = salesReturnMaster.LedgerId,
                            VoucherTypeId = salesReturnMaster.VoucherTypeId,
                            VoucherNo = salesReturnMaster.VoucherNo,
                            GrossAmount = salesReturnDetail.GrossAmount,
                            DiscountAmount = salesReturnDetail.Discount,
                            NetAmount = salesReturnDetail.NetAmount,
                            Amount = salesReturnDetail.Amount,
                            TaxAmount = salesReturnDetail.TaxAmount,
                            IsValueIncrease = true, // Returns increase stock value
                            ProductId = salesReturnDetail.ProductId,
                            UnitId = salesReturnDetail.UnitId,
                            AgainstVoucherTypeId = originalSalesMaster?.VoucherTypeId ?? salesInvoiceVoucherTypeId,
                            AgainstVoucherNo = originalSalesMaster?.VoucherNo ?? "",
                            InWardQty = salesReturnDetail.Qty, // Returns add to stock
                            OutWardQty = 0,
                            Rate = salesReturnDetail.Rate,
                            FinancialYearId = salesReturnMaster.FinancialYearId,
                            MasterId = salesReturnMaster.Id,
                            TenantId = tenantId
                        };

                        stockPostingsToInsert.Add(stockPosting);

                    }

                    // Update stock levels using the stock management service
                    foreach (var item in salesReturnGroup)
                    {
                        var salesReturnDetail = item.SalesReturnDetail;

                        var stockManage = new StockMaintainDto
                        {
                            DateMiti = salesReturnMaster.DateMiti,
                            ProductId = salesReturnDetail.ProductId,
                            Qty = salesReturnDetail.Qty, // Positive quantity for returns (adds to stock)
                            Rate = salesReturnDetail.Rate,
                            FinancialYearId = salesReturnMaster.FinancialYearId,
                            Type = StockMaintainTypeEnum.Inward, // Returns are inward movements
                            UnitId = salesReturnDetail.UnitId,
                        };

                        try
                        {
                            await stockManagementAppService.MaintainStock(stockManage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not update stock levels for product {salesReturnDetail.ProductId} " +
                                        $"in sales return {salesReturnMaster.VoucherNo}: {ex.Message}");
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

                Logger.Info($"Completed processing missing stock postings for {groupedBySalesReturn.Count} sales returns");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting stock data for sales returns: {ex.Message}", ex);
                throw;
            }
        }


        [AbpAuthorize(AppPermissions.PagesSalesReturnMastersPrint)]
        public async Task<byte[]> GetPdfDownload(Guid id)
        {
            var model = await GetSalesReturnForPdf(id);
            var list = new List<PdfForSalesReturnModel> { model };
            if (model.SalesType == SalesType.Sales)
            {
                if (model.ReturnType is ReturnType.ProductWise or ReturnType.RateDifference or ReturnType.NA)
                {
                    var document = new SalesReturnMasterPdf(list);
                    return document.GeneratePdf();
                }
                else
                {
                    var document = new SalesReturnPartyPdf(list);
                    return document.GeneratePdf();
                }
            }
            {
                var document = new SalesReturnPosPdf(model);
                return document.GeneratePdf();
            }

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    Process.Start("explorer.exe", filePath);
            //}
            //else
            //{
            //    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            //    Console.WriteLine($"Output PDF file is available here: {fullPath}");
            //}
        }

        public async Task<PdfForSalesPosModel> GetSalesPosForPdf(Guid id)
        {
            var serial = 1;
            var list = new List<PdfForSalesPosDetailModel>();

            var salesInvoice = await salesMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (salesInvoice != null)
            {
                var mainbranch = await branchRepository.GetAll().Where(e => e.TenantId == AbpSession.GetTenantId())
                    .FirstOrDefaultAsync(x => x.IsMain);
                var tzf = TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time");
                var dt = TimeZoneInfo.ConvertTime(DateTime.Now, tzf);
                //var branchPhone = salesInvoice.BranchFk.PhoneNo1;
                //if (!string.IsNullOrWhiteSpace(salesInvoice.BranchFk.PhoneNo2))
                //    branchPhone += $"/{salesInvoice.BranchFk.PhoneNo2}";
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
                    //  Address = salesInvoice.BranchFk.Address,
                    DateMiti = DateConverter.ConvertToNepali(salesInvoice.Date),
                    //  BranchPan = mainbranch.PanNumber,
                    //  BranchPhone = branchPhone,
                    TaxAmount = salesInvoice.TaxAmount,
                    //SalesAdditional = salesInvoice.SalesAdditionalFk != null || salesInvoice.SalesAdditionalFk?.Name != null
                    //    ? salesInvoice.SalesAdditionalFk.Name
                    //    : "",
                    TermsOfPayment = salesInvoice.PaymentMethod.ToString(),
                    PrintDate = DateConverter.ConvertToNepali(dt) + "  Time : " +
                                dt.ToString("h:mm:ss tt"),
                    OrderNo = salesInvoice.VoucherNo,
                    //LoyaltyAmount = salesInvoice.LoyaltyAmount,
                    TotalAmountInWord = CurrencyToAmount.AmountWords(salesInvoice.GrandTotal),
                    GrandTotal = salesInvoice.GrandTotal,
                    CustomerName = salesInvoice.LedgerName,
                    CustomerPan = salesInvoice.VatNo,
                    NoOfCopy = copyString,
                    IsInvoice = isInvoice,
                    //IsLoyatlyPoint = isLoyatlyPoint,
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
