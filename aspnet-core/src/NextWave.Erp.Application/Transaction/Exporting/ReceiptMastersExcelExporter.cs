using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Exporting
{
    public class ReceiptMastersExcelExporter(
    ITimeZoneConverter timeZoneConverter,
    IAbpSession abpSession,
    ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IReceiptMastersExcelExporter
    {
        public FileDto ExportToFile(List<GetReceiptMasterForViewDto> receiptMasters)
        {
            return CreateExcelPackage(
                "ReceiptMasters.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("ReceiptMasters");

                    AddHeader(
                        sheet,
                        "Date",
                        "VoucherNo",
                        "LedgerName",
                        "TotalAmount",
                        "Description"
                    );

                    AddObjects(
                        sheet, receiptMasters,
                        d => timeZoneConverter.Convert(d.Date, abpSession.TenantId,
                            abpSession.GetUserId()),
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.TotalAmount,
                        d => d.Description
                    );

                    //for (var i = 1; i <= receiptMasters.Count; i++)
                    //    SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    //sheet.AutoSizeColumn(2);
                });
        }
    }
}