using System.Globalization;

namespace NextWave.Erp.Localization;

public interface IApplicationCulturesProvider
{
    CultureInfo[] GetAllCultures();
}

