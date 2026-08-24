using Abp.Auditing;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common.Dto;
using NextWave.Erp.Configuration;
using NextWave.Erp.Configuration.Tenants.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport
{

    [AbpAuthorize]
    [DisableAuditing]
    public class ReportingAppService(
    IRepository<AccountGroup, Guid> accountGroupRepository,
    IRepository<Tax, Guid> taxRepository,
    IRepository<Product, Guid> productRepository,
    IRepository<ProductGroup, Guid> productGroupRepository,
    IRepository<VoucherType, Guid> voucherTypeRepository,
    IRepository<AccountLedger, Guid> accountLedgerRepository,
    IRepository<FinancialYear, Guid> financialYeaRepository,
    IRepository<User, long> userRepository)
    : ErpAppServiceBase
    {
        public async Task<AllTenantSettingBundleDto> GetAllSettings()
        {
            return new AllTenantSettingBundleDto
            {
                IsTax = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.IsTaxEnabled, AbpSession.GetTenantId()),


                AutomaticProductCodeGeneration =
                    await SettingManager.GetSettingValueForTenantAsync<bool>(
                        AppSettings.ErpSettings.ProductCode, AbpSession.GetTenantId()),

                IsAbt = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.IsAbt, AbpSession.GetTenantId()),

                ShowSalesRate = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.AllowSalesRate, AbpSession.GetTenantId()),
                ShowMrp = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.Mrp, AbpSession.GetTenantId()),

                DuplicateLedgerName = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.DuplicateLedgerName, AbpSession.GetTenantId()),

                DuplicatePAN = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.DuplicatePAN, AbpSession.GetTenantId()),

                IsCbms = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.IsCBMS, AbpSession.GetTenantId()),

                CbmsUserName = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.CBMSUsername, AbpSession.GetTenantId()),

                CbmsPassword = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.CBMSPassword, AbpSession.GetTenantId()),

                NumberOfPrint = await SettingManager.GetSettingValueForTenantAsync<int>(
                    AppSettings.ErpSettings.NoOfPrint, AbpSession.GetTenantId()),

                IsIrdSoftware = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.IsIRDSoftware, AbpSession.GetTenantId()),

                SalesRate = await SettingManager.GetSettingValueForTenantAsync<int>(
                    AppSettings.ErpSettings.SalesRate, AbpSession.GetTenantId()),

                ShowPurchaseRate = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.PurchaseRate, AbpSession.GetTenantId()),
                ShowUnit = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.Unit, AbpSession.GetTenantId()),

                ShowDiscountAmount = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.DiscountAmount, AbpSession.GetTenantId()),
                ShowProdutCode = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.ProductCode, AbpSession.GetTenantId()),

                ShowSalesAdditional = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.ShowSalesAdditional, AbpSession.GetTenantId()),
                ShowDiscountPercentage = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.DiscountPercent, AbpSession.GetTenantId()),
                NegativeStockStatus = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.NegativeStockStatus, AbpSession.GetTenantId()),
                RestaurantVatPercent = await SettingManager.GetSettingValueForTenantAsync<decimal>(
                    AppSettings.ErpSettings.RestaurantVatPercent, AbpSession.GetTenantId()),
                RestaurantServiceChargePercent = await SettingManager.GetSettingValueForTenantAsync<decimal>(
                    AppSettings.ErpSettings.RestaurantServiceChargePercent, AbpSession.GetTenantId()),
                RestaurantRequireManagerPinForSensitiveActions = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions, AbpSession.GetTenantId()),
                RestaurantTicketPrintingEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.RestaurantTicketPrintingEnabled, AbpSession.GetTenantId()),
                RestaurantChannelAvailabilityEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled, AbpSession.GetTenantId()),
                RestaurantTableWorkflow = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.RestaurantTableWorkflow, AbpSession.GetTenantId()),
                SalesBillFormat = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.SalesBillFormat, AbpSession.GetTenantId()),
                IsEmailSent = await SettingManager.GetSettingValueForTenantAsync<bool>(
                    AppSettings.ErpSettings.IsEmailSent, AbpSession.GetTenantId()),
                Transation = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.Transaction, AbpSession.GetTenantId()),
                StockCalculation = await SettingManager.GetSettingValueForTenantAsync(
                    AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId())
            };
        }

        public async Task<List<GetUserDropdownDto>> GetAllUserDropdown()
        {
            var result = new List<GetUserDropdownDto>
            {
                new()
                {
                    Id = 0,
                    Name = "All",
                    Email = ""
                }
            };

            var data = await userRepository.GetAll()
                .Select(x => new GetUserDropdownDto
                {
                    Id = x.Id,
                    Name = x.FullName,
                    Email = x.EmailAddress
                }).ToListAsync();

            result.AddRange(data);
            return result;
        }

        public async Task<CurrentFinancialYearDto> GetFinancialYears()
        {
            var tenantId = AbpSession.TenantId;
            if (!tenantId.HasValue) return null;
            var data = await financialYeaRepository
                .FirstOrDefaultAsync(x => x.Id == FinancialYearId && x.TenantId == AbpSession.TenantId);
            return new CurrentFinancialYearDto
            {
                FromDate = data.FromDate,
                ToDate = data.ToDate,
                FromMiti = data.FromMiti,
                ToMiti = data.ToMiti
            };
        }

        public async Task<AllTenantSettingBundleDto> GetAllTenantSetting()
        {
            try
            {
                return new AllTenantSettingBundleDto
                {
                    IsAbt = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.IsAbt, AbpSession.GetTenantId()),

                    IsTax = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.IsTaxEnabled, AbpSession.GetTenantId()),

                    AutomaticProductCodeGeneration =
                   await SettingManager.GetSettingValueForTenantAsync<bool>(
                       AppSettings.ErpSettings.ProductCode, AbpSession.GetTenantId()),

                    DuplicateLedgerName = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.DuplicateLedgerName, AbpSession.GetTenantId()),

                    DuplicatePAN = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.DuplicatePAN, AbpSession.GetTenantId()),

                    IsCbms = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.IsCBMS, AbpSession.GetTenantId()),

                    CbmsUserName = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.CBMSUsername, AbpSession.GetTenantId()),

                    CbmsPassword = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.CBMSPassword, AbpSession.GetTenantId()),

                    NumberOfPrint = await SettingManager.GetSettingValueForTenantAsync<int>(
                   AppSettings.ErpSettings.NoOfPrint, AbpSession.GetTenantId()),

                    IsIrdSoftware = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.IsIRDSoftware, AbpSession.GetTenantId()),

                    SalesRate = await SettingManager.GetSettingValueForTenantAsync<int>(
                   AppSettings.ErpSettings.SalesRate, AbpSession.GetTenantId()),

                    ShowSalesRate = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.AllowSalesRate, AbpSession.GetTenantId()),
                    ShowMrp = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.Mrp, AbpSession.GetTenantId()),
                    ShowPurchaseRate = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.PurchaseRate, AbpSession.GetTenantId()),
                    ShowUnit = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.Unit, AbpSession.GetTenantId()),
                    ShowDiscountAmount = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.DiscountAmount, AbpSession.GetTenantId()),
                    ShowProdutCode = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.ProductCode, AbpSession.GetTenantId()),
                    ShowSalesAdditional = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.ShowSalesAdditional, AbpSession.GetTenantId()),

                    ShowDiscountPercentage = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.DiscountPercent, AbpSession.GetTenantId()),
                    NegativeStockStatus = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.NegativeStockStatus, AbpSession.GetTenantId()),
                    RestaurantVatPercent = await SettingManager.GetSettingValueForTenantAsync<decimal>(
                   AppSettings.ErpSettings.RestaurantVatPercent, AbpSession.GetTenantId()),
                    RestaurantServiceChargePercent = await SettingManager.GetSettingValueForTenantAsync<decimal>(
                   AppSettings.ErpSettings.RestaurantServiceChargePercent, AbpSession.GetTenantId()),
                    RestaurantRequireManagerPinForSensitiveActions = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions, AbpSession.GetTenantId()),
                    RestaurantTicketPrintingEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.RestaurantTicketPrintingEnabled, AbpSession.GetTenantId()),
                    RestaurantChannelAvailabilityEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.RestaurantChannelAvailabilityEnabled, AbpSession.GetTenantId()),
                    RestaurantTableWorkflow = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.RestaurantTableWorkflow, AbpSession.GetTenantId()),
                    SalesBillFormat = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.SalesBillFormat, AbpSession.GetTenantId()),
                    IsEmailSent = await SettingManager.GetSettingValueForTenantAsync<bool>(
                   AppSettings.ErpSettings.IsEmailSent, AbpSession.GetTenantId()),
                    Transation = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.Transaction, AbpSession.GetTenantId()),
                    StockCalculation = await SettingManager.GetSettingValueForTenantAsync(
                   AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId())
                };
            }
            catch
            {
                return new AllTenantSettingBundleDto();
            }

        }

        public async Task<List<UniversalDropdownDto>> GetAllCashOrBankForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete).AsNoTracking()
                .Where(x => x.AccountGroupFk.Name == "Cash-in Hand" || x.AccountGroupFk.Name == "Bank Account" ||
                            x.AccountGroupFk.Name == "Bank OD A/C")
                .Where(x => !x.IsDelete)
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null
                        ? ""
                        : accountLedger.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllTaxForTableDropdown()
        {
            var result = new List<UniversalDropdownDto>();
            var data = new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            };
            result.Add(data);

            result.AddRange(await taxRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync());
            return result;
        }

        public async Task<List<UniversalDropdownDto>> GetAllVoucherTypeForTableDropdown()
        {
            return await voucherTypeRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgers()
        {
            return (await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete).ToListAsync())
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToList();
        }

        public async Task<List<UniversalDropdownDto>> GetAllAccountGroupForTableDropdown()
        {
            var result = new List<UniversalDropdownDto>();
            var data = new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            };
            result.Add(data);

            result.AddRange(await accountGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Select(ag => new UniversalDropdownDto
                {
                    Id = ag.Id,
                    DisplayName = ag == null || ag.Name == null ? "" : ag.Name.ToString()
                }).ToListAsync());
            return result;
        }

        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerForTableDropdown()
        {
            var result = new List<UniversalDropdownDto>();
            var data = new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            };
            result.Add(data);

            result.AddRange(await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync());
            return result;
        }

        public async Task<List<UniversalDropdownDto>> GetAllLedgerForDropdown(Guid accountGroupId)
        {
            var result = new List<UniversalDropdownDto>();
            var data = new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            };
            result.Add(data);
            result.AddRange(await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Where(x => x.AccountGroupId == accountGroupId)
                .Where(x => !x.IsDelete)
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync());

            return result;
        }

        public async Task<List<UniversalDropdownDto>> GetAllProductGroupForTableDropdown()
        {
            var list = new List<UniversalDropdownDto>();
            list.Add(new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            });

            var proudctgroup = await productGroupRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch == null || branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
            list.AddRange(proudctgroup);
            return list;
        }

        public async Task<List<UniversalDropdownDto>> GetAllProductsForTableDropdown()
        {
            var list = new List<UniversalDropdownDto>();
            list.Add(new UniversalDropdownDto
            {
                Id = Guid.Empty,
                DisplayName = "All"
            });
            list.AddRange(await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync());

            return list;
        }


        public async Task<List<UniversalDropdownDto>> GetAllProductsByBranchForDropdown()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllProductsByGroupForDropdown(Guid productGroupId)
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Where(x => x.ProductGroupId == productGroupId)
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllAccountLedgerByAccountGroupIdForTableDropdown(
            Guid accountGroupId)
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Where(x => x.AccountGroupId == accountGroupId)
                .Where(x => !x.IsDelete)
                .Select(branch => new UniversalDropdownDto
                {
                    Id = branch.Id,
                    DisplayName = branch.Name == null ? "" : branch.Name.ToString()
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllSuppliersForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors")
                .Select(accountLedger => new UniversalDropdownDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger == null || accountLedger.Name == null ? "" : accountLedger.Name.ToString()
                }).ToListAsync();
        }
    }
}
