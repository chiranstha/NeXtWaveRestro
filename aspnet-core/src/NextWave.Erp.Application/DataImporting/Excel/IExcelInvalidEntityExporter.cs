using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using NextWave.Erp.Dto;

namespace NextWave.Erp.DataImporting.Excel;

public interface IExcelInvalidEntityExporter<TEntityDto> : ITransientDependency
{
    Task<FileDto> ExportToFile(List<TEntityDto> entities);
}