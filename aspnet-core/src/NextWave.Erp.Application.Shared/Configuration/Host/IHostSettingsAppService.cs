using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.Configuration.Host.Dto;

namespace NextWave.Erp.Configuration.Host;

public interface IHostSettingsAppService : IApplicationService
{
    Task<HostSettingsEditDto> GetAllSettings();

    Task UpdateAllSettings(HostSettingsEditDto input);

    Task SendTestEmail(SendTestEmailInput input);
}

