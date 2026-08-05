using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();

    Task<UpdateUserSignInTokenOutput> UpdateUserSignInToken();
}

