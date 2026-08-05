using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.FinancialStatement;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.GeneralSetting.Exporting;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace NextWave.Erp.GeneralSetting
{
    [AbpAuthorize(AppPermissions.PagesTaxes)]
    public class TaxesAppService(
        IRepository<Tax, Guid> taxRepository,
        ITaxesExcelExporter taxesExcelExporter,
        IRepository<Branch, Guid> branchRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository)
        : ErpAppServiceBase, ITaxesAppService
    {
        public async Task<PagedResultDto<GetTaxForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var filteredTaxes = taxRepository.GetAll()
                .Include(e => e.AccountLedgerFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter.Trim()));

            var pagedAndFilteredTaxes = filteredTaxes
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var taxes = pagedAndFilteredTaxes.Select(o => new GetTaxForViewDto
            {
                Name = o.Name,
                IsActive = o.IsActive,
                Rate = o.Rate,
                Description = o.Description,
                Id = o.Id
            });
            var totalCount = await filteredTaxes.CountAsync();

            return new PagedResultDto<GetTaxForViewDto>(
                totalCount,
                await taxes.ToListAsync()
            );
        }

        public async Task<GetTaxForViewDto> GetTaxForView(Guid id)
        {
            var tax = await taxRepository.GetAsync(id);

            var output = new GetTaxForViewDto
            {
                Id = tax.Id,
                Name = tax.Name,
                IsActive = tax.IsActive,
                Rate = tax.Rate,
                Description = tax.Description,
                LedgerId = tax.LedgerId
            };
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesTaxesEdit)]
        public async Task<GetTaxForEditOutput> GetTaxForEdit(EntityDto<Guid> input)
        {
            var tax = await taxRepository.GetAll().Include(x => x.AccountLedgerFk)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            var output = new GetTaxForEditOutput
            {
                Id = tax.Id,
                Name = tax.Name,
                Rate = tax.Rate,
                Description = tax.Description,
                IsActive = tax.IsActive,
                LedgerId = tax.LedgerId,
                LedgerName = tax.AccountLedgerFk.Name
            };
            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditTaxDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesTaxesDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (await ledgerPostingRepository.CountAsync(x => x.Id == tax.LedgerId) > 0)
                throw new UserFriendlyException("Reference Exists In AccountLedger");

            await accountLedgerRepository.DeleteAsync(x => x.Id == tax.LedgerId);
            await taxRepository.DeleteAsync(tax);
        }

        public async Task<FileDto> GetTaxesToExcel(GetAllUniversalInput input)
        {
            var filteredTaxes = taxRepository.GetAll().Include(e => e.AccountLedgerFk);

            var query = from o in filteredTaxes
                        join o2 in accountLedgerRepository.GetAll() on o.LedgerId equals o2.Id into j2
                        from s2 in j2.DefaultIfEmpty()
                        select new GetTaxForViewDto
                        {
                            Name = o.Name,
                            IsActive = o.IsActive,
                            Id = o.Id,
                            Rate = o.Rate
                        };

            var taxListDtos = await query.ToListAsync();

            return taxesExcelExporter.ExportToFile(taxListDtos);
        }

        [AbpAuthorize(AppPermissions.PagesTaxes)]
        public async Task<List<UniversalDropdownDto>> GetAllBranchForTableDropdown()
        {
            return await branchRepository.GetAll()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesTaxes)]
        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll()
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        [AbpAuthorize(AppPermissions.PagesTaxesCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditTaxDto input)
        {
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null)
                tenantId = AbpSession.TenantId;
            var mainBranch = await branchRepository.FirstOrDefaultAsync(x => x.IsMain);
            var accountGroupId = (await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Duties & Taxes")).Id;
            var accountLedger = new AccountLedger
            {
                Name = input.Name,
                OpeningBalance = 0,
                IsDefault = true,
                CrOrDr = DrOrCr.Dr,
                Narration = null,
                Address = null,
                Phone = null,
                Email = null,
                CreditPeriod = null,
                CreditLimit = null,
                IsBillByBill = false,
                Pan = null,
                Status = true,
                AccountGroupId = accountGroupId,
                TenantId = tenantId
            };
            var ledgerId = await accountLedgerRepository.InsertAndGetIdAsync(accountLedger);

            var tax = new Tax
            {
                Name = input.Name,
                Rate = input.Rate,
                Description = input.Description,
                IsActive = input.IsActive,
                LedgerId = ledgerId,
                TenantId = tenantId
            };
            return await taxRepository.InsertAndGetIdAsync(tax);
        }

        [AbpAuthorize(AppPermissions.PagesTaxesEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditTaxDto input)
        {
            var tax = await taxRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (tax != null)
            {
                tax.Name = input.Name;
                tax.Rate = input.Rate;
                tax.Description = input.Description;
                tax.IsActive = input.IsActive;
                await taxRepository.UpdateAsync(tax);
            }

            var accountLedger = await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == tax.LedgerId);
            if (accountLedger != null)
            {
                accountLedger.Name = input.Name;
                await accountLedgerRepository.UpdateAsync(accountLedger);
            }

            if (tax != null) return tax.Id;
            return Guid.Empty;
        }
    }
}
