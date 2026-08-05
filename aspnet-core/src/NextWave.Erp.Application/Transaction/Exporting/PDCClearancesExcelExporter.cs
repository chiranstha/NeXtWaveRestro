using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    internal class PDCClearancesExcelExporter(
    ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IPDCClearancesExcelExporter
    {
        public FileDto ExportToFile(List<GetPDCClearanceForViewDto> pdcClearances)
        {
            return CreateExcelPackage(
                "PDCClearances.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PDCClearances");

                    AddHeader(
                        sheet,
                        "VoucherNo",
                        "DateMiti",
                        "Amount",
                        "ChequNo",
                        "ChequMiti",
                        "BankName",
                        "ReceivedIn/PaidFrom",
                        "Type",
                        "Status",
                        "LedgerName",
                        "Description"
                    );

                    AddObjects(
                        sheet, pdcClearances,
                        d => d.VoucherNo,
                        d => d.DateMiti,
                        d => d.Amount,
                        d => d.ChequeNo,
                        d => d.ChequeMiti,
                        d => d.BankName,
                        d => d.ReceivedInOrPaidFrom,
                        d => d.AgainstMode.ToString(),
                        d => d.Status.ToString(),
                        d => d.LedgerName,
                        d => d.Description
                    );
                });
        }
    }
}
