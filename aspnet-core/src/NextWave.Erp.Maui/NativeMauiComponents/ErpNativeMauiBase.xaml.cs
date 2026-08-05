using Abp;
using NextWave.Erp.Maui.Core;

namespace NextWave.Erp.Maui.NativeMauiComponents;

public partial class ErpNativeMauiComponentBase : ContentPage
{
    public ErpNativeMauiComponentBase()
    {
        InitializeComponent();
    }
    
    protected static T Resolve<T>()
    {
        return DependencyResolver.Resolve<T>();
    }

    protected string L(string text)
    {
        return Core.Localization.L.Localize(text);
    }

    protected Page GetMainPage()
    {
        var mainPage = Application.Current?.Windows[0].Page;
        
        if (mainPage is null)
        {
            throw new AbpException("Main page is not set yet.");
        }

        return mainPage;
    }
}