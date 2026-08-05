using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace NextWave.Erp.Web.Public.Views;

public abstract class ErpRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected ErpRazorPage()
    {
        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }
}

