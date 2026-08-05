using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Exporting
{
    public interface ISalesReturnMastersExcelExporter
    {
        FileDto ExportToFile(List<GetSalesReturnMasterForViewDto> salesReturnMasters);
    }
}
