using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Notifications;
using Abp.Runtime.Session;
using Abp.UI;
using Abp;
using Microsoft.AspNetCore.Http;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Notifications;
using NextWave.Erp.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Abp.IO.Extensions;
using Abp.Linq.Extensions;
using Abp.Localization;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Inventory.Importing;
using NextWave.Erp.Inventory.Exporting;

namespace NextWave.Erp.Inventory
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesUnits)]
    public class UnitsAppService(
        IRepository<Unit, Guid> unitRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IUnitsExcelExporter unitsExcelExporter,
        IBinaryObjectManager binaryObjectManager,
        IBackgroundJobManager backgroundJobManager,
        IAppNotifier appNotifier)
        : ErpAppServiceBase, IUnitsAppService
    {
        protected readonly IBackgroundJobManager BackgroundJobManager = backgroundJobManager;
        protected readonly IBinaryObjectManager BinaryObjectManager = binaryObjectManager;

        [DisableAuditing]
        public async Task<PagedResultDto<GetUnitForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredUnits = unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter),
                    e => e.Name.Contains(input.Filter) ||
                         e.FormalName.Contains(input.Filter));

            var pagedAndFilteredUnits = filteredUnits
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var units = from o in pagedAndFilteredUnits
                        select new GetUnitForViewDto
                        {
                            Name = o.Name,
                            FormalName = o.FormalName,
                            Id = o.Id
                        };

            var totalCount = await filteredUnits.CountAsync();

            return new PagedResultDto<GetUnitForViewDto>(
                totalCount,
                await units.ToListAsync()
            );
        }

        public async Task<GetUnitForViewDto> GetUnitForView(Guid id)
        {
            var unit = await unitRepository.GetAsync(id);

            var output = new GetUnitForViewDto
            {
                Id = unit.Id,
                Name = unit.Name,
                FormalName = unit.FormalName
            };
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesUnitsEdit)]
        public async Task<GetUnitForEditOutput> GetUnitForEdit(EntityDto<Guid> input)
        {
            var unit = await unitRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetUnitForEditOutput
            {
                Id = unit.Id,
                Name = unit.Name,
                FormalName = unit.FormalName
            };
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditUnitDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesUnitsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var isExists = false;
            if (await productRepository.CountAsync(x => x.UnitId == input.Id) > 0) isExists = true;
            if (await stockPostingRepository.CountAsync(x => x.UnitId == input.Id) > 0) isExists = true;
            if (await unitConversionRepository.CountAsync(x => x.UnitId == input.Id) > 0) isExists = true;
            if (isExists) throw new UserFriendlyException("Unit Reference is used");

            var unit = await unitRepository.FirstOrDefaultAsync(input.Id);
            await unitRepository.DeleteAsync(input.Id);
            //await appNotifier.SendMessageAsync(AbpSession.ToUserIdentifier(),
            //    new LocalizableString(unit.Name + "  is Delete!", ERPConsts.LocalizationSourceName),
            //    null,
            //    NotificationSeverity.Success);
        }

        public async Task<FileDto> GetUnitsToExcel()
        {
            var filteredUnits = unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId);

            var query = from o in filteredUnits
                        select new GetUnitForViewDto
                        {
                            Name = o.Name,
                            FormalName = o.FormalName,
                            Id = o.Id
                        };

            var unitListDtos = await query.ToListAsync();

            return unitsExcelExporter.ExportToFile(unitListDtos);
        }

        public async Task ImportUnitFromExcel(IFormFile file)
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

            await BackgroundJobManager.EnqueueAsync<ImportUnitToExcelJob, ImportUniversalFromExcelJobArgs>(
                new ImportUniversalFromExcelJobArgs
                {
                    TenantId = tenantId,
                    BinaryObjectId = fileObject.Id
                });
        }

        [AbpAuthorize(AppPermissions.PagesUnitsCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditUnitDto input)
        {
            if (await unitRepository.CountAsync(x => x.Name == input.Name && x.TenantId == AbpSession.TenantId) > 0)
                throw new UserFriendlyException("Unit is Already Exists!");
            var unit = new Unit
            {
                TenantId = null,
                Name = input.Name,
                FormalName = input.FormalName,
                IsDefault = false
            };
            if (AbpSession.TenantId != null) unit.TenantId = AbpSession.TenantId;
            var unitId = await unitRepository.InsertAndGetIdAsync(unit);
            return unitId;
        }

        [AbpAuthorize(AppPermissions.PagesUnitsEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditUnitDto input)
        {
            if (await unitRepository.CountAsync(x =>
                    x.Id != input.Id && x.Name == input.Name && x.TenantId == AbpSession.TenantId) > 0)
                throw new UserFriendlyException("Unit is Already Exists!");
            var unit = await unitRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (unit != null)
            {
                unit.FormalName = input.FormalName;
                unit.Name = input.Name;
                await unitRepository.UpdateAsync(unit);
            }
            else
            {
                throw new UserFriendlyException("Id " + input.Id + " not found");
            }

            return unit.Id;
        }
    }
}
