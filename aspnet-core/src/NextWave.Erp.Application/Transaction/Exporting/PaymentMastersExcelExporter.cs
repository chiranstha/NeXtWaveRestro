using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    internal class PaymentMastersExcelExporter(
    ITimeZoneConverter timeZoneConverter,
    IAbpSession abpSession,
    ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IPaymentMastersExcelExporter
    {
        public FileDto ExportToFile(List<GetPaymentMasterForViewDto> paymentMasters)
        {
            return CreateExcelPackage(
                "PaymentMasters.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PaymentMasters");

                    AddHeader(
                        sheet,
                        "DateMiti",
                        "VoucherNo",
                        "CashBank",
                        "TotalAmount"
                    );

                    AddObjects(
                        sheet, paymentMasters,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.TotalAmount
                    );
                });
        }
    }
}