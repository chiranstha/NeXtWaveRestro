using Abp.AutoMapper;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}

