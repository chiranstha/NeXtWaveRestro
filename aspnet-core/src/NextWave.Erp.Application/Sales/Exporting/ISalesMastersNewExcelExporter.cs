using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Exporting
{
    public interface ISalesMastersNewExcelExporter
    {
        FileDto ExportToFile(List<GetSalesMasterExportDto> salesMasters);
        FileDto ExportToFileDetails(GetSalesMasterExportMasterDto data);
        //FileDto ExportToFileSwastik(SalesDtoForSwastik data);
        FileDto ExportToFileWithDetails(List<GetSalesMasterExportWithProductDto> salesMasters);
    }
}
