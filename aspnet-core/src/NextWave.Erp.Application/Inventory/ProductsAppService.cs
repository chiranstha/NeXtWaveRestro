using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.IO.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NepDate;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Inventory.Exporting;
using NextWave.Erp.Inventory.Importing;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;


namespace NextWave.Erp.Inventory
{
    [AbpAuthorize(AppPermissions.PagesProducts)]
    [Audited]
    public class ProductAppService(
       IProductsExcelExporter productsExcelExporter,
      //   IRepository<Barcode, Guid> barcodeRepository,
      IProductNewExcelExporter productsNewExcelExporter,
     //  IRepository<StandardRate, Guid> standardRateRepository,
     IRepository<Tax, Guid> taxRepository,
     //IRepository<DeliveryNoteDetail, Guid> deliveryNoteDetailRepository,
     //IRepository<SalesQuotationDetail, Guid> salesQuotationDetailRepository,
     //IRepository<RejectionInDetail, Guid> rejectionInDetailRepository,
     //IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailRepository,
     //IRepository<SalesReturnDetail, Guid> salesReturnDetailRepository,
     //IRepository<SalesDetail, Guid> salesDetailRepository,
     IRepository<UnitConversion, Guid> unitConversionRepository,
     //IRepository<RejectionOutDetail, Guid> rejectionOutDetailRepository,
     IBinaryObjectManager binaryObjectManager,
     //   UserManager userManager,
     IRepository<UserBranch, Guid> userBranchRepository,
     IBackgroundJobManager backgroundJobManager,
     //IRepository<MaterialReceiptDetail, Guid> materialReceiptDetailRepository,
     IRepository<Bom, Guid> bomRepository,
     //IRepository<PurchaseOrderDetails, Guid> purchaseOrderDetailsRepository,
     //IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
     //IRepository<SalesOrderDetail, Guid> salesOrderDetailRepository,
     IRepository<Product, Guid> productRepository,
     IRepository<StockMaintain, Guid> stockMaintainRepository,
     IRepository<StockFifoTable, Guid> stockFifoTableRepository,
     IRepository<ProductGroup, Guid> productGroupRepository,
     //    IRepository<Size, Guid> sizeRepository,
     IRepository<Branch, Guid> branchRepository,
     IRepository<StockPosting, Guid> stockPostingRepository,
     IRepository<Unit, Guid> unitRepository,
     IUnitOfWorkManager unitOfWorkManager,
     IRepository<LedgerPosting, Guid> ledgerPostingRepository,
     IRepository<AccountLedger, Guid> accountLedgerRepository,
        StockManagementAppService stockManagementAppService
     )
     : ErpAppServiceBase, IProductsAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        private static bool UsesStockCalculation(ProductTypeEnum productType)
        {
            return productType is ProductTypeEnum.Product or ProductTypeEnum.RawMaterial;
        }

        [DisableAuditing]
        public async Task<PagedResultDto<GetProductForGetAllView>> GetAll(GetAllUniversalInput input)
        {
            // await BarcodeUpdate();
            var filteredProducts = productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.ProductGroupFk).Include(e => e.UnitFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    x => x.Name.Contains(input.Filter.Trim()));

            var pagedAndFilteredProducts = filteredProducts
                .OrderBy(input.Sorting ?? "id desc")
                .PageBy(input);

            var products = from o in pagedAndFilteredProducts
                           select new GetProductForGetAllView
                           {
                               Name = o.Name,
                               Id = o.Id,
                               ProductType = o.ProductType,
                               UnitId = o.UnitId,
                               HSCode = o.HsCode,
                               ProductGroupName = o.ProductGroupFk.Name,
                               UnitName = o.UnitFk == null || o.UnitFk.Name == null ? "" : o.UnitFk.Name
                           };
            var totalCount = await filteredProducts.CountAsync(); //await OpeingStockLedgerFixed();
            return new PagedResultDto<GetProductForGetAllView>(
                totalCount,
                await products.ToListAsync()
            );
        }


        [DisableAuditing]
        public async Task<GetProductForViewDto> GetProductForView(Guid id)
        {
            var product = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.TaxFk)
                .Include(x => x.ProductGroupFk)
                .Include(x => x.UnitFk)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (product == null) throw new UserFriendlyException("Product not found");

            var taxRate = product.TaxFk.Rate / 100;

            var output = new GetProductForViewDto
            {
                Id = product.Id,
                Name = product.Name,
                ProductGroupName = product.ProductGroupFk.Name,
                Mrp = product.Mrp,
                SalesRate = product.SalesRate,
                PurchaseRate = product.PurchaseRate,
                ProductId = product.Id,
                MinimumStock = product.MinimumStock,
                IsOpeningStock = product.IsOpeningStock,
                Description = product.Description,
                IsActive = product.IsActive,
                TaxId = product.TaxId,
                TaxRate = taxRate,
                UnitId = product.UnitId,
                UnitName = product.UnitFk.Name,
                Rate = product.PurchaseRate,
                ProductGroupId = product.ProductGroupId,
            };
            output.Unit = new UniversalDropdownDto { Id = product.UnitId, DisplayName = product.UnitFk.Name };
            return output;
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesProductsEdit)]
        public virtual async Task<GetProductForEditOutput> GetProductForEdit(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            var product = await productRepository.FirstOrDefaultAsync(input.Id);
            var getStockPosting = await stockPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.ProductId == input.Id && x.TenantId == tenantId)
                .Select(x => new { x.InWardQty, x.OutWardQty, x.UnitId }).ToListAsync();
            var output = new GetProductForEditOutput
            {
                Id = product.Id,
                ProductType = product.ProductType,
                ProductCode = product.ProductCode,
                Name = product.Name,
                Mrp = product.Mrp,
                SalesRate = product.SalesRate,
                PurchaseRate = product.PurchaseRate,
                MinimumStock = product.MinimumStock,
                HsCode = product.HsCode,
                MaximumStock = product.MaximumStock,
                IsUnitEdit = getStockPosting.Any(e => e.InWardQty > 0 || e.OutWardQty > 0),
                IsOpeningStock = product.IsOpeningStock,
                Margin = product.Margin,
                Description = product.Description,
                IsActive = product.IsActive,
                TaxId = product.TaxId,
                ProductGroupId = product.ProductGroupId,
                UnitId = product.UnitId,
            };


            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var stocks = await stockPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId &&
                                                                          x.ProductId == input.Id &&
                                                                          x.TenantId == tenantId &&
                                                                          x.VoucherTypeId == voucherTypeId &&
                                                                          x.FinancialYearId == FinancialYearId)
                .AsNoTracking().ToListAsync();
            var stockData = new List<StockPostingCreateDto>();
            foreach (var stock in stocks)
            {
                var openingStock = new StockPostingCreateDto
                {
                    Id = stock.Id,
                    Rate = stock.Rate,
                    OpeningQty = stock.InWardQty,
                    UnitId = stock.UnitId
                };

                stockData.Add(openingStock);
            }

            output.OpeningStock = stockData;
            output.IsOpeningStock = stockData.Count > 0;
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditProductDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesProductsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var product = await productRepository.FirstOrDefaultAsync(input.Id);
            if (product == null)
                throw new UserFriendlyException("Product not found");

            product.IsDeleted = true;

            var boms = await bomRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ProductId == input.Id).ToListAsync();
            foreach (var bom in boms)
            {
                bom.IsDeleted = true;
                await bomRepository.DeleteAsync(bom);
            }

            //Purchase Start
            //var purchaseOrderDetails = await purchaseOrderDetailsRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (purchaseOrderDetails.Count > 0)
            //    throw new UserFriendlyException("Purchase Order Details Reference Exist");

            //var materialReceiptDetails = await materialReceiptDetailRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (materialReceiptDetails.Count > 0)
            //    throw new UserFriendlyException("Material Receipt Reference Exist");

            //var rejectionOutDetail = await rejectionOutDetailRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId).Where(x => x.ProductId == input.Id)
            //    .ToListAsync();
            //if (rejectionOutDetail.Count > 0)
            //    throw new UserFriendlyException("RejectionOutReference Exist");

            //var purchaseDetail =
            //    await purchaseDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //        .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (purchaseDetail.Count > 0)
            //    throw new UserFriendlyException("Purchase Reference Exist");

            //var purchaseReturnDetail = await purchaseReturnDetailRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (purchaseReturnDetail.Count > 0)
            //    throw new UserFriendlyException("PurchaseReturnReference Exist");
            ////purchase End

            ////sales Start
            //var salesQuotationDetails = await salesQuotationDetailRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (salesQuotationDetails.Count > 0)
            //    throw new UserFriendlyException("SalesQuotationReference Exist");

            //var salesOrderDetails =
            //    await salesOrderDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //        .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (salesOrderDetails.Count > 0)
            //    throw new UserFriendlyException("SalesOrderDetails Reference Exist");

            //var deliverNoteDetails = await deliveryNoteDetailRepository.GetAll()
            //    .Where(x => x.TenantId == AbpSession.TenantId).Where(x => x.ProductId == input.Id)
            //    .ToListAsync();
            //if (deliverNoteDetails.Count > 0)
            //    throw new UserFriendlyException("DeliveryNoteReference Exist");

            //var rejectionIn = await rejectionInDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id)
            //    .ToListAsync();
            //if (rejectionIn.Count > 0)
            //    throw new UserFriendlyException("Rejection In Reference Exist");


            //var salesInvoice = await salesDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id).ToListAsync();
            //if (salesInvoice.Count > 0)
            //    throw new UserFriendlyException("Sales Reference Exist");

            //var salesReturn = await salesReturnDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.ProductId == input.Id)
            //    .ToListAsync();
            //if (salesReturn.Count > 0)
            //    throw new UserFriendlyException("SalesReturn reference Exists");
            //sales End

            var stocks = await stockPostingRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ProductId == input.Id).ToListAsync();

            var voucherId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var existNonStockOpening = stocks.Where(e => e.VoucherTypeId != voucherId)
                .Select(x => new { x.VoucherTypeId, x.VoucherTypeFk.Name }).ToList();

            if (existNonStockOpening.Count > 0)
            {
                var uniqueStockOpening = existNonStockOpening.DistinctBy(e => new { e.VoucherTypeId, e.Name }).ToList();
                var errorMsg = string.Join(',', uniqueStockOpening.Select(e => e.Name));
                throw new UserFriendlyException("existing Posting ", errorMsg);
            }


            foreach (var stock in stocks) await stockPostingRepository.DeleteAsync(stock);



            var unitConversions =
                await unitConversionRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.ProductId == input.Id).ToListAsync();
            foreach (var objConversion in unitConversions) await unitConversionRepository.DeleteAsync(objConversion);

            await stockFifoTableRepository.DeleteAsync(x => x.ProductId == input.Id);
            await stockMaintainRepository.DeleteAsync(x => x.ProductId == input.Id);
            await productRepository.DeleteAsync(product);
        }

        public async Task<FileDto> GetProductsToExcel(GetAllUniversalInput input)
        {
            var filteredProducts = productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.ProductGroupFk).Include(e => e.UnitFk);
            var query = from o in filteredProducts
                        join o5 in productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on
                            o.ProductGroupId equals o5.Id into j5
                        from s5 in j5.DefaultIfEmpty()
                        join o7 in unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on o.UnitId
                            equals o7.Id into j7
                        from s7 in j7.DefaultIfEmpty()
                        select new GetProductForViewDto
                        {
                            Mrp = o.Mrp,
                            SalesRate = o.SalesRate,
                            Name = o.Name,
                            PurchaseRate = o.PurchaseRate,
                            MinimumStock = o.MinimumStock,
                            IsOpeningStock = o.IsOpeningStock,
                            Description = o.Description,
                            IsActive = o.IsActive,
                            Id = o.Id,
                            ProductGroupName = s5 == null || s5.Name == null ? "" : s5.Name,
                            UnitName = s7 == null || s7.Name == null ? "" : s7.Name
                        };

            var productListDtos = await query.ToListAsync();

            return productsExcelExporter.ExportToFile(productListDtos);
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesProducts)]
        public async Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown()
        {
            var result = new List<UniversalDropdownDto>();

            result.AddRange(await userBranchRepository.GetAll()
                .Include(x => x.BranchFk)
                .Where(x => x.UserId == AbpSession.GetUserId())
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.BranchId,
                    DisplayName = x.BranchFk.Name
                }).ToListAsync());

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllProductForTableDropdown()
        {
            return await productRepository.GetAll().AsNoTracking().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllRawMaterialForTableDropdown()
        {
            return await productRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.ProductType == ProductTypeEnum.RawMaterial &&
                            x.IsActive &&
                            !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).ToListAsync();
        }



        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllUnitForTableDropdown()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(unit => new UniversalDropdownDto
                {
                    Id = unit.Id,
                    DisplayName = unit.Name == null ? "" : unit.Name.ToString()
                }).ToListAsync();
        }

        protected async Task OpeningStockLedgerFixed()
        {
            var products = await productRepository.GetAllListAsync();
            var financialYear = ERPCommonManager.GetFinancialYear(FinancialYearId);
            var stockposting = await stockPostingRepository.GetAllListAsync();
            var openingStockLedger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Name == "OpeningStock");
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            foreach (var item in products)
            {
                var itemstockposting =
                    stockposting.Where(x => x.ProductId == item.Id && x.FinancialYearId == FinancialYearId);
                await ledgerPostingRepository.DeleteAsync(x => x.VoucherNo == item.Id.ToString() &&
                                                               x.VoucherTypeId == voucherTypeId &&
                                                               x.FinancialYearId == FinancialYearId);
                foreach (var unititem in itemstockposting)
                {
                    var ledgerPosting = new LedgerPosting
                    {
                        Id = Guid.Empty,
                        VoucherNumbering = 0,
                        TenantId = AbpSession.TenantId,
                        Date = financialYear.FromDate,
                        DateMiti = financialYear.FromMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = "",
                        LedgerId = openingStockLedger.Id,
                        DetailId = Guid.Empty,
                        MasterId = item.Id,
                        Debit = unititem.Amount,
                        FinancialYearId = FinancialYearId,
                        Credit = 0,
                        InvoiceNo = "",
                        PostingNumber = 0
                    };
                    await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }
            }
        }

        protected async Task OpeningStockFixed()
        {
            var products = await productRepository.GetAllListAsync();
            var stockPosting = await stockPostingRepository.GetAllListAsync();
            var allunits = await unitConversionRepository.GetAllListAsync();
            foreach (var item in products)
            {
                var itemstockposting = stockPosting.Where(x => x.ProductId == item.Id).Select(x => x.UnitId).Distinct();
                foreach (var unititem in itemstockposting)
                    if (allunits.Count(x => x.ProductId == item.Id && x.UnitId == unititem) == 0)
                        await unitConversionRepository.InsertAsync(
                            new UnitConversion
                            {
                                ProductId = item.Id,
                                ConversionRate = 1,
                                PrimaryQty = 1,
                                Qty = 1,
                                IsDeleted = false,
                                UnitId = unititem,
                                TenantId = AbpSession.TenantId
                            });
            }
        }


        protected async Task StockUnitFixed()
        {
            var products = await productRepository.GetAllListAsync();
            var stockposting = await stockPostingRepository.GetAllListAsync();
            var allunits = await unitConversionRepository.GetAllListAsync();
            foreach (var item in products)
            {
                var itemstockposting = stockposting.Where(x => x.ProductId == item.Id).Select(x => x.UnitId).Distinct();
                foreach (var unititem in itemstockposting)
                    if (allunits.Count(x => x.ProductId == item.Id && x.UnitId == unititem) == 0)
                        await unitConversionRepository.InsertAsync(
                            new UnitConversion
                            {
                                ProductId = item.Id,
                                ConversionRate = 1,
                                PrimaryQty = 1,
                                Qty = 1,
                                IsDeleted = false,
                                UnitId = unititem,
                                TenantId = AbpSession.TenantId
                            }
                        );
            }
        }

        public async Task ImportProductFromExcel(IFormFile file)
        {
            if (file == null) throw new UserFriendlyException(L("File_Empty_Error"));

            if (file.Length > 1048576 * 100) //100 MB
                throw new UserFriendlyException(L("File_SizeLimit_Error"));

            byte[] fileBytes;
            await using (var stream = file.OpenReadStream())
            {
                fileBytes = stream.GetAllBytes();
            }

            var tenantId = AbpSession.TenantId;
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.Now} import from excel file.");

            await BinaryObjectManager.SaveAsync(fileObject);

            await BackgroundJobManager.EnqueueAsync<ImportProductsToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                });
        }


        //public async Task<GetProductForViewNewDto> GetProductForViewNew(Guid id)
        //{
        //    var product = (await productRepository.GetAll().Where(x => x.Id == id)
        //        .Include(x => x.UnitFk).Include(x => x.ProductGroupFk).Include(x => x.TaxFk)
        //        .ToListAsync()).FirstOrDefault();

        //    if (product == null)
        //        throw new UserFriendlyException($"Product of id {id} not found.");

        //    var multipleUnits = await unitConversionRepository.GetAll()
        //        .Where(x => x.ProductId == id && !x.IsDeleted && x.ConversionRate != 1).Include(x => x.UnitFk)
        //        .Select(x => new MultipleUnitProduct
        //        {
        //            UnitName = x.UnitFk.Name,
        //            Qty = x.Qty,
        //            PrimaryUnitName = product.UnitFk.Name,
        //            PrimaryQty = x.PrimaryQty
        //        }).ToListAsync();

        //    var openingStockVoucherType = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
        //    var openingStock = await stockPostingRepository.GetAll()
        //        .Where(x => x.VoucherTypeId == openingStockVoucherType && x.ProductId == id)
        //        .Include(x => x.BranchFk).Include(x => x.UnitFk)
        //        .Select(x => new OpeningStockProduct
        //        {
        //            BranchId = x.BranchId,
        //            BranchName = x.BranchFk.Name,
        //            Qty = x.InWardQty,
        //            UnitId = x.UnitId,
        //            UnitName = x.UnitFk.Name,
        //            Rate = x.Rate
        //        }).ToListAsync();

        //    var boms = new List<BomProduct>();

        //    if (product.IsBom)
        //        boms = await bomRepository.GetAll().Where(x => x.ProductId == id && !x.IsDeleted)
        //            .Include(x => x.UnitFk).Select(x => new BomProduct
        //            {
        //                ProductId = x.RawMaterialId,
        //                ProductName = "",
        //                ProductQty = x.Quantity,
        //                UnitId = x.UnitId,
        //                UnitName = x.UnitFk.Name
        //            }).ToListAsync();

        //    var result = new GetProductForViewNewDto
        //    {
        //        ProductId = product.Id,
        //        ProductCode = product.ProductCode,
        //        Name = product.Name,
        //        UnitId = product.UnitId,
        //        UnitName = product.UnitFk.Name,
        //        ProductType = product.ProductType.ToString(),
        //        ProductGroupId = product.ProductGroupId,
        //        ProductGroupName = product.ProductGroupFk.Name,
        //        TaxId = product.TaxId,
        //        TaxName = product.TaxFk.Name,
        //        BrandId = product.BrandId,
        //        BrandName = product.BrandFk.Name,
        //        ModelNoId = product.ModelNoId,
        //        ModelNoName = product.ModelNoFk.Name,
        //        IsAllowSerialNo = product.IsAllowSerialNo,
        //        WarrantyPeriod = product.WarrantyPeriod,
        //        GuaranteePeriod = product.GuaranteePeriod,
        //        MinimumStock = product.MinimumStock,
        //        IsMultipleUnit = product.IsMultipleUnit,
        //        IsBom = product.IsBom,
        //        IsOpeningStock = product.IsOpeningStock,
        //        IsShowRemember = product.IsShowRemember,
        //        IsAllowBatch = product.IsAllowBatch,
        //        IsActive = product.IsActive,
        //        Mrp = product.Mrp,
        //        SalesRate = product.SalesRate,
        //        PurchaseRate = product.PurchaseRate,
        //        Units = multipleUnits,
        //        BomList = boms,
        //        OpeningStock = openingStock
        //    };

        //    return result;
        //}

        [DisableAuditing]
        public async Task<GetUnitsOfEdit> GetUnitsOfEdit(Guid productId)
        {
            var result = new GetUnitsOfEdit
            {
                UnitId = (await productRepository.FirstOrDefaultAsync(productId)).UnitId
            };
            result.UnitConversion = (await unitConversionRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.ProductId == productId).AsNoTracking().ToListAsync())
                .Select(x => new ProductUnitConversionDto
                {
                    Id = x.Id,
                    InitializedQty = x.Qty,
                    InitializeUnitId = x.UnitId,
                    FinalQty = x.PrimaryQty,
                    UnitId = result.UnitId
                }).ToList();

            return result;
        }

        public async Task<FileDto> GetProductExport()
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var products = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.UnitFk).Include(x => x.ProductGroupFk)
                .Select(x => new ImportProductDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProductGroupName = x.ProductGroupFk.Name,
                    UnitName = x.UnitFk.Name,
                    PurchaseRate = x.PurchaseRate,
                    SalesRate = x.SalesRate,
                    Mrp = x.Mrp,
                    IsOpeningStock = x.IsOpeningStock,
                    HsCode = x.HsCode,
                    IsTaxable = x.TaxFk.Name == "NA" ? 0 : 1,
                    OpeningQty = 0,
                    ProductCode = x.ProductCode
                }).ToListAsync();

            foreach (var product in products.Where(x => x.IsOpeningStock))
            {
                var opening = (await stockPostingRepository.GetAll().Where(x =>
                        x.ProductId == product.Id && x.VoucherTypeId == voucherTypeId &&
                        x.FinancialYearId == FinancialYearId)
                    .Select(x => new
                    {
                        x.InWardQty,
                    }).ToListAsync()).FirstOrDefault();
                if (opening != null)
                {
                    product.OpeningQty = opening.InWardQty;
                }
            }

            return productsNewExcelExporter.ExportToFile(products);
        }
             

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllOpeningProductForTableDropdown()
        {
            //var stockPostings = await stockPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Where(x => x.VoucherTypeFk.Name == "OpeningStock" && x.BranchId == branchId).Select(x => x.ProductId)
            //    .ToListAsync();
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => (x.ProductType == ProductTypeEnum.Product || x.ProductType == ProductTypeEnum.RawMaterial) && !x.IsOpeningStock)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).ToListAsync();
        }


        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllProductGroupsForTableDropdown()
        {
            return await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UnitsByProductIdDto>> GetAllUnitByProductForTableDropdown(Guid productId)
        {
            var product = (await productRepository.GetAll().Where(x => x.Id == productId).Include(x => x.UnitFk).ToListAsync()).FirstOrDefault();
            var units = new List<UnitsByProductIdDto> { new UnitsByProductIdDto { Id = product.UnitId, DisplayName = product.UnitFk.Name, Rate = product.PurchaseRate } };
            return units;
            //return await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
            //    .Include(x => x.UnitFk)
            //    .Where(x => x.ProductId == productId)
            //    .Select(unit => new UnitsByProductIdDto
            //    {
            //        Id = unit.UnitId,
            //        DisplayName = unit.UnitFk.Name == null ? "" : unit.UnitFk.Name.ToString(),
            //        Rate = unit.ConversionRate * product.PurchaseRate
            //    }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<ProductTaxDto>> GetAllTaxForTableDropdown()
        {
            return await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(tax => new ProductTaxDto
                {
                    TaxId = tax.Id,
                    TaxName = tax.Name,
                    TaxRate = tax.Rate
                }).OrderByDescending(x => x.TaxRate).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        protected virtual async Task<List<UniversalDropdownDto>> GetProductUnitForTableDropdown(Guid productId)
        {
            return await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.UnitFk).Where(x => x.ProductId == productId).AsNoTracking()
                .Select(unit => new UniversalDropdownDto
                {
                    Id = unit.Id,
                    DisplayName = unit.UnitFk.Name == null ? "" : unit.UnitFk.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetUnitByProduct(Guid productId)
        {
            var units = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.UnitFk)
                .Where(x => x.ProductId == productId).ToListAsync();

            var unitlist = new List<UniversalDropdownDto>();
            foreach (var objUnit in units)
            {
                var data = new UniversalDropdownDto
                {
                    Id = objUnit.UnitId,
                    DisplayName = objUnit.UnitFk.Name
                };
                unitlist.Add(data);
            }

            return unitlist;
        }

        [Audited]
        [AbpAuthorize(AppPermissions.PagesProductsCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditProductDto input)
        {
            var tenantId = AbpSession.GetTenantId();

            //  int voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var financialYear = ERPCommonManager.GetFinancialYear(FinancialYearId);
            if (await productRepository.CountAsync(x =>
                    x.Name == input.Name && !x.IsDeleted && x.TenantId == tenantId) > 0)
                throw new UserFriendlyException("The product with this name already exist.");

            using var unitOfWork = unitOfWorkManager.Begin();
            var usesStockCalculation = UsesStockCalculation(input.ProductType);
            var openingStockItems = usesStockCalculation
                ? input.OpeningStock ?? new List<StockPostingCreateDto>()
                : new List<StockPostingCreateDto>();
            var isOpeningStock = usesStockCalculation && input.IsOpeningStock;
            var product = new Product
            {
                TenantId = tenantId,
                ProductCode = input.ProductCode,
                DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                ProductType = input.ProductType,
                Name = input.Name,
                Mrp = input.Mrp,
                SalesRate = input.SalesRate ?? 0,
                PurchaseRate = input.PurchaseRate ?? 0,
                MinimumStock = usesStockCalculation ? input.MinimumStock ?? 0 : 0,
                MaximumStock = usesStockCalculation ? input.MaximumStock ?? 0 : 0,
                HsCode = input.HsCode,
                Margin = input.Margin,
                IsOpeningStock = isOpeningStock,
                Description = input.Description,
                IsActive = input.IsActive,
                TaxId = input.TaxId,
                ProductGroupId = input.ProductGroupId,
                UnitId = input.UnitId
            };
            var productId = await productRepository.InsertAndGetIdAsync(product);

            if (isOpeningStock)
            {
                foreach (var stocks in openingStockItems)
                {
                    var stock = new StockPosting
                    {
                        TenantId = tenantId,
                        Date = financialYear.FromDate,
                        VoucherNumbering = 0,
                        DateMiti = financialYear.FromMiti,
                        VoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock"),
                        VoucherNo = "",
                        ProductId = productId,
                        UnitId = stocks.UnitId,
                        Amount = stocks.Rate * stocks.OpeningQty,
                        GrossAmount = stocks.Rate * stocks.OpeningQty,
                        NetAmount = stocks.Rate * stocks.OpeningQty,
                        DiscountAmount = 0,
                        TaxAmount = 0,
                        VendorVoucherNo = "",
                        LedgerId = null,
                        AgainstVoucherTypeId = Guid.Empty,
                        AgainstVoucherNo = "",
                        InWardQty = stocks.OpeningQty,
                        OutWardQty = 0,
                        Rate = stocks.Rate,
                        MasterId = productId,
                        FinancialYearId = FinancialYearId
                    };
                    var stockId = await stockPostingRepository.InsertAndGetIdAsync(stock);


                    var st = new StockMaintainDto
                    {
                        DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                        ProductId = productId,
                        UnitId = stocks.UnitId,
                        Qty = stocks.OpeningQty,
                        Rate = stocks.Rate,
                        FinancialYearId = FinancialYearId,
                        Type = StockMaintainTypeEnum.Opening
                    };
                    await stockManagementAppService.MaintainStock(st);
                }
            }
            await unitOfWork.CompleteAsync();

            return productId;
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var stringChars = new char[length];

            for (var i = 0; i < length; i++) stringChars[i] = chars[random.Next(chars.Length)];

            return new string(stringChars);
        }

        private static long GenerateRandomInt(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task<List<ProductOpeningStockUpdate>> GetAllNonOpeningStockProducts()
        {
            var result = new List<ProductOpeningStockUpdate>();
            var products = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.IsOpeningStock == false &&
                            (x.ProductType == ProductTypeEnum.Product || x.ProductType == ProductTypeEnum.RawMaterial))
                .Include(x => x.UnitFk).Include(x => x.ProductGroupFk).ToListAsync();
            foreach (var product in products)
            {
                var data = new ProductOpeningStockUpdate
                {
                    ProductId = product.Id,
                    UnitId = product.UnitId,
                    OpeningQty = 0,
                    Rate = product.PurchaseRate
                };
                result.Add(data);
            }

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesProducts)]
        [DisableAuditing]
        public async Task CreateNonOpeningStockProduct(OpeningStockProductDto inputs)
        {
            var tenantId = AbpSession.TenantId;
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;
            foreach (var product in inputs.Products)
                if (product.OpeningQty > 0)
                {
                    var productData = await productRepository.FirstOrDefaultAsync(x => x.Id == product.ProductId);
                    if (productData == null || !UsesStockCalculation(productData.ProductType))
                        continue;

                    var stock = new StockPosting
                    {
                        TenantId = tenantId,
                        Date = FinancialYear.FromDate,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = "",
                        ProductId = productData.Id,
                        UnitId = product.UnitId,
                        DateMiti = FinancialYear.FromMiti,
                        LedgerId = null,
                        VendorVoucherNo = "",
                        MasterId = productData.Id,
                        AgainstVoucherTypeId = Guid.Empty,
                        AgainstVoucherNo = "",
                        InWardQty = product.OpeningQty,
                        Amount = product.OpeningQty * product.Rate,
                        NetAmount = product.OpeningQty * product.Rate,
                        GrossAmount = product.OpeningQty * product.Rate,
                        DiscountAmount = 0,
                        TaxAmount = 0,
                        OutWardQty = 0,
                        Rate = product.Rate,
                        FinancialYearId = FinancialYearId
                    };
                    var stockId = await stockPostingRepository.InsertAndGetIdAsync(stock);

                    productData.IsOpeningStock = true;
                    await productRepository.UpdateAsync(productData);
                }
        }

        [AbpAuthorize(AppPermissions.PagesProductsCreate)]
        public async Task CreateMultipleProduct(MultipleProductCreate data)
        {
            var tenantId = AbpSession.GetTenantId();

            using var unitOfWork = unitOfWorkManager.Begin();
            var count = 0;
            foreach (var input in data.ProductList)
            {
                var product = new Product
                {
                    TenantId = tenantId,
                    DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                    Name = input.ProductName,
                    Mrp = input.Mrp,
                    SalesRate = input.SalesRate,
                    PurchaseRate = input.PurchaseRate,
                    MinimumStock = 0,
                    MaximumStock = 0,
                    IsOpeningStock = false,
                    Description = "",
                    IsActive = true,
                    TaxId = input.TaxId,
                    ProductGroupId = data.ProductGroupId,
                    UnitId = data.UnitId
                };

                var productId = await productRepository.InsertAndGetIdAsync(product);
                //var batch = new Batch
                //{
                //    TenantId = tenantId,
                //    Name = "NA",
                //    ManufacturingDate = DateTime.Today,
                //    ExpiryDate = DateTime.Today,
                //    IsDefault = true,
                //    Description = input.ProductName,
                //    ProductId = productId,
                //    BranchId = data.BranchId
                //};
                //await _batchRepository.InsertAsync(batch);

                var unit = new UnitConversion
                {
                    ConversionRate = 1,
                    Qty = 1,
                    PrimaryQty = 1,
                    ProductId = productId,
                    UnitId = data.UnitId,
                    TenantId = tenantId
                };
                await unitConversionRepository.InsertAsync(unit);

                count++;
            }

            await unitOfWork.CompleteAsync();
        }

        [AbpAuthorize(AppPermissions.PagesProductsEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditProductDto input)
        {
            var tenantId = AbpSession.TenantId;

            var product = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.UnitFk).FirstOrDefaultAsync(x => x.Id == input.Id);


            if (product == null) throw new UserFriendlyException("Data not found");

            var stockPostingList = await stockPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Include(x => x.UnitFk).AsNoTracking()
                .Where(x => x.ProductId == input.Id && !x.IsDeleted).ToListAsync();

            if (product.UnitId != input.UnitId)
                if (stockPostingList.Count(x => x.UnitId == product.UnitId) > 0)
                    throw new UserFriendlyException($"This Main Unit {product.UnitFk.Name} is Used");

            var usesStockCalculation = UsesStockCalculation(input.ProductType);
            var openingStockItems = usesStockCalculation
                ? input.OpeningStock ?? new List<StockPostingCreateDto>()
                : new List<StockPostingCreateDto>();
            var isOpeningStock = usesStockCalculation && input.IsOpeningStock;

            product.Name = input.Name;
            product.ProductType = input.ProductType;
            product.Mrp = input.Mrp;
            product.SalesRate = input.SalesRate ?? 0;
            product.PurchaseRate = input.PurchaseRate ?? 0;
            product.MinimumStock = usesStockCalculation ? input.MinimumStock ?? 0 : 0;
            product.MaximumStock = usesStockCalculation ? input.MaximumStock ?? 0 : 0;
            product.HsCode = input.HsCode;
            product.IsOpeningStock = isOpeningStock;
            product.Description = input.Description;
            product.Margin = input.Margin;
            product.IsActive = input.IsActive;
            product.TaxId = input.TaxId;
            product.ProductGroupId = input.ProductGroupId;
            product.UnitId = input.UnitId;
            await productRepository.UpdateAsync(product);


            var noMainUnitStock = stockPostingList.Where(x => x.UnitId != product.UnitId).ToList();
            var unitsList = await unitConversionRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ProductId == input.Id).ToListAsync();


            var deleteUnit = unitsList.Where(x => x.UnitId != input.UnitId).ToList();
            if (noMainUnitStock.Count > 0)
            {
                var deleteProductUnit = noMainUnitStock
                    .Where(x => deleteUnit.Select(a => a.UnitId).Contains(x.UnitId)).ToList();
                if (deleteProductUnit.Count > 0)
                {
                    var joinUnit = string.Join(",", deleteProductUnit.Select(x => x.UnitFk.Name).Distinct());
                    throw new UserFriendlyException($"This Delete Multiple Unit {joinUnit}  is Used");
                }
            }

            foreach (var unitDelete in deleteUnit) await unitConversionRepository.DeleteAsync(unitDelete);

            var openingStockVoucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var openingIds = openingStockItems.Select(x => x.Id).ToList();
            var openingDatabaseIds = await stockPostingRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.ProductId == input.Id && x.IsDeleted == false && x.VoucherNumbering == 0 &&
                            x.VoucherTypeId == openingStockVoucherTypeId && x.FinancialYearId == FinancialYearId)
                .Select(x => x.Id).ToListAsync();

            foreach (var databaseId in openingDatabaseIds)
                if (!openingIds.Contains(databaseId))
                    await stockPostingRepository.DeleteAsync(databaseId);

            if (!isOpeningStock)
            {
                var stocks = await stockPostingRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.ProductId == input.Id && x.VoucherTypeId == openingStockVoucherTypeId &&
                                x.VoucherNumbering == 0).ToListAsync();
                foreach (var stock in stocks)
                {
                    stock.IsDeleted = true;
                    await stockPostingRepository.UpdateAsync(stock);
                }
            }

            if (isOpeningStock)
            {
                var imeList = new List<string>();

                foreach (var inputstock in openingStockItems)
                {
                    Guid stockId;
                    if (inputstock.Id == Guid.Empty || inputstock.Id == null)
                    {
                        var stock = new StockPosting
                        {
                            TenantId = tenantId,
                            Date = FinancialYear.FromDate,
                            DateMiti = FinancialYear.FromMiti,
                            VoucherTypeId = openingStockVoucherTypeId,
                            VoucherNo = "",
                            ProductId = product.Id,
                            UnitId = inputstock.UnitId,
                            Amount = inputstock.Rate * inputstock.OpeningQty,
                            NetAmount = inputstock.Rate * inputstock.OpeningQty,
                            GrossAmount = inputstock.Rate * inputstock.OpeningQty,
                            DiscountAmount = 0,
                            TaxAmount = 0,
                            LedgerId = null,
                            AgainstVoucherTypeId = Guid.Empty,
                            AgainstVoucherNo = "",
                            InWardQty = inputstock.OpeningQty,
                            OutWardQty = 0,
                            Rate = inputstock.Rate,
                            MasterId = product.Id,
                            FinancialYearId = FinancialYearId,
                            IsDeleted = false
                        };
                        stockId = await stockPostingRepository.InsertAndGetIdAsync(stock);
                    }
                    else
                    {
                        var openingStock = await stockPostingRepository.FirstOrDefaultAsync(x =>
                            x.IsDeleted == false &&
                            x.Id == inputstock.Id && x.TenantId == tenantId);
                        if (openingStock != null)
                        {
                            openingStock.UnitId = inputstock.UnitId;
                            openingStock.Amount = inputstock.Rate * inputstock.OpeningQty;
                            openingStock.NetAmount = inputstock.Rate * inputstock.OpeningQty;
                            openingStock.GrossAmount = inputstock.Rate * inputstock.OpeningQty;
                            openingStock.DiscountAmount = 0;
                            openingStock.InWardQty = inputstock.OpeningQty;
                            openingStock.Rate = inputstock.Rate;
                            openingStock.FinancialYearId = FinancialYearId;
                            await stockPostingRepository.UpdateAsync(openingStock);
                        }

                        stockId = (Guid)inputstock.Id;
                    }
                }
            }


            if (await unitConversionRepository.CountAsync(x =>
                    x.ProductId == input.Id && x.UnitId == input.UnitId && !x.IsDeleted) == 0)
            {
                var productUnit = new UnitConversion
                {
                    IsDeleted = false,
                    ConversionRate = 1,
                    Qty = 1,
                    PrimaryQty = 1,
                    ProductId = product.Id,
                    UnitId = input.UnitId,
                    TenantId = tenantId
                };
                await unitConversionRepository.InsertAsync(productUnit);
            }

            return product.Id;
        }

        public async Task<List<string>> GetAllHsCodes()
        {
            return await productRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).Select(x => x.HsCode ?? "").Where(x => x != "")
                .ToListAsync();
        }

        public async Task<string> GetProductCode()
        {
            var productCode = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(x => x.ProductCode).ToListAsync();
            {
                var x = 100;
                var productCodes = productCode.Where(str => int.TryParse(str, out x)).Select(_ => x).ToList();
                return productCodes.Count == 0 ? "100" : Convert.ToString(productCodes.Max() + 1);
            }
        }

        public async Task<int> GetProductCodeInt()
        {
            var productCode = await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(x => x.ProductCode).ToListAsync();
            {
                var x = 100;
                var productCodes = productCode.Where(str => int.TryParse(str, out x)).Select(_ => x).ToList();
                if (productCodes.Count == 0)
                    return 100;
                return productCodes.Max() + 1;
            }
        }
    }
}
