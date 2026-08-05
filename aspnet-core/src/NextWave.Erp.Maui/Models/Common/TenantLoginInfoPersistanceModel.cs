using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Maui.Models.Common;

[AutoMapFrom(typeof(TenantLoginInfoDto)),
 AutoMapTo(typeof(TenantLoginInfoDto))]
public class TenantLoginInfoPersistanceModel : EntityDto
{
    public string TenancyName { get; set; }

    public string Name { get; set; }
}