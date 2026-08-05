using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public class PdcPayablesExcelExporter(
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager)
    {
        public FileDto ExportToFile(List<GetPDCPayableForViewDto> pdcPayables)
        {
            return CreateExcelPackage(
                "PDCPayables.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PDCPayables");

                    AddHeader(
                        sheet,
                        "VoucherNo",
                        "DateMiti",
                        "LedgerName",
                        "Amount",
                        "ChequeNo",
                        "ChequeMiti",
                        "Bank",
                        "Cleared",
                        "Status",
                        "Description"
                    );

                    AddObjects(
                        sheet, pdcPayables,
                        d => d.VoucherNo,
                        d => d.DateMiti,
                        d => d.LedgerName,
                        d => d.Amount,
                        d => d.ChequeNo,
                        d => d.ChequeMiti,
                        d => d.BankName,
                        d => d.Editable,
                        d => d.Editable ? d.Status : "",
                        d => d.Description
                    );
                });
        }
    }
}
