using Abp.AutoMapper;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.MultiTenancy.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Tenants;

[AutoMapFrom(typeof(GetTenantFeaturesEditOutput))]
public class TenantFeaturesEditViewModel : GetTenantFeaturesEditOutput, IFeatureEditViewModel
{
    public Tenant Tenant { get; set; }
}

