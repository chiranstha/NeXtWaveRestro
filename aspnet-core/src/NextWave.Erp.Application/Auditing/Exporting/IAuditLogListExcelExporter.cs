using NextWave.Erp.Auditing.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.EntityChanges.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Auditing.Exporting
{
    public interface IAuditLogListExcelExporter
    {
        Task<FileDto> ExportToFile(List<AuditLogListDto> auditLogListDtos);

        Task<FileDto> ExportToFile(List<EntityChangeListDto> entityChangeListDtos);
    }
}
