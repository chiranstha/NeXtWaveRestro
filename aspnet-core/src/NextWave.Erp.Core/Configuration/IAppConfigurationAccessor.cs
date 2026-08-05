using Microsoft.Extensions.Configuration;

namespace NextWave.Erp.Configuration;

public interface IAppConfigurationAccessor
{
    IConfigurationRoot Configuration { get; }
}

