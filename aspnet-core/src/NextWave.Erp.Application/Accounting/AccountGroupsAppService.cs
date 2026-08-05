using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.IO.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Accounting.Exporting;
using NextWave.Erp.Accounting.Importing;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.Accounting
{
    [Audited]
    [AbpAuthorize(AppPermissions.PagesAccountGroups)]
    public class AccountGroupsAppService(
     IAccountGroupsExcelExporter accountGroupsExcelExporter,
     IRepository<AccountGroup, Guid> accountGroupRepository,
     IRepository<AccountLedger, Guid> accountLedgerRepository,
     IBinaryObjectManager binaryObjectManager,
     IBackgroundJobManager backgroundJobManager)
     : ErpAppServiceBase, IAccountGroupsAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        [DisableAuditing]
        public async Task<PagedResultDto<GetAccountGroupForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredAccountGroups = accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.GetTenantId())
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter.Trim()));

            var pagedAndFilteredAccountGroups = filteredAccountGroups
                .OrderBy(string.IsNullOrWhiteSpace(input.Sorting) ? "id desc" : input.Sorting)
                .PageBy(input);

            var accountGroups = from o in pagedAndFilteredAccountGroups
                                join o2 in accountGroupRepository.GetAll().AsNoTracking() on o.GroupUnder equals o2.Id into j2
                                from s2 in j2.DefaultIfEmpty()
                                select new GetAccountGroupForViewDto
                                {
                                    Name = o.Name,
                                    Nature = o.Nature,
                                    Id = o.Id,
                                    AccountGroupId = o.Id,
                                    IsDefault = o.IsDefault,
                                    NatureName = o.Nature.ToString(),
                                    AffectGrossProfit = o.AffectGrossProfit,
                                    Narration = o.Narration,
                                    AccountGroupName = s2 == null || s2.Name == null ? "" : s2.Name
                                };

            var totalCount = await filteredAccountGroups.CountAsync();

            return new PagedResultDto<GetAccountGroupForViewDto>(
                totalCount,
                await accountGroups.ToListAsync()
            );
        }

        public async Task<GetAccountGroupForViewDto> GetAccountGroupForView(Guid id)
        {
            var accountGroup = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Include(e => e.AccountGroupFk)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (accountGroup == null)
            {
                throw new UserFriendlyException("AccountGroup not found.");
            }

            var output = new GetAccountGroupForViewDto
            {
                Id = accountGroup.Id,
                Name = accountGroup.Name,
                Narration = accountGroup.Narration,
                IsDefault = accountGroup.IsDefault,
                Nature = accountGroup.Nature,
                AccountGroupId = accountGroup.GroupUnder ?? Guid.Empty,
                AccountGroupName = accountGroup.AccountGroupFk?.Name ?? ""
            };

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesAccountGroupsEdit)]
        public async Task<GetAccountGroupForEditOutput> GetAccountGroupForEdit(EntityDto<Guid> input)
        {
            var accountGroup = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Include(e => e.AccountGroupFk)
                .FirstOrDefaultAsync(e => e.Id == input.Id);

            if (accountGroup == null)
            {
                throw new UserFriendlyException("AccountGroup not found.");
            }

            var output = new GetAccountGroupForEditOutput
            {
                Id = accountGroup.Id,
                Name = accountGroup.Name,
                Narration = accountGroup.Narration,
                AffectGrossProfit = accountGroup.AffectGrossProfit,
                Nature = accountGroup.Nature,
                GroupUnder = accountGroup.GroupUnder ?? Guid.Empty,
                GroupUnderName = accountGroup.AccountGroupFk?.Name ?? ""
            };


            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditAccountGroupDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesAccountGroupsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var tenantId = AbpSession.TenantId;

            var isDefault = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == input.Id)
                .Select(x => x.IsDefault)
                .FirstOrDefaultAsync();

            if (isDefault)
                throw new UserFriendlyException("Default AccountGroup cannot be Deleted.");

            if (await accountLedgerRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(x => x.TenantId == tenantId && x.AccountGroupId == input.Id))
            {
                throw new UserFriendlyException("Account Ledger of this account group already Exists.");
            }

            if (await accountGroupRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(x => x.TenantId == tenantId && x.GroupUnder == input.Id))
            {
                throw new UserFriendlyException("Children of this account group Exists.");
            }

            await accountGroupRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetAccountGroupsToExcel(GetAllUniversalInput input)
        {
            var tenantId = AbpSession.GetTenantId();

            var filteredAccountGroups = accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter.Trim()));

            var query = from o in filteredAccountGroups
                        join o2 in accountGroupRepository.GetAll().AsNoTracking().Where(x => x.TenantId == tenantId) on o.GroupUnder equals o2.Id into j2
                        from s2 in j2.DefaultIfEmpty()
                        select new GetAccountGroupForViewDto
                        {
                            Name = o.Name,
                            Narration = o.Narration,
                            IsDefault = o.IsDefault,
                            AffectGrossProfit = o.AffectGrossProfit,
                            Nature = o.Nature,
                            Id = o.Id,
                            AccountGroupName = s2 == null || s2.Name == null ? "" : s2.Name
                        };

            var accountGroupListDtos = await query.ToListAsync();

            return accountGroupsExcelExporter.ExportToFile(accountGroupListDtos);
        }

       
        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesAccountGroups)]
        public async Task<List<AccountGroupsWithNatureDto>> GetAllAccountGroupForTableDropdown()
        {
            return await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.GetTenantId())
                .OrderBy(x => x.Name)
                .Select(accountGroup => new AccountGroupsWithNatureDto
                {
                    Id = accountGroup.Id,
                    DisplayName = accountGroup.Name == null ? "" : accountGroup.Name.ToString(),
                    Nature = accountGroup.Nature,
                    AffectGrossProfit = accountGroup.AffectGrossProfit
                }).ToListAsync();
        }

        public async Task ImportAccountGroupFromExcel(IFormFile file)
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
            var fileObject = new BinaryObject(tenantId, fileBytes, $"{DateTime.UtcNow} import from excel file.");

            await BinaryObjectManager.SaveAsync(fileObject);

            await BackgroundJobManager.EnqueueAsync<ImportAccountGroupsToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                    //     User = AbpSession.ToUserIdentifier()
                });
        }

        [DisableAuditing]
        public async Task<List<UniversalDropdownDto>> GetAllExplicitAccountGroupForTableDropdown(
            Guid groupId)
        {
            if (groupId == Guid.Empty)
            {
                return await accountGroupRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.TenantId == AbpSession.GetTenantId())
                    .OrderBy(x => x.Name)
                    .Select(accountGroup => new UniversalDropdownDto
                    {
                        Id = accountGroup.Id,
                        DisplayName = accountGroup.Name == null ? "" : accountGroup.Name.ToString()
                    }).ToListAsync();
            }

            var accountGroupLookupItems = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == AbpSession.GetTenantId())
                .Select(x => new AccountGroupLookupItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    GroupUnder = x.GroupUnder
                })
                .ToListAsync();

            var childrenGroupIds = GetChildGroupIds(accountGroupLookupItems, groupId);

            var accountGroups = accountGroupLookupItems
                .Where(x => !childrenGroupIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                })
                .ToList();

            return accountGroups;
        }

        private static HashSet<Guid> GetChildGroupIds(IEnumerable<AccountGroupLookupItem> accountGroups, Guid id)
        {
            var childrenLookup = accountGroups
                .Where(x => x.GroupUnder != null)
                .GroupBy(x => x.GroupUnder.Value)
                .ToDictionary(x => x.Key, x => x.Select(group => group.Id).ToList());

            var groupIds = new HashSet<Guid> { id };
            var pendingGroupIds = new Stack<Guid>();
            pendingGroupIds.Push(id);

            while (pendingGroupIds.Count > 0)
            {
                var currentGroupId = pendingGroupIds.Pop();

                if (!childrenLookup.TryGetValue(currentGroupId, out var children))
                {
                    continue;
                }

                foreach (var childId in children.Where(groupIds.Add))
                {
                    pendingGroupIds.Push(childId);
                }
            }

            return groupIds;
        }

        private sealed class AccountGroupLookupItem
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public Guid? GroupUnder { get; set; }
        }

        [AbpAuthorize(AppPermissions.PagesAccountGroupsCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditAccountGroupDto input)
        {
            var tenantId = AbpSession.TenantId;


            var accountGroupExists = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.Name == input.Name);

            if (accountGroupExists) throw new UserFriendlyException("This GroupName is Already Exists");

            if (tenantId != null)
            {
                var accountGroup = new AccountGroup
                {
                    TenantId = tenantId,
                    Name = input.Name,
                    Narration = input.Narration,
                    IsDefault = false,
                    AffectGrossProfit = input.AffectGrossProfit,
                    Nature = input.Nature,
                    GroupUnder = input.GroupUnder
                };
                return await accountGroupRepository.InsertAndGetIdAsync(accountGroup);
            }

            return Guid.Empty;
        }

        [AbpAuthorize(AppPermissions.PagesAccountGroupsEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditAccountGroupDto input)
        {
            var tenantId = AbpSession.TenantId;

            var accountGroupExists = await accountGroupRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Name == input.Name)
                .AnyAsync(x => x.Id != input.Id);

            if (accountGroupExists) throw new UserFriendlyException("This account group name already Exists");


            var accountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

            if (accountGroup == null)
            {
                var data = new AccountGroup
                {
                    TenantId = tenantId,
                    Name = input.Name,
                    Narration = input.Narration,
                    IsDefault = false,
                    AffectGrossProfit = input.AffectGrossProfit,
                    Nature = input.Nature,
                    GroupUnder = input.GroupUnder == Guid.Empty ? null : input.GroupUnder
                };
                await accountGroupRepository.InsertAndGetIdAsync(data);
            }
            else
            {
                if (!accountGroup.IsDefault)
                {
                    accountGroup.Name = input.Name;
                    accountGroup.AffectGrossProfit = input.AffectGrossProfit;
                    accountGroup.Nature = input.Nature;
                    accountGroup.GroupUnder = input.GroupUnder;
                }
                accountGroup.Narration = input.Narration;
                await accountGroupRepository.UpdateAsync(accountGroup);
            }
            return (Guid)input.Id;
        }
    }
}
