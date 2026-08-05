using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.PurchaseReport.Exporting
{
    public interface IPurchaseReportExport
    {
        FileDto ExportToFile(List<PurchaseReportList> sales);
        FileDto ExportToFileDetails(GetPurchaseMasterExcelExportMasterDto purchaseMasters);
        FileDto ExportToFileNepali(GetPurchaseMasterExcelExportMasterDto purchaseMasters);
    }
}
