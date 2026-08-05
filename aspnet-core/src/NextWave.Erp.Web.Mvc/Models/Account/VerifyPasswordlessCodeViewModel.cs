using Abp.Localization;
using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Web.Models.Account;

public class VerifyPasswordlessCodeViewModel
{
    [Required]
    [AbpDisplayName(ErpConsts.LocalizationSourceName, "Code")]
    public string Code { get; set; }

    public string ProviderValue { get; set; }

    public string ProviderType { get; set; }

}

