using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{

    public class JournalMastersExcelExporter(
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IJournalMastersExcelExporter
    {
        public FileDto ExportToFile(List<GetJournalMasterForViewDto> journalMasters)
        {
            return CreateExcelPackage(
                "JournalMasters.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("JournalMasters");

                    AddHeader(
                        sheet,
                        "DateMiti",
                        "VoucherNo",
                        "Reference No",
                        "Credit",
                        "Debit",
                        "Description"
                    );

                    AddObjects(
                        sheet, journalMasters,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.ReferenceNo,
                        d => d.CreditTotal,
                        d => d.DebitTotal,
                        d => d.Description
                    );

                    //for (var i = 1; i <= journalMasters.Count; i++)
                    //    SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    //sheet.AutoSizeColumn(2);
                });
        }
    }
}
