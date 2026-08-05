using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.Install.Dto;

namespace NextWave.Erp.Install;

public interface IInstallAppService : IApplicationService
{
    Task Setup(InstallDto input);

    AppSettingsJsonDto GetAppSettingsJson();

    CheckDatabaseOutput CheckDatabase();
}
