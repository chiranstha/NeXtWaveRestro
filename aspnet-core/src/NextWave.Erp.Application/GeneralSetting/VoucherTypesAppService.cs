using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;

namespace NextWave.Erp.GeneralSetting
{

    [AbpAuthorize(AppPermissions.PagesVoucherTypes)]
    public class VoucherTypesAppService(
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<VoucherNumbering, Guid> voucherNuberingRepository)
        : ErpAppServiceBase, IVoucherTypesAppService
    {
        public async Task<PagedResultDto<GetVoucherTypeForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredVoucherTypes = voucherNuberingRepository.GetAll()
                .Include(x => x.VoucherTypeFk)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.VoucherTypeFk.Name.Contains(input.Filter.Trim()));

            var pagedAndFilteredVoucherTypes = filteredVoucherTypes
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var voucherTypes = from o in pagedAndFilteredVoucherTypes
                               select new GetVoucherTypeForViewDto
                               {
                                   VoucherGenerateType = o.VoucherGenerateType,
                                   Id = o.Id,
                                   VoucherName = o.VoucherTypeFk.Name,
                                   StartIndex = o.StartingIndex,
                                   Prefix = o.Prefix,
                                   Postfix = o.Postfix
                               };

            var totalCount = await filteredVoucherTypes.CountAsync();

            return new PagedResultDto<GetVoucherTypeForViewDto>(
                totalCount,
                await voucherTypes.ToListAsync()
            );
        }

        public async Task<GetVoucherTypeForViewDto> GetVoucherTypeForView(Guid id)
        {
            var voucherType = await voucherTypeRepository.GetAsync(id);

            var output = new GetVoucherTypeForViewDto();

            return output;
        }

        [AbpAuthorize(AppPermissions.PagesVoucherTypesEdit)]
        public async Task<GetVoucherTypeForEditOutput> GetVoucherTypeForEdit(EntityDto<Guid> input)
        {
            var voucherType = await voucherNuberingRepository.GetAll().Include(x => x.FinancialYearFk)
                .FirstOrDefaultAsync(x => x.Id == input.Id && x.FinancialYearId == FinancialYearId);

            var output = new GetVoucherTypeForEditOutput
            {
                Id = voucherType.Id,
                VoucherTypeId = voucherType.VoucherTypeId,
                StartIndex = voucherType.StartingIndex,
                Prefix = voucherType.Prefix,
                Postfix = voucherType.Postfix,
                FinancialYearName = voucherType.FinancialYearFk.Name,
                VoucherGenerateType = voucherType.VoucherGenerateType,
                FinancialYearId = voucherType.FinancialYearId
            };
            return output;
        }

        public async Task CreateOrEdit(CreateOrEditVoucherTypeDto input)
        {
            if (await voucherNuberingRepository.CountAsync(x =>
                    x.VoucherTypeId == input.VoucherTypeId && x.FinancialYearId == FinancialYearId) > 0)
                await Update(input);
            else
                await Create(input);
        }

        [AbpAuthorize(AppPermissions.PagesVoucherTypesDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            await voucherTypeRepository.DeleteAsync(input.Id);
        }

        [AbpAuthorize(AppPermissions.PagesVoucherTypes)]
        public async Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown()
        {
            return await branchRepository.GetAll()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesVoucherTypesCreate)]
        protected virtual async Task Create(CreateOrEditVoucherTypeDto input)
        {
            var voucherNumbering = await voucherNuberingRepository.FirstOrDefaultAsync(x =>
                x.VoucherTypeId == input.VoucherTypeId && x.FinancialYearId == FinancialYearId);

            if (voucherNumbering == null)
            {
                var voucherType = new VoucherNumbering
                {
                    Id = Guid.Empty,
                    VoucherTypeId = input.VoucherTypeId,
                    StartingIndex = input.StartIndex == 0 ? 1 : input.StartIndex,
                    Prefix = input.Prefix,
                    Postfix = input.Postfix,
                    FinancialYearId = FinancialYearId,
                    TenantId = AbpSession.TenantId,
                    VoucherGenerateType = input.VoucherGenerateType
                };
                await voucherNuberingRepository.InsertAsync(voucherType);
            }
            else
            {
                voucherNumbering.StartingIndex = input.StartIndex;
                voucherNumbering.Prefix = input.Prefix;
                voucherNumbering.VoucherTypeId = input.VoucherTypeId;
                voucherNumbering.Postfix = input.Postfix;
                voucherNumbering.VoucherGenerateType = input.VoucherGenerateType;
                await voucherNuberingRepository.UpdateAsync(voucherNumbering);
            }
        }

        [AbpAuthorize(AppPermissions.PagesVoucherTypesEdit)]
        protected virtual async Task Update(CreateOrEditVoucherTypeDto input)
        {
            var voucherNumbering = await voucherNuberingRepository.FirstOrDefaultAsync(x =>
                x.VoucherTypeId == input.VoucherTypeId && x.FinancialYearId == FinancialYearId);

            if (voucherNumbering != null)
            {
                voucherNumbering.VoucherGenerateType = input.VoucherGenerateType;
                voucherNumbering.StartingIndex = input.StartIndex;
                voucherNumbering.VoucherTypeId = input.VoucherTypeId;
                voucherNumbering.Prefix = input.Prefix;
                voucherNumbering.Postfix = input.Postfix;
                await voucherNuberingRepository.UpdateAsync(voucherNumbering);
            }
        }


        [AbpAuthorize(AppPermissions.PagesVoucherTypes)]
        public async Task<List<UniversalDropdownDto>> GetAllVouchertypeForDropdown()
        {
            return await voucherTypeRepository.GetAll()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }
    }
}
