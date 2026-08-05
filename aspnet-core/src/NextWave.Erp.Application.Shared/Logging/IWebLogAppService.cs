using Abp.Application.Services;
using NextWave.Erp.Dto;
using NextWave.Erp.Logging.Dto;

namespace NextWave.Erp.Logging;

public interface IWebLogAppService : IApplicationService
{
    GetLatestWebLogsOutput GetLatestWebLogs();

    FileDto DownloadWebLogs();
}

