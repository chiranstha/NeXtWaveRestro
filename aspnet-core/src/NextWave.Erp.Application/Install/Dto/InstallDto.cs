using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using NextWave.Erp.Configuration.Dto;
using NextWave.Erp.Configuration.Host.Dto;

namespace NextWave.Erp.Install.Dto;

public class InstallDto
{
    [Required]
    [DisableAuditing]
    public string ConnectionString { get; set; }

    [Required]
    [DisableAuditing]
    public string AdminPassword { get; set; }

    [Required]
    public string WebSiteUrl { get; set; }

    public string ServerUrl { get; set; }

    [Required]
    public string DefaultLanguage { get; set; }

    public EmailSettingsEditDto SmtpSettings { get; set; }

    public HostBillingSettingsEditDto BillInfo { get; set; }
}
