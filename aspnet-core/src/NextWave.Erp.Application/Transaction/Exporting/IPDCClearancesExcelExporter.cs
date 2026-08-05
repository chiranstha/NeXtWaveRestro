using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public interface IPDCClearancesExcelExporter
    {
        FileDto ExportToFile(List<GetPDCClearanceForViewDto> pdcClearances);
    }
}
