using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Exporting
{
    public interface IFinancialYearsExcelExporter
    {
        FileDto ExportToFile(List<GetFinancialYearForViewDto> financialYears);
    }
}
