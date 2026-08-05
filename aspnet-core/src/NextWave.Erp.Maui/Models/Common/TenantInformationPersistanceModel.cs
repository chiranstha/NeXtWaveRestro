using Abp.AutoMapper;
using NextWave.Erp.ApiClient;

namespace NextWave.Erp.Maui.Models.Common;

[AutoMapFrom(typeof(TenantInformation)),
 AutoMapTo(typeof(TenantInformation))]
public class TenantInformationPersistanceModel
{
    public string TenancyName { get; set; }

    public int TenantId { get; set; }
}