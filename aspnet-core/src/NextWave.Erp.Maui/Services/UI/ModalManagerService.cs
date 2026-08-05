using Abp.Dependency;
using Microsoft.JSInterop;

namespace NextWave.Erp.Maui.Services.UI;

public class ModalManagerService : ITransientDependency
{
    public async Task Show(IJSRuntime JS, string jquerySelector)
    {
        await JS.InvokeVoidAsync("BlazorModalManagerService.show", jquerySelector);
    }

    public async Task Hide(IJSRuntime JS, string jquerySelector)
    {
        await JS.InvokeVoidAsync("BlazorModalManagerService.hide", jquerySelector);
            
    }
}