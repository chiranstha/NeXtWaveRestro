using Microsoft.AspNetCore.Components;
using NextWave.Erp.Maui.Core.Components;
using NextWave.Erp.Maui.Core.Threading;
using NextWave.Erp.Maui.Services.UI;


namespace NextWave.Erp.Maui.Pages.MySettings;

public partial class ThemeSwitch : ErpComponentBase
{
    private string _selectedTheme = ThemeService.GetUserTheme();
    
    private string[] _themes = ThemeService.GetAllThemes();

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;
            AsyncRunner.Run(ThemeService.SetUserTheme(JS, SelectedTheme));
        }
    }
}