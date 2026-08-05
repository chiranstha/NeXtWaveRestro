using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using NepDate;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Notifications;
using NextWave.Erp.Storage;
using Stripe;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Importing
{

    public class ImportProductsToExcelJob(
        IAppNotifier appNotifier,
        IBinaryObjectManager binaryObjectManager,
        IProductListExcelDataReader productListExcelDataReader,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Tax, Guid> taxRepository,
        IRepository<FinancialYear, Guid> financialYearRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductGroup, Guid> productGroupRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<StockPosting, Guid> stockPostingRepository)
        : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        private readonly IAppNotifier _appNotifier = appNotifier;


        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var products = GetProductListFromExcelOrNull(args);
            if (products == null || !products.Any())
            {
                SendInvalidExcelNotification(args);
                return;
            }

            CreateProducts(args, products);
        }

        private List<ImportProductDto> GetProductListFromExcelOrNull(ImportUniversalFromExcelJobArgs args)
        {
            using var uow = unitOfWorkManager.Begin();
            using (CurrentUnitOfWork.SetTenantId(args.TenantId))
            {
                try
                {
                    var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                    return productListExcelDataReader.GetProductsFromExcel(file.Bytes);
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    uow.Complete();
                }
            }
        }

        private void SendInvalidExcelNotification(ImportUniversalFromExcelJobArgs args)
        {
            using var uow = unitOfWorkManager.Begin();
            using (CurrentUnitOfWork.SetTenantId(args.TenantId))
            {
                //AsyncHelper.RunSync(() => _appNotifier.SendMessageAsync(
                //    args.BinaryObjectId,
                //    new LocalizableString("FileCantBeConvertedToUserList", ERPConsts.LocalizationSourceName),
                //    null,
                //    NotificationSeverity.Warn));
            }

            uow.Complete();
        }

        private void CreateProducts(ImportUniversalFromExcelJobArgs args, List<ImportProductDto> products)
        {
            var invalidProducts = new List<ImportProductDto>();
            var successCount = 0;

            foreach (var product in products)
            {
                using var uow = unitOfWorkManager.Begin();
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    try
                    {
                        AsyncHelper.RunSync(() => CreateProductAsync(product));
                        successCount++;
                    }
                    catch (UserFriendlyException exception)
                    {
                        // Log the specific error for this product
                        Console.WriteLine($"Failed to import product '{product.Name}': {exception.Message}");
                        invalidProducts.Add(product);
                    }
                    catch (Exception exception)
                    {
                        // Log the generic error for this product
                        Console.WriteLine($"Failed to import product '{product.Name}': {exception.Message}");
                        invalidProducts.Add(product);
                    }
                }

                uow.Complete();
            }

            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    // Send notification about import results
                    var message = $"Product import completed. Success: {successCount}, Failed: {invalidProducts.Count}";
                    if (invalidProducts.Count > 0)
                    {
                        var failedProducts = string.Join(", ", invalidProducts.Select(p => p.Name).Take(5));
                        if (invalidProducts.Count > 5)
                            failedProducts += $" and {invalidProducts.Count - 5} more";
                        message += $". Failed products: {failedProducts}";
                    }

                    // Log the final result
                    Console.WriteLine(message);

                    // You can implement notification sending here if needed
                    // AsyncHelper.RunSync(() => _appNotifier.SendMessageAsync(
                    //     args.TenantId,
                    //     message,
                    //     null,
                    //     NotificationSeverity.Info));
                }

                uow.Complete();
            }
        }

        private async Task CreateProductAsync(ImportProductDto input)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Product name is required");

            // Ensure ProductCode is set
            if (string.IsNullOrWhiteSpace(input.ProductCode))
                input.ProductCode = SanitizeProductCode(input.Name, tenantId);

            var ope = await stockPostingRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductFk.Name == input.Name);
            decimal opqty = 0;
            if (ope != null)
                opqty = ope.InWardQty > 0 ? ope.InWardQty : 0;
            // Check for existing product by name first, then by product code
            var productData = await productRepository.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                (x.Name.ToLower() == input.Name.ToLower() &&
                  x.PurchaseRate == input.PurchaseRate && input.OpeningQty == opqty));

            if (productData == null)
            {               
                var taxId = Guid.Empty;
                if (input.IsTaxable == 1)
                    taxId = (await taxRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Rate != 0))?.Id ?? Guid.Empty;
                else
                    taxId = (await taxRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Rate == 0))?.Id ?? Guid.Empty;

                var productGroupId = await productGroupRepository.FirstOrDefaultAsync(x => x.Name == input.ProductGroupName && x.TenantId == tenantId);
                var unitNameId = await unitRepository.FirstOrDefaultAsync(x => x.Name == input.UnitName && x.TenantId == tenantId);


                var newProductGroupId = Guid.Empty;
                if (productGroupId == null)
                {
                    var productGroup = new ProductGroup
                    {
                        Name = input.ProductGroupName,
                        GroupUnder = null,
                        Description = "",
                        IsDefult = false,
                        TenantId = tenantId
                    };
                    newProductGroupId = await productGroupRepository.InsertAndGetIdAsync(productGroup);
                }

                var newunitId = Guid.Empty;
                if (unitNameId == null)
                {
                    var unitName = new Unit
                    {
                        Name = input.UnitName,
                        FormalName = input.UnitName,
                        IsDefault = false,
                        TenantId = tenantId
                    };
                    newunitId = await unitRepository.InsertAndGetIdAsync(unitName);
                }

                var product = new Product
                {
                    DateMiti = DateTime.Now.ToNepaliDate().ToString(),
                    ProductType = ProductTypeEnum.Product,
                    Name = input.Name,
                    Mrp = input.Mrp,
                    SalesRate = input.SalesRate,
                    PurchaseRate = input.PurchaseRate,
                    MinimumStock = 1,
                    MaximumStock = 1,
                    IsOpeningStock = input.OpeningQty > 0 ? true : false,
                    Description = "",
                    IsActive = true,
                    IsDeleted = false,
                    TaxId = taxId,
                    ProductGroupId = productGroupId?.Id ?? newProductGroupId,
                    HsCode = input.HsCode,
                    UnitId = unitNameId?.Id ?? newunitId,
                    TenantId = (int)tenantId
                };
                var productId = await productRepository.InsertAndGetIdAsync(product);

                var unitModel = new UnitConversion
                {
                    Id = Guid.Empty,
                    IsDeleted = false,
                    ConversionRate = 1,
                    Qty = 1,
                    PrimaryQty = 1,
                    ProductId = productId,
                    UnitId = product.UnitId,
                    TenantId = tenantId
                };
                await unitConversionRepository.InsertAndGetIdAsync(unitModel);

                var voucherType = await voucherTypeRepository.FirstOrDefaultAsync(x => x.Name == "OpeningStock" && x.TenantId == tenantId);
                var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain && x.TenantId == tenantId);
                var financialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Status);

                if (input.OpeningQty > 0)
                {
                    var stockposting = new StockPosting
                    {
                        Id = Guid.Empty,
                        VoucherNumbering = 0,
                        IsDeleted = false,
                        Date = financialYear?.FromDate ?? DateTime.Today,
                        DateMiti = financialYear?.FromMiti ?? DateTime.Now.ToNepaliDate().ToString(),
                        VoucherTypeId = voucherType?.Id ?? Guid.Empty,
                        VoucherNo = "",
                        Amount = input.PurchaseRate * input.OpeningQty,
                        TaxAmount = 0,
                        ProductId = productId,
                        UnitId = unitNameId?.Id ?? newunitId,
                        GrossAmount = input.PurchaseRate * input.OpeningQty,
                        DiscountAmount = 0,
                        NetAmount = input.PurchaseRate * input.OpeningQty,
                        AgainstVoucherTypeId = Guid.Empty,
                        AgainstVoucherNo = null,
                        InWardQty = input.OpeningQty,
                        OutWardQty = 0,
                        Rate = input.PurchaseRate,
                        FinancialYearId = financialYear?.Id ?? Guid.Empty,
                        VendorVoucherNo = null,
                        MasterId = productId,
                        TenantId = tenantId,
                        IsValueIncrease = true,
                        LedgerId = null
                    };
                    var stockId = await stockPostingRepository.InsertAndGetIdAsync(stockposting);
                }

                
            }
            else if (input.OpeningQty > 0)
            {
                // Update existing product with opening stock
                var voucherType = await voucherTypeRepository.FirstOrDefaultAsync(x => x.Name == "OpeningStock" && x.TenantId == tenantId);
                var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain && x.TenantId == tenantId);
                var financialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId);
                var stockData = await stockPostingRepository.FirstOrDefaultAsync(x => x.VoucherTypeId == voucherType.Id
                    && x.ProductId == productData.Id && x.FinancialYearId == financialYear.Id);
                if (stockData == null)
                {
                    var stockposting = new StockPosting
                    {
                        Id = Guid.Empty,
                        VoucherNumbering = 0,
                        IsDeleted = false,
                        Date = financialYear?.FromDate ?? DateTime.Today,
                        DateMiti = financialYear?.FromMiti ?? DateTime.Now.ToNepaliDate().ToString(),
                        VoucherTypeId = voucherType?.Id ?? Guid.Empty,
                        VoucherNo = "",
                        Amount = input.PurchaseRate * input.OpeningQty,
                        TaxAmount = 0,
                        ProductId = productData.Id,
                        UnitId = productData.UnitId,
                        IsValueIncrease = true,
                        LedgerId = Guid.Empty,
                        GrossAmount = input.PurchaseRate * input.OpeningQty,
                        DiscountAmount = 0,
                        NetAmount = input.PurchaseRate * input.OpeningQty,
                        AgainstVoucherTypeId = Guid.Empty,
                        AgainstVoucherNo = null,
                        InWardQty = input.OpeningQty,
                        OutWardQty = 0,
                        Rate = input.PurchaseRate,
                        FinancialYearId = financialYear?.Id ?? Guid.Empty,
                        VendorVoucherNo = null,
                        MasterId = productData.Id,
                        TenantId = tenantId
                    };
                    await stockPostingRepository.InsertAsync(stockposting);
                }
            }
        }

        private string SanitizeProductCode(string productName, int? tenantId)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return $"PROD_{Guid.NewGuid():N}";

            // Take first 50 characters, remove special characters, replace spaces with underscores
            var sanitized = productName
                .Trim()
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("-", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("/", "_")
                .ToUpperInvariant();

            // Ensure it doesn't start with a number
            if (char.IsDigit(sanitized[0]))
                sanitized = "PROD_" + sanitized;

            // Limit length to 50 characters
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);

            return sanitized;
        }
    }
}
