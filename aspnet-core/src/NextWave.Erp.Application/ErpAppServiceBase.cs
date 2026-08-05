using Abp.Application.Services;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Identity;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ErpBaseService;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.MultiTenancy;
using NPOI.SS.Formula.Functions;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp;

/// <summary>
/// Derive your application services from this class.
/// </summary>
public abstract class ErpAppServiceBase : ApplicationService
{
    public TenantManager TenantManager { get; set; }

    public UserManager UserManager { get; set; }

    protected ErpAppServiceBase()
    {
        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }

    protected virtual async Task<User> GetCurrentUserAsync()
    {
        var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());
        if (user == null)
        {
            throw new Exception("There is no current user!");
        }

        return user;
    }

    //protected virtual User GetCurrentUser()
    //{
    //    return AsyncHelper.RunSync(GetCurrentUserAsync);
    //}

    protected virtual Task<Tenant> GetCurrentTenantAsync()
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return TenantManager.GetByIdAsync(AbpSession.GetTenantId());
        }
    }

    //protected virtual Tenant GetCurrentTenant()
    //{
    //    using (CurrentUnitOfWork.SetTenantId(null))
    //    {
    //        return TenantManager.GetById(AbpSession.GetTenantId());
    //    }
    //}

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }

    public VoucherTypeManager VoucherTypeManager { get; set; }
    public ErpCommonManager ERPCommonManager { get; set; }
    protected virtual Guid FinancialYearId =>  ERPCommonManager.GetCurrentFinancialYearId(AbpSession.GetUserId());

    protected virtual FinancialYear FinancialYear => ERPCommonManager.GetFinancialYear(FinancialYearId);

    protected virtual decimal PostingNumbering => ERPCommonManager.GetPostingNumbering();
    public UnitConversionManager UnitConversionManager { get; set; }
}