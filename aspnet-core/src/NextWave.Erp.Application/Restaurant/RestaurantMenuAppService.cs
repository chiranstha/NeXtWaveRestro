using Abp.Authorization;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantMenu)]
    public class RestaurantMenuAppService(
        IRepository<RestaurantMenuCategory, Guid> categoryRepository,
        IRepository<RestaurantMenuItem, Guid> menuItemRepository,
        IRepository<RestaurantMenuVariant, Guid> variantRepository,
        IRepository<RestaurantModifierGroup, Guid> modifierGroupRepository,
        IRepository<RestaurantModifier, Guid> modifierRepository,
        IRepository<RestaurantMenuItemModifierGroup, Guid> menuItemModifierGroupRepository,
        IRepository<RestaurantMenuItemTag, Guid> menuItemTagRepository,
        IRepository<RestaurantStation, Guid> stationRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Bom, Guid> bomRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<RestaurantChangeLog, Guid> changeLogRepository)
        : ErpAppServiceBase, IRestaurantMenuAppService
    {
        public async Task<RestaurantMenuEditorDataDto> GetMenuEditorData()
        {
            return new RestaurantMenuEditorDataDto
            {
                Categories = await GetCategories(),
                MenuItems = await GetMenuItems(null),
                ModifierGroups = await GetModifierGroupsInternal(),
                Stations = await stationRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .Select(x => new RestaurantStationDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StationType = x.StationType,
                        IsActive = x.IsActive
                    }).ToListAsync()
            };
        }

        public async Task<List<UniversalDropdownDto>> GetMenuProducts()
        {
            var tenantId = AbpSession.GetTenantId();
            return await productRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            (x.ProductType == ProductTypeEnum.Product ||
                             x.ProductType == ProductTypeEnum.KitchenItem) &&
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

        public async Task<List<RestaurantMenuCategoryDto>> GetCategories()
        {
            return await categoryRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new RestaurantMenuCategoryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuCreate, AppPermissions.PagesRestaurantMenuEdit)]
        public async Task<Guid> CreateOrEditCategory(CreateOrEditRestaurantMenuCategoryDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Menu category name is required");

            var tenantId = AbpSession.GetTenantId();
            RestaurantMenuCategory category;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                category = new RestaurantMenuCategory { TenantId = tenantId };
                await categoryRepository.InsertAsync(category);
            }
            else
            {
                category = await categoryRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (category == null) throw new UserFriendlyException("Menu category not found");
            }

            category.Name = input.Name.Trim();
            category.Description = input.Description;
            category.SortOrder = input.SortOrder;
            category.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.MenuCategory, category.Id, new { category.Name, category.SortOrder, category.IsActive });
            return category.Id;
        }

        public async Task<List<RestaurantMenuItemDto>> GetMenuItems(Guid? categoryId)
        {
            return await GetMenuItemsInternal(categoryId, null, false);
        }

        public async Task<List<RestaurantMenuItemDto>> GetPosMenu(Guid? categoryId, string search)
        {
            return await GetMenuItemsInternal(categoryId, search, true);
        }

        private async Task<List<RestaurantMenuItemDto>> GetMenuItemsInternal(Guid? categoryId, string search, bool onlyActive)
        {
            var tenantId = AbpSession.TenantId;
            var recipeProductIds = await bomRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted && x.IsActive)
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();

            var query = menuItemRepository.GetAll()
                .Include(x => x.CategoryFk)
                .Include(x => x.ProductFk)
                .Include(x => x.StationFk)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Where(x => x.ProductFk != null &&
                            (x.ProductFk.ProductType == ProductTypeEnum.Product ||
                             x.ProductFk.ProductType == ProductTypeEnum.KitchenItem))
                .Where(x => !categoryId.HasValue || x.CategoryId == categoryId);

            if (onlyActive)
                query = query.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.DisplayName.Contains(term) ||
                    x.ProductFk.Name.Contains(term) ||
                    x.CategoryFk.Name.Contains(term) ||
                    x.ShortCode.Contains(term));
            }

            var items = await query
                .OrderBy(x => x.CategoryFk.SortOrder)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();

            var result = new List<RestaurantMenuItemDto>();
            foreach (var item in items)
            {
                result.Add(new RestaurantMenuItemDto
                {
                    Id = item.Id,
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryFk.Name,
                    ProductId = item.ProductId,
                    ProductName = item.ProductFk.Name,
                    ProductType = item.ProductFk.ProductType,
                    StationId = item.StationId,
                    StationName = item.StationFk == null ? "" : item.StationFk.Name,
                    DisplayName = item.DisplayName,
                    ShortCode = item.ShortCode,
                    Description = item.Description,
                    ColorHex = item.ColorHex,
                    ImageUrl = item.ImageUrl,
                    Price = item.Price,
                    PreparationMinutes = item.PreparationMinutes,
                    SortOrder = item.SortOrder,
                    IsAvailable = item.IsAvailable,
                    UnavailableUntil = item.UnavailableUntil,
                    IsVeg = item.IsVeg,
                    SpiceLevel = item.SpiceLevel,
                    IsFeatured = item.IsFeatured,
                    HasVariants = item.HasVariants,
                    HasModifiers = item.HasModifiers,
                    IsActive = item.IsActive,
                    HasRecipe = recipeProductIds.Contains(item.ProductId),
                    Variants = await GetVariantsForMenuItem(item.Id),
                    ModifierGroups = await GetModifierGroupsInternal(item.Id),
                    Tags = await GetTagsForMenuItem(item.Id)
                });
            }

            return result;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuCreate, AppPermissions.PagesRestaurantMenuEdit)]
        public async Task<Guid> CreateOrEditMenuItem(CreateOrEditRestaurantMenuItemDto input)
        {
            if (input.ProductId == Guid.Empty)
                throw new UserFriendlyException("Product is required");

            var tenantId = AbpSession.GetTenantId();
            var product = await productRepository.FirstOrDefaultAsync(x => x.Id == input.ProductId && x.TenantId == tenantId);
            if (product == null) throw new UserFriendlyException("Product not found");
            if (!IsRestaurantMenuProductType(product.ProductType))
                throw new UserFriendlyException("Only stock items and kitchen items can be added to the restaurant menu");

            if (await categoryRepository.CountAsync(x => x.Id == input.CategoryId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Menu category not found");

            if (input.StationId.HasValue &&
                await stationRepository.CountAsync(x => x.Id == input.StationId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Restaurant station not found");

            RestaurantMenuItem menuItem;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                menuItem = new RestaurantMenuItem { TenantId = tenantId };
                await menuItemRepository.InsertAsync(menuItem);
            }
            else
            {
                menuItem = await menuItemRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (menuItem == null) throw new UserFriendlyException("Menu item not found");
            }

            menuItem.CategoryId = input.CategoryId;
            menuItem.ProductId = input.ProductId;
            menuItem.StationId = input.StationId;
            menuItem.DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? product.Name : input.DisplayName.Trim();
            menuItem.ShortCode = input.ShortCode?.Trim();
            menuItem.Description = input.Description;
            menuItem.ColorHex = input.ColorHex;
            menuItem.ImageUrl = input.ImageUrl;
            menuItem.Price = input.Price > 0 ? input.Price : product.SalesRate;
            menuItem.PreparationMinutes = input.PreparationMinutes;
            menuItem.SortOrder = input.SortOrder;
            menuItem.IsAvailable = input.IsAvailable;
            menuItem.UnavailableUntil = input.UnavailableUntil;
            menuItem.IsVeg = input.IsVeg;
            menuItem.SpiceLevel = input.SpiceLevel;
            menuItem.IsFeatured = input.IsFeatured;
            menuItem.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.MenuItem, menuItem.Id, new { menuItem.DisplayName, menuItem.Price, menuItem.IsAvailable, menuItem.IsActive });
            return menuItem.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuVariants)]
        public async Task<Guid> CreateOrEditVariant(CreateOrEditRestaurantMenuVariantDto input)
        {
            if (input.MenuItemId == Guid.Empty)
                throw new UserFriendlyException("Menu item is required");
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Variant name is required");

            var tenantId = AbpSession.GetTenantId();
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
            if (menuItem == null)
                throw new UserFriendlyException("Menu item not found");

            RestaurantMenuVariant variant;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                variant = new RestaurantMenuVariant { TenantId = tenantId, MenuItemId = input.MenuItemId };
                await variantRepository.InsertAsync(variant);
            }
            else
            {
                variant = await variantRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (variant == null)
                    throw new UserFriendlyException("Variant not found");
            }

            variant.Name = input.Name.Trim();
            variant.PriceDelta = input.PriceDelta;
            variant.IsAbsolutePrice = input.IsAbsolutePrice;
            variant.IsDefault = input.IsDefault;
            variant.SortOrder = input.SortOrder;
            variant.IsActive = input.IsActive;
            variant.IsDeleted = false;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RefreshMenuFlags(input.MenuItemId);
            await RecordSyncChange(RestaurantSyncEntityType.MenuVariant, variant.Id, new { variant.MenuItemId, variant.Name, variant.PriceDelta, variant.IsActive });
            return variant.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuVariants)]
        public async Task DeleteVariant(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.GetTenantId();
            var variant = await variantRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
            if (variant == null)
                return;

            variant.IsDeleted = true;
            variant.IsActive = false;
            await variantRepository.UpdateAsync(variant);
            await CurrentUnitOfWork.SaveChangesAsync();
            await RefreshMenuFlags(variant.MenuItemId);
            await RecordSyncChange(RestaurantSyncEntityType.MenuVariant, variant.Id, new { variant.MenuItemId, variant.Name }, RestaurantSyncOperation.Delete);
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuModifiers)]
        public async Task<Guid> CreateOrEditModifierGroup(CreateOrEditRestaurantModifierGroupDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Modifier group name is required");
            if (input.MaxSelect < input.MinSelect)
                throw new UserFriendlyException("Max select cannot be less than min select");

            var tenantId = AbpSession.GetTenantId();
            RestaurantModifierGroup group;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                group = new RestaurantModifierGroup { TenantId = tenantId };
                await modifierGroupRepository.InsertAsync(group);
            }
            else
            {
                group = await modifierGroupRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (group == null)
                    throw new UserFriendlyException("Modifier group not found");
            }

            group.Name = input.Name.Trim();
            group.MinSelect = input.MinSelect;
            group.MaxSelect = input.MaxSelect;
            group.IsRequired = input.IsRequired;
            group.SortOrder = input.SortOrder;
            group.IsActive = input.IsActive;
            group.IsDeleted = false;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.ModifierGroup, group.Id, new { group.Name, group.MinSelect, group.MaxSelect, group.IsRequired, group.IsActive });
            return group.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuModifiers)]
        public async Task<Guid> CreateOrEditModifier(CreateOrEditRestaurantModifierDto input)
        {
            if (input.ModifierGroupId == Guid.Empty)
                throw new UserFriendlyException("Modifier group is required");
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new UserFriendlyException("Modifier name is required");

            var tenantId = AbpSession.GetTenantId();
            if (await modifierGroupRepository.CountAsync(x => x.Id == input.ModifierGroupId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                throw new UserFriendlyException("Modifier group not found");

            RestaurantModifier modifier;
            if (!input.Id.HasValue || input.Id == Guid.Empty)
            {
                modifier = new RestaurantModifier { TenantId = tenantId, ModifierGroupId = input.ModifierGroupId };
                await modifierRepository.InsertAsync(modifier);
            }
            else
            {
                modifier = await modifierRepository.FirstOrDefaultAsync(x => x.Id == input.Id && x.TenantId == tenantId);
                if (modifier == null)
                    throw new UserFriendlyException("Modifier not found");
            }

            modifier.Name = input.Name.Trim();
            modifier.PriceDelta = input.PriceDelta;
            modifier.SortOrder = input.SortOrder;
            modifier.IsActive = input.IsActive;
            modifier.IsDeleted = false;

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.Modifier, modifier.Id, new { modifier.ModifierGroupId, modifier.Name, modifier.PriceDelta, modifier.IsActive });
            return modifier.Id;
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenuModifiers)]
        public async Task SaveMenuItemModifierGroups(SaveRestaurantMenuItemModifierGroupsDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
            if (menuItem == null)
                throw new UserFriendlyException("Menu item not found");

            var groupIds = input.ModifierGroupIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            var existing = await menuItemModifierGroupRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.MenuItemId == input.MenuItemId)
                .ToListAsync();

            foreach (var link in existing.Where(x => !groupIds.Contains(x.ModifierGroupId)))
                await menuItemModifierGroupRepository.DeleteAsync(link);

            var existingGroupIds = existing.Select(x => x.ModifierGroupId).ToHashSet();
            for (var i = 0; i < groupIds.Count; i++)
            {
                var groupId = groupIds[i];
                if (await modifierGroupRepository.CountAsync(x => x.Id == groupId && x.TenantId == tenantId && !x.IsDeleted) == 0)
                    throw new UserFriendlyException("Modifier group not found");

                if (existingGroupIds.Contains(groupId))
                    continue;

                await menuItemModifierGroupRepository.InsertAsync(new RestaurantMenuItemModifierGroup
                {
                    TenantId = tenantId,
                    MenuItemId = input.MenuItemId,
                    ModifierGroupId = groupId,
                    SortOrder = i
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await RefreshMenuFlags(input.MenuItemId);
            await RecordSyncChange(RestaurantSyncEntityType.MenuItem, input.MenuItemId, new { input.MenuItemId, ModifierGroupIds = groupIds });
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantMenu)]
        public async Task SaveMenuItemTags(SaveRestaurantMenuItemTagsDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x => x.Id == input.MenuItemId && x.TenantId == tenantId && !x.IsDeleted);
            if (menuItem == null)
                throw new UserFriendlyException("Menu item not found");

            var existing = await menuItemTagRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.MenuItemId == input.MenuItemId)
                .ToListAsync();

            foreach (var tag in existing)
                await menuItemTagRepository.DeleteAsync(tag);

            var tags = input.Tags?
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select((x, index) => new
                {
                    Name = x.Name.Trim(),
                    ColorHex = string.IsNullOrWhiteSpace(x.ColorHex) ? "#eef6ff" : x.ColorHex.Trim(),
                    SortOrder = x.SortOrder == 0 ? index : x.SortOrder
                })
                .ToList() ?? [];

            foreach (var tag in tags)
            {
                await menuItemTagRepository.InsertAsync(new RestaurantMenuItemTag
                {
                    TenantId = tenantId,
                    MenuItemId = input.MenuItemId,
                    Name = tag.Name,
                    ColorHex = tag.ColorHex,
                    SortOrder = tag.SortOrder
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.MenuItem, input.MenuItemId, new { input.MenuItemId, Tags = tags });
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantItemAvailability)]
        public async Task SetItemAvailability(SetRestaurantMenuItemAvailabilityDto input)
        {
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x =>
                x.Id == input.MenuItemId &&
                x.TenantId == AbpSession.TenantId &&
                !x.IsDeleted);
            if (menuItem == null)
                throw new UserFriendlyException("Menu item not found");

            menuItem.IsAvailable = input.IsAvailable;
            menuItem.UnavailableUntil = input.IsAvailable ? null : input.UnavailableUntil;
            await menuItemRepository.UpdateAsync(menuItem);
            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.MenuItem, menuItem.Id, new { menuItem.DisplayName, menuItem.IsAvailable, menuItem.UnavailableUntil });
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantRecipe)]
        public async Task<List<RestaurantRecipeLineDto>> GetRecipe(Guid productId)
        {
            return await bomRepository.GetAll()
                .Include(x => x.RawMaterialFk)
                .Include(x => x.UnitFk)
                .Where(x => x.TenantId == AbpSession.TenantId && x.ProductId == productId && !x.IsDeleted)
                .OrderBy(x => x.RawMaterialFk.Name)
                .Select(x => new RestaurantRecipeLineDto
                {
                    Id = x.Id,
                    RawMaterialId = x.RawMaterialId,
                    RawMaterialName = x.RawMaterialFk.Name,
                    Quantity = x.Quantity,
                    UnitId = x.UnitId,
                    UnitName = x.UnitFk.Name,
                    WastagePercentage = x.WastagePercentage,
                    CostRate = x.CostRate,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantRecipe)]
        public async Task SaveRecipe(SaveRestaurantRecipeDto input)
        {
            var tenantId = AbpSession.GetTenantId();
            var product = await productRepository.FirstOrDefaultAsync(x => x.Id == input.ProductId && x.TenantId == tenantId);
            if (product == null) throw new UserFriendlyException("Recipe product not found");
            if (product.ProductType == ProductTypeEnum.Services)
                throw new UserFriendlyException("Service products cannot consume recipe stock");

            input.Lines ??= new List<RestaurantRecipeLineDto>();
            await ValidateRecipeLines(input, tenantId);

            var inputIds = input.Lines.Where(x => x.Id.HasValue && x.Id != Guid.Empty).Select(x => x.Id.Value).ToList();
            var existing = await bomRepository.GetAll()
                .Where(x => x.TenantId == tenantId && x.ProductId == input.ProductId)
                .ToListAsync();

            foreach (var oldLine in existing.Where(x => !inputIds.Contains(x.Id)))
            {
                oldLine.IsDeleted = true;
                oldLine.IsActive = false;
                await bomRepository.UpdateAsync(oldLine);
            }

            foreach (var line in input.Lines)
            {
                Bom bom;
                if (line.Id.HasValue && line.Id != Guid.Empty)
                {
                    bom = existing.FirstOrDefault(x => x.Id == line.Id);
                    if (bom == null) throw new UserFriendlyException("Recipe line not found");
                }
                else
                {
                    bom = new Bom
                    {
                        TenantId = tenantId,
                        ProductId = input.ProductId,
                        Date = DateTime.UtcNow
                    };
                    await bomRepository.InsertAsync(bom);
                }

                bom.RawMaterialId = line.RawMaterialId;
                bom.Quantity = line.Quantity;
                bom.UnitId = line.UnitId;
                bom.WastagePercentage = line.WastagePercentage;
                bom.CostRate = line.CostRate;
                bom.IsActive = line.IsActive;
                bom.IsDeleted = false;
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await RecordSyncChange(RestaurantSyncEntityType.MenuItem, input.ProductId, new { Entity = "Recipe", input.ProductId, LineCount = input.Lines.Count });
        }

        private async Task ValidateRecipeLines(SaveRestaurantRecipeDto input, int tenantId)
        {
            var activeKeys = new HashSet<string>();

            foreach (var line in input.Lines)
            {
                if (line.RawMaterialId == Guid.Empty)
                    throw new UserFriendlyException("Raw material is required on every recipe line");
                if (line.RawMaterialId == input.ProductId)
                    throw new UserFriendlyException("Recipe product cannot be used as its own raw material");
                if (line.UnitId == Guid.Empty)
                    throw new UserFriendlyException("Recipe unit is required on every recipe line");
                if (line.Quantity <= 0)
                    throw new UserFriendlyException("Recipe quantity must be greater than zero");
                if (line.WastagePercentage < 0)
                    throw new UserFriendlyException("Recipe wastage cannot be negative");
                if (line.CostRate < 0)
                    throw new UserFriendlyException("Recipe cost cannot be negative");

                var rawMaterial = await productRepository.FirstOrDefaultAsync(x =>
                    x.Id == line.RawMaterialId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);
                if (rawMaterial == null)
                    throw new UserFriendlyException("Raw material not found");
                if (rawMaterial.ProductType != ProductTypeEnum.RawMaterial)
                    throw new UserFriendlyException("Recipe lines must use raw material products");

                if (await unitRepository.CountAsync(x => x.Id == line.UnitId && x.TenantId == tenantId) == 0)
                    throw new UserFriendlyException("Recipe unit not found");

                if (!line.IsActive)
                    continue;

                var key = $"{line.RawMaterialId:N}:{line.UnitId:N}";
                if (!activeKeys.Add(key))
                    throw new UserFriendlyException("Duplicate active raw material/unit recipe lines are not allowed");
            }
        }

        [AbpAuthorize(AppPermissions.PagesRestaurantRecipe)]
        public async Task<RestaurantRecipeCostDto> GetRecipeCost(Guid productId)
        {
            var product = await productRepository.FirstOrDefaultAsync(x => x.Id == productId && x.TenantId == AbpSession.TenantId);
            if (product == null) throw new UserFriendlyException("Recipe product not found");

            var lines = await GetRecipe(productId);
            foreach (var line in lines.Where(x => x.CostRate <= 0))
            {
                var rawMaterial = await productRepository.FirstOrDefaultAsync(line.RawMaterialId);
                line.CostRate = rawMaterial?.PurchaseRate ?? 0;
            }

            return new RestaurantRecipeCostDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Lines = lines,
                TotalCost = lines
                    .Where(x => x.IsActive)
                    .Sum(x => x.Quantity * (1 + x.WastagePercentage / 100) * x.CostRate)
            };
        }

        private async Task<List<RestaurantMenuVariantDto>> GetVariantsForMenuItem(Guid menuItemId)
        {
            return await variantRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId &&
                            x.MenuItemId == menuItemId &&
                            !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new RestaurantMenuVariantDto
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    Name = x.Name,
                    PriceDelta = x.PriceDelta,
                    IsAbsolutePrice = x.IsAbsolutePrice,
                    IsDefault = x.IsDefault,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        private async Task<List<RestaurantMenuItemTagDto>> GetTagsForMenuItem(Guid menuItemId)
        {
            return await menuItemTagRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.MenuItemId == menuItemId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new RestaurantMenuItemTagDto
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    Name = x.Name,
                    ColorHex = x.ColorHex,
                    SortOrder = x.SortOrder
                }).ToListAsync();
        }

        private async Task<List<RestaurantModifierGroupDto>> GetModifierGroupsInternal(Guid? menuItemId = null)
        {
            var query = modifierGroupRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && !x.IsDeleted);

            if (menuItemId.HasValue)
            {
                var groupIds = await menuItemModifierGroupRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId && x.MenuItemId == menuItemId.Value)
                    .Select(x => x.ModifierGroupId)
                    .ToListAsync();
                query = query.Where(x => groupIds.Contains(x.Id));
            }

            var groups = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new RestaurantModifierGroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    MinSelect = x.MinSelect,
                    MaxSelect = x.MaxSelect,
                    IsRequired = x.IsRequired,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive
                }).ToListAsync();

            foreach (var group in groups)
            {
                group.Modifiers = await modifierRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId &&
                                x.ModifierGroupId == group.Id &&
                                !x.IsDeleted)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new RestaurantModifierDto
                    {
                        Id = x.Id,
                        ModifierGroupId = x.ModifierGroupId,
                        Name = x.Name,
                        PriceDelta = x.PriceDelta,
                        SortOrder = x.SortOrder,
                        IsActive = x.IsActive
                    }).ToListAsync();
            }

            return groups;
        }

        private async Task RefreshMenuFlags(Guid menuItemId)
        {
            var menuItem = await menuItemRepository.FirstOrDefaultAsync(x =>
                x.Id == menuItemId &&
                x.TenantId == AbpSession.TenantId);
            if (menuItem == null)
                return;

            menuItem.HasVariants = await variantRepository.CountAsync(x =>
                x.TenantId == AbpSession.TenantId &&
                x.MenuItemId == menuItemId &&
                x.IsActive &&
                !x.IsDeleted) > 0;

            menuItem.HasModifiers = await menuItemModifierGroupRepository.CountAsync(x =>
                x.TenantId == AbpSession.TenantId &&
                x.MenuItemId == menuItemId) > 0;

            await menuItemRepository.UpdateAsync(menuItem);
        }

        private static bool IsRestaurantMenuProductType(ProductTypeEnum productType)
        {
            return productType is ProductTypeEnum.Product or ProductTypeEnum.KitchenItem;
        }

        private async Task RecordSyncChange(
            RestaurantSyncEntityType entityType,
            Guid entityId,
            object payload,
            RestaurantSyncOperation operation = RestaurantSyncOperation.Upsert)
        {
            var tenantId = AbpSession.GetTenantId();
            var lastSeq = await changeLogRepository.GetAll()
                .Where(x => x.TenantId == tenantId)
                .Select(x => (long?)x.Seq)
                .MaxAsync() ?? 0;

            await changeLogRepository.InsertAsync(new RestaurantChangeLog
            {
                TenantId = tenantId,
                Seq = lastSeq + 1,
                EntityType = entityType,
                Operation = operation,
                EntityId = entityId.ToString(),
                PayloadJson = JsonSerializer.Serialize(payload),
                ChangedAt = DateTime.Now
            });
        }
    }
}
