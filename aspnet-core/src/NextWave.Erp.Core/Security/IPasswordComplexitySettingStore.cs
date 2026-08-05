using System.Threading.Tasks;

namespace NextWave.Erp.Security;

public interface IPasswordComplexitySettingStore
{
    Task<PasswordComplexitySetting> GetSettingsAsync();
}

