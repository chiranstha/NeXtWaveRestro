using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using NextWave.Erp.Authorization.Users;

namespace NextWave.Erp.Authorization.PasswordlessLogin;

public interface IPasswordlessLoginManager : IDomainService
{
    Task<User> GetUserByPasswordlessProviderAndKeyAsync(string provider, string providerKey);

    Task VerifyPasswordlessLoginCode(int? tenantId, string providerValue, string code);

    Task<string> GeneratePasswordlessLoginCode(int? tenantId, string providerKey);

    Task RemovePasswordlessLoginCode(int? tenantId, string providerKey);

    List<SelectListItem> GetProviders();
}

