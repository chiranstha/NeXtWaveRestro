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

    public class PurchaseReturnsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IPurchaseReturnsExcelExporter
    {
        public FileDto ExportToFile(List<GetPurchaseReturnForViewDto> purchaseReturns)
        {
            return CreateExcelPackage(
                "PurchaseReturns.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PurchaseReturns");

                    AddHeader(
                        sheet,
                        "Date",
                        "VoucherNo",
                        "PurchaseVoucherNo",
                        "LedgerName",
                        "TotalAmount",
                        "Discount",
                        "TotalTax",
                        "GrandTotal",
                        "Description"
                    );

                    AddObjects(
                        sheet, purchaseReturns,
                        d => timeZoneConverter.Convert(d.Date, abpSession.TenantId,
                            abpSession.GetUserId()),
                        d => d.VoucherNo,
                        d => d.PurchaseMasterVoucherNo,
                        d => d.LedgerName,
                        d => d.TotalAmount,
                        d => d.TotalDiscount,
                        d => d.TotalTax,
                        d => d.GrandTotal,
                        d => d.Description
                    );
                });
        }
    }
}
