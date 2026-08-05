using Abp.Domain.Services;

namespace NextWave.Erp;

public abstract class ErpDomainServiceBase : DomainService
{
    /* Add your common members for all your domain services. */

    protected ErpDomainServiceBase()
    {
        LocalizationSourceName = ErpConsts.LocalizationSourceName;
    }
}

