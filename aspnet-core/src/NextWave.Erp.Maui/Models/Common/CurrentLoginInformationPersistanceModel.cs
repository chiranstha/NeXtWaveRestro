using Abp.AutoMapper;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Maui.Models.Common;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput)),
 AutoMapTo(typeof(GetCurrentLoginInformationsOutput))]
public class CurrentLoginInformationPersistanceModel
{
    public UserLoginInfoPersistanceModel User { get; set; }

    public TenantLoginInfoPersistanceModel Tenant { get; set; }

    public ApplicationInfoPersistanceModel Application { get; set; }
}