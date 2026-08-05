using CommunityToolkit.Maui;
using NextWave.Erp.Maui.Core;
using ZXing.Net.Maui.Controls;

namespace NextWave.Erp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        ApplicationBootstrapper.InitializeIfNeeds<ErpMauiModule>();

        var app = builder.Build();
        return app;
    }
}