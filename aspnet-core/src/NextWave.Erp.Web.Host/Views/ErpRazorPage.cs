using Abp.AspNetCore.Mvc.Views;

namespace NextWave.Erp.Web.Views;

public abstract class ErpRazorPage<TModel> : AbpRazorPage<TModel>
{
    protected ErpRazorPage()
    {
        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }
}

