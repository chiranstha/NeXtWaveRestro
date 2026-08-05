using Abp.Extensions;

namespace NextWave.Erp.Authentication;

public class FacebookExternalLoginProviderSettings
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }

    public bool IsValid()
    {
        return !AppId.IsNullOrWhiteSpace() && !AppSecret.IsNullOrWhiteSpace();
    }
}

