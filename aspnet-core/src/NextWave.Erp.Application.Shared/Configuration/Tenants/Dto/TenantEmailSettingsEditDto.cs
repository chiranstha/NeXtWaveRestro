using Abp.Auditing;
using NextWave.Erp.Configuration.Dto;

namespace NextWave.Erp.Configuration.Tenants.Dto;

public class TenantEmailSettingsEditDto : EmailSettingsEditDto
{
    public bool UseHostDefaultEmailSettings { get; set; }
}

