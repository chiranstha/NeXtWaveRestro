using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.SalesReport.Exporting
{
    public interface ISalesReportExport
    {
        FileDto ExportToFile(List<PurchaseReportList> sales);
        FileDto NewExportToFile(List<SalesReportDtoNew> sales);
        FileDto ExportToFileDetails(GetSalesMasterExportMasterDto sales);
    }
}
