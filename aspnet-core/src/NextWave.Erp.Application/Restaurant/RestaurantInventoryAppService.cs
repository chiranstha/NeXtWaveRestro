using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Restaurant.Dtos;
using NextWave.Erp.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantInventory)]
    public class RestaurantInventoryAppService(
        IRepository<RestaurantSupplierItemMapping, Guid> supplierItemMappingRepository,
        IRepository<RestaurantStockAdjustment, Guid> stockAdjustmentRepository,
        IRepository<RestaurantStockAdjustmentLine, Guid> stockAdjustmentLineRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<User, long> userRepository,
        IRepository<RestaurantOrder, Guid> orderRepository,
        IRepository<RestaurantOrderItem, Guid> orderItemRepository,
        IRepository<RestaurantBillLine, Guid> billLineRepository,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        MaterialStockPostingService materialStockPostingService,
        RestaurantReorderService reorderService,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantInventoryAppService
    {
        public async Task<List<UniversalDropdownDto>> GetRawMaterials()
        {
            var tenantId = AbpSession.GetTenantId();
            return await productRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.ProductType == ProductTypeEnum.RawMaterial &&
                            x.IsActive &&
                            !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                })
                .ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetUnits()
        {
            var tenantId = AbpSession.GetTenantId();
            return await unitRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Name)
                .Select(unit => new UniversalDropdownDto
                {
                    Id = unit.Id,
                    DisplayName = unit.Name == null ? "" : unit.Name.ToString()
                })
                .ToListAsync();
        }

        public async Task<List<RestaurantSupplierItemMappingDto>> GetSupplierItemMappings(Guid? productId)
        {
            var tenantId = AbpSession.GetTenantId();
            var query = supplierItemMappingRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.SupplierLedgerFk)
                .Include(x => x.UnitFk)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (productId.HasValue && productId.Value != Guid.Empty)
                query = query.Where(x => x.ProductId == productId.Value);

            return await query
                .OrderBy(x => x.ProductFk.Name)
                .ThenByDescending(x => x.IsPreferred)
                .ThenBy(x => x.SupplierLedgerFk.Name)
                .Select(x => new RestaurantSupplierItemMappingDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.ProductFk.Name,
                    SupplierLedgerId = x.SupplierLedgerId,
                    SupplierName = x.SupplierLedgerFk.Name,
                    SupplierSku = x.SupplierSku,
                    UnitId = x.UnitId,
                    UnitName = x.UnitFk.Name,
                    Rate = x.Rate,
                    LeadTimeDays = x.LeadTimeDays,
                    MinimumOrderQty = x.MinimumOrderQty,
                    IsPreferred = x.IsPreferred,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantInventorySupplierMapping)]
        public async Task<Guid> CreateOrEditSupplierItemMapping(CreateOrEditRestaurantSupplierItemMappingDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            await ValidateSupplierItemMapping(input, tenantId);

            RestaurantSupplierItemMapping mapping;
            if (input.Id.HasValue && input.Id.Value != Guid.Empty)
            {
                mapping = await supplierItemMappingRepository.FirstOrDefaultAsync(x =>
                    x.Id == input.Id.Value && x.TenantId == tenantId && !x.IsDeleted);
                if (mapping == null)
                    throw new UserFriendlyException("Supplier item mapping not found");
            }
            else
            {
                mapping = new RestaurantSupplierItemMapping
                {
                    TenantId = tenantId,
                    CreatedAt = DateTime.Now
                };
                await supplierItemMappingRepository.InsertAsync(mapping);
            }

            mapping.ProductId = input.ProductId;
            mapping.SupplierLedgerId = input.SupplierLedgerId;
            mapping.SupplierSku = input.SupplierSku ?? "";
            mapping.UnitId = input.UnitId;
            mapping.Rate = input.Rate;
            mapping.LeadTimeDays = Math.Max(0, input.LeadTimeDays);
            mapping.MinimumOrderQty = Math.Max(0, input.MinimumOrderQty);
            mapping.IsPreferred = input.IsPreferred;
            mapping.IsActive = input.IsActive;

            return mapping.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantInventorySupplierMapping)]
        public async Task DeleteSupplierItemMapping(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var mapping = await supplierItemMappingRepository.FirstOrDefaultAsync(x =>
                x.Id == input.Id && x.TenantId == tenantId && !x.IsDeleted);
            if (mapping == null)
                return;

            mapping.IsDeleted = true;
            mapping.IsActive = false;
            await supplierItemMappingRepository.UpdateAsync(mapping);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantInventoryReorder)]
        public Task<List<RestaurantLowStockSuggestionDto>> GetLowStockSuggestions()
        {
            return reorderService.GetLowStockSuggestionsAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantInventoryReorder)]
        public Task<GenerateDraftPurchaseOrdersResultDto> GenerateDraftPurchaseOrders(GenerateDraftPurchaseOrdersDto input)
        {
            return reorderService.GenerateDraftPurchaseOrdersAsync(input);
        }

        public async Task<Guid> CreateStockAdjustment(CreateRestaurantStockAdjustmentDto input)
        {
            input ??= new CreateRestaurantStockAdjustmentDto();
            input.Lines ??= new List<CreateRestaurantStockAdjustmentLineDto>();
            if (input.Lines.Count == 0)
                throw new UserFriendlyException("At least one adjustment line is required");

            var requiredPermission = input.AdjustmentType == RestaurantStockAdjustmentType.Wastage
                ? AppPermissions.PagesRestaurantInventoryWastage
                : AppPermissions.PagesRestaurantInventoryStockAdjustment;
            if (!await PermissionChecker.IsGrantedAsync(requiredPermission))
                throw new AbpAuthorizationException("You do not have permission to post this restaurant inventory entry");

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var tenantId = AbpSession.GetTenantId();
            var dateMiti = string.IsNullOrWhiteSpace(input.DateMiti)
                ? DateConverter.ConvertToNepali(DateTime.Today)
                : input.DateMiti;
            var voucherTypeName = GetVoucherTypeName(input.AdjustmentType);
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId(voucherTypeName);
            var voucherNumbering = await GetNextAdjustmentVoucherNumbering(voucherTypeId);
            var voucherNo = await GetAdjustmentVoucherNo(voucherTypeName, voucherNumbering);

            var adjustment = new RestaurantStockAdjustment
            {
                TenantId = tenantId,
                VoucherNo = voucherNo,
                VoucherNumbering = voucherNumbering,
                Date = DateConverter.ConvertToEnglish(dateMiti),
                DateMiti = dateMiti,
                AdjustmentType = input.AdjustmentType,
                Description = input.Description ?? "",
                VoucherTypeId = voucherTypeId,
                FinancialYearId = FinancialYearId,
                CreateUserId = AbpSession.UserId,
                CreatedAt = DateTime.Now
            };

            var adjustmentId = await stockAdjustmentRepository.InsertAndGetIdAsync(adjustment);
            var affectedProductIds = new HashSet<Guid>();

            foreach (var inputLine in input.Lines)
            {
                var lineId = Guid.NewGuid();
                var product = await ValidateAdjustmentLine(inputLine, tenantId);
                if (input.AdjustmentType != RestaurantStockAdjustmentType.PhysicalCount && inputLine.Qty <= 0)
                    throw new UserFriendlyException("Adjustment quantity must be greater than zero");
                if (input.AdjustmentType == RestaurantStockAdjustmentType.Wastage && string.IsNullOrWhiteSpace(inputLine.Reason))
                    throw new UserFriendlyException("Wastage reason is required");
                var systemQty = await materialStockPostingService.GetAvailableStockAsync(inputLine.ProductId, inputLine.UnitId);
                var movementQty = GetMovementQty(input.AdjustmentType, inputLine, systemQty);
                var movementType = GetMovementType(input.AdjustmentType, movementQty);
                var amount = Math.Abs(movementQty) * inputLine.Rate;

                if (movementQty != 0)
                {
                    if (movementType == StockMaintainTypeEnum.Outward)
                        await EnsureOutwardAllowed(inputLine.ProductId, inputLine.UnitId, Math.Abs(movementQty), product.Name);

                    await materialStockPostingService.ApplyDirectStockAsync(new MaterialStockPostingRequest
                    {
                        DateMiti = dateMiti,
                        VoucherTypeId = voucherTypeId,
                        VoucherNo = voucherNo,
                        VoucherNumbering = voucherNumbering,
                        ProductId = inputLine.ProductId,
                        UnitId = inputLine.UnitId,
                        Qty = Math.Abs(movementQty),
                        Rate = inputLine.Rate,
                        GrossAmount = amount,
                        DiscountAmount = 0,
                        NetAmount = amount,
                        Amount = amount,
                        TaxAmount = 0,
                        MovementType = movementType,
                        IsValueIncrease = movementType == StockMaintainTypeEnum.Inward,
                        FinancialYearId = FinancialYearId,
                        MasterId = adjustmentId,
                        SourceDetailId = lineId,
                        TenantId = tenantId
                    });
                }

                await stockAdjustmentLineRepository.InsertAsync(new RestaurantStockAdjustmentLine
                {
                    Id = lineId,
                    TenantId = tenantId,
                    StockAdjustmentId = adjustmentId,
                    ProductId = inputLine.ProductId,
                    UnitId = inputLine.UnitId,
                    Qty = Math.Abs(movementQty),
                    Rate = inputLine.Rate,
                    Amount = amount,
                    SystemQty = systemQty,
                    CountedQty = input.AdjustmentType == RestaurantStockAdjustmentType.PhysicalCount
                        ? inputLine.CountedQty ?? inputLine.Qty
                        : 0,
                    Reason = inputLine.Reason ?? ""
                });

                affectedProductIds.Add(inputLine.ProductId);
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await reorderService.GenerateDraftPurchaseOrdersForProductsAsync(affectedProductIds.ToList(), dateMiti);
            await uow.CompleteAsync();
            return adjustmentId;
        }

        public async Task<List<RestaurantStockAdjustmentDto>> GetStockAdjustments(RestaurantReportFilterDto input)
        {
            input ??= new RestaurantReportFilterDto();
            var tenantId = AbpSession.GetTenantId();
            var query = stockAdjustmentRepository.GetAll()
                .Include(x => x.Lines)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (input.FromDate.HasValue)
            {
                var from = input.FromDate.Value.Date;
                query = query.Where(x => x.Date >= from);
            }

            if (input.ToDate.HasValue)
            {
                var to = input.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.Date < to);
            }

            var adjustments = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.VoucherNumbering)
                .ToListAsync();
            var adjustmentIds = adjustments.Select(x => x.Id).ToList();
            var lines = adjustmentIds.Count == 0
                ? new List<RestaurantStockAdjustmentLineDto>()
                : await stockAdjustmentLineRepository.GetAll()
                    .Include(x => x.ProductFk)
                    .Include(x => x.UnitFk)
                    .Where(x => x.TenantId == tenantId && adjustmentIds.Contains(x.StockAdjustmentId) && !x.IsDeleted)
                    .Select(x => new RestaurantStockAdjustmentLineDto
                    {
                        Id = x.Id,
                        StockAdjustmentId = x.StockAdjustmentId,
                        ProductId = x.ProductId,
                        ProductName = x.ProductFk.Name,
                        UnitId = x.UnitId,
                        UnitName = x.UnitFk.Name,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Amount = x.Amount,
                        SystemQty = x.SystemQty,
                        CountedQty = x.CountedQty,
                        Reason = x.Reason
                    })
                    .ToListAsync();

            var users = await userRepository.GetAll()
                .Where(x => x.TenantId == tenantId && adjustments.Select(a => a.CreateUserId).Contains(x.Id))
                .Select(x => new { x.Id, Name = x.Name + " " + x.Surname })
                .ToDictionaryAsync(x => (long?)x.Id, x => x.Name);

            return adjustments.Select(x =>
            {
                users.TryGetValue(x.CreateUserId, out var userName);
                var adjustmentLines = lines.Where(line => line.StockAdjustmentId == x.Id).ToList();
                return new RestaurantStockAdjustmentDto
                {
                    Id = x.Id,
                    VoucherNo = x.VoucherNo,
                    Date = x.Date,
                    DateMiti = x.DateMiti,
                    AdjustmentType = x.AdjustmentType,
                    Description = x.Description,
                    CreateUserName = userName ?? "",
                    TotalAmount = adjustmentLines.Sum(line => line.Amount),
                    Lines = adjustmentLines
                };
            }).ToList();
        }

        public async Task<List<RestaurantConsumptionLedgerDto>> GetConsumptionLedger(RestaurantConsumptionFilterDto input)
        {
            input ??= new RestaurantConsumptionFilterDto();
            var tenantId = AbpSession.GetTenantId();

            var query =
                from stockPosting in stockPostingRepository.GetAll()
                join billLine in billLineRepository.GetAll() on stockPosting.SourceDetailId equals billLine.SalesDetailId
                join salesMaster in salesMasterRepository.GetAll() on billLine.SalesMasterId equals salesMaster.Id
                join order in orderRepository.GetAll() on billLine.OrderId equals order.Id
                join orderItem in orderItemRepository.GetAll() on billLine.OrderItemId equals orderItem.Id
                join rawMaterial in productRepository.GetAll() on stockPosting.ProductId equals rawMaterial.Id
                join menuProduct in productRepository.GetAll() on orderItem.ProductId equals menuProduct.Id
                join unit in unitRepository.GetAll() on stockPosting.UnitId equals unit.Id
                join menuItem in menuItemRepository.GetAll() on orderItem.MenuItemId equals (Guid?)menuItem.Id into menuItems
                from menuItem in menuItems.DefaultIfEmpty()
                where stockPosting.TenantId == tenantId &&
                      billLine.TenantId == tenantId &&
                      order.TenantId == tenantId &&
                      orderItem.TenantId == tenantId &&
                      stockPosting.OutWardQty > 0 &&
                      !stockPosting.IsDeleted
                select new
                {
                    stockPosting.Date,
                    stockPosting.DateMiti,
                    stockPosting.VoucherNo,
                    stockPosting.SourceDetailId,
                    stockPosting.MasterId,
                    RawMaterialId = stockPosting.ProductId,
                    RawMaterialName = rawMaterial.Name,
                    stockPosting.UnitId,
                    UnitName = unit.Name,
                    Qty = stockPosting.OutWardQty,
                    stockPosting.Rate,
                    stockPosting.Amount,
                    billLine.OrderId,
                    order.OrderNo,
                    order.TableId,
                    order.WaiterUserId,
                    billLine.OrderItemId,
                    billLine.SalesMasterId,
                    SalesVoucherNo = salesMaster.VoucherNo,
                    billLine.SalesDetailId,
                    MenuProductId = orderItem.ProductId,
                    MenuProductName = menuProduct.Name,
                    orderItem.ItemNameSnapshot,
                    MenuItemDisplayName = menuItem.DisplayName,
                    CategoryId = (Guid?)menuItem.CategoryId
                };

            if (input.FromDate.HasValue)
            {
                var from = input.FromDate.Value.Date;
                query = query.Where(x => x.Date >= from);
            }

            if (input.ToDate.HasValue)
            {
                var to = input.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.Date < to);
            }

            if (input.RawMaterialId.HasValue && input.RawMaterialId.Value != Guid.Empty)
                query = query.Where(x => x.RawMaterialId == input.RawMaterialId.Value);

            if (input.MenuProductId.HasValue && input.MenuProductId.Value != Guid.Empty)
                query = query.Where(x => x.MenuProductId == input.MenuProductId.Value);

            if (input.OrderId.HasValue && input.OrderId.Value != Guid.Empty)
                query = query.Where(x => x.OrderId == input.OrderId.Value);

            if (input.SalesMasterId.HasValue && input.SalesMasterId.Value != Guid.Empty)
                query = query.Where(x => x.SalesMasterId == input.SalesMasterId.Value);

            if (input.TableId.HasValue && input.TableId.Value != Guid.Empty)
                query = query.Where(x => x.TableId == input.TableId.Value);

            if (input.WaiterUserId.HasValue)
                query = query.Where(x => x.WaiterUserId == input.WaiterUserId.Value);

            if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
                query = query.Where(x => x.CategoryId == input.CategoryId.Value);

            var rows = await query
                .AsNoTracking()
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.VoucherNo)
                .ThenBy(x => x.RawMaterialName)
                .ToListAsync();

            return rows.Select(x => new RestaurantConsumptionLedgerDto
            {
                Date = x.Date,
                DateMiti = x.DateMiti,
                OrderId = x.OrderId,
                OrderNo = x.OrderNo,
                OrderItemId = x.OrderItemId,
                SalesMasterId = x.SalesMasterId,
                SalesVoucherNo = x.SalesVoucherNo,
                SalesDetailId = x.SalesDetailId ?? x.SourceDetailId,
                VoucherNo = x.VoucherNo,
                MenuProductId = x.MenuProductId,
                MenuProductName = x.MenuProductName,
                MenuItemName = string.IsNullOrWhiteSpace(x.ItemNameSnapshot)
                    ? string.IsNullOrWhiteSpace(x.MenuItemDisplayName) ? x.MenuProductName : x.MenuItemDisplayName
                    : x.ItemNameSnapshot,
                RawMaterialId = x.RawMaterialId,
                RawMaterialName = x.RawMaterialName,
                UnitId = x.UnitId,
                UnitName = x.UnitName,
                Qty = x.Qty,
                Rate = x.Rate,
                Amount = x.Amount
            }).ToList();
        }

        public async Task<List<RestaurantRecipeCoverageDto>> GetRecipeCoverage(RestaurantReportFilterDto input)
        {
            input ??= new RestaurantReportFilterDto();
            var tenantId = AbpSession.GetTenantId();

            var menuQuery = menuItemRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.CategoryFk)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
                menuQuery = menuQuery.Where(x => x.CategoryId == input.CategoryId.Value);

            var menuItems = await menuQuery
                .Select(x => new
                {
                    MenuItemId = x.Id,
                    x.ProductId,
                    ProductName = x.DisplayName == null || x.DisplayName == "" ? x.ProductFk.Name : x.DisplayName,
                    x.CategoryId,
                    CategoryName = x.CategoryFk == null ? "" : x.CategoryFk.Name,
                    x.IsActive,
                    x.IsAvailable,
                    x.Price
                })
                .AsNoTracking()
                .ToListAsync();

            var productIds = menuItems.Select(x => x.ProductId).Distinct().ToList();
            var recipeLines = productIds.Count == 0
                ? new List<RecipeCoverageLine>()
                : await bomRepository.GetAll()
                    .Include(x => x.RawMaterialFk)
                    .Where(x => x.TenantId == tenantId &&
                                productIds.Contains(x.ProductId) &&
                                !x.IsDeleted &&
                                x.IsActive)
                    .Select(x => new RecipeCoverageLine
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        WastagePercentage = x.WastagePercentage,
                        CostRate = x.CostRate,
                        PurchaseRate = x.RawMaterialFk.PurchaseRate
                    })
                    .AsNoTracking()
                    .ToListAsync();

            return menuItems
                .Select(item =>
                {
                    var lines = recipeLines.Where(x => x.ProductId == item.ProductId).ToList();
                    var recipeCost = lines.Sum(x =>
                    {
                        var rate = x.CostRate > 0 ? x.CostRate : x.PurchaseRate;
                        return x.Quantity * (1 + x.WastagePercentage / 100) * rate;
                    });

                    return new RestaurantRecipeCoverageDto
                    {
                        MenuItemId = item.MenuItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        IsActive = item.IsActive,
                        IsAvailable = item.IsAvailable,
                        HasRecipe = lines.Count > 0,
                        ActiveRecipeLineCount = lines.Count,
                        EstimatedRecipeCost = recipeCost,
                        MenuPrice = item.Price,
                        FoodCostPercent = item.Price == 0 ? 0 : recipeCost * 100 / item.Price,
                        MissingRawMaterialSetup = lines.Count == 0
                    };
                })
                .OrderByDescending(x => x.MissingRawMaterialSetup)
                .ThenBy(x => x.CategoryName)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        private async Task ValidateSupplierItemMapping(CreateOrEditRestaurantSupplierItemMappingDto input, int tenantId)
        {
            if (input.ProductId == Guid.Empty)
                throw new UserFriendlyException("Raw material is required");
            if (input.SupplierLedgerId == Guid.Empty)
                throw new UserFriendlyException("Supplier is required");
            if (input.UnitId == Guid.Empty)
                throw new UserFriendlyException("Unit is required");
            if (input.Rate < 0)
                throw new UserFriendlyException("Supplier rate cannot be negative");
            if (input.MinimumOrderQty < 0)
                throw new UserFriendlyException("Minimum order quantity cannot be negative");

            var product = await productRepository.FirstOrDefaultAsync(x =>
                x.Id == input.ProductId && x.TenantId == tenantId && !x.IsDeleted);
            if (product == null || product.ProductType != ProductTypeEnum.RawMaterial)
                throw new UserFriendlyException("Supplier mappings can only be created for raw materials");

            if (await accountLedgerRepository.CountAsync(x => x.Id == input.SupplierLedgerId && x.TenantId == tenantId) == 0)
                throw new UserFriendlyException("Supplier ledger not found");

            if (await unitRepository.CountAsync(x => x.Id == input.UnitId && x.TenantId == tenantId) == 0)
                throw new UserFriendlyException("Unit not found");

            if (input.IsPreferred && input.IsActive)
            {
                var currentId = input.Id ?? Guid.Empty;
                var alreadyPreferred = await supplierItemMappingRepository.GetAll().AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.Id != currentId &&
                    x.ProductId == input.ProductId &&
                    x.IsPreferred &&
                    x.IsActive &&
                    !x.IsDeleted);
                if (alreadyPreferred)
                    throw new UserFriendlyException("Only one active preferred supplier is allowed per raw material");
            }
        }

        private async Task<Product> ValidateAdjustmentLine(CreateRestaurantStockAdjustmentLineDto input, int tenantId)
        {
            if (input.ProductId == Guid.Empty)
                throw new UserFriendlyException("Raw material is required on every adjustment line");
            if (input.UnitId == Guid.Empty)
                throw new UserFriendlyException("Unit is required on every adjustment line");
            if (input.Rate < 0)
                throw new UserFriendlyException("Rate cannot be negative");

            var product = await productRepository.FirstOrDefaultAsync(x =>
                x.Id == input.ProductId && x.TenantId == tenantId && !x.IsDeleted);
            if (product == null || product.ProductType != ProductTypeEnum.RawMaterial)
                throw new UserFriendlyException("Stock adjustments can only be posted for raw materials");

            if (await unitRepository.CountAsync(x => x.Id == input.UnitId && x.TenantId == tenantId) == 0)
                throw new UserFriendlyException("Adjustment unit not found");

            return product;
        }

        private static decimal GetMovementQty(RestaurantStockAdjustmentType adjustmentType, CreateRestaurantStockAdjustmentLineDto line, decimal systemQty)
        {
            return adjustmentType switch
            {
                RestaurantStockAdjustmentType.Increase => line.Qty,
                RestaurantStockAdjustmentType.Decrease => -line.Qty,
                RestaurantStockAdjustmentType.Wastage => -line.Qty,
                RestaurantStockAdjustmentType.PhysicalCount => (line.CountedQty ?? line.Qty) - systemQty,
                _ => 0
            };
        }

        private static StockMaintainTypeEnum GetMovementType(RestaurantStockAdjustmentType adjustmentType, decimal movementQty)
        {
            return adjustmentType == RestaurantStockAdjustmentType.PhysicalCount
                ? movementQty >= 0 ? StockMaintainTypeEnum.Inward : StockMaintainTypeEnum.Outward
                : adjustmentType == RestaurantStockAdjustmentType.Increase
                    ? StockMaintainTypeEnum.Inward
                    : StockMaintainTypeEnum.Outward;
        }

        private static string GetVoucherTypeName(RestaurantStockAdjustmentType adjustmentType)
        {
            return adjustmentType switch
            {
                RestaurantStockAdjustmentType.Increase => "StockReceipt",
                RestaurantStockAdjustmentType.Decrease => "StockIssue",
                RestaurantStockAdjustmentType.Wastage => "StockIssue",
                RestaurantStockAdjustmentType.PhysicalCount => "PhysicalStock",
                _ => "StockJournal"
            };
        }

        private async Task EnsureOutwardAllowed(Guid productId, Guid unitId, decimal qty, string productName)
        {
            var availableQty = await materialStockPostingService.GetAvailableStockAsync(productId, unitId);
            if (qty <= availableQty || !await ShouldBlockNegativeStockAsync())
                return;

            throw new UserFriendlyException("Insufficient stock", $"{productName} requires {qty}, available {availableQty}");
        }

        private async Task<bool> ShouldBlockNegativeStockAsync()
        {
            var status = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.NegativeStockStatus,
                AbpSession.GetTenantId());
            return string.Equals(status, "Block", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int> GetNextAdjustmentVoucherNumbering(Guid voucherTypeId)
        {
            var existing = await stockAdjustmentRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.FinancialYearId == FinancialYearId &&
                            x.VoucherTypeId == voucherTypeId)
                .Select(x => x.VoucherNumbering)
                .ToListAsync();

            if (existing.Count > 0)
                return existing.Max() + 1;

            var voucherTypeName = await VoucherTypeManager.GetVoucherTypeName(voucherTypeId);
            var numbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, voucherTypeName);
            return numbering.StartIndex;
        }

        private async Task<string> GetAdjustmentVoucherNo(string voucherTypeName, int voucherNumbering)
        {
            var numbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, voucherTypeName);
            return $"{numbering.Prefix}{voucherNumbering}{numbering.Postfix}";
        }

        private class RecipeCoverageLine
        {
            public Guid ProductId { get; set; }
            public decimal Quantity { get; set; }
            public decimal WastagePercentage { get; set; }
            public decimal CostRate { get; set; }
            public decimal PurchaseRate { get; set; }
        }
    }
}
