using Abp.Web.Models.AbpUserConfiguration;

namespace NextWave.Erp.Configuration;

public static class AbpUserConfigurationDtoExtensions
{
    public static bool HasSessionUserId(this AbpUserConfigurationDto userConfiguration)
    {
        return userConfiguration.Session?.UserId != null;
    }
}

