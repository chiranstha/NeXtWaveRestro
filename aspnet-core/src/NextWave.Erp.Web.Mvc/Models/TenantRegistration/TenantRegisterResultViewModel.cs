using Abp.AutoMapper;
using NextWave.Erp.MultiTenancy.Dto;

namespace NextWave.Erp.Web.Models.TenantRegistration;

[AutoMapFrom(typeof(RegisterTenantOutput))]
public class TenantRegisterResultViewModel : RegisterTenantOutput
{
    public string TenantLoginAddress { get; set; }
}

