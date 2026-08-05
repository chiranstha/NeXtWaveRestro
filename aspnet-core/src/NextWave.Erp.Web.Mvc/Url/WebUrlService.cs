using Abp.Dependency;
using NextWave.Erp.Configuration;
using NextWave.Erp.Url;

namespace NextWave.Erp.Web.Url;

public class WebUrlService : WebUrlServiceBase, IWebUrlService, ITransientDependency
{
    public WebUrlService(
        IAppConfigurationAccessor configurationAccessor) :
        base(configurationAccessor)
    {
    }

    public override string WebSiteRootAddressFormatKey => "App:WebSiteRootAddress";

    public override string ServerRootAddressFormatKey => "App:WebSiteRootAddress";
}

