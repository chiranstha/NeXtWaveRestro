using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Exporting
{

    public class PurchaseOrderMastersExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IPurchaseOrderMastersExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetPurchaseOrderMasterForViewDto> purchaseOrderMasters)
        {
            return CreateExcelPackage(
                "PurchaseOrder.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PurchaseOrder");
                    AddHeader(
                        sheet,
                        "Date",
                        //"Branchname",
                        "VoucherNo",
                        "Ledgername",
                        "Totalamount",
                        "Description",
                        "Cancelled"
                    );

                    AddObjects(
                        sheet, purchaseOrderMasters,
                        d => d.DateMiti,
                        //d => d.BranchName,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.TotalAmount,
                        d => d.Description,
                        d => d.Cancelled
                    );
                });
        }
    }
}
