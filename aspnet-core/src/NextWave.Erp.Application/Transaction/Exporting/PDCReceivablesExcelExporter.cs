using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public class PDCReceivablesExcelExporter(
    ITimeZoneConverter timeZoneConverter,
    IAbpSession abpSession,
    ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IPDCReceivablesExcelExporter
    {
        public FileDto ExportToFile(List<GetPDCReceivableForViewDto> pcdReceivables)
        {
            return CreateExcelPackage(
                "PcdReceivables.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet(L("PcdReceivables"));

                    AddHeader(
                        sheet,
                        L("VoucherNo"),
                        L("Date"),
                        L("Amount"),
                        L("ChequeNo"),
                        L("ChequeDate"),
                        L("Description"),
                        L("LedgerId"),
                        L("BankId"),
                        L("DateMiti")
                    );

                    AddObjects(
                        sheet, pcdReceivables,
                        d => d.VoucherNo,
                        d => timeZoneConverter.Convert(d.Date, abpSession.TenantId, abpSession.GetUserId()),
                        d => d.Amount,
                        d => d.ChequeNo,
                        d => timeZoneConverter.Convert(d.ChequeDate, abpSession.TenantId, abpSession.GetUserId()),
                        d => d.Description,
                        d => d.LedgerId,
                        d => d.BankId,
                        d => d.DateMiti
                    );

                    for (var i = 1; i <= pcdReceivables.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(2);
                    for (var i = 1; i <= pcdReceivables.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[5], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(5);
                });
        }
    }
}
