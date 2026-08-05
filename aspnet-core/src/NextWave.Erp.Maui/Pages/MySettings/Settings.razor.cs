using NextWave.Erp.Maui.Core.Components;
using NextWave.Erp.Maui.Pages.Layout;
using NextWave.Erp.Maui.Services.Account;
using NextWave.Erp.Maui.Services.Navigation;

namespace NextWave.Erp.Maui.Pages.MySettings;

public partial class Settings : ErpMainLayoutPageComponentBase
{
    protected IAccountService AccountService { get; set; }
    protected NavMenu NavMenu { get; set; }

    protected INavigationService navigationService { get; set; }
    ChangePasswordModal changePasswordModal;

    public Settings()
    {
        AccountService = Resolve<IAccountService>();
        navigationService = Resolve<INavigationService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("MySettings"));
    }

    private async Task LogOut()
    {
        await AccountService.LogoutAsync();
        navigationService.NavigateTo(NavigationUrlConsts.Login);
    }

    private async Task OnChangePasswordAsync()
    {
        await changePasswordModal.Hide();
        await Task.Delay(300);
        await LogOut();
    }

    private async Task OnLanguageSwitchAsync()
    {
        await SetPageHeader(L("MySettings"));
        StateHasChanged();
    }

    private async Task ChangePassword()
    {
        await changePasswordModal.Show();
    }

}