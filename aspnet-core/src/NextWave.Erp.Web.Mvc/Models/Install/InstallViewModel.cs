using System.Collections.Generic;
using Abp.Localization;
using NextWave.Erp.Install.Dto;

namespace NextWave.Erp.Web.Models.Install;

public class InstallViewModel
{
    public List<ApplicationLanguage> Languages { get; set; }

    public AppSettingsJsonDto AppSettingsJson { get; set; }
}

