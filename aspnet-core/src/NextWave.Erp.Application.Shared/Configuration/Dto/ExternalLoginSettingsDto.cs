using System.Collections.Generic;

namespace NextWave.Erp.Configuration.Dto;

public class ExternalLoginSettingsDto
{
    public List<string> EnabledSocialLoginSettings { get; set; } = new List<string>();
}

