using System.Threading.Tasks;
using Abp.Dependency;

namespace NextWave.Erp.UiCustomization;

public interface IUiThemeCustomizerFactory : ISingletonDependency
{
    Task<IUiCustomizer> GetCurrentUiCustomizer();

    IUiCustomizer GetUiCustomizer(string theme);
}
