using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting.Dtos;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.GeneralSetting
{
    public class VoucherTypeManager(
    IRepository<VoucherNumbering, Guid> voucherNumberingRepository,
    IRepository<Branch, Guid> branchRepository,
    IRepository<VoucherType, Guid> voucherTypeRepository)
    : ErpDomainServiceBase
    {
        [UnitOfWork]
        public async Task<Guid> GetVoucherTypeId(string voucherTypeName)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var branch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain) ??
                         throw new UserFriendlyException("The Branch doesn't exist");

            var voucherType =
                await voucherTypeRepository.FirstOrDefaultAsync(x =>
                    x.Name == voucherTypeName && x.TenantId == tenantId);
            if (voucherType != null) return voucherType.Id;
            var model = new VoucherType
            {
                Id = default,
                TenantId = tenantId,
                Name = voucherTypeName,
                TypeOfVoucher = voucherTypeName,
                StartIndex = 1,
                Description = "",
                IsActive = true,
                IsDefault = true
            };
            return await voucherTypeRepository.InsertAndGetIdAsync(model);
        }

        public async Task<VoucherNumberingDto> GetVoucherNumbering( Guid financialYearId,
            string voucherTypeName)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();

            // Create default dto with common values
            var defaultDto = new VoucherNumberingDto
            {
                Prefix = "",
                Postfix = "",
                StartIndex = 1
            };

            // Find or create voucher type
            var voucherType = await voucherTypeRepository.FirstOrDefaultAsync(x =>
                x.Name == voucherTypeName && x.TenantId == tenantId);

            if (voucherType == null)
            {
                // Create new voucher type
                voucherType = new VoucherType
                {
                    TenantId = tenantId,
                    Name = voucherTypeName,
                    TypeOfVoucher = voucherTypeName,
                    StartIndex = 1,
                    Description = "",
                    IsActive = true,
                    IsDefault = true
                };

                var objVoucherId = await voucherTypeRepository.InsertAndGetIdAsync(voucherType);

                // Create new voucher numbering
                await voucherNumberingRepository.InsertAsync(new VoucherNumbering
                {
                    VoucherTypeId = objVoucherId,
                    StartingIndex = defaultDto.StartIndex,
                    Prefix = defaultDto.Prefix,
                    Postfix = defaultDto.Postfix,
                    FinancialYearId = financialYearId,
                    TenantId = tenantId
                });

                return defaultDto;
            }

            // Try to find existing voucher numbering
            var voucherNumbering = await voucherNumberingRepository.FirstOrDefaultAsync(x =>
                x.VoucherTypeId == voucherType.Id &&
                x.FinancialYearId == financialYearId &&
                x.TenantId == tenantId);

            if (voucherNumbering != null)
                // Return existing voucher numbering
                return new VoucherNumberingDto
                {
                    Prefix = voucherNumbering.Prefix,
                    Postfix = voucherNumbering.Postfix,
                    StartIndex = voucherNumbering.StartingIndex
                };

            // Create new voucher numbering
            await voucherNumberingRepository.InsertAsync(new VoucherNumbering
            {
                VoucherTypeId = voucherType.Id,
                StartingIndex = defaultDto.StartIndex,
                Prefix = defaultDto.Prefix,
                Postfix = defaultDto.Postfix,
                FinancialYearId = financialYearId,
                TenantId = tenantId
            });

            return defaultDto;
        }

        [UnitOfWork]
        public async Task<string> GetVoucherGenerateType(Guid financialYearId, string voucherTypeName)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var voucherTypeId = await GetVoucherTypeId(voucherTypeName);
            var voucherType = await voucherNumberingRepository
                .FirstOrDefaultAsync(x => x.VoucherTypeId == voucherTypeId &&
                                          x.TenantId == tenantId &&
                                          x.FinancialYearId == financialYearId);
            return voucherType != null ? voucherType.VoucherGenerateType.ToString() : "Automatic";
        }

        [UnitOfWork]
        public async Task<string> GetVoucherTypeName(Guid voucherTypeId)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var financialYear =
                await voucherTypeRepository.FirstOrDefaultAsync(x => x.Id == voucherTypeId && x.TenantId == tenantId);
            return financialYear.Name;
        }
    }
}
