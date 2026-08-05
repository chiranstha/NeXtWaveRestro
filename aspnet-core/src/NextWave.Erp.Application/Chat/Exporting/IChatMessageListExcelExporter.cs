using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using NextWave.Erp.Chat.Dto;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Chat.Exporting
{
    public interface IChatMessageListExcelExporter
    {
        Task<FileDto> ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages);
    }
}