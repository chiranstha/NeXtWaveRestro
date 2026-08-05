using Abp.Dependency;
using NextWave.Erp.ApiClient;
using NextWave.Erp.ApiClient.Models;
using NextWave.Erp.Maui.Core.Localization;
using NextWave.Erp.Maui.Core.Threading;
using NextWave.Erp.Maui.Services.Navigation;
using NextWave.Erp.Maui.Services.Storage;
using NextWave.Erp.Maui.Services.UI;
using NextWave.Erp.Sessions;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Maui.Services.Account;

public class AccountService : IAccountService, ISingletonDependency
{
    private readonly IApplicationContext _applicationContext;
    private readonly ISessionAppService _sessionAppService;
    private readonly IAccessTokenManager _accessTokenManager;
    private readonly IDataStorageService _dataStorageService;
    private readonly INavigationService _navigationService;

    public AccountService(
        IApplicationContext applicationContext,
        ISessionAppService sessionAppService,
        IAccessTokenManager accessTokenManager,
        AbpAuthenticateModel abpAuthenticateModel,
        IDataStorageService dataStorageService,
        INavigationService navigationService
    )
    {
        _applicationContext = applicationContext;
        _sessionAppService = sessionAppService;
        _accessTokenManager = accessTokenManager;
        _dataStorageService = dataStorageService;
        AbpAuthenticateModel = abpAuthenticateModel;
        _navigationService = navigationService;
    }

    public AbpAuthenticateModel AbpAuthenticateModel { get; set; }
    public AbpAuthenticateResultModel AuthenticateResultModel { get; set; }

    public async Task LoginUserAsync()
    {
        await WebRequestExecuter.Execute(_accessTokenManager.LoginAsync, AuthenticateSucceed, ex => Task.CompletedTask);
    }

    public Task LogoutAsync()
    {
        _accessTokenManager.Logout();
        _applicationContext.ClearLoginInfo();
        _dataStorageService.ClearSessionPersistance();
        return Task.CompletedTask;
    }

    private async Task AuthenticateSucceed(AbpAuthenticateResultModel result)
    {
        AuthenticateResultModel = result;

        if (AuthenticateResultModel.ShouldResetPassword)
        {
            await UserDialogsService.Instance.AlertError(L.Localize("LoginFailed") + " " + L.Localize("ChangePasswordToLogin"));
            return;
        }

        if (AuthenticateResultModel.RequiresTwoFactorVerification)
        {
            _navigationService.NavigateTo(NavigationUrlConsts.SendTwoFactorCode);
            return;
        }

        await _dataStorageService.StoreAuthenticateResultAsync(AuthenticateResultModel);

        AbpAuthenticateModel.Password = null;
        await SetCurrentUserInfoAsync();
        await UserConfigurationManager.GetAsync();
        _navigationService.NavigateTo(NavigationUrlConsts.Users);
    }

    private async Task SetCurrentUserInfoAsync()
    {
        await WebRequestExecuter.Execute(async () =>
            await _sessionAppService.GetCurrentLoginInformations(), GetCurrentUserInfoExecuted);
    }

    private async Task GetCurrentUserInfoExecuted(GetCurrentLoginInformationsOutput result)
    {
        _applicationContext.SetLoginInfo(result);

        await _dataStorageService.StoreLoginInformationAsync(_applicationContext.LoginInfo);
    }
}