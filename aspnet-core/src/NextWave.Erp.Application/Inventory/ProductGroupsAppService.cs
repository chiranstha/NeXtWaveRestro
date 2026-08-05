using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Inventory.Exporting;

namespace NextWave.Erp.Inventory
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesProductGroups)]
    public class ProductGroupsAppService(
        IRepository<ProductGroup, Guid> productGroupRepository,
        IRepository<Product, Guid> productRepository,
        IProductGroupsExcelExporter productGroupsExcelExporter)
        : ErpAppServiceBase, IProductGroupsAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<ProductGroupTreeDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredProductGroups = productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter.Trim()));

            var pagedAndFilteredProductGroups = filteredProductGroups
                .OrderBy(input.Sorting ?? "id desc")
                .PageBy(input);

            var productGroups = pagedAndFilteredProductGroups.Include(x => x.ProductGroupFk).ToList().Select(a =>
                new ProductGroupTreeDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    GroupUnder = a.ProductGroupFk == null ? " " : a.ProductGroupFk.Name
                });

            var totalCount = await filteredProductGroups.CountAsync();

            return new PagedResultDto<ProductGroupTreeDto>(
                totalCount,
                productGroups.ToList()
            );
        }

        public async Task<GetProductGroupForViewDto> GetProductGroupForView(Guid id)
        {
            var productGroup = await productGroupRepository.GetAsync(id);
            var productGroupUnder = await productGroupRepository.FirstOrDefaultAsync(x => x.Id == productGroup.GroupUnder);
            var productGroupUnderString = string.Empty;
            if (productGroupUnder != null) productGroupUnderString = productGroupUnder.Name;
            var output = new GetProductGroupForViewDto
            {
                Name = productGroup.Name,
                GroupUnder = productGroupUnderString,
                Description = productGroup.Description,
                IsDefult = productGroup.IsDefult,
                Id = productGroup.Id
            };
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesProductGroupsEdit)]
        public async Task<GetProductGroupForEditOutput> GetProductGroupForEdit(EntityDto<Guid> input)
        {
            var productGroup = await productGroupRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetProductGroupForEditOutput
            {
                Id = productGroup.Id,
                Name = productGroup.Name,
                GroupUnder = productGroup.GroupUnder,
                Description = productGroup.Description,
                IsDefult = productGroup.IsDefult
            };
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditProductGroupDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
            {
                var id = await Create(input);
                return id;
            }

            await Update(input);
            return (Guid)input.Id;
        }

        [AbpAuthorize(AppPermissions.PagesProductGroupsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            if (await productRepository.CountAsync(x => x.ProductGroupId == input.Id) > 0)
                throw new UserFriendlyException("Product Group Reference is used in product.");
            if (await productGroupRepository.CountAsync(x => x.GroupUnder == input.Id) > 0)
                throw new UserFriendlyException("Child productgroups exists.");
            await productGroupRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetProductGroupsToExcel(GetAllUniversalInput input)
        {
            var filteredProductGroups = productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.Name.Contains(input.Filter) || e.Description.Contains(input.Filter));

            var query = from o in filteredProductGroups
                        select new GetProductGroupForViewDto
                        {
                            Name = o.Name,
                            IsDefult = o.IsDefult,
                            GroupUnder = o.ProductGroupFk == null ? "" : o.ProductGroupFk.Name,
                            Id = o.Id
                        };

            var productGroupListDtos = await query.ToListAsync();

            return productGroupsExcelExporter.ExportToFile(productGroupListDtos);
        }

        public async Task<List<UniversalDropdownDto>> GetProductGroupDropdown()
        {
            var productGroup = await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .ToListAsync();
            var output = productGroup.Select(x => new UniversalDropdownDto
            {
                Id = x.Id,
                DisplayName = IncludeParentName(x)
            }).ToList();
            return output;
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllExplicitProductGroupsForTableDropdown(Guid groupId)
        {
            if (groupId == Guid.Empty)
                return await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Select(accountGroup => new UniversalDropdownDto
                    {
                        Id = accountGroup.Id,
                        DisplayName = accountGroup.Name == null ? "" : accountGroup.Name.ToString()
                    }).ToListAsync();
            if (groupId != Guid.Empty)
            {
                var childrenGroupIds = await FuncRecursive(groupId);
                var accountGroups =
                    (await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => !childrenGroupIds.Contains(x.Id)).ToListAsync()).Select(x => new UniversalDropdownDto
                        {
                            Id = x.Id,
                            DisplayName = x.Name
                        }).ToList();
                return accountGroups;
            }

            return null;
        }

        private async Task<List<Guid>> FuncRecursive(Guid id)
        {
            var result = await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
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

        [AbpAuthorize(AppPermissions.PagesProductGroupsCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditProductGroupDto input)
        {
            if (await productGroupRepository.CountAsync(x => x.Name == input.Name && x.TenantId == AbpSession.TenantId) >
                0)
                throw new UserFriendlyException("Product group name '" + input.Name + "' already exists!");
            var productGroup = new ProductGroup
            {
                Name = input.Name,
                GroupUnder = input.GroupUnder,
                Description = input.Description,
                IsDefult = false,
                TenantId = AbpSession.GetTenantId()
            };

            if (AbpSession.TenantId != null) productGroup.TenantId = AbpSession.TenantId;

            var id = await productGroupRepository.InsertAndGetIdAsync(productGroup);
            return id;
        }

        [AbpAuthorize(AppPermissions.PagesProductGroupsEdit)]
        protected virtual async Task Update(CreateOrEditProductGroupDto input)
        {
            if (input.Id == Guid.Empty || input.Id == null)
            {
                if (await productGroupRepository.CountAsync(x =>
                        x.Id != input.Id && x.Name == input.Name && x.TenantId == AbpSession.TenantId) > 0)
                    throw new UserFriendlyException("Product group name '" + input.Name + "' already exists!");

                if ((await productGroupRepository.FirstOrDefaultAsync(x => x.Id == input.Id)).IsDefult)
                    throw new UserFriendlyException("Primary Group Can't Update!");
                var productGroup = new ProductGroup
                {
                    TenantId = null,
                    Name = input.Name,
                    GroupUnder = input.GroupUnder,
                    Description = input.Description,
                    IsDefult = false
                };
                await productGroupRepository.InsertAsync(productGroup);
            }
            else
            {
                if (await productGroupRepository.CountAsync(x => x.Name == input.Name && x.Id != input.Id) > 0)
                    throw new UserFriendlyException("ProductGroup is Already Exists!");
                var productGroup = await productGroupRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
                productGroup.Name = input.Name;
                productGroup.GroupUnder = input.GroupUnder;
                productGroup.Description = input.Description;
                await productGroupRepository.UpdateAsync(productGroup);
            }
        }

        private string IncludeParentName(ProductGroup category)
        {
            var categoryName = string.Empty;
            if (category == null)
                return categoryName;

            categoryName = category.Name;

            var parentCategory = category.ProductGroupFk;
            while (parentCategory != null)
            {
                categoryName = $"{parentCategory.Name} >> {categoryName}";
                parentCategory = parentCategory.ProductGroupFk;
            }

            return categoryName;
        }

        public async Task<ProductGroupTreeViewDto> GetProductGroupTree(Guid productGroupId)
        {
            if (productGroupId == Guid.Empty)
                productGroupId = (await productGroupRepository.FirstOrDefaultAsync(x => x.Name == "PRIMARY")).Id;
            return await GetProductGroupRecursive(productGroupId);
        }

        private async Task<ProductGroupTreeViewDto> GetProductGroupRecursive(Guid groupId)
        {
            var productGroup = await productGroupRepository.FirstOrDefaultAsync(x => x.Id == groupId);
            var childGroups = await productGroupRepository.GetAll().Where(x => x.GroupUnder == groupId).ToListAsync();

            var result = new ProductGroupTreeViewDto
            {
                Id = groupId
            };
            var childData = new List<ProductGroupTreeViewDto>();
            foreach (var productGroupId in childGroups) childData.Add(await GetProductGroupRecursive(productGroupId.Id));
            childData.AddRange(await GetProductList(groupId));
            var dat = new ProductGroupTreeViewDataDto
            {
                Id = productGroup.Id,
                Name = productGroup.Name,
                IsGroup = true
            };
            result.Data = dat;
            result.Children = childData;
            return result;
        }

        private async Task<List<ProductGroupTreeViewDto>> GetProductList(Guid groupId)
        {
            var result = new List<ProductGroupTreeViewDto>();
            var products = await productRepository.GetAll().Where(x => x.ProductGroupId == groupId).ToListAsync();
            foreach (var product in products)
            {
                var dat = new ProductGroupTreeViewDataDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    IsGroup = false
                };
                result.Add(new ProductGroupTreeViewDto
                {
                    Id = product.Id,
                    Data = dat
                });
            }

            return result;
        }
    }
}
