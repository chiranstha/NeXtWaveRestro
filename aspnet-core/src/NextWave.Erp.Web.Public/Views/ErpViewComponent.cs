using Abp.AspNetCore.Mvc.ViewComponents;

namespace NextWave.Erp.Web.Public.Views;

public abstract class ErpViewComponent : AbpViewComponent
{
    protected ErpViewComponent()
    {
        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }
}

