using NextWave.Erp.ApiClient.Models;

namespace NextWave.Erp.Maui.Services.Account;

public interface IAccountService
{
    AbpAuthenticateModel AbpAuthenticateModel { get; set; }
        
    AbpAuthenticateResultModel AuthenticateResultModel { get; set; }
        
    Task LoginUserAsync();

    Task LogoutAsync();
}